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
namespace CompetingConsumers.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    public class QueueManager
    {
        private readonly string queueName;
        private readonly string connectionString;
        private QueueClient client;
        private ManualResetEvent pauseProcessingEvent;

        public QueueManager(string queueName, string connectionString)
        {
            this.queueName = queueName;
            this.connectionString = connectionString;
            this.pauseProcessingEvent = new ManualResetEvent(true);
        }

        public async Task SendMessagesAsync()
        {
            // Simulate sending a batch of messages to the queue.
            var messages = new List<BrokeredMessage>();

            for (int i = 0; i < 10; i++)
            {
                var message = new BrokeredMessage() { MessageId = Guid.NewGuid().ToString() };
                messages.Add(message);
            }

            await this.client.SendBatchAsync(messages);
        }

        public void ReceiveMessages(Func<BrokeredMessage, Task> processMessageTask)
        {
            // Setup the options for the message pump.
            var options = new OnMessageOptions();

            // When AutoComplete is disabled, you have to manually complete/abandon the messages and handle errors, if any.
            options.AutoComplete = false;
            options.MaxConcurrentCalls = 10;
            options.ExceptionReceived += this.OptionsOnExceptionReceived;

            // Use of Service Bus OnMessage message pump. The OnMessage method must be called once, otherwise an exception will occur.
            this.client.OnMessageAsync(
                async (msg) =>
                {
                    // Will block the current thread if Stop is called.
                    this.pauseProcessingEvent.WaitOne();

                    // Execute processing task here
                    await processMessageTask(msg);
                },
                options);
        }

        public async Task Start()
        {
            // Check queue existence.
            var manager = NamespaceManager.CreateFromConnectionString(this.connectionString);
            if (!manager.QueueExists(this.queueName))
            {
                try
                {
                    var queueDescription = new QueueDescription(this.queueName);

                    // Set the maximum delivery count for messages. A message is automatically deadlettered after this number of deliveries.  Default value is 10.
                    queueDescription.MaxDeliveryCount = 3;

                    await manager.CreateQueueAsync(queueDescription);
                }
                catch (MessagingEntityAlreadyExistsException)
                {
                    Trace.TraceWarning(
                        "MessagingEntityAlreadyExistsException Creating Queue - Queue likely already exists for path: {0}", this.queueName);
                }
                catch (MessagingException ex)
                {
                    var webException = ex.InnerException as WebException;
                    if (webException != null)
                    {
                        var response = webException.Response as HttpWebResponse;

                        // It's likely the conflicting operation being performed by the service bus is another queue create operation
                        // If we don't have a web response with status code 'Conflict' it's another exception
                        if (response == null || response.StatusCode != HttpStatusCode.Conflict)
                        {
                            throw;
                        }

                        Trace.TraceWarning("MessagingException HttpStatusCode.Conflict - Queue likely already exists or is being created or deleted for path: {0}", this.queueName);
                    }
                }
            }

            // Create the queue client. By default, the PeekLock method is used.
            this.client = QueueClient.CreateFromConnectionString(this.connectionString, this.queueName);
        }
        
        public async Task Stop(TimeSpan waitTime)
        {
            // Pause the processing threads
            this.pauseProcessingEvent.Reset();

            // There is no clean approach to wait for the threads to complete processing.
            // We simply stop any new processing, wait for existing thread to complete, then close the message pump and then return
            Thread.Sleep(waitTime);

            await this.client.CloseAsync();

            var manager = NamespaceManager.CreateFromConnectionString(this.connectionString);

            if (await manager.QueueExistsAsync(this.queueName))
            {
                try
                {
                    await manager.DeleteQueueAsync(this.queueName);
                }
                catch (MessagingEntityNotFoundException)
                {
                    Trace.TraceWarning(
                        "MessagingEntityNotFoundException Deleting Queue - Queue does not exist at path: {0}", this.queueName);
                }
            }
        }

        private void OptionsOnExceptionReceived(object sender, ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            var exceptionMessage = "null";
            if (exceptionReceivedEventArgs != null && exceptionReceivedEventArgs.Exception != null)
            {
                exceptionMessage = exceptionReceivedEventArgs.Exception.Message;
                Trace.TraceError("Exception in QueueClient.ExceptionReceived: {0}", exceptionMessage);
            }
        }
    }
}
