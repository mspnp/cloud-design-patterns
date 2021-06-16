// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace PriorityQueue.Shared
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
        private readonly string serviceBusConnectionString;
        private readonly string topicName;
        private ServiceBusSender sender;
        private ServiceBusProcessor processor;
        private ServiceBusAdministrationClient subscriptionClient;
        private ServiceBusClient topicClient;
        private readonly ManualResetEvent pauseProcessingEvent;

        public QueueManager(string serviceBusConnectionString, string topicName)
        {
            this.serviceBusConnectionString = serviceBusConnectionString;
            this.topicName = topicName;
            this.pauseProcessingEvent = new ManualResetEvent(true);
        }

        public async Task SendMessageAsync(ServiceBusMessage message)
        {
            await this.sender.SendMessageAsync(message);
        }

        public async Task SendBatchAsync(Queue<ServiceBusMessage> messages)
        {
            int messageCount = messages.Count;
            while (messages.Count > 0)
            {
                ServiceBusMessageBatch messageBatch = await this.sender.CreateMessageBatchAsync();

                if (messageBatch.TryAddMessage(messages.Peek()))
                {
                    // dequeue the message from the .NET queue once the message is added to the batch
                    messages.Dequeue();
                }
                else
                {
                    // if the first message can't fit, then it is too large for the batch
                    throw new Exception($"Message {messageCount - messages.Count} is too large and cannot be sent.");
                }

                while (messages.Count > 0 && messageBatch.TryAddMessage(messages.Peek()))
                {
                    // dequeue the message from the .NET queue as it has been added to the batch
                    messages.Dequeue();
                }

                await this.sender.SendMessagesAsync(messages);
            }     
        }

        public void ReceiveMessages(Func<ServiceBusReceivedMessage, Task> processMessageTask)
        {
            var options = new ServiceBusProcessorOptions();
            options.AutoCompleteMessages = true;
            options.MaxConcurrentCalls = 10;

            this.processor = this.topicClient.CreateProcessor(this.topicName, options);

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

        public async void Setup(string subscription, string priority)
        {
            var namespaceManager = new ServiceBusAdministrationClient(this.serviceBusConnectionString);

            // Setup the topic.
            if (!(await namespaceManager.TopicExistsAsync(this.topicName)))
            {
                try
                {
                    await namespaceManager.CreateTopicAsync(this.topicName);
                }
                catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
                {
                    Trace.TraceInformation("Messaging entity already created: " + this.topicName);
                }
                // It's likely the conflicting operation being performed by the service bus is another queue create operation
                // If we don't have a web response with status code 'Conflict' it's another exception
                catch (ServiceBusException ex) when (((ex.InnerException as WebException)?.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.Conflict)
                {
                    Trace.TraceWarning("MessagingException HttpStatusCode.Conflict - Queue likely already exists or is being created or deleted for path: {0}", this.topicName);
                }
            }

            this.topicClient = new ServiceBusClient(this.serviceBusConnectionString);
            this.sender = topicClient.CreateSender(this.topicName);

            // Setup the subscription.
            if (string.IsNullOrEmpty(subscription))
                return;

            if (!(await namespaceManager.SubscriptionExistsAsync(this.topicName, subscription)))
            {
                // Setup the filter for the subscription based on the priority.
                var filter = new SqlRuleFilter("Priority = '" + priority + "'");
                var options = new CreateSubscriptionOptions(this.topicName, subscription);
                var ruleDescription = new CreateRuleOptions
                {
                    Name = "PriorityFilter",
                    Filter = filter
                };

                try
                {
                    await namespaceManager.CreateSubscriptionAsync(options, ruleDescription);
                }
                catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
                {
                    Trace.TraceInformation("Messaging entity already created: " + subscription);
                }
                // It's likely the conflicting operation being performed by the service bus is another queue create operation
                // If we don't have a web response with status code 'Conflict' it's another exception
                catch (ServiceBusException ex) when (((ex.InnerException as WebException)?.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.Conflict)
                {
                    Trace.TraceWarning("MessagingException HttpStatusCode.Conflict - subscription likely already exists or is being created or deleted for path: {0}", subscription);
                }
            }

            this.subscriptionClient = new ServiceBusAdministrationClient(this.serviceBusConnectionString);
        }

        public void SetupTopic()
        {
            this.Setup(subscription: null, priority: null);
        }

        public async Task StopReceiver(TimeSpan waitTime)
        {
           // Pause the processing threads
            this.pauseProcessingEvent.Reset();

            // There is no clean approach to wait for the threads to complete processing.
            // We simply stop any new processing, wait for existing thread to complete, then close the message pump and then return
            Thread.Sleep(waitTime);

            await this.processor.CloseAsync();

            var manager = new ServiceBusAdministrationClient(this.serviceBusConnectionString);

            if (await manager.TopicExistsAsync(this.topicName))
            {
                try
                {
                    await manager.DeleteTopicAsync(this.topicName);
                }
                catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
                {
                    Trace.TraceWarning(
                        "MessagingEntityNotFoundException Deleting Topic - Topic does not exist at path: {0}", this.topicName);
                }
            }
        }

        public async Task StopSender()
        {
            await this.sender.CloseAsync();
        }

        Task OptionsOnExceptionReceived(ProcessErrorEventArgs exceptionReceivedEventArgs)
        {
            var exceptionMessage = exceptionReceivedEventArgs.Exception.Message;
            Trace.TraceError("Exception in QueueClient.ExceptionReceived: {0}", exceptionMessage);
            return Task.FromResult<object>(null);
        }
    }
}
