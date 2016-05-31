// ==============================================================================================================
// Microsoft patterns & practices
// Cloud Design Patterns project
// ==============================================================================================================
// ©2013 Microsoft. All rights reserved. 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================
namespace PipesAndFilters.Shared
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public class ServiceBusPipeFilter
    {
        private readonly string connectionString;
        private readonly string inQueuePath;
        private readonly string outQueuePath;

        // Create a reset event to pause processing before shutting down and create the event signaled to allow processing
        private readonly ManualResetEvent pauseProcessingEvent = new ManualResetEvent(true);

        private QueueClient inQueue;
        private QueueClient outQueue;

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

                this.outQueue = QueueClient.CreateFromConnectionString(this.connectionString, this.outQueuePath);
            }

            // Wait for queue creations to complete
            createInQueueTask.Wait();

            // Create inbound and outbound queue clients
            this.inQueue = QueueClient.CreateFromConnectionString(this.connectionString, this.inQueuePath);
        }

        public void OnPipeFilterMessageAsync(Func<BrokeredMessage, Task<BrokeredMessage>> asyncFilterTask, int maxConcurrentCalls = 1)
        {
            var options = new OnMessageOptions()
            {
                AutoComplete = true,
                MaxConcurrentCalls = maxConcurrentCalls
            };

            options.ExceptionReceived += this.OptionsOnExceptionReceived;

            this.inQueue.OnMessageAsync(
                async (msg) =>
            {
                pauseProcessingEvent.WaitOne();

                // Perform a simple check to dead letter potential poison messages.
                //  If we have dequeued the message more than the max count we can assume the message is poison and deadletter it.
                if (msg.DeliveryCount > Constants.MaxServiceBusDeliveryCount)
                {
                    await msg.DeadLetterAsync();

                    Trace.TraceWarning("Maximum Message Count Exceeded: {0} for MessageID: {1} ", Constants.MaxServiceBusDeliveryCount, msg.MessageId);

                    return;
                }

                // Process the filter and send the output to the next queue in the pipeline
                var outMessage = await asyncFilterTask(msg);

                // Send the message from the filter processor to the next queue in the pipeline
                if (outQueue != null)
                {
                    await outQueue.SendAsync(outMessage);
                }

                //// Note: There is a chance we could send the same message twice or that a message may be processed by an upstream or downstream filter at the same time.
                ////       This would happen in a situation where we completed processing of a message, sent it to the next pipe/queue, and then failed to Complete it when using PeakLock
                ////       Idempotent message processing and concurrency should be considered in the implementation.
            },
            options);
        }

        public async Task Close(TimeSpan timespan)
        {
            // Pause the processing threads
            this.pauseProcessingEvent.Reset();

            // There is no clean approach to wait for the threads to complete processing.
            //  We simply stop any new processing, wait for existing thread to complete, then close the message pump and then return
            Thread.Sleep(timespan);

            this.inQueue.Close();

            // Cleanup resources.
            await ServiceBusUtilities.DeleteQueueIfExistsAsync(Settings.ServiceBusConnectionString, this.inQueue.Path);
        }

        private void OptionsOnExceptionReceived(object sender, ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            //There is currently an issue in the Service Bus SDK that raises a null exception
            if (exceptionReceivedEventArgs.Exception != null)
            {
                Trace.TraceError("Exception in QueueClient.ExceptionReceived: {0}", exceptionReceivedEventArgs.Exception.Message);
            }
        }
    }
}
