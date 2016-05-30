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
namespace HealthEndPointMonitoring.Web
{
    using HealthEndpointMonitoring.Web;

    public class DataStore
    {
        public static readonly DataStore Instance = new DataStore();

        public void CoreHealthCheck()
        {
            var blobClient = Settings.StorageAccount.CreateCloudBlobClient();
            
            //Check to see if the service is available and handling requests
            //This could be a listing or simply retrieving metadata for a known file
            //The operation selected here is not based on the lowest impact/load on the service but simple set of requirements.  Consider an operation that does not put a of load on the service when performing a simple ping to determine if the service is alive.
            blobClient.ListContainers("healthcheck");

            //In addition to a simple operation to determine if the service is available and responding
            //  We could check that connection strings match the environment
            //  We could check containers on access policies and other store dependencies for the application
        }
    }
}