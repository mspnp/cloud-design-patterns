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
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    public class QueueManager
    {
        private readonly string serviceBusConnectionString;
        private readonly string topicName;
        private SubscriptionClient subscriptionClient;
        private TopicClient topicClient;
        private readonly ManualResetEvent pauseProcessingEvent;

        public QueueManager(string serviceBusConnectionString, string topicName)
        {
            this.serviceBusConnectionString = serviceBusConnectionString;
            this.topicName = topicName;
            this.pauseProcessingEvent = new ManualResetEvent(true);
        }

        public async Task SendMessageAsync(BrokeredMessage message)
        {
            await this.topicClient.SendAsync(message);
        }

        public async Task SendBatchAsync(IEnumerable<BrokeredMessage> messages)
        {
            await this.topicClient.SendBatchAsync(messages);
        }

        public void ReceiveMessages(string subscription, Func<BrokeredMessage, Task> processMessageTask)
        {
            var options = new OnMessageOptions();
            options.AutoComplete = true;
            options.MaxConcurrentCalls = 10;
            options.ExceptionReceived += this.OptionsOnExceptionReceived;

            this.subscriptionClient.OnMessageAsync(
                 async msg =>
                 {
                     // Will block the current thread if Stop is called.
                     this.pauseProcessingEvent.WaitOne();

                     // Execute processing task here
                     await processMessageTask(msg);
                 },
                 options);
        }

        public void Setup(string subscription, string priority)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(this.serviceBusConnectionString);

            // Setup the topic.
            if (!namespaceManager.TopicExists(this.topicName))
            {
                try
                {
                    namespaceManager.CreateTopic(this.topicName);
                }
                catch (MessagingEntityAlreadyExistsException)
                {
                    Trace.TraceInformation("Messaging entity already created: " + this.topicName);
                }
                // It's likely the conflicting operation being performed by the service bus is another queue create operation
                // If we don't have a web response with status code 'Conflict' it's another exception
                catch (MessagingException ex) when (((ex.InnerException as WebException)?.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.Conflict)
                {
                    Trace.TraceWarning("MessagingException HttpStatusCode.Conflict - Queue likely already exists or is being created or deleted for path: {0}", this.topicName);
                }
            }

            this.topicClient = TopicClient.CreateFromConnectionString(this.serviceBusConnectionString, this.topicName);
            this.topicClient.RetryPolicy = RetryPolicy.Default;

            // Setup the subscription.
            if (string.IsNullOrEmpty(subscription))
                return;

            if (!namespaceManager.SubscriptionExists(this.topicName, subscription))
            {
                // Setup the filter for the subscription based on the priority.
                var filter = new SqlFilter("Priority = '" + priority + "'");
                var ruleDescription = new RuleDescription
                {
                    Name = "PriorityFilter",
                    Filter = filter
                };

                try
                {
                    namespaceManager.CreateSubscription(this.topicName, subscription, ruleDescription);
                }
                catch (MessagingEntityAlreadyExistsException)
                {
                    Trace.TraceInformation("Messaging entity already created: " + subscription);
                }
                // It's likely the conflicting operation being performed by the service bus is another queue create operation
                // If we don't have a web response with status code 'Conflict' it's another exception
                catch (MessagingException ex) when (((ex.InnerException as WebException)?.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.Conflict)
                {
                    Trace.TraceWarning("MessagingException HttpStatusCode.Conflict - subscription likely already exists or is being created or deleted for path: {0}", subscription);
                }
            }

            this.subscriptionClient = SubscriptionClient.CreateFromConnectionString(this.serviceBusConnectionString, this.topicName, subscription);
            this.subscriptionClient.RetryPolicy = RetryPolicy.Default;
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

            await this.subscriptionClient.CloseAsync();

            var manager = NamespaceManager.CreateFromConnectionString(this.serviceBusConnectionString);

            if (await manager.TopicExistsAsync(this.topicName))
            {
                try
                {
                    await manager.DeleteTopicAsync(this.topicName);
                }
                catch (MessagingEntityNotFoundException)
                {
                    Trace.TraceWarning(
                        "MessagingEntityNotFoundException Deleting Topic - Topic does not exist at path: {0}", this.topicName);
                }
            }
        }

        public async Task StopSender()
        {
            await this.topicClient.CloseAsync();
        }

        private void OptionsOnExceptionReceived(object sender, ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            if (exceptionReceivedEventArgs?.Exception == null)
                return;

            var exceptionMessage = exceptionReceivedEventArgs.Exception.Message;
            Trace.TraceError("Exception in QueueClient.ExceptionReceived: {0}", exceptionMessage);
        }
    }
}
