﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Fabrikam.Choreography.ChoreographyService.Models;

namespace Fabrikam.Choreography.ChoreographyService.Services
{
    public class PackageServiceCaller : IPackageServiceCaller
    {
        private readonly HttpClient _httpClient;

        public PackageServiceCaller(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PackageGen> UpsertPackageAsync(PackageInfo packageInfo)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{packageInfo.PackageId}", packageInfo);
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    return await JsonSerializer.DeserializeAsync<PackageGen>(await response.Content.ReadAsStreamAsync());
                }
                else if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return  await this.GetPackageAsync(packageInfo.PackageId);
                }

                throw new BackendServiceCallFailedException(response.ReasonPhrase);
            }
            catch (BackendServiceCallFailedException)
            {
                throw;
            }
           catch (Exception e)
            {
                throw new BackendServiceCallFailedException(e.Message, e);
            }
        }

        private async Task<PackageGen> GetPackageAsync(string packageId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{packageId}");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return await JsonSerializer.DeserializeAsync<PackageGen>(await response.Content.ReadAsStreamAsync());
                }

                throw new BackendServiceCallFailedException(response.ReasonPhrase);
            }
            catch (BackendServiceCallFailedException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new BackendServiceCallFailedException(e.Message, e);
            }
        }
    }
}
