// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Fabrikam.DroneDelivery.DeliveryService.Middlewares;

namespace Fabrikam.DroneDelivery.DeliveryService.Tests
{
    [TestClass]
    public class GlobalLoggerMiddlewareFixture
    {
        [TestMethod]
        public async Task IfHandledInternalServerError_ItLogsError()
        {
            // Arrange

            // Logger
            var loggerMock = new Mock<ILogger<GlobalLoggerMiddleware>>();
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
                         .Returns(loggerMock.Object);

            // Diagnostic
            var diagnoticSourceMock = new Mock<DiagnosticSource>();


            // Request
            var requestMock = new Mock<HttpRequest>();
            requestMock.Setup(x => x.Scheme).Returns("http");
            requestMock.Setup(x => x.Host).Returns(new HostString("localhost"));
            requestMock.Setup(x => x.Path).Returns(new PathString("/FooBar"));
            requestMock.Setup(x => x.PathBase).Returns(new PathString("/"));
            requestMock.Setup(x => x.Method).Returns("GET");
            requestMock.Setup(x => x.Body).Returns(new MemoryStream());
            
            // Response
            var responseMock = new Mock<HttpResponse>();
            responseMock.SetupGet(y => y.StatusCode).Returns(500);

            // Context
            var features = new FeatureCollection();
            var exMessage = "I'm just exceptional";
            var exceptionHandlerFeature = new ExceptionHandlerFeature()
            {
                Error = new Exception(exMessage),
            };
            features.Set<IExceptionHandlerFeature>(exceptionHandlerFeature);

            var contextMock = new Mock<HttpContext>();
            contextMock.Setup(z => z.Request).Returns(requestMock.Object);
            contextMock.Setup(z => z.Response).Returns(responseMock.Object);

            contextMock.Setup(z => z.Features).Returns(features);

            // Middleware
            var logRequestMiddleware = new GlobalLoggerMiddleware(next: (innerHttpContext) => Task.FromResult(0), loggerFactory: loggerFactory.Object, diagnosticSource: diagnoticSourceMock.Object);

            // Act
            await logRequestMiddleware.Invoke(contextMock.Object);

            // Assert
            Assert.AreEqual(1, loggerMock.Invocations.Count);
            StringAssert.Contains(loggerMock.Invocations[0].ToString(), $"An internal handled exception has occurred: {exMessage}");
            StringAssert.Contains(loggerMock.Invocations[0].ToString(), "LogLevel.Error");
        }

        [TestMethod]
        public async Task IfHandledClientError_ItLogsError()
        {
            // Arrange

            // Logger
            var loggerMock = new Mock<ILogger<GlobalLoggerMiddleware>>();
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
                         .Returns(loggerMock.Object);

            // Diagnostic
            var diagnoticSourceMock = new Mock<DiagnosticSource>();

            // Request
            var requestMock = new Mock<HttpRequest>();
            requestMock.Setup(x => x.Scheme).Returns("http");
            requestMock.Setup(x => x.Host).Returns(new HostString("localhost"));
            requestMock.Setup(x => x.Path).Returns(new PathString("/FooBar"));
            requestMock.Setup(x => x.PathBase).Returns(new PathString("/"));
            requestMock.Setup(x => x.Method).Returns("GET");
            requestMock.Setup(x => x.Body).Returns(new MemoryStream());
            
            // Response
            var responseMock = new Mock<HttpResponse>();
            responseMock.SetupGet(y => y.StatusCode).Returns(499);

            // Context
            var contextMock = new Mock<HttpContext>();
            contextMock.Setup(z => z.Request).Returns(requestMock.Object);
            contextMock.Setup(z => z.Response).Returns(responseMock.Object);

            // Middleware
            var logRequestMiddleware = new GlobalLoggerMiddleware(next: (innerHttpContext) => Task.FromResult(0), loggerFactory: loggerFactory.Object, diagnosticSource: diagnoticSourceMock.Object);

            // Act
            await logRequestMiddleware.Invoke(contextMock.Object);

            // Assert
            Assert.AreEqual(1, loggerMock.Invocations.Count);
            StringAssert.Contains(loggerMock.Invocations[0].ToString(), "An error has occurred: 499");
            StringAssert.Contains(loggerMock.Invocations[0].ToString(), "LogLevel.Error");
        }

        [TestMethod]
        public async Task IfTooManyRequestIsHandled_ItLogsError()
        {
            // Arrange

            // Logger
            var loggerMock = new Mock<ILogger<GlobalLoggerMiddleware>>();
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
                         .Returns(loggerMock.Object);

            // Diagnostic
            var diagnoticSourceMock = new Mock<DiagnosticSource>();

            // Request
            var requestMock = new Mock<HttpRequest>();
            requestMock.Setup(x => x.Scheme).Returns("http");
            requestMock.Setup(x => x.Host).Returns(new HostString("localhost"));
            requestMock.Setup(x => x.Path).Returns(new PathString("/FooBar"));
            requestMock.Setup(x => x.PathBase).Returns(new PathString("/"));
            requestMock.Setup(x => x.Method).Returns("GET");
            requestMock.Setup(x => x.Body).Returns(new MemoryStream());

            // Response
            var responseMock = new Mock<HttpResponse>();
            responseMock.SetupGet(y => y.StatusCode).Returns(429);

            // Context
            var contextMock = new Mock<HttpContext>();
            contextMock.Setup(z => z.Request).Returns(requestMock.Object);
            contextMock.Setup(z => z.Response).Returns(responseMock.Object);

            // Middleware
            var logRequestMiddleware = new GlobalLoggerMiddleware(next: (innerHttpContext) => Task.FromResult(0), loggerFactory: loggerFactory.Object, diagnosticSource: diagnoticSourceMock.Object);

            // Act
            await logRequestMiddleware.Invoke(contextMock.Object);

            // Assert
            Assert.AreEqual(1, loggerMock.Invocations.Count);
            StringAssert.Contains(loggerMock.Invocations[0].ToString(), "An error has occurred: 429");
            StringAssert.Contains(loggerMock.Invocations[0].ToString(), "LogLevel.Error");
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task IfUnhandledExceptionWhileResponding_ItLogsErrorPlusWarningAndRethrowException()
        {
            // Arrange

            // Logger
            var loggerMock = new Mock<ILogger<GlobalLoggerMiddleware>>();
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
                         .Returns(loggerMock.Object);

            // Diagnostic
            var diagnoticSourceMock = new Mock<DiagnosticSource>();

            // Request
            var requestMock = new Mock<HttpRequest>();
            requestMock.Setup(x => x.Scheme).Returns("http");
            requestMock.Setup(x => x.Host).Returns(new HostString("localhost"));
            requestMock.Setup(x => x.Path).Returns(new PathString("/FooBar"));
            requestMock.Setup(x => x.PathBase).Returns(new PathString("/"));
            requestMock.Setup(x => x.Method).Returns("GET");
            requestMock.Setup(x => x.Body).Returns(new MemoryStream());
            // Response
            var responseMock = new Mock<HttpResponse>();
            responseMock.SetupGet(y => y.StatusCode).Returns(500);
            responseMock.SetupGet(y => y.HasStarted).Returns(true);

            // Context
            var contextMock = new Mock<HttpContext>();
            contextMock.Setup(z => z.Request).Returns(requestMock.Object);
            contextMock.Setup(z => z.Response).Returns(responseMock.Object);
            
            // Middleware
            var exMessage = "I'm just exceptional";
            var logRequestMiddleware = new GlobalLoggerMiddleware(next: (innerHttpContext) => throw new Exception(exMessage), loggerFactory: loggerFactory.Object, diagnosticSource: diagnoticSourceMock.Object);

            // Act
            try
            {
                await logRequestMiddleware.Invoke(contextMock.Object);
            }
            // Assert
            catch (Exception)
            {
                Assert.AreEqual(2, loggerMock.Invocations.Count);
                StringAssert.Contains(loggerMock.Invocations[0].ToString(), $"An exception was thrown attempting to execute the global internal server error handler: {exMessage}");
                StringAssert.Contains(loggerMock.Invocations[0].ToString(), "LogLevel.Error");

                StringAssert.Contains(loggerMock.Invocations[1].ToString(), "The response has already started, the error handler will not be executed.");
                StringAssert.Contains(loggerMock.Invocations[1].ToString(), "LogLevel.Warning");

                throw;
            }
        }
    }
}
