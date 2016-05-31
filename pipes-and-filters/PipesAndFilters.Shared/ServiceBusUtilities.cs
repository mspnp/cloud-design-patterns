// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace PipesAndFilters.Shared
{
    using System.Diagnostics;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    public static class ServiceBusUtilities
    {
        public static async Task CreateQueueIfNotExistsAsync(string connectionString, string path)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

            if (!await namespaceManager.QueueExistsAsync(path))
            {
                try
                {
                    await namespaceManager.CreateQueueAsync(path);
                }
                catch (MessagingEntityAlreadyExistsException)
                {
                    Trace.TraceWarning(
                        "MessagingEntityAlreadyExistsException Creating Queue - Queue likely already exists for path: {0}", path);
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

                        Trace.TraceWarning("MessagingException HttpStatusCode.Conflict - Queue likely already exists or is being created or deleted for path: {0}", path);
                    }
                }
            }
        }

        public static async Task DeleteQueueIfExistsAsync(string connectionString, string path)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

            if (await namespaceManager.QueueExistsAsync(path))
            {
                try
                {
                    await namespaceManager.DeleteQueueAsync(path);
                }
                catch (MessagingEntityNotFoundException)
                {
                    Trace.TraceWarning(
                        "MessagingEntityNotFoundException Deleting Queue - Queue does not exist at path: {0}", path);
                }
            }
        }
    }
}
