using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Moq;
using Google.ProtocolBuffers;
using KRPC.Schema.KRPC;
using KRPC.Service;

namespace KRPCTest.Service
{
    [TestFixture]
    public class ServicesTest
    {
        [Test]
        public void NonExistantService ()
        {
            var request = Request.CreateBuilder()
                .SetService ("NonExistantService")
                .SetMethod ("NonExistantMethod")
                .Build();
            Assert.Throws<NoSuchServiceException>(
                () => { KRPC.Service.Services.HandleRequest (request); });
        }

        [Test]
        public void NonExistantMethod ()
        {
            var request = Request.CreateBuilder()
                .SetService ("TestService")
                .SetMethod ("NonExistantMethod")
                .Build();
            Assert.Throws<NoSuchServiceMethodException> (
                () => {    KRPC.Service.Services.HandleRequest (request); });
        }

        [Test]
        public void MethodWithoutAttribute ()
        {
            var request = Request.CreateBuilder()
                .SetService ("TestService")
                .SetMethod ("MethodWithoutAttribute")
                .Build();
            Assert.Throws<NoSuchServiceMethodException>(
                () => { KRPC.Service.Services.HandleRequest (request); });
        }

        /// <summary>
        /// Test service method with no argument and no return value
        /// </summary>
        [Test]
        public void HandleRequestNoArgsNoReturn () {
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.MethodNoArgsNoReturn ());
            TestService.service = mock.Object;
            // Create request
            var request = Request.CreateBuilder()
                .SetService ("TestService")
                .SetMethod ("MethodNoArgsNoReturn")
                .Build();
            // Run the request
            KRPC.Service.Services.HandleRequest(request);
            mock.Verify (x => x.MethodNoArgsNoReturn (), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method with an argument and no return value
        /// </summary>
        [Test]
        public void HandleRequestArgsNoReturn () {
            // Create argument
            var arg = KRPC.Schema.KRPC.Response.CreateBuilder ()
                .SetError ("foo").SetTime (42).Build ();
            byte[] argBytes;
            using (MemoryStream stream = new MemoryStream()) {
                arg.WriteTo (stream);
                argBytes = stream.ToArray ();
            }
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.MethodArgsNoReturn (It.IsAny<KRPC.Schema.KRPC.Response>()))
                .Callback((KRPC.Schema.KRPC.Response x) => {
                    // Check the argument
                    Assert.AreEqual (argBytes, x.ToByteArray());
                } );
            TestService.service = mock.Object;
            // Create request
            var request = Request.CreateBuilder()
                .SetService ("TestService")
                .SetMethod ("MethodArgsNoReturn")
                .SetRequest_(ByteString.CopyFrom(argBytes))
                .Build();
            // Run the request
            KRPC.Service.Services.HandleRequest(request);
            mock.Verify (x => x.MethodArgsNoReturn (It.IsAny<KRPC.Schema.KRPC.Response>()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method with an argument and no return value
        /// </summary>
        [Test]
        public void HandleRequestNoArgsReturn () {
            // Create response
            var expectedResponse = KRPC.Schema.KRPC.Response.CreateBuilder ()
                .SetError ("foo").SetTime (42).Build ();
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.MethodNoArgsReturns ())
                .Returns(expectedResponse);
            TestService.service = mock.Object;
            // Create request
            var request = Request.CreateBuilder()
                .SetService ("TestService")
                .SetMethod ("MethodNoArgsReturns")
                .Build();
            // Run the request
            Response.Builder responseBuilder = KRPC.Service.Services.HandleRequest(request);
            responseBuilder.SetTime (42);
            Response response = responseBuilder.Build ();
            mock.Verify (x => x.MethodNoArgsReturns (), Times.Once ());
            // Check the return value
            Response innerResponse = Response.CreateBuilder ().MergeFrom (response.Response_).Build ();
            Assert.AreEqual (expectedResponse.Error, innerResponse.Error);
        }

        /// <summary>
        /// Test calling a service method with an argument and return value
        /// </summary>
        [Test]
        public void HandleRequestArgsReturn () {
            // Create resonse
            var expectedResponse = Response.CreateBuilder ()
                .SetTime (42).SetError ("bar").Build ();
            byte[] expectedResponseBytes;
            using (MemoryStream stream = new MemoryStream()) {
                expectedResponse.WriteTo (stream);
                expectedResponseBytes = stream.ToArray ();
            }
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.MethodArgsReturns (It.IsAny<Response>()))
                .Returns((Response x) => Response.CreateBuilder().MergeFrom(x).Build());
            TestService.service = mock.Object;
            // Create request
            var request = Request.CreateBuilder ()
                .SetService ("TestService")
                .SetMethod ("MethodArgsReturns")
                .SetRequest_ (ByteString.CopyFrom (expectedResponseBytes))
                .Build();
            // Run the request
            Response.Builder responseBuilder = KRPC.Service.Services.HandleRequest(request);
            responseBuilder.Time = 42;
            Response response = responseBuilder.Build ();
            mock.Verify (x => x.MethodArgsReturns (It.IsAny<Response>()), Times.Once ());
            // Check the return value
            Response innerResponse = Response.CreateBuilder ().MergeFrom (response.Response_).Build ();
            Assert.AreEqual (expectedResponse.Error, innerResponse.Error);
        }

        [Test]
        public void GetServices ()
        {
            var services = KRPC.Service.KRPC.GetServices () as KRPC.Schema.KRPC.Services;
            Assert.IsNotNull (services);
            Assert.AreEqual (2, services.Services_Count);
            foreach (KRPC.Schema.KRPC.Service service in services.Services_List) {
                if (service.Name == "TestService") {
                    Assert.AreEqual (4, service.MethodsCount);
                    foreach (KRPC.Schema.KRPC.Method method in service.MethodsList) {
                        if (method.Name == "MethodNoArgsNoReturn") {
                            Assert.IsFalse (method.HasParameterType);
                            Assert.IsFalse (method.HasReturnType);
                        }
                        if (method.Name == "MethodArgsNoReturn") {
                            Assert.IsTrue (method.HasParameterType);
                            Assert.AreEqual ("KRPC.Response", method.ParameterType);
                            Assert.IsFalse (method.HasReturnType);
                        }
                        if (method.Name == "MethodNoArgsReturns") {
                            Assert.IsFalse (method.HasParameterType);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("KRPC.Response", method.ReturnType);
                        }
                        if (method.Name == "MethodArgsReturns") {
                            Assert.IsTrue (method.HasParameterType);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("KRPC.Response", method.ParameterType);
                            Assert.AreEqual ("KRPC.Response", method.ReturnType);
                        }
                    }
                }
            }
        }
    }
}

