using System.Collections.Concurrent;
using System.Threading.Channels;
using FRELODYSHRD.Dtos;

namespace FRELODYAPIs.Services.Analysis
{
    /// <summary>
    /// Bounded, environment-aware analysis queue. Submissions are accepted immediately and
    /// drained by a fixed pool of consumers (see <see cref="AnalysisQueueWorker"/>), so a
    /// burst of N requests <b>queues</b> rather than fanning out into N concurrent CPU/RAM-
    /// hungry pipelines that OOM the box. Each job runs on a background flow decoupled from
    /// the HTTP request (a client disconnect / proxy cut can't cancel or discard it), writes
    /// its result to the DB on completion, and identical concurrent requests dedupe onto the
    /// one job. The concurrency ceiling auto-expands with hardware (see <see cref="MaxConcurrency"/>).
    /// </summary>
    public sealed class AnalysisJobService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AnalysisJobService> _logger;
        private readonly ConcurrentDictionary<string, AnalysisJob> _jobs = new();
        private readonly Channel<AnalysisWorkItem> _queue = Channel.CreateUnbounded<AnalysisWorkItem>();
        private long _seq;

        // Keep terminal jobs briefly so late pollers read the final stage/error before they
        // fall back to the DB (Done) or are free to re-submit (Failed).
        private static readonly TimeSpan TerminalTtl = TimeSpan.FromMinutes(5);

        /// <summary>Max analyses allowed to run at once. The rest wait in the queue.</summary>
        public int MaxConcurrency { get; }

        public ChannelReader<AnalysisWorkItem> Reader => _queue.Reader;

        public AnalysisJobService(
            IServiceScopeFactory scopeFactory,
            ILogger<AnalysisJobService> logger,
            IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            MaxConcurrency = ComputeMaxConcurrency(config, logger);
        }

        /// <summary>
        /// Resolve the concurrency ceiling using the classic admission-control bound
        /// <c>min_k ⌊capacity_k / demand_k⌋</c> over the two binding resources (CPU, RAM).
        /// Capacity is read <b>live</b> from the environment (cgroup-aware core count + current
        /// free memory), so the ceiling rises on its own as hardware grows and never needs a
        /// code change. The floor is 1 — we always let at least one run and queue the rest
        /// (wait, never reject). An explicit <c>Analysis:MaxConcurrency</c> overrides everything
        /// (escape hatch for nested-cgroup setups where the runtime mis-reads limits).
        /// </summary>
        private static int ComputeMaxConcurrency(IConfiguration config, ILogger logger)
        {
            var explicitLimit = config.GetValue<int?>("Analysis:MaxConcurrency");
            if (explicitLimit is > 0)
            {
                logger.LogInformation("Analysis MaxConcurrency = {Limit} (explicit Analysis:MaxConcurrency)", explicitLimit.Value);
                return explicitLimit.Value;
            }

            // Demand per analysis, grounded in measurement + Spleeter/Demucs docs and the
            // chordmini thread cap (OMP/MKL/OPENBLAS_NUM_THREADS=3). All tunable.
            double coresPerJob = config.GetValue<double?>("Analysis:CoresPerAnalysis") ?? 3.0;
            double ramPerJobGb = config.GetValue<double?>("Analysis:MemoryPerAnalysisGb") ?? 4.5;
            // Commitment factor κ: fraction of free RAM we're willing to commit to analyses,
            // leaving headroom for spikes / co-located services.
            double commit = config.GetValue<double?>("Analysis:MemoryCommitFraction") ?? 0.85;
            int cap = config.GetValue<int?>("Analysis:MaxConcurrencyCap") ?? 32;

            int cores = Environment.ProcessorCount;          // cgroup/container-aware on .NET 6+
            double freeGb = ReadAvailableMemoryGb();          // live: /proc/meminfo MemAvailable

            int cpuBased = Math.Max(1, (int)Math.Floor(cores / coresPerJob));
            int memBased = Math.Max(1, (int)Math.Floor(freeGb * commit / ramPerJobGb));
            int limit = Math.Clamp(Math.Min(cpuBased, memBased), 1, cap);

            logger.LogInformation(
                "Analysis MaxConcurrency = {Limit} (auto: cores={Cores}/{CoresPerJob}->{CpuBased}, " +
                "freeRAM={FreeGb:F1}GB*{Commit}/{RamPerJob}GB->{MemBased}; cap={Cap}). " +
                "Override with Analysis:MaxConcurrency.",
                limit, cores, coresPerJob, cpuBased, freeGb, commit, ramPerJobGb, memBased, cap);
            return limit;
        }

        /// <summary>
        /// Current free memory in GB. Prefers Linux <c>/proc/meminfo</c> MemAvailable (the
        /// kernel's own estimate of what's allocatable without swapping — reflects whatever
        /// else is running on the shared host right now), falling back to the GC's
        /// container-aware figure off Linux or if the file can't be read.
        /// </summary>
        private static double ReadAvailableMemoryGb()
        {
            try
            {
                foreach (var line in File.ReadLines("/proc/meminfo"))
                {
                    if (!line.StartsWith("MemAvailable:", StringComparison.Ordinal)) continue;
                    var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && long.TryParse(parts[1], out var kb))
                        return kb / (1024.0 * 1024.0); // kB -> GB
                    break;
                }
            }
            catch { /* not Linux / unreadable — fall through */ }

            return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024 * 1024);
        }

        public static string BuildKey(string platform, string videoId, string beatModel, string chordModel, string chordDict)
            => string.Join('|', platform, videoId, beatModel, chordModel, chordDict);

        /// <summary>
        /// Enqueue the analysis, or attach to one already queued/running for the same key. The
        /// work delegate receives a fresh service scope (the request scope is gone by the time
        /// a consumer runs it), a progress sink, and a non-request <see cref="CancellationToken"/>.
        /// </summary>
        public AnalysisJob Submit(
            string key,
            Func<IServiceProvider, IProgress<AnalysisStage>, CancellationToken, Task> work)
        {
            lock (_jobs)
            {
                if (_jobs.TryGetValue(key, out var existing))
                {
                    // Reuse a queued/running/done job; only a prior failure re-queues.
                    if (existing.Stage != AnalysisStage.Failed)
                        return existing;
                    _jobs.TryRemove(key, out _);
                }

                var job = new AnalysisJob
                {
                    Key = key,
                    Stage = AnalysisStage.Queued,
                    Seq = Interlocked.Increment(ref _seq),
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                _jobs[key] = job;
                // Unbounded queue: holding a Queued job is cheap; the bounded resource is the
                // running slot, capped by the consumer pool. A 1000-song burst simply waits.
                _queue.Writer.TryWrite(new AnalysisWorkItem(job, work));
                return job;
            }
        }

        public AnalysisJob? Get(string key)
            => _jobs.TryGetValue(key, out var job) ? job : null;

        /// <summary>
        /// How many analyses are ahead of this one (running or queued earlier). 0 means it's
        /// next/running. Drives the "N ahead of you" UI so a queued user knows they're held,
        /// not stuck.
        /// </summary>
        public int QueueAhead(AnalysisJob job)
        {
            int ahead = 0;
            foreach (var other in _jobs.Values)
            {
                if (other.Seq >= job.Seq) continue;
                if (other.Stage is AnalysisStage.Done or AnalysisStage.Failed or AnalysisStage.NotStarted) continue;
                ahead++;
            }
            return ahead;
        }

        /// <summary>Run one dequeued item to completion. Called only by the consumer pool.</summary>
        public async Task RunItemAsync(AnalysisWorkItem item)
        {
            var job = item.Job;
            var progress = new SyncProgress(stage =>
            {
                job.Stage = stage;
                job.UpdatedAt = DateTimeOffset.UtcNow;
            });

            try
            {
                using var scope = _scopeFactory.CreateScope();
                await item.Work(scope.ServiceProvider, progress, CancellationToken.None);
                job.Stage = AnalysisStage.Done;
            }
            catch (Exception ex)
            {
                job.Stage = AnalysisStage.Failed;
                job.Error = ex.Message;
                _logger.LogWarning(ex, "Analysis job failed: {Key}", job.Key);
            }
            finally
            {
                job.UpdatedAt = DateTimeOffset.UtcNow;
                _ = EvictAfterTtlAsync(job.Key);
            }
        }

        private async Task EvictAfterTtlAsync(string key)
        {
            await Task.Delay(TerminalTtl);
            _jobs.TryRemove(key, out _);
        }

        // Reports synchronously on the pipeline thread so stage order is exact (unlike
        // System.Progress<T>, which posts to a captured SynchronizationContext / the pool).
        private sealed class SyncProgress : IProgress<AnalysisStage>
        {
            private readonly Action<AnalysisStage> _onReport;
            public SyncProgress(Action<AnalysisStage> onReport) => _onReport = onReport;
            public void Report(AnalysisStage value) => _onReport(value);
        }
    }

    public sealed record AnalysisWorkItem(
        AnalysisJob Job,
        Func<IServiceProvider, IProgress<AnalysisStage>, CancellationToken, Task> Work);

    public sealed class AnalysisJob
    {
        public string Key { get; init; } = default!;
        public long Seq;                  // submission order, for queue-position reporting
        public AnalysisStage Stage;       // enum read/write is atomic; good enough for progress
        public string? Error;
        public DateTimeOffset UpdatedAt;
    }
}
