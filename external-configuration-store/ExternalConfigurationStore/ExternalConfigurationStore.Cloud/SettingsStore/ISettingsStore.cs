// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace ExternalConfigurationStore.Cloud.SettingsStore
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ISettingsStore
    {
        Task<string> GetVersionAsync();

        Task<Dictionary<string, string>> FindAllAsync();
    }
}
