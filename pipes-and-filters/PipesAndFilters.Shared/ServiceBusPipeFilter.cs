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

        // Create a reset event to pause processing before shutting down and create the event signaled to allow processing
        private readonly ManualResetEvent pauseProcessingEvent = new ManualResetEvent(true);

        private ServiceBusClient inQueue;
        private ServiceBusClient outQueue;

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

                this.outQueue = new ServiceBusClient(this.connectionString);
            }

            // Wait for queue creations to complete
            createInQueueTask.Wait();

            // Create inbound and outbound queue clients
            this.inQueue = new ServiceBusClient(this.connectionString);
        }

        public void OnPipeFilterMessageAsync(Func<ServiceBusReceivedMessage, Task<ServiceBusMessage>> asyncFilterTask, int maxConcurrentCalls = 1)
        {
            var options = new ServiceBusProcessorOptions()
            {
                AutoCompleteMessages = true,
                MaxConcurrentCalls = maxConcurrentCalls
            };

            this.processor = new ServiceBusClient(this.connectionString).CreateProcessor(this.inQueuePath, options);

            processor.ProcessMessageAsync +=
                async args =>
            {
                ServiceBusReceivedMessage message = args.Message;
                pauseProcessingEvent.WaitOne();

                // Perform a simple check to dead letter potential poison messages.
                //  If we have dequeued the message more than the max count we can assume the message is poison and deadletter it.
                if (message.DeliveryCount > Constants.MaxServiceBusDeliveryCount)
                {
                    ServiceBusReceiver receiver = new ServiceBusClient(this.connectionString).CreateReceiver(this.inQueuePath);
                    await receiver.DeadLetterMessageAsync(message);

                    Trace.TraceWarning("Maximum Message Count Exceeded: {0} for MessageID: {1} ", Constants.MaxServiceBusDeliveryCount, message.MessageId);

                    return;
                }

                // Process the filter and send the output to the next queue in the pipeline
                var outMessage = await asyncFilterTask(message);

                // Send the message from the filter processor to the next queue in the pipeline
                if (outQueue != null)
                {
                    await outQueue.CreateSender(this.outQueuePath).SendMessageAsync(outMessage);
                }

                //// Note: There is a chance we could send the same message twice or that a message may be processed by an upstream or downstream filter at the same time.
                ////       This would happen in a situation where we completed processing of a message, sent it to the next pipe/queue, and then failed to Complete it when using PeakLock
                ////       Idempotent message processing and concurrency should be considered in the implementation.
            };
            processor.ProcessErrorAsync += this.OptionsOnExceptionReceived;

            processor.StartProcessingAsync();
        }

        public async Task Close(TimeSpan timespan)
        {
            // Pause the processing threads
            this.pauseProcessingEvent.Reset();

            // There is no clean approach to wait for the threads to complete processing.
            //  We simply stop any new processing, wait for existing thread to complete, then close the message pump and then return
            Thread.Sleep(timespan);

            await this.processor.CloseAsync();

            // Cleanup resources.
            await ServiceBusUtilities.DeleteQueueIfExistsAsync(Settings.ServiceBusConnectionString, this.inQueue.FullyQualifiedNamespace);
        }

        Task OptionsOnExceptionReceived(ProcessErrorEventArgs exceptionReceivedEventArgs)
        {
            //There is currently an issue in the Service Bus SDK that raises a null exception
            if (exceptionReceivedEventArgs.Exception != null)
            {
                Trace.TraceError("Exception in QueueClient.ExceptionReceived: {0}", exceptionReceivedEventArgs.Exception.Message);
            }
            return Task.FromResult<object>(null);
        }
    }
}
