// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace PipesAndFilters.Shared
{
    using Microsoft.Azure;

    public class Settings
    {
        public static string ServiceBusConnectionString
        {
            get
            {
                return CloudConfigurationManager.GetSetting("ServiceBus.ConnectionString");
            }
        }
    }
}
