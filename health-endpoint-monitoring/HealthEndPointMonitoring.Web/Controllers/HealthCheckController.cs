// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace HealthEndPointMonitoring.Web.Controllers
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Web.Mvc;
    using Microsoft.Azure;

    public class HealthCheckController : Controller
    {
        /// <summary>
        /// Controller action to perform a simple check on dependent services
        /// </summary>
        /// <returns></returns>
        public ActionResult CoreServices()
        {
            // The id could be utilized as a basic method to obscure/hide the endpoint
            // where an id to match could be retrieved from configuration if matched perform test and return result, if not return 404
            try
            {
                // Run a simple check to ensure database is available
                DataStore.Instance.CoreHealthCheck();

                // Run a simple check on our external service
                MyExternalService.Instance.CoreHealthCheck();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception in basic health check: {0}", ex.Message);

                // This can optionally return different status codes based on the exception
                // Optionally we could return more details about the exception.
                // The additional information could be used by a devops person hitting the endpoint with a browser or some of the ping utilities will utilize additional information
                return new HttpStatusCodeResult((int)HttpStatusCode.InternalServerError);
            }

            return new HttpStatusCodeResult((int)HttpStatusCode.OK);
        }

        /// <summary>
        /// Perform a check that uses a configurable obscure path
        /// /HealthCheck/ObscurePath/{key-path} F3CJ34X9
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public ActionResult ObscurePath(string id)
        {
            // We can set this through configuration in order to hide the endpoint
            var hiddenPathKey = CloudConfigurationManager.GetSetting("Test.ObscurePath");

            // If the value passed does not match that in configuration return 403 not found
            if (!string.Equals(id, hiddenPathKey))
            {
                return new HttpStatusCodeResult((int)HttpStatusCode.NotFound);
            }

            //// Else we continue and run our tests...

            // Return results from the configuration test
            return this.CoreServices();
        }

        /// <summary>
        /// Test check that randomly returns 500 or 200 status codes
        /// emulating an unstable service.
        /// /HealthCheck/CheckUnstableServiceHealth 
        /// </summary>
        /// <returns></returns>
        public ActionResult CheckUnstableServiceHealth()
        {
            // Get a random number
            var rnd = new Random().Next(10);
           
            // If the random number is less than 8 return a 200 else return 500
            return new HttpStatusCodeResult((rnd < 8) ? 200 : 500);
        }

        /// <summary>
        /// Test health check that returns a response code set in configuration for testing
        /// /HealthCheck/TestResponseFromConfig 
        /// </summary>
        /// <returns></returns>
        public ActionResult TestResponseFromConfig()
        {
            var returnStatusCodeSetting = CloudConfigurationManager.GetSetting("Test.ReturnStatusCode");

            int returnStatusCode;

            if (!int.TryParse(returnStatusCodeSetting, out returnStatusCode))
            {
                returnStatusCode = (int)HttpStatusCode.OK;
            }

            return new HttpStatusCodeResult(returnStatusCode);
        }
    }
}