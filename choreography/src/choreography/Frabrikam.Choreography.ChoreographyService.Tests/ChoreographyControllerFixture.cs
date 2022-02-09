// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Fabrikam.Choreography.ChoreographyService.Controllers;
using Fabrikam.Choreography.ChoreographyService.Models;
using Fabrikam.Choreography.ChoreographyService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Fabrikam.Choreography.ChoreographyService.Tests
{
    [TestClass]
    public class ChoreographyControllerFixture
    {
        private static Delivery delivery = new Delivery()
        {
            ConfirmationRequired = ConfirmationRequired.None,
            DeliveryId = Guid.NewGuid().ToString(),
            PickupLocation = "local",
            PickupTime = DateTime.UtcNow.AddDays(1),
            OwnerId = Guid.NewGuid().ToString()
        };

        SubscriptionValidationEventData eventValidationData = EventGridModelFactory.SubscriptionValidationEventData(Guid.NewGuid().ToString(), "http://url");

        [TestMethod]
        public async Task Post_Returns200_ForSubscriptionValidationData()
        {
            var eventOp = new EventGridEvent(
                "EventSubject",
                "EventType",
                "1.0",
                "This is the event data");
            eventOp.EventType = SystemEventNames.EventGridSubscriptionValidation;

            var eventValidationDataAsJson = Newtonsoft.Json.JsonConvert.SerializeObject(eventValidationData);

            eventOp.Data = new BinaryData(Encoding.UTF8.GetBytes(eventValidationDataAsJson));

            var target = new ChoreographyController(
                new Mock<IPackageServiceCaller>().Object,
                new Mock<IDroneSchedulerServiceCaller>().Object,
                new Mock<IDeliveryServiceCaller>().Object,
                new Mock<IEventRepository>().Object,
                new Mock<ILogger<ChoreographyController>>().Object);

            EventGridEvent[] events = new EventGridEvent[1];
            events[0] = eventOp;
            var result = await target.Operation(events) as OkObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);
        }

        [TestMethod]
        public async Task Post_Returns400_ForInvalidSubscriptionValidationData()
        {
            var loggerMock = new Mock<ILogger<ChoreographyController>>();

            var eventOp = new EventGridEvent(
                "EventSubject",
                "EventType",
                "1.0",
                "This is the event data");
            eventOp.EventType = SystemEventNames.EventGridSubscriptionValidation;
            eventOp.Data = null;

            var target = new ChoreographyController(
                new Mock<IPackageServiceCaller>().Object,
                new Mock<IDroneSchedulerServiceCaller>().Object,
                new Mock<IDeliveryServiceCaller>().Object,
                new Mock<IEventRepository>().Object,
                loggerMock.Object);

            EventGridEvent[] events = new EventGridEvent[1];
            events[0] = eventOp;
            var result = await target.Operation(events) as BadRequestObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual(1, loggerMock.Invocations.Count);
            StringAssert.Contains(loggerMock.Invocations[0].ToString(), "Event Grid Subscription validation error");
        }

        [TestMethod]
        public async Task Post_Returns200_ForValidDeliveryService()
        {
            var deliveryCallerMock = new Mock<IDeliveryServiceCaller>();
            deliveryCallerMock.Setup(r => r.ScheduleDeliveryAsync(It.IsAny<Delivery>(), It.IsAny<string>()))
                .ReturnsAsync(new DeliverySchedule { Id = "deliveryid" }).Verifiable();

            var eventOp = new EventGridEvent(
                "EventSubject",
                "EventType",
                "1.0",
                "This is the event data");
            eventOp.EventType = "GetDrone";

            var deliveryEventDataAsJson = Newtonsoft.Json.JsonConvert.SerializeObject(delivery);

            eventOp.Data = new BinaryData(Encoding.UTF8.GetBytes(deliveryEventDataAsJson));

            var target = new ChoreographyController(
                new Mock<IPackageServiceCaller>().Object,
                new Mock<IDroneSchedulerServiceCaller>().Object,
                deliveryCallerMock.Object,
                new Mock<IEventRepository>().Object,
                new Mock<ILogger<ChoreographyController>>().Object);
            EventGridEvent[] events = new EventGridEvent[1];

            events[0] = eventOp;
            var result = await target.Operation(events) as OkObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);
            deliveryCallerMock.Verify();
        }

        [TestMethod]
        public async Task Post_Returns200_ForValidPackageService()
        {
            var packageServiceCallerMock = new Mock<IPackageServiceCaller>();
            packageServiceCallerMock.Setup(r => r.UpsertPackageAsync(It.IsAny<PackageInfo>()))
                .ReturnsAsync(new PackageGen { Id = "someid" }).Verifiable();

            var eventRepositoryMock = new Mock<IEventRepository>();
            eventRepositoryMock.Setup(e => e.SendEventAsync(It.IsAny<List<EventGridEvent>>()))
                .Returns(Task.CompletedTask).Verifiable();

            var eventOp = new EventGridEvent(
                "EventSubject",
                "EventType",
                "1.0",
                "This is the event data");
            eventOp.EventType = "ScheduleDelivery";

            Delivery delivery = new Delivery();
            var deliveryEventDataAsJson = Newtonsoft.Json.JsonConvert.SerializeObject(delivery);

            eventOp.Data = new BinaryData(Encoding.UTF8.GetBytes(deliveryEventDataAsJson));

            var target = new ChoreographyController(
                packageServiceCallerMock.Object,
                new Mock<IDroneSchedulerServiceCaller>().Object,
                new Mock<IDeliveryServiceCaller>().Object,
                eventRepositoryMock.Object,
                new Mock<ILogger<ChoreographyController>>().Object);

            EventGridEvent[] events = new EventGridEvent[1];
            events[0] = eventOp;
            var result = await target.Operation(events) as OkObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);
            packageServiceCallerMock.Verify();
            eventRepositoryMock.Verify();
        }

        [TestMethod]
        public async Task Post_Returns200_ForValidDroneService()
        {
            var droneServiceCallerMock = new Mock<IDroneSchedulerServiceCaller>();
            droneServiceCallerMock.Setup(r => r.GetDroneIdAsync(It.IsAny<Delivery>()))
                .ReturnsAsync("droneId").Verifiable();

            var eventRepositoryMock = new Mock<IEventRepository>();
            eventRepositoryMock.Setup(e => e.SendEventAsync(It.IsAny<List<EventGridEvent>>()))
                .Returns(Task.CompletedTask).Verifiable();

            var eventOp = new EventGridEvent(
                "EventSubject",
                "EventType",
                "1.0",
                "This is the event data");
            eventOp.EventType = "CreatePackage";

            Delivery delivery = new Delivery();
            var deliveryEventDataAsJson = Newtonsoft.Json.JsonConvert.SerializeObject(delivery);

            eventOp.Data = new BinaryData(Encoding.UTF8.GetBytes(deliveryEventDataAsJson));

            var target = new ChoreographyController(
                new Mock<IPackageServiceCaller>().Object,
                droneServiceCallerMock.Object,
                new Mock<IDeliveryServiceCaller>().Object,
                eventRepositoryMock.Object,
                new Mock<ILogger<ChoreographyController>>().Object);

            EventGridEvent[] events = new EventGridEvent[1];
            events[0] = eventOp;
            var result = await target.Operation(events) as OkObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);
            droneServiceCallerMock.Verify();
            eventRepositoryMock.Verify();
        }


        [TestMethod]
        public async Task Post_Returns400_ForNoEvent_AndIslogged()
        {
            var loggerMock = new Mock<ILogger<ChoreographyController>>();
                       
            var target = new ChoreographyController(
                new Mock<IPackageServiceCaller>().Object,
                new Mock<IDroneSchedulerServiceCaller>().Object,
                new Mock<IDeliveryServiceCaller>().Object,
                new Mock<IEventRepository>().Object,
                loggerMock.Object);

            var result = await target.Operation(null) as BadRequestObjectResult; 
            Assert.IsNotNull(result);
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual(1, loggerMock.Invocations.Count);
            StringAssert.Contains(loggerMock.Invocations[0].ToString(), "event is Null");
        }

        [TestMethod]
        public async Task Post_Returns400_ForNullDelivery()
        {
            var loggerMock = new Mock<ILogger<ChoreographyController>>();

            var eventOp = new EventGridEvent(
                "EventSubject",
                "EventType",
                "1.0",
                "This is the event data");
            eventOp.EventType = "GetDrone";
            eventOp.Data = null;

            var target = new ChoreographyController(
                new Mock<IPackageServiceCaller>().Object,
                new Mock<IDroneSchedulerServiceCaller>().Object,
                new Mock<IDeliveryServiceCaller>().Object,
                new Mock<IEventRepository>().Object,
                loggerMock.Object);

            EventGridEvent[] events = new EventGridEvent[1];
            events[0] = eventOp;
            var result = await target.Operation(events) as BadRequestObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual(1, loggerMock.Invocations.Count);
            StringAssert.Contains(loggerMock.Invocations[0].ToString(), "null delivery in delivery data");
        }

        [TestMethod]
        public async Task Post_Returns400_ForInvalidDelivery()
        {
            var loggerMock = new Mock<ILogger<ChoreographyController>>();

            var eventOp = new EventGridEvent(
                "EventSubject",
                "EventType",
                "1.0",
                "This is the event data");
            eventOp.EventType = "GetDrone";
            var content = new { invalidId = 10, InvalidName = "invalid" };

            var contentEventDataAsJson = Newtonsoft.Json.JsonConvert.SerializeObject(content);

            eventOp.Data = new BinaryData(Encoding.UTF8.GetBytes(contentEventDataAsJson));

            var target = new ChoreographyController(
                new Mock<IPackageServiceCaller>().Object,
                new Mock<IDroneSchedulerServiceCaller>().Object,
                new Mock<IDeliveryServiceCaller>().Object,
                new Mock<IEventRepository>().Object,
                loggerMock.Object);

            EventGridEvent[] events = new EventGridEvent[1];
            events[0] = eventOp;
            var result = await target.Operation(events) as BadRequestObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual(1, loggerMock.Invocations.Count);
            StringAssert.Contains(loggerMock.Invocations[0].ToString(), "Invalid delivery Object for delivery payload");
        }

        [TestMethod]
        public async Task Post_Returns400_WhenPackageServiceFailswithEventException()
        {
            var loggerMock = new Mock<ILogger<ChoreographyController>>();
            var packageServiceCallerMock = new Mock<IPackageServiceCaller>();

            packageServiceCallerMock.Setup(r => r.UpsertPackageAsync(It.IsAny<PackageInfo>()))
                .ReturnsAsync(new PackageGen { Id = "someid" }).Verifiable();

            var eventRepositoryMock = new Mock<IEventRepository>();

            eventRepositoryMock.Setup(e => e.SendEventAsync(It.IsAny<List<EventGridEvent>>()))
                .ThrowsAsync(new EventException()).Verifiable();

            var eventOp = new EventGridEvent(
                "EventSubject",
                "EventType",
                "1.0",
                "This is the event data");
            eventOp.EventType = "ScheduleDelivery";

            var deliveryEventDataAsJson = Newtonsoft.Json.JsonConvert.SerializeObject(delivery);

            eventOp.Data = new BinaryData(Encoding.UTF8.GetBytes(deliveryEventDataAsJson));

            var target = new ChoreographyController(
                packageServiceCallerMock.Object,
                new Mock<IDroneSchedulerServiceCaller>().Object,
                new Mock<IDeliveryServiceCaller>().Object,
                eventRepositoryMock.Object,
                loggerMock.Object);

            EventGridEvent[] events = new EventGridEvent[1];
            events[0] = eventOp;
            var result = await target.Operation(events) as BadRequestObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(400, result.StatusCode);
            packageServiceCallerMock.Verify();
            eventRepositoryMock.Verify();
            Assert.AreEqual(1, loggerMock.Invocations.Count);
            StringAssert.Contains(loggerMock.Invocations[0].ToString(), "ChoreographyService.Services.EventException");
        }

        [TestMethod]
        public async Task Post_Returns400_WhenDroneServiceFailsToSendEvent()
        {
            var loggerMock = new Mock<ILogger<ChoreographyController>>();
            var droneServiceCallerMock = new Mock<IDroneSchedulerServiceCaller>();

            droneServiceCallerMock.Setup(r => r.GetDroneIdAsync(It.IsAny<Delivery>()))
                .ReturnsAsync("droneId").Verifiable();

            var eventRepositoryMock = new Mock<IEventRepository>();

            eventRepositoryMock.Setup(e => e.SendEventAsync(It.IsAny<List<EventGridEvent>>()))
                .ThrowsAsync(new EventException()).Verifiable();

            var eventOp = new EventGridEvent(
                "EventSubject",
                "EventType",
                "1.0",
                "This is the event data");
            eventOp.EventType = "CreatePackage";

            var deliveryEventDataAsJson = Newtonsoft.Json.JsonConvert.SerializeObject(delivery);

            eventOp.Data = new BinaryData(Encoding.UTF8.GetBytes(deliveryEventDataAsJson));

            var target = new ChoreographyController(
                new Mock<IPackageServiceCaller>().Object,
                droneServiceCallerMock.Object,
                new Mock<IDeliveryServiceCaller>().Object,
                eventRepositoryMock.Object,
                loggerMock.Object);

            EventGridEvent[] events = new EventGridEvent[1];
            events[0] = eventOp;
            var result = await target.Operation(events) as BadRequestObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(400, result.StatusCode);
            droneServiceCallerMock.Verify();
            eventRepositoryMock.Verify();
            Assert.AreEqual(1, loggerMock.Invocations.Count);
            StringAssert.Contains(loggerMock.Invocations[0].ToString(), "ChoreographyService.Services.EventException");
        }
    }
}
