// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace HealthEndPointMonitoring.Web
{
    public class MyExternalService
    {
        public static readonly MyExternalService Instance = new MyExternalService();

        public void CoreHealthCheck()
        {
            // Basic Functional Test

            // Perform some simple tests against the service
            //    Ping the service to see that it was accessible
            //    Authenticate against the service
        }
    }
}