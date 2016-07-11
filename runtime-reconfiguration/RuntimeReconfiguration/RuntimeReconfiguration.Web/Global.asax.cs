// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
