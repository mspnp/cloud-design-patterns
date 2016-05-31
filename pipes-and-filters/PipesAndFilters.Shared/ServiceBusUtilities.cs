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
