// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace HealthEndPointMonitoring.Web
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class WebRole : RoleEntryPoint
    {
        public override bool OnStart()
        {
            RoleEnvironment.Changing += (sender, e) =>
            {
                if (e.Changes.OfType<RoleEnvironmentConfigurationSettingChange>()
                .Any(c => !string.Equals(c.ConfigurationSettingName, "Test.ReturnStatusCode", StringComparison.Ordinal)))
                {
                    Trace.TraceInformation("Cancelling instance (rebooting)");
                    e.Cancel = true;
                }
                else
                {
                    Trace.TraceInformation("Handling change without recycle");
                    e.Cancel = false;
                }
            };

            return base.OnStart();
        }
    }
}
