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
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Linq;

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

        public ChoreographyController(
            IPackageServiceCaller packageServiceCaller,
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
                logger.LogError("event is Null");
                return BadRequest("No Event for Choreography");
            }

            if (events[0].EventType is SystemEventNames.EventGridSubscriptionValidation)
            {

                events[0].TryGetSystemEventData(out object systemEvent);

                if (systemEvent == null)
                {
                    var ex = new NullReferenceException("systemEvent is not set");
                    logger.LogError("Event Grid Subscription validation error", ex);
                    return BadRequest(ex);
                }

                switch (systemEvent)
                {
                    case SubscriptionValidationEventData subscriptionValidation:
                        return new OkObjectResult(new SubscriptionValidationResponse()
                        {
                            ValidationResponse = subscriptionValidation.ValidationCode
                        });
                    default:
                        break;
                }
            }

            var schema = GenerateDeliverySchema();

            foreach (var e in events)
            {
                Delivery delivery;

                try
                {
                    if (!IsDeliveryObjectValid(e.Data, schema))
                    {
                        logger.LogError("Invalid delivery Object for delivery payload");
                        return BadRequest("Invalid delivery");
                    }

                    delivery = e.Data.ToObjectFromJson<Delivery>();
                }
                catch (NullReferenceException ex)
                {
                    logger.LogError("null delivery in delivery data. " + ex.ToString());
                    return BadRequest("Invalid delivery");
                }

                List<EventGridEvent> listEvents = new List<EventGridEvent>();
                e.Topic = eventRepository.GetTopic();
                e.EventTime = DateTime.UtcNow;

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
                            catch (EventException ex)
                            {
                                logger.LogError(ex.Message, ex);
                                return BadRequest(ex);

                            }
                            catch (BackendServiceCallFailedException ex)
                            {
                                logger.LogError(ex.Message, ex);
                                return StatusCode(500);
                            }
                            catch (Exception ex)
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
                            catch (EventException ex)
                            {
                                logger.LogError(ex.Message, ex);
                                return BadRequest(ex);
                            }
                            catch (BackendServiceCallFailedException ex)
                            {
                                logger.LogError(ex.Message, ex);
                                return StatusCode(500);
                            }
                        }
                    case Operations.ChoreographyOperation.GetDrone:
                        {
                            try
                            {
                                var deliverySchedule = await deliveryServiceCaller.ScheduleDeliveryAsync(delivery, e.Subject);
                                return Ok("Delivery Completed");
                            }
                            catch (BackendServiceCallFailedException ex)
                            {
                                logger.LogError(ex.Message, ex);
                                return BadRequest(ex);
                            }
                        }
                }
            }
            return BadRequest();
        }

        private JSchema GenerateDeliverySchema()
        {
            JSchemaGenerator generator = new JSchemaGenerator();
            JSchema schema = generator.Generate(typeof(Delivery));
            schema.AllowAdditionalPropertiesSpecified = false;
            schema.AllowAdditionalProperties = false;
            return schema;
        }

        private bool IsDeliveryObjectValid(BinaryData bdata, JSchema schema)
        {
            JObject parsedDelivery = JObject.Parse(bdata.ToString());
            return parsedDelivery.IsValid(schema);
        }
    }
}