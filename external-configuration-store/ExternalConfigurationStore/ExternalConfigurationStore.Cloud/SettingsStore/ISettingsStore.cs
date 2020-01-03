// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExternalConfigurationStore.Cloud.SettingsStore
{
    public interface ISettingsStore
    {
        Task<ETag> GetVersionAsync();

        Task<Dictionary<string, string>> FindAllAsync();
    }
}