// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace ExternalConfigurationStore.Cloud
{
    using System;
    using Microsoft.Azure;

    public static class ExternalConfiguration
    {
        private static readonly Lazy<ExternalConfigurationManager> configuredInstance = new Lazy<ExternalConfigurationManager>(
            () =>
            {
                var environment = CloudConfigurationManager.GetSetting("environment");
                return new ExternalConfigurationManager(environment);
            });

        public static ExternalConfigurationManager Instance => configuredInstance.Value;
    }
}
