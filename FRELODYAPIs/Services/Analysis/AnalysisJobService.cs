using System.Collections.Concurrent;
using FRELODYSHRD.Dtos;

namespace FRELODYAPIs.Services.Analysis
{
    /// <summary>
    /// Runs chord/beat analysis on a background task that is <b>decoupled from the HTTP
    /// request</b> — a client disconnect or proxy/Cloudflare ~100s cut can no longer cancel
    /// or discard the work. Identical concurrent requests (retries, second viewers) attach
    /// to the one running job instead of launching a duplicate on the CPU-bound sidecar.
    /// The job writes the transcription to the DB on completion regardless of whether any
    /// caller is still connected, so the result is always recoverable via a cache read.
    /// </summary>
    public sealed class AnalysisJobService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AnalysisJobService> _logger;
        private readonly ConcurrentDictionary<string, AnalysisJob> _jobs = new();

        // Keep terminal jobs briefly so late pollers can read the final stage/error before
        // they fall back to the DB (Done) or are free to re-submit (Failed).
        private static readonly TimeSpan TerminalTtl = TimeSpan.FromMinutes(5);

        public AnalysisJobService(IServiceScopeFactory scopeFactory, ILogger<AnalysisJobService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public static string BuildKey(string platform, string videoId, string beatModel, string chordModel, string chordDict)
            => string.Join('|', platform, videoId, beatModel, chordModel, chordDict);

        /// <summary>
        /// Start the analysis, or attach to one already running for the same key. The work
        /// delegate receives a fresh service scope (the request scope is gone by the time it
        /// runs), a progress sink, and a non-request <see cref="CancellationToken"/>.
        /// </summary>
        public AnalysisJob Submit(
            string key,
            Func<IServiceProvider, IProgress<AnalysisStage>, CancellationToken, Task> work)
        {
            lock (_jobs)
            {
                if (_jobs.TryGetValue(key, out var existing))
                {
                    // Reuse a running or completed job; only a prior failure restarts.
                    if (existing.Stage != AnalysisStage.Failed)
                        return existing;
                    _jobs.TryRemove(key, out _);
                }

                var job = new AnalysisJob
                {
                    Key = key,
                    Stage = AnalysisStage.Extracting,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                _jobs[key] = job;
                _ = Task.Run(() => RunAsync(job, work));
                return job;
            }
        }

        public AnalysisJob? Get(string key)
            => _jobs.TryGetValue(key, out var job) ? job : null;

        private async Task RunAsync(
            AnalysisJob job,
            Func<IServiceProvider, IProgress<AnalysisStage>, CancellationToken, Task> work)
        {
            var progress = new SyncProgress(stage =>
            {
                job.Stage = stage;
                job.UpdatedAt = DateTimeOffset.UtcNow;
            });

            try
            {
                using var scope = _scopeFactory.CreateScope();
                await work(scope.ServiceProvider, progress, CancellationToken.None);
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

    public sealed class AnalysisJob
    {
        public string Key { get; init; } = default!;
        public AnalysisStage Stage;       // enum read/write is atomic; good enough for progress
        public string? Error;
        public DateTimeOffset UpdatedAt;
    }
}
