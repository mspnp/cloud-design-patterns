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
    using Azure;
    using Azure.Messaging.ServiceBus;
    using Azure.Messaging.ServiceBus.Administration;

    public class QueueManager
    {
        private readonly string serviceBusConnectionString;
        private readonly string topicName;
        private ServiceBusSender sender;
        private ServiceBusProcessor processor;
        private ServiceBusClient topicClient;

        public QueueManager(string serviceBusConnectionString, string topicName)
        {
            this.serviceBusConnectionString = serviceBusConnectionString;
            this.topicName = topicName;
        }

        public async Task SendMessageAsync(ServiceBusMessage message) => await this.sender.SendMessageAsync(message).ConfigureAwait(false);

        public async Task SendBatchAsync(IEnumerable<ServiceBusMessage> messages) =>
            await this.sender.SendMessagesAsync(messages).ConfigureAwait(false);

        public async Task StopSenderAsync() => await this.sender.CloseAsync().ConfigureAwait(false);

        Task OptionsOnExceptionReceived(ProcessErrorEventArgs exceptionReceivedEventArgs)
        {
            Trace.TraceError($"Exception in QueueClient.ExceptionReceived: {exceptionReceivedEventArgs?.Exception?.Message}");
            return Task.FromResult<object>(null);
        }
            

        public async Task SetupTopicAsync() => await this.SetupAsync(subscription: null, priority: null).ConfigureAwait(false);

        public void ReceiveMessages(Func<ServiceBusReceivedMessage, Task> processMessageTask, CancellationToken cancellationToken)
        {
            var options = new ServiceBusProcessorOptions();
            options.AutoCompleteMessages = true;
            options.MaxConcurrentCalls = 10;

            this.processor = this.topicClient.CreateProcessor(this.topicName, options);

            processor.ProcessMessageAsync +=
                 async args =>
                 {
                     ServiceBusReceivedMessage message = args.Message;

                     if (!cancellationToken.IsCancellationRequested)
                     {
                         // Execute processing task here
                         await processMessageTask(message)
                            .ConfigureAwait(false);
                     }
                 };
            processor.ProcessErrorAsync += this.OptionsOnExceptionReceived;

            processor.StartProcessingAsync();
        }

        public async Task SetupAsync(string subscription, string priority)
        {
            var namespaceManager = new ServiceBusAdministrationClient(this.serviceBusConnectionString);

            // Setup the topic.
            if (!await namespaceManager.TopicExistsAsync(this.topicName).ConfigureAwait(false))
            {
                try
                {
                    await namespaceManager.CreateTopicAsync(this.topicName).ConfigureAwait(false);
                }
                catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
                {
                    Trace.TraceInformation($"Messaging entity already created: {this.topicName}");
                }
                catch (ServiceBusException ex) when (((ex.InnerException as RequestFailedException)?.Status) == 409)
                {
                    Trace.TraceWarning($"MessagingException HttpStatusCode.Conflict - Queue likely already exists or is being created or deleted for path: {this.topicName}");
                }
            }

            this.topicClient = new ServiceBusClient(this.serviceBusConnectionString);
            this.sender = topicClient.CreateSender(this.topicName);

            // Setup the subscription.
            if (!string.IsNullOrWhiteSpace(subscription) && !string.IsNullOrWhiteSpace(priority))
            {
                if (!await namespaceManager.SubscriptionExistsAsync(this.topicName, subscription)
                    .ConfigureAwait(false))
                {
                    // Setup the filter for the subscription based on the priority.
                    var ruleDescription = new CreateRuleOptions
                    {
                        Name = "PriorityFilter",
                        Filter = new SqlRuleFilter($"Priority = '{priority}'")
                    };

                    var options = new CreateSubscriptionOptions(this.topicName, subscription);

                    try
                    {
                        await namespaceManager.CreateSubscriptionAsync(options, ruleDescription).ConfigureAwait(false);
                    }
                    catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
                    {
                        Trace.TraceInformation($"Messaging entity already created: {subscription}");
                    }
                    catch (ServiceBusException ex) when (((ex.InnerException as RequestFailedException)?.Status) == 409)
                    {
                        Trace.TraceWarning($"MessagingException HttpStatusCode.Conflict - subscription likely already exists or is being created or deleted for path: {subscription}");
                    }
                }
            }
        }

        public async Task StopReceiverAsync()
        {
            await this.processor.CloseAsync()
                .ConfigureAwait(false);
            await this.topicClient.DisposeAsync();

            var manager = new ServiceBusAdministrationClient(this.serviceBusConnectionString);

            if (!await manager.TopicExistsAsync(this.topicName)
                .ConfigureAwait(false))
            {
                try
                {
                    await manager.DeleteTopicAsync(this.topicName)
                        .ConfigureAwait(false);
                }
                catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
                {
                    Trace.TraceWarning(
                        $"MessagingEntityNotFoundException Deleting Topic - Topic does not exist at path: {this.topicName}");
                }
            }
        }
    }
}
