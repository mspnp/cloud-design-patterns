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
    using System.Web;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class Global : HttpApplication
    {
        private const string CustomSettingName = "CustomSetting";

        protected void Application_Start(object sender, EventArgs e)
        {
            ConfigureFromSetting(CustomSettingName);

            RoleEnvironment.Changed += this.RoleEnvironment_Changed;
        }

        private static void ConfigureFromSetting(string settingName)
        {
            var value = RoleEnvironment.GetConfigurationSettingValue(settingName);
            SomeRuntimeComponent.Instance.CurrentValue = value;
        }

        private void RoleEnvironment_Changed(object sender, RoleEnvironmentChangedEventArgs e)
        {
            Trace.TraceInformation("Updating instance with new configuration settings.");

            foreach (var settingChange in e.Changes.OfType<RoleEnvironmentConfigurationSettingChange>())
            {
                if (string.Equals(settingChange.ConfigurationSettingName, CustomSettingName, StringComparison.Ordinal))
                {
                    ConfigureFromSetting(CustomSettingName);
                }
            }
        }
    }
}
