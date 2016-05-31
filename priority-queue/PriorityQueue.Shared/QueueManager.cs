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
        private ManualResetEvent pauseProcessingEvent;

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
                 async (msg) =>
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

                        Trace.TraceWarning("MessagingException HttpStatusCode.Conflict - Queue likely already exists or is being created or deleted for path: {0}", this.topicName);
                    }
                }
            }

            this.topicClient = TopicClient.CreateFromConnectionString(this.serviceBusConnectionString, this.topicName);
            this.topicClient.RetryPolicy = RetryPolicy.Default;

            // Setup the subscription.
            if (!string.IsNullOrEmpty(subscription))
            {
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

                            Trace.TraceWarning("MessagingException HttpStatusCode.Conflict - subscription likely already exists or is being created or deleted for path: {0}", subscription);
                        }
                    }
                }

                this.subscriptionClient = SubscriptionClient.CreateFromConnectionString(this.serviceBusConnectionString, this.topicName, subscription);
                this.subscriptionClient.RetryPolicy = RetryPolicy.Default;
            }
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
            var exceptionMessage = "null";
            if (exceptionReceivedEventArgs != null && exceptionReceivedEventArgs.Exception != null)
            {
                exceptionMessage = exceptionReceivedEventArgs.Exception.Message;
                Trace.TraceError("Exception in QueueClient.ExceptionReceived: {0}", exceptionMessage);
            }
        }
    }
}
