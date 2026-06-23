namespace FRELODYAPIs.Services.Analysis
{
    /// <summary>
    /// Drains the <see cref="AnalysisJobService"/> queue with a fixed pool of consumers sized
    /// to <see cref="AnalysisJobService.MaxConcurrency"/>. This is what turns a burst of
    /// requests into a graceful wait instead of an OOM cascade: at most MaxConcurrency analyses
    /// run at once; everything else sits in the queue until a slot frees. The ceiling is
    /// environment-aware, so adding hardware raises throughput with no code change.
    /// </summary>
    public sealed class AnalysisQueueWorker : BackgroundService
    {
        private readonly AnalysisJobService _jobs;
        private readonly ILogger<AnalysisQueueWorker> _logger;

        public AnalysisQueueWorker(AnalysisJobService jobs, ILogger<AnalysisQueueWorker> logger)
        {
            _jobs = jobs;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var n = _jobs.MaxConcurrency;
            _logger.LogInformation("Analysis queue worker starting {Consumers} consumer(s)", n);

            // N consumers share one ChannelReader → each queued item is handed to exactly one
            // consumer, so at most N analyses run concurrently.
            var consumers = new Task[n];
            for (int i = 0; i < n; i++)
                consumers[i] = ConsumeAsync(stoppingToken);

            return Task.WhenAll(consumers);
        }

        private async Task ConsumeAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var item in _jobs.Reader.ReadAllAsync(stoppingToken))
                    await _jobs.RunItemAsync(item);
            }
            catch (OperationCanceledException)
            {
                // App shutting down — stop dequeuing. In-flight items already running on this
                // consumer finish on their own; queued items are abandoned (only completed
                // analyses are persisted, so nothing half-written leaks).
            }
        }
    }
}
