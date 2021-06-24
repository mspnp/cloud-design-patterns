// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace PipesAndFilters.Shared
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus;

    public class ServiceBusPipeFilter
    {
        private readonly string connectionString;
        private readonly string inQueuePath;
        private readonly string outQueuePath;
        private ServiceBusProcessor processor;
        private ServiceBusSender sender;

        // Create a reset event to pause processing before shutting down and create the event signaled to allow processing
        private readonly ManualResetEvent pauseProcessingEvent = new ManualResetEvent(true);

        private ServiceBusClient client;

        public ServiceBusPipeFilter(string connectionString, string inQueuePath, string outQueuePath = null)
        {
            this.connectionString = connectionString;
            this.inQueuePath = inQueuePath;
            this.outQueuePath = outQueuePath;
        }

        public void Start()
        {
            // Create the inbound filter queue if it does not exist
            var createInQueueTask = ServiceBusUtilities.CreateQueueIfNotExistsAsync(this.connectionString, this.inQueuePath);

            // Create the outbound filter queue if it does not exist
            if(!string.IsNullOrEmpty(outQueuePath))
            {
                ServiceBusUtilities.CreateQueueIfNotExistsAsync(this.connectionString, this.outQueuePath).Wait();

                this.client = new ServiceBusClient(this.connectionString);
            }

            // Wait for queue creations to complete
            createInQueueTask.Wait();

            var options = new ServiceBusProcessorOptions()
            {
                AutoCompleteMessages = true,
                MaxConcurrentCalls = 1
            };

            // Create inbound and outbound queue clients
            this.client = new ServiceBusClient(this.connectionString);
            this.processor = this.client.CreateProcessor(this.inQueuePath, options);
            this.sender = this.client.CreateSender(this.outQueuePath);
        }

        public void OnPipeFilterMessageAsync(Func<ServiceBusReceivedMessage, Task<ServiceBusMessage>> asyncFilterTask)
        {
            this.processor.ProcessMessageAsync +=
                async args =>
            {
                ServiceBusReceivedMessage message = args.Message;
                pauseProcessingEvent.WaitOne();

                // Perform a simple check to dead letter potential poison messages.
                //  If we have dequeued the message more than the max count we can assume the message is poison and deadletter it.
                if (message.DeliveryCount > Constants.MaxServiceBusDeliveryCount)
                {
                    await args.DeadLetterMessageAsync(message);

                    Trace.TraceWarning("Maximum Message Count Exceeded: {0} for MessageID: {1} ", Constants.MaxServiceBusDeliveryCount, message.MessageId);

                    return;
                }

                // Process the filter and send the output to the next queue in the pipeline
                var outMessage = await asyncFilterTask(message);

                // Send the message from the filter processor to the next queue in the pipeline
                if (sender != null)
                {
                    await this.sender.SendMessageAsync(outMessage);
                }

                //// Note: There is a chance we could send the same message twice or that a message may be processed by an upstream or downstream filter at the same time.
                ////       This would happen in a situation where we completed processing of a message, sent it to the next pipe/queue, and then failed to Complete it when using PeakLock
                ////       Idempotent message processing and concurrency should be considered in the implementation.
            };
            this.processor.ProcessErrorAsync += this.OptionsOnExceptionReceived;

            this.processor.StartProcessingAsync();
        }

        public async Task Close(TimeSpan timespan)
        {
            // Pause the processing threads
            this.pauseProcessingEvent.Reset();

            // There is no clean approach to wait for the threads to complete processing.
            //  We simply stop any new processing, wait for existing thread to complete, then close the message pump and then return
            Thread.Sleep(timespan);

            await this.processor.CloseAsync();
            await this.client.DisposeAsync();

            // Cleanup resources.
            await ServiceBusUtilities.DeleteQueueIfExistsAsync(Settings.ServiceBusConnectionString, this.inQueuePath);
        }

        Task OptionsOnExceptionReceived(ProcessErrorEventArgs args)
        {
            Trace.TraceError("An exception occurred during processing. Error source: {0}, Exception: {1}", args.ErrorSource, args.Exception.Message);

            return Task.CompletedTask;
        }
    }
}
