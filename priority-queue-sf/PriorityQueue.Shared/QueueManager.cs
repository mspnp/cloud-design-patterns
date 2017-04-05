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

        public QueueManager(string serviceBusConnectionString, string topicName)
        {
            this.serviceBusConnectionString = serviceBusConnectionString;
            this.topicName = topicName;
        }

        public async Task SendMessageAsync(BrokeredMessage message) => await this.topicClient.SendAsync(message).ConfigureAwait(false);

        public async Task SendBatchAsync(IEnumerable<BrokeredMessage> messages) =>
            await this.topicClient.SendBatchAsync(messages).ConfigureAwait(false);

        public async Task StopSenderAsync() => await this.topicClient.CloseAsync().ConfigureAwait(false);

        private void OptionsOnExceptionReceived(object sender, ExceptionReceivedEventArgs exceptionReceivedEventArgs) =>
            Trace.TraceError($"Exception in QueueClient.ExceptionReceived: {exceptionReceivedEventArgs?.Exception?.Message}");

        public async Task SetupTopicAsync() => await this.SetupAsync(subscription: null, priority: null).ConfigureAwait(false);

        public void ReceiveMessages(string subscription, Func<BrokeredMessage, Task> processMessageTask,
            CancellationToken cancellationToken)
        {
            var options = new OnMessageOptions();
            options.AutoComplete = true;
            options.MaxConcurrentCalls = 10;
            options.ExceptionReceived += this.OptionsOnExceptionReceived;

            this.subscriptionClient.OnMessageAsync(
                 async (msg) =>
                 {
                     if (!cancellationToken.IsCancellationRequested)
                     {
                         // Execute processing task here
                         await processMessageTask(msg)
                            .ConfigureAwait(false);
                     }
                 },
                 options);
        }

        public async Task SetupAsync(string subscription, string priority)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(this.serviceBusConnectionString);

            // Setup the topic.
            if (!await namespaceManager.TopicExistsAsync(this.topicName)
                .ConfigureAwait(false))
            {
                try
                {
                    await namespaceManager.CreateTopicAsync(this.topicName)
                        .ConfigureAwait(false);
                }
                catch (MessagingEntityAlreadyExistsException)
                {
                    Trace.TraceInformation($"Messaging entity already created: {this.topicName}");
                }
                catch (MessagingException ex) when (((ex.InnerException as WebException)?.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.Conflict)
                {
                    Trace.TraceWarning($"MessagingException HttpStatusCode.Conflict - Queue likely already exists or is being created or deleted for path: {this.topicName}");
                }
            }

            this.topicClient = TopicClient.CreateFromConnectionString(this.serviceBusConnectionString, this.topicName);
            this.topicClient.RetryPolicy = RetryPolicy.Default;

            // Setup the subscription.
            if (!string.IsNullOrWhiteSpace(subscription) && !string.IsNullOrWhiteSpace(priority))
            {
                if (!await namespaceManager.SubscriptionExistsAsync(this.topicName, subscription)
                    .ConfigureAwait(false))
                {
                    // Setup the filter for the subscription based on the priority.
                    var ruleDescription = new RuleDescription
                    {
                        Name = "PriorityFilter",
                        Filter = new SqlFilter($"Priority = '{priority}'")
                    };

                    try
                    {
                        await namespaceManager.CreateSubscriptionAsync(this.topicName, subscription, ruleDescription)
                            .ConfigureAwait(false);
                    }
                    catch (MessagingEntityAlreadyExistsException)
                    {
                        Trace.TraceInformation($"Messaging entity already created: {subscription}");
                    }
                    catch (MessagingException ex) when (((ex.InnerException as WebException)?.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.Conflict)
                    {
                        Trace.TraceWarning($"MessagingException HttpStatusCode.Conflict - subscription likely already exists or is being created or deleted for path: {subscription}");
                    }
                }

                this.subscriptionClient = SubscriptionClient.CreateFromConnectionString(this.serviceBusConnectionString, this.topicName, subscription);
                this.subscriptionClient.RetryPolicy = RetryPolicy.Default;
            }
        }

        public async Task StopReceiverAsync()
        {
            await this.subscriptionClient.CloseAsync()
                .ConfigureAwait(false);

            var manager = NamespaceManager.CreateFromConnectionString(this.serviceBusConnectionString);

            if (!await manager.TopicExistsAsync(this.topicName)
                .ConfigureAwait(false))
            {
                try
                {
                    await manager.DeleteTopicAsync(this.topicName)
                        .ConfigureAwait(false);
                }
                catch (MessagingEntityNotFoundException)
                {
                    Trace.TraceWarning(
                        $"MessagingEntityNotFoundException Deleting Topic - Topic does not exist at path: {this.topicName}");
                }
            }
        }
    }
}
