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
        private KRPC.Service.Services services;

        [SetUp]
        public void SetUp ()
        {
            services = new KRPC.Service.Services ();
        }

        [Test]
        public void NonExistantService ()
        {
            var request = Request.CreateBuilder()
                .SetService ("NonExistantService")
                .SetProcedure ("NonExistantMethod")
                .Build();
            Assert.Throws<RPCException>(
                () => { services.HandleRequest (request); });
        }

        [Test]
        public void NonExistantMethod ()
        {
            var request = Request.CreateBuilder()
                .SetService ("TestService")
                .SetProcedure ("NonExistantMethod")
                .Build();
            Assert.Throws<RPCException> (
                () => { services.HandleRequest (request); });
        }

        [Test]
        public void MethodWithoutAttribute ()
        {
            var request = Request.CreateBuilder()
                .SetService ("TestService")
                .SetProcedure ("MethodWithoutAttribute")
                .Build();
            Assert.Throws<RPCException>(
                () => { services.HandleRequest (request); });
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
                .SetProcedure ("MethodNoArgsNoReturn")
                .Build();
            // Run the request
            services.HandleRequest(request);
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
                .SetProcedure ("MethodArgsNoReturn")
                .AddParameters(ByteString.CopyFrom(argBytes))
                .Build();
            // Run the request
            services.HandleRequest(request);
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
                .SetProcedure ("MethodNoArgsReturns")
                .Build();
            // Run the request
            Response.Builder responseBuilder = services.HandleRequest(request);
            responseBuilder.SetTime (42);
            Response response = responseBuilder.Build ();
            mock.Verify (x => x.MethodNoArgsReturns (), Times.Once ());
            // Check the return value
            Response innerResponse = Response.CreateBuilder ().MergeFrom (response.Return).Build ();
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
                .SetProcedure ("MethodArgsReturns")
                .AddParameters (ByteString.CopyFrom (expectedResponseBytes))
                .Build();
            // Run the request
            Response.Builder responseBuilder = services.HandleRequest(request);
            responseBuilder.Time = 42;
            Response response = responseBuilder.Build ();
            mock.Verify (x => x.MethodArgsReturns (It.IsAny<Response>()), Times.Once ());
            // Check the return value
            Response innerResponse = Response.CreateBuilder ().MergeFrom (response.Return).Build ();
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
                    Assert.AreEqual (4, service.ProceduresCount);
                    foreach (KRPC.Schema.KRPC.Procedure method in service.ProceduresList) {
                        if (method.Name == "MethodNoArgsNoReturn") {
                            Assert.AreEqual (0, method.ParameterTypesCount);
                            Assert.IsFalse (method.HasReturnType);
                        }
                        if (method.Name == "MethodArgsNoReturn") {
                            Assert.AreEqual (1, method.ParameterTypesCount);
                            Assert.AreEqual ("KRPC.Response", method.ParameterTypesList[0]);
                            Assert.IsFalse (method.HasReturnType);
                        }
                        if (method.Name == "MethodNoArgsReturns") {
                            Assert.AreEqual (0, method.ParameterTypesCount);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("KRPC.Response", method.ReturnType);
                        }
                        if (method.Name == "MethodArgsReturns") {
                            Assert.AreEqual (1, method.ParameterTypesCount);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("KRPC.Response", method.ParameterTypesList[0]);
                            Assert.AreEqual ("KRPC.Response", method.ReturnType);
                        }
                    }
                }
            }
        }
    }
}

