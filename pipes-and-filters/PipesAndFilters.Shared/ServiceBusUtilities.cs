// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace PipesAndFilters.Shared
{
    using System.Diagnostics;
    using System.Net;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Messaging.ServiceBus;
    using Azure.Messaging.ServiceBus.Administration;

    public static class ServiceBusUtilities
    {
        public static async Task CreateQueueIfNotExistsAsync(string connectionString, string path)
        {
            var adminClient = new ServiceBusAdministrationClient(connectionString);

            if (!await adminClient.QueueExistsAsync(path))
            {
                try
                {
                    await adminClient.CreateQueueAsync(path);
                }
                catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
                {
                    Trace.TraceWarning(
                        "MessagingEntityAlreadyExistsException Creating Queue - Queue likely already exists for path: {0}", path);
                }
                catch (ServiceBusException ex)
                {
                    var requestFailedException = ex.InnerException as RequestFailedException;
                    if (requestFailedException != null)
                    {
                        var status = requestFailedException.Status;

                        // It's likely the conflicting operation being performed by the service bus is another queue create operation
                        // If we don't have a web response with status code 'Conflict' it's another exception
                        if (status != 409)
                        {
                            throw;
                        }

                        Trace.TraceWarning("MessagingException HttpStatusCode.Conflict - Queue likely already exists or is being created or deleted for path: {0}", path);
                    }
                }
            }
        }

        public static async Task DeleteQueueIfExistsAsync(string connectionString, string path)
        {
            var adminClient = new ServiceBusAdministrationClient(connectionString);

            if (await adminClient.QueueExistsAsync(path))
            {
                try
                {
                    await adminClient.DeleteQueueAsync(path);
                }
                catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
                {
                    Trace.TraceWarning(
                        "MessagingEntityNotFoundException Deleting Queue - Queue does not exist at path: {0}", path);
                }
            }
        }
    }
}
