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
