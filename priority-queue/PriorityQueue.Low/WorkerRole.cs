// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace PriorityQueue.Low
{
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using PriorityQueue.Shared;

    public class WorkerRole : PriorityWorkerRole
    {
        protected override async Task ProcessMessage(BrokeredMessage message)
        {
            // simulate message processing for Low priority messages
            await base.ProcessMessage(message);
            Trace.TraceInformation("Low priority message processed by " + RoleEnvironment.CurrentRoleInstance.Id + " MessageId: " + message.MessageId);
        }
    }
}
