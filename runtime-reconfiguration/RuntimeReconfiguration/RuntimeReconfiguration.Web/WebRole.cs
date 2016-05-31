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
