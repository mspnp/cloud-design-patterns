// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace CompetingConsumers.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
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

        public QueueManager(string queueName, string connectionString)
        {
            this.queueName = queueName;
            this.connectionString = connectionString;
        }

        private void OptionsOnExceptionReceived(object sender, ExceptionReceivedEventArgs exceptionReceivedEventArgs) =>
            Trace.TraceError($"Exception in QueueClient.ExceptionReceived: {exceptionReceivedEventArgs?.Exception?.Message}");

        public async Task SendMessagesAsync()
        {
            // Simulate sending a batch of messages to the queue.
            var messages = Enumerable.Range(0, 10)
                .Select(i => new BrokeredMessage()
                {
                    MessageId = Guid.NewGuid().ToString()
                })
                .ToList();

            await this.client.SendBatchAsync(messages)
                .ConfigureAwait(false);
        }

        public void ReceiveMessages(Func<BrokeredMessage, Task> processMessageTask, CancellationToken cancellationToken)
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
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        // Execute processing task here
                        await processMessageTask(msg)
                            .ConfigureAwait(false);
                    }
                },
                options);
        }

        public async Task StartAsync()
        {
            // Check queue existence.
            var manager = NamespaceManager.CreateFromConnectionString(this.connectionString);
            if (!await manager.QueueExistsAsync(this.queueName)
                .ConfigureAwait(false))
            {
                try
                {
                    await manager.CreateQueueAsync(new QueueDescription(this.queueName)
                    {
                        // Set the maximum delivery count for messages. A message is automatically deadlettered after
                        // this number of deliveries.  Default value is 10.
                        MaxDeliveryCount = 3
                    }).ConfigureAwait(false);
                }
                catch (MessagingEntityAlreadyExistsException)
                {
                    Trace.TraceWarning(
                        $"MessagingEntityAlreadyExistsException Creating Queue - Queue likely already exists for path: {this.queueName}");
                }
                catch (MessagingException ex) when (((ex.InnerException as WebException)?.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.Conflict)
                {
                    Trace.TraceWarning($"MessagingException HttpStatusCode.Conflict - Queue likely already exists or is being created or deleted for path: {this.queueName}");
                }
            }

            // Create the queue client. By default, the PeekLock method is used.
            this.client = QueueClient.CreateFromConnectionString(this.connectionString, this.queueName);
        }
        
        public async Task StopAsync()
        {
            await this.client.CloseAsync()
                .ConfigureAwait(false);

            var manager = NamespaceManager.CreateFromConnectionString(this.connectionString);

            if (await manager.QueueExistsAsync(this.queueName)
                .ConfigureAwait(false))
            {
                try
                {
                    await manager.DeleteQueueAsync(this.queueName)
                        .ConfigureAwait(false);
                }
                catch (MessagingEntityNotFoundException)
                {
                    Trace.TraceWarning(
                        $"MessagingEntityNotFoundException Deleting Queue - Queue does not exist at path: {this.queueName}");
                }
            }
        }
    }
}
