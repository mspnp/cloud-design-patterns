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
using Fabrikam.Communicator.Service.Utils;

namespace Fabrikam.Choreography.ChoreographyService.Services
{
    public class DeliveryServiceCaller : IDeliveryServiceCaller
    {
        private readonly HttpClient _httpClient;

        public DeliveryServiceCaller(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<DeliverySchedule> ScheduleDeliveryAsync(Delivery deliveryRequest, string droneId)
        {
            try
            {
                var schedule = CreateDeliverySchedule(deliveryRequest, droneId);

                var response = await _httpClient.PutAsJsonAsync(schedule.Id, schedule);
                if (response.StatusCode == HttpStatusCode.Created)
                {

                    return await JsonSerializer.DeserializeAsync<DeliverySchedule>(await response.Content.ReadAsStreamAsync());
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

        private DeliverySchedule CreateDeliverySchedule(Delivery deliveryRequest, string droneId)
        {
            DeliverySchedule scheduleDelivery = new DeliverySchedule
            {
                Id = deliveryRequest.DeliveryId,
                Owner = new UserAccount { AccountId = Guid.NewGuid().ToString(), UserId = deliveryRequest.OwnerId },
                Pickup = LocationRandomizer.GetRandomLocation(),
                Dropoff = LocationRandomizer.GetRandomLocation(),
                Deadline = deliveryRequest.Deadline,
                Expedited = deliveryRequest.Expedited,
                ConfirmationRequired = (ConfirmationType)deliveryRequest.ConfirmationRequired,
                DroneId = droneId,
            };

            return scheduleDelivery;
        }
    }
}
