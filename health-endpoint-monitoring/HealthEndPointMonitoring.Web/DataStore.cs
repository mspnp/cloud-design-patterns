// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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