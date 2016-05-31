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
namespace PriorityQueue.Low
{
    using System;
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
