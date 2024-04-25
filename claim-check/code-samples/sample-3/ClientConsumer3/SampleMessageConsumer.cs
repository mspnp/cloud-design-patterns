using Microsoft.Extensions.Logging;

namespace Pnp.Samples.ClaimCheckPattern
{
    /// <summary>
    /// A sample message consumer that processes claim check messages. 
    /// Implements a cancellable message loop to retrieve claim-check messages using the messaging service specific processMessagesAsync function.
    /// </summary>
    /// <param name="processMessagesAsync"></param>
    public class SampleMessageConsumer(Func<CancellationToken, Task> processMessagesAsync, ILoggerFactory loggerFactory)
    {
        readonly ILogger _logger = loggerFactory.CreateLogger<SampleMessageConsumer>();

        /// <summary>
        /// Starts a new background task to process claim check messages. The task runs a loop that executes until the cancellation token is triggered.
        /// </summary>
        public Task<Task> Start(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(MessageLoopInternalAsync!,
                cancellationToken,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning,
                TaskScheduler.Default
            );
        }

        /// <summary>
        /// Runs a loop that processes claim check messages until the cancellation token is triggered.
        /// </summary>
        async Task MessageLoopInternalAsync(object state)
        {
            var cancellationToken = (CancellationToken)state;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await processMessagesAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Cancelling downloads.");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error: {Message}.", ex.Message);
                    throw;
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }
    }
}