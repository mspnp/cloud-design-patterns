using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using PriorityQueue.Shared;
using System.Collections.ObjectModel;
using System.Fabric.Description;
using Microsoft.ServiceBus.Messaging;
using System.Diagnostics;
using System.Net;

namespace PriorityQueue.High
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class Fabric : PriorityStatelessService
    {
        public Fabric(StatelessServiceContext context)
            : base(context)
        { }

        protected override async Task ProcessMessageAsync(BrokeredMessage message)
        {
            // simulate message processing for High priority messages
            await base.ProcessMessageAsync(message)
                .ConfigureAwait(false);
            Trace.TraceInformation($"High priority message processed by {this.Context.NodeContext.NodeId.ToString()} MessageId: {message.MessageId}");
        }
    }
}
