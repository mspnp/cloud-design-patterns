// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace RuntimeReconfiguration.Web
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.WindowsAzure.Diagnostics;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class WebRole : RoleEntryPoint
    {
        private const string CustomSettingName = "CustomSetting";

        public override bool OnStart()
        {
            // Add the trace listener, as the WebRole process is not configured by the web.config
            Trace.Listeners.Add(new DiagnosticMonitorTraceListener());

            RoleEnvironment.Changing += this.RoleEnvironment_Changing;

            return base.OnStart();
        }

        private void RoleEnvironment_Changing(object sender, RoleEnvironmentChangingEventArgs e)
        {
            var changedSettings = e.Changes.OfType<RoleEnvironmentConfigurationSettingChange>()
                                            .Select(c => c.ConfigurationSettingName).ToList();

            Trace.TraceInformation("Configuration Changing notification. Settings being changed: "
                + string.Join(", ", changedSettings));

            if (changedSettings
                .Any(settingName => !string.Equals(settingName, CustomSettingName, StringComparison.Ordinal)))
            {
                Trace.TraceInformation("Cancelling dynamic configuration change (restarting).");

                // Setting this to true will restart the role gracefully. If Cancel is not set to true,
                // and the change is not handled by the application, then the application will not use
                // the new value until it is restarted (either manually or for some other reason).
                e.Cancel = true;
            }
            else
            {
                Trace.TraceInformation("Handling configuration change without restarting.");
            }
        }
    }
}
