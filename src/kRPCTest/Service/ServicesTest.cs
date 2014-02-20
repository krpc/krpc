using System;
using System.IO;
using System.Collections.Generic;
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
                .SetProcedure ("NonExistantProcedure")
                .Build();
            Assert.Throws<RPCException>(
                () => { services.HandleRequest (request); });
        }

        [Test]
        public void NonExistantProcedure ()
        {
            var request = Request.CreateBuilder()
                .SetService ("TestService")
                .SetProcedure ("NonExistantProcedure")
                .Build();
            Assert.Throws<RPCException> (
                () => { services.HandleRequest (request); });
        }

        [Test]
        public void ProcedureWithoutAttribute ()
        {
            var request = Request.CreateBuilder()
                .SetService ("TestService")
                .SetProcedure ("ProcedureWithoutAttribute")
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
            mock.Setup (x => x.ProcedureNoArgsNoReturn ());
            TestService.service = mock.Object;
            // Create request
            var request = Request.CreateBuilder()
                .SetService ("TestService")
                .SetProcedure ("ProcedureNoArgsNoReturn")
                .Build();
            // Run the request
            services.HandleRequest(request);
            mock.Verify (x => x.ProcedureNoArgsNoReturn (), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method with a malformed argument
        /// </summary>
        [Test]
        public void HandleRequestSingleMalformedArgNoReturn () {
            // Create argument
            var arg = KRPC.Schema.KRPC.Response.CreateBuilder ()
                .SetError ("foo").SetTime (42).Build ();
            byte[] argBytes;
            using (MemoryStream stream = new MemoryStream()) {
                arg.WriteTo (stream);
                argBytes = stream.ToArray ();
            }
            // Screw it up!
            for (int i = 0; i < argBytes.Length; i++)
                argBytes[i] = (byte)(argBytes[i]+1);
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureSingleArgNoReturn (It.IsAny<KRPC.Schema.KRPC.Response>()))
                .Callback((KRPC.Schema.KRPC.Response x) => {
                    // Check the argument
                    Assert.AreEqual (argBytes, x.ToByteArray());
                } );
            TestService.service = mock.Object;
            // Create request
            var request = Request.CreateBuilder()
                .SetService ("TestService")
                .SetProcedure ("ProcedureSingleArgNoReturn")
                .AddParameters(ByteString.CopyFrom(argBytes))
                .Build();
            // Run the request
            Assert.Throws<RPCException> (() => services.HandleRequest (request));
            mock.Verify (x => x.ProcedureSingleArgNoReturn (It.IsAny<KRPC.Schema.KRPC.Response>()), Times.Never ());
        }

        /// <summary>
        /// Test calling a service method with an argument and no return value
        /// </summary>
        [Test]
        public void HandleRequestSingleArgNoReturn () {
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
            mock.Setup (x => x.ProcedureSingleArgNoReturn (It.IsAny<KRPC.Schema.KRPC.Response>()))
                .Callback((KRPC.Schema.KRPC.Response x) => {
                    // Check the argument
                    Assert.AreEqual (argBytes, x.ToByteArray());
                } );
            TestService.service = mock.Object;
            // Create request
            var request = Request.CreateBuilder()
                .SetService ("TestService")
                .SetProcedure ("ProcedureSingleArgNoReturn")
                .AddParameters(ByteString.CopyFrom(argBytes))
                .Build();
            // Run the request
            services.HandleRequest(request);
            mock.Verify (x => x.ProcedureSingleArgNoReturn (It.IsAny<KRPC.Schema.KRPC.Response>()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method with multiple parameters and no return
        /// </summary>
        [Test]
        public void HandleRequestThreeArgsNoReturn () {
            // Create arguments
            IMessage[] args = new IMessage [3];
            args[0] = KRPC.Schema.KRPC.Response.CreateBuilder ()
                .SetError ("foo").SetTime (42).Build ();
            args[1] = KRPC.Schema.KRPC.Request.CreateBuilder ()
                .SetService ("bar").SetProcedure ("bar").Build ();
            args[2] = KRPC.Schema.KRPC.Response.CreateBuilder ()
                .SetError ("baz").SetTime (123).Build ();
            List<byte[]> argBytes = new List<byte[]> ();
            for (int i = 0; i < 3; i++) {
                MemoryStream stream = new MemoryStream();
                args[i].WriteTo (stream);
                argBytes.Add (stream.ToArray ());
            }
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureThreeArgsNoReturn (
                It.IsAny<KRPC.Schema.KRPC.Response>(),
                It.IsAny<KRPC.Schema.KRPC.Request>(),
                It.IsAny<KRPC.Schema.KRPC.Response>()))
                .Callback((KRPC.Schema.KRPC.Response x,
                           KRPC.Schema.KRPC.Request y,
                           KRPC.Schema.KRPC.Response z) => {
                    // Check the argument
                    Assert.AreEqual (argBytes[0], x.ToByteArray());
                    Assert.AreEqual (argBytes[1], y.ToByteArray());
                    Assert.AreEqual (argBytes[2], z.ToByteArray());
                } );
            TestService.service = mock.Object;
            // Create request
            var request = Request.CreateBuilder()
                .SetService ("TestService")
                .SetProcedure ("ProcedureThreeArgsNoReturn")
                .AddParameters(ByteString.CopyFrom(argBytes[0]))
                .AddParameters(ByteString.CopyFrom(argBytes[1]))
                .AddParameters(ByteString.CopyFrom(argBytes[2]))
                .Build();
            // Run the request
            services.HandleRequest(request);
            mock.Verify (x => x.ProcedureThreeArgsNoReturn (
                It.IsAny<KRPC.Schema.KRPC.Response>(),
                It.IsAny<KRPC.Schema.KRPC.Request>(),
                It.IsAny<KRPC.Schema.KRPC.Response>()), Times.Once ());
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
            mock.Setup (x => x.ProcedureNoArgsReturns ())
                .Returns(expectedResponse);
            TestService.service = mock.Object;
            // Create request
            var request = Request.CreateBuilder()
                .SetService ("TestService")
                .SetProcedure ("ProcedureNoArgsReturns")
                .Build();
            // Run the request
            Response.Builder responseBuilder = services.HandleRequest(request);
            responseBuilder.SetTime (42);
            Response response = responseBuilder.Build ();
            mock.Verify (x => x.ProcedureNoArgsReturns (), Times.Once ());
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
            mock.Setup (x => x.ProcedureSingleArgReturns (It.IsAny<Response>()))
                .Returns((Response x) => Response.CreateBuilder().MergeFrom(x).Build());
            TestService.service = mock.Object;
            // Create request
            var request = Request.CreateBuilder ()
                .SetService ("TestService")
                .SetProcedure ("ProcedureSingleArgReturns")
                .AddParameters (ByteString.CopyFrom (expectedResponseBytes))
                .Build();
            // Run the request
            Response.Builder responseBuilder = services.HandleRequest(request);
            responseBuilder.Time = 42;
            Response response = responseBuilder.Build ();
            mock.Verify (x => x.ProcedureSingleArgReturns (It.IsAny<Response>()), Times.Once ());
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
                    Assert.AreEqual (5, service.ProceduresCount);
                    foreach (KRPC.Schema.KRPC.Procedure method in service.ProceduresList) {
                        if (method.Name == "ProcedureNoArgsNoReturn") {
                            Assert.AreEqual (0, method.ParameterTypesCount);
                            Assert.IsFalse (method.HasReturnType);
                        }
                        if (method.Name == "ProcedureSingleArgNoReturn") {
                            Assert.AreEqual (1, method.ParameterTypesCount);
                            Assert.AreEqual ("KRPC.Response", method.ParameterTypesList[0]);
                            Assert.IsFalse (method.HasReturnType);
                        }
                        if (method.Name == "ProcedureThreeArgsNoReturn") {
                            Assert.AreEqual (3, method.ParameterTypesCount);
                            Assert.AreEqual ("KRPC.Response", method.ParameterTypesList[0]);
                            Assert.AreEqual ("KRPC.Request", method.ParameterTypesList[1]);
                            Assert.AreEqual ("KRPC.Response", method.ParameterTypesList[2]);
                            Assert.IsFalse (method.HasReturnType);
                        }
                        if (method.Name == "ProcedureNoArgsReturns") {
                            Assert.AreEqual (0, method.ParameterTypesCount);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("KRPC.Response", method.ReturnType);
                        }
                        if (method.Name == "ProcedureSingleArgReturns") {
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

