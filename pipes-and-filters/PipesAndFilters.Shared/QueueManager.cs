// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace PipesAndFilters.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus;
    using Azure.Messaging.ServiceBus.Administration;

    public class QueueManager
    {
        private readonly string queueName;
        private readonly string connectionString;
        private ServiceBusClient client;
        private ServiceBusSender sender;
        private ServiceBusProcessor processor;
        private ManualResetEvent pauseProcessingEvent;

        public QueueManager(string queueName, string connectionString)
        {
            this.queueName = queueName;
            this.connectionString = connectionString;
            this.pauseProcessingEvent = new ManualResetEvent(true);
        }

        public async Task SendMessageAsync(ServiceBusMessage message)
        {
            await this.sender.SendMessageAsync(message);
        }

        public void ReceiveMessages(Func<ServiceBusReceivedMessage, Task> processMessageTask)
        {
            // Setup the options for the message pump.
            var options = new ServiceBusProcessorOptions();

            // When AutoComplete is disabled, you have to manually complete/abandon the messages and handle errors, if any.
            options.AutoCompleteMessages = false;
            options.MaxConcurrentCalls = 10;

            this.processor = this.client.CreateProcessor(this.queueName, options);
            // Use of Service Bus OnMessage message pump. The OnMessage method must be called once, otherwise an exception will occur.
            processor.ProcessMessageAsync +=
                async args =>
                {
                    ServiceBusReceivedMessage message = args.Message;

                    // Will block the current thread if Stop is called.
                    this.pauseProcessingEvent.WaitOne();

                    // Execute processing task here
                    await processMessageTask(message);
                };
            processor.ProcessErrorAsync += this.OptionsOnExceptionReceived;

            processor.StartProcessingAsync();
        }

        public async Task Start()
        {
            // Check queue existence.
            var manager = new ServiceBusAdministrationClient(this.connectionString);
            if (!await manager.QueueExistsAsync(this.queueName))
            {
                try
                {
                    var queueDescription = new CreateQueueOptions(this.queueName);

                    // Set the maximum delivery count for messages. A message is automatically deadlettered after this number of deliveries.  Default value is 10.
                    queueDescription.MaxDeliveryCount = 3;

                    await manager.CreateQueueAsync(queueDescription);
                }
                catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
                {
                    Trace.TraceWarning(
                        "MessagingEntityAlreadyExistsException Creating Queue - Queue likely already exists for path: {0}", this.queueName);
                }
                catch (ServiceBusException ex)
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
            this.client = new ServiceBusClient(this.connectionString);
            this.sender = client.CreateSender(this.queueName);
        }
        
        public async Task Stop(TimeSpan? waitTime)
        {
            if (waitTime.HasValue)
            {
                // Pause the processing threads on the message pump.
                this.pauseProcessingEvent.Reset();

                // There is no clean approach to wait for the threads to complete processing.
                // We simply stop any new processing, wait for existing thread to complete, then close the message pump and then return
                Thread.Sleep(waitTime.Value);
            }

            await this.processor.CloseAsync();

            await ServiceBusUtilities.DeleteQueueIfExistsAsync(this.connectionString, this.queueName);
        }

        public async Task Stop()
        {
            await this.Stop(null);
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
