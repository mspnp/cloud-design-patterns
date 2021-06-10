// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabrikam.Choreography.ChoreographyService.Controllers;
using Fabrikam.Choreography.ChoreographyService.Models;
using Fabrikam.Choreography.ChoreographyService.Services;
using Microsoft.AspNetCore.Mvc;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Fabrikam.Choreography.ChoreographyService.Tests
{
    [TestClass]
    public class ChoreographyControllerFixture
    {
        private static Delivery delivery = new Delivery();
        public static object obj = Activator.CreateInstance(typeof(SubscriptionValidationEventData));
        SubscriptionValidationEventData eventValidationData = (SubscriptionValidationEventData) obj;
     
        [TestMethod]
        public async Task Post_Returns200_ForSubscriptionValidationData()
        {
            var eventOp = new EventGridEvent(
                "EventSubject",
                "EventType",
                "1.0",
                "This is the event data");
            eventOp.EventType = "Microsoft.EventGrid.SubscriptionValidationEvent";
            eventOp.Data = new BinaryData(eventValidationData);
            var target = new ChoreographyController(new Mock<IPackageServiceCaller>().Object,
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
            eventOp.EventType = "Microsoft.EventGrid.SubscriptionValidationEvent";
            eventOp.Data = null;
            var target = new ChoreographyController(new Mock<IPackageServiceCaller>().Object,
                                                new Mock<IDroneSchedulerServiceCaller>().Object,
                                                new Mock<IDeliveryServiceCaller>().Object,
                                                new Mock<IEventRepository>().Object,
                                                loggerMock.Object);

            EventGridEvent[] events = new EventGridEvent[1];

            events[0] = eventOp;
            var result = await target.Operation(events) as BadRequestObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(400, result.StatusCode);
            loggerMock.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<FormattedLogValues>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()), Times.Once);
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
            Delivery delivery = new Delivery();
            eventOp.Data = new BinaryData(delivery);
            var target = new ChoreographyController(new Mock<IPackageServiceCaller>().Object,
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
            eventOp.Data = new BinaryData(delivery);
            var target = new ChoreographyController(packageServiceCallerMock.Object,
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
            eventOp.Data = new BinaryData(delivery);
            var target = new ChoreographyController(new Mock<IPackageServiceCaller>().Object,
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
                       
            var target = new ChoreographyController(new Mock<IPackageServiceCaller>().Object,
                                                new Mock<IDroneSchedulerServiceCaller>().Object,
                                                new Mock<IDeliveryServiceCaller>().Object,
                                                new Mock<IEventRepository>().Object,
                                                loggerMock.Object);

            var result = await target.Operation(null) as BadRequestObjectResult; 
            Assert.IsNotNull(result);
            Assert.AreEqual(400, result.StatusCode);
            loggerMock.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<FormattedLogValues>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()), Times.Once);


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
            var target = new ChoreographyController(new Mock<IPackageServiceCaller>().Object,
                                                new Mock<IDroneSchedulerServiceCaller>().Object,
                                                new Mock<IDeliveryServiceCaller>().Object,
                                                new Mock<IEventRepository>().Object,
                                                loggerMock.Object);
            EventGridEvent[] events = new EventGridEvent[1];
            events[0] = eventOp;
            var result = await target.Operation(events) as BadRequestObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(400, result.StatusCode);
            loggerMock.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<FormattedLogValues>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()), Times.Once);


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
            eventOp.Data = new BinaryData(content);

            var target = new ChoreographyController(new Mock<IPackageServiceCaller>().Object,
                                                new Mock<IDroneSchedulerServiceCaller>().Object,
                                                new Mock<IDeliveryServiceCaller>().Object,
                                                new Mock<IEventRepository>().Object,
                                                loggerMock.Object);

            EventGridEvent[] events = new EventGridEvent[1];
            events[0] = eventOp;
            var result = await target.Operation(events) as BadRequestObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(400, result.StatusCode);
            loggerMock.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<FormattedLogValues>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()), Times.Once);


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
            Delivery delivery = new Delivery();
            eventOp.Data = new BinaryData(delivery);
            var target = new ChoreographyController(packageServiceCallerMock.Object,
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
            loggerMock.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<FormattedLogValues>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()), Times.Once);

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
            Delivery delivery = new Delivery();
            eventOp.Data = new BinaryData(delivery);
            var target = new ChoreographyController(new Mock<IPackageServiceCaller>().Object,
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
            loggerMock.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<FormattedLogValues>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()), Times.Once);
        }
    }
}
