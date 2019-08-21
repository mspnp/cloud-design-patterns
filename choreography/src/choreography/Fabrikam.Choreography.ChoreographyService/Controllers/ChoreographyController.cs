// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// -

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabrikam.Choreography.ChoreographyService.Models;
using Fabrikam.Choreography.ChoreographyService.Services;
using Fabrikam.Communicator.Service.Operations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;


namespace Fabrikam.Choreography.ChoreographyService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChoreographyController : ControllerBase
    {

        private readonly ILogger<ChoreographyController> logger;
        private readonly IPackageServiceCaller packageServiceCaller;
        private readonly IDroneSchedulerServiceCaller droneSchedulerServiceCaller;
        private readonly IDeliveryServiceCaller deliveryServiceCaller;
        private readonly IEventRepository eventRepository;

        public ChoreographyController(IPackageServiceCaller packageServiceCaller,
                             IDroneSchedulerServiceCaller droneSchedulerServiceCaller,
                             IDeliveryServiceCaller deliveryServiceCaller,
                             IEventRepository eventRepository,
                             ILogger<ChoreographyController> logger)
        {
            this.packageServiceCaller = packageServiceCaller;
            this.droneSchedulerServiceCaller = droneSchedulerServiceCaller;
            this.deliveryServiceCaller = deliveryServiceCaller;
            this.eventRepository = eventRepository;
            this.logger = logger;
           
        }

        [HttpPost]
        [Route("/api/[controller]/operation")]
        [ProducesResponseType(typeof(void), 200)]
        [ProducesResponseType(typeof(void), 400)]
        [ProducesResponseType(typeof(void), 500)]
        public async Task<IActionResult> Operation([FromBody] EventGridEvent[] events)
        {

            if (events == null)
            {
                logger.LogError("event is  Null");
                return BadRequest("No Event for Choreography");
            }
                
            if (events[0].EventType is EventTypes.EventGridSubscriptionValidationEvent)
            {
                try
                {
                    var data = Operations.ConvertDataEventToType<SubscriptionValidationEventData>(events[0].Data);
                    var response = new SubscriptionValidationResponse(data.ValidationCode);
                    return Ok(response);
                }
                catch (NullReferenceException ex)
                {
                    logger.LogError("Event Grid Subscription validation error", ex);
                    return BadRequest(ex);
                }

            }

            foreach(var e in events)
            {
                Delivery delivery;

                try
                {
                    delivery = Operations.ConvertDataEventToType<Delivery>(e.Data);
                }
                catch (InvalidCastException ex)
                {
                    logger.LogError("Invalid delivery Object for delivery payload", ex);
                    return BadRequest(ex);
                }

                if (delivery is null)
                {
                    logger.LogError("null delivery in delivery data");
                    return BadRequest("Invalid delivery");
                }

                List<EventGridEvent> listEvents = new List<EventGridEvent>();
                e.Topic = eventRepository.GetTopic();
                e.EventTime = DateTime.Now;
                switch (e.EventType)
                {
                    case Operations.ChoreographyOperation.ScheduleDelivery:
                        {
                            try
                            {
                                var packageGen = await packageServiceCaller.UpsertPackageAsync(delivery.PackageInfo).ConfigureAwait(false);
                                if (packageGen is null)
                                {
                                    //we return bad request and allow the event to be reprocessed by event grid
                                    return BadRequest("could not get a package object from package service");
                                }

                                //we set the eventype to the next choreography step
                                e.EventType = Operations.ChoreographyOperation.CreatePackage;
                                listEvents.Add(e);
                                await eventRepository.SendEventAsync(listEvents);
                                return Ok("Created Package Completed");
                            }
                            catch (Exception ex) when (ex is BackendServiceCallFailedException ||
                                                     ex is EventException || ex is Exception)
                            {
                                logger.LogError(ex.Message, ex);
                                return BadRequest(ex);

                            }

                        }
                    case Operations.ChoreographyOperation.CreatePackage:
                        {
                            try
                            {
                                var droneId = await droneSchedulerServiceCaller.GetDroneIdAsync(delivery).ConfigureAwait(false);
                                if (droneId is null)
                                {
                                    //we return bad request and allow the event to be reprocessed by event grid
                                    return BadRequest("could not get a drone id");
                                }
                                e.Subject = droneId;
                                e.EventType = Operations.ChoreographyOperation.GetDrone;
                                listEvents.Add(e);
                                await eventRepository.SendEventAsync(listEvents);
                                return Ok("Drone Completed");

                            }
                            catch (Exception ex) when (ex is BackendServiceCallFailedException ||
                                                       ex is EventException)
                            {
                                logger.LogError(ex.Message, ex);
                                return BadRequest(ex);
                            }
                        }
                    case Operations.ChoreographyOperation.GetDrone:
                        {
                            try
                            {
                                var deliverySchedule = await deliveryServiceCaller.ScheduleDeliveryAsync(delivery, e.Subject);
                                return Ok("Delivery Completed");
                            }
                            catch (Exception ex) when (ex is BackendServiceCallFailedException)
                            {
                                logger.LogError(ex.Message, ex);
                                return BadRequest(ex);
                            }
                        }
                }



            }


            return BadRequest();
        }

    }
}


   

     


    
