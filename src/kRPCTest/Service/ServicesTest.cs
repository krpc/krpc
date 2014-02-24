using System;
using System.IO;
using System.Collections.Generic;
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
            var request = Request.CreateBuilder ()
                .SetService ("NonExistantService")
                .SetProcedure ("NonExistantProcedure")
                .Build ();
            Assert.Throws<RPCException> (() => KRPC.Service.Services.Instance.HandleRequest (request));
        }

        [Test]
        public void NonExistantProcedure ()
        {
            var request = Request.CreateBuilder ()
                .SetService ("TestService")
                .SetProcedure ("NonExistantProcedure")
                .Build ();
            Assert.Throws<RPCException> (() => KRPC.Service.Services.Instance.HandleRequest (request));
        }

        [Test]
        public void ProcedureWithoutAttribute ()
        {
            var request = Request.CreateBuilder ()
                .SetService ("TestService")
                .SetProcedure ("ProcedureWithoutAttribute")
                .Build ();
            Assert.Throws<RPCException> (() => KRPC.Service.Services.Instance.HandleRequest (request));
        }

        /// <summary>
        /// Test service method with no argument and no return value
        /// </summary>
        [Test]
        public void HandleRequestNoArgsNoReturn ()
        {
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureNoArgsNoReturn ());
            TestService.Service = mock.Object;
            // Create request
            var request = Request.CreateBuilder ()
                .SetService ("TestService")
                .SetProcedure ("ProcedureNoArgsNoReturn")
                .Build ();
            // Run the request
            KRPC.Service.Services.Instance.HandleRequest (request);
            mock.Verify (x => x.ProcedureNoArgsNoReturn (), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method with a malformed argument
        /// </summary>
        [Test]
        public void HandleRequestSingleMalformedArgNoReturn ()
        {
            // Create argument
            var arg = Response.CreateBuilder ()
                .SetError ("foo").SetTime (42).Build ();
            byte[] argBytes;
            using (var stream = new MemoryStream ()) {
                arg.WriteTo (stream);
                argBytes = stream.ToArray ();
            }
            // Screw it up!
            for (int i = 0; i < argBytes.Length; i++)
                argBytes [i] = (byte)(argBytes [i] + 1);
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureSingleArgNoReturn (It.IsAny<Response> ()))
                .Callback ((Response x) => {
                // Check the argument
                Assert.AreEqual (argBytes, x.ToByteArray ());
            });
            TestService.Service = mock.Object;
            // Create request
            var request = Request.CreateBuilder ()
                .SetService ("TestService")
                .SetProcedure ("ProcedureSingleArgNoReturn")
                .AddParameters (ByteString.CopyFrom (argBytes))
                .Build ();
            // Run the request
            Assert.Throws<RPCException> (() => KRPC.Service.Services.Instance.HandleRequest (request));
            mock.Verify (x => x.ProcedureSingleArgNoReturn (It.IsAny<Response> ()), Times.Never ());
        }

        /// <summary>
        /// Test calling a service method that returns null
        /// </summary>
        [Test]
        public void HandleRequestNoArgsReturnsNull ()
        {
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureNoArgsReturns ()).Returns ((Response)null);
            TestService.Service = mock.Object;
            // Create request
            var request = Request.CreateBuilder ()
                .SetService ("TestService")
                .SetProcedure ("ProcedureNoArgsReturns")
                .Build ();
            // Run the request
            Assert.Throws<RPCException> (() => KRPC.Service.Services.Instance.HandleRequest (request));
            mock.Verify (x => x.ProcedureNoArgsReturns (), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method that throws an exception
        /// </summary>
        [Test]
        public void HandleRequestNoArgsThrows ()
        {
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureNoArgsReturns ()).Throws (new ArgumentException ());
            TestService.Service = mock.Object;
            // Create request
            var request = Request.CreateBuilder ()
                .SetService ("TestService")
                .SetProcedure ("ProcedureNoArgsReturns")
                .Build ();
            // Run the request
            Assert.Throws<RPCException> (() => KRPC.Service.Services.Instance.HandleRequest (request));
            mock.Verify (x => x.ProcedureNoArgsReturns (), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method with an argument and no return value
        /// </summary>
        [Test]
        public void HandleRequestSingleArgNoReturn ()
        {
            // Create argument
            var arg = Response.CreateBuilder ()
                .SetError ("foo").SetTime (42).Build ();
            byte[] argBytes;
            using (var stream = new MemoryStream ()) {
                arg.WriteTo (stream);
                argBytes = stream.ToArray ();
            }
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureSingleArgNoReturn (It.IsAny<Response> ()))
                .Callback ((Response x) => {
                // Check the argument
                Assert.AreEqual (argBytes, x.ToByteArray ());
            });
            TestService.Service = mock.Object;
            // Create request
            var request = Request.CreateBuilder ()
                .SetService ("TestService")
                .SetProcedure ("ProcedureSingleArgNoReturn")
                .AddParameters (ByteString.CopyFrom (argBytes))
                .Build ();
            // Run the request
            KRPC.Service.Services.Instance.HandleRequest (request);
            mock.Verify (x => x.ProcedureSingleArgNoReturn (It.IsAny<Response> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method with multiple parameters and no return
        /// </summary>
        [Test]
        public void HandleRequestThreeArgsNoReturn ()
        {
            // Create arguments
            var args = new IMessage [3];
            args [0] = Response.CreateBuilder ()
                .SetError ("foo").SetTime (42).Build ();
            args [1] = Request.CreateBuilder ()
                .SetService ("bar").SetProcedure ("bar").Build ();
            args [2] = Response.CreateBuilder ()
                .SetError ("baz").SetTime (123).Build ();
            var argBytes = new List<byte[]> ();
            for (int i = 0; i < 3; i++) {
                var stream = new MemoryStream ();
                args [i].WriteTo (stream);
                argBytes.Add (stream.ToArray ());
            }
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureThreeArgsNoReturn (
                It.IsAny<Response> (),
                It.IsAny<Request> (),
                It.IsAny<Response> ()))
                .Callback ((Response x,
                            Request y,
                            Response z) => {
                // Check the argument
                Assert.AreEqual (argBytes [0], x.ToByteArray ());
                Assert.AreEqual (argBytes [1], y.ToByteArray ());
                Assert.AreEqual (argBytes [2], z.ToByteArray ());
            });
            TestService.Service = mock.Object;
            // Create request
            var request = Request.CreateBuilder ()
                .SetService ("TestService")
                .SetProcedure ("ProcedureThreeArgsNoReturn")
                .AddParameters (ByteString.CopyFrom (argBytes [0]))
                .AddParameters (ByteString.CopyFrom (argBytes [1]))
                .AddParameters (ByteString.CopyFrom (argBytes [2]))
                .Build ();
            // Run the request
            KRPC.Service.Services.Instance.HandleRequest (request);
            mock.Verify (x => x.ProcedureThreeArgsNoReturn (
                It.IsAny<Response> (),
                It.IsAny<Request> (),
                It.IsAny<Response> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method with an argument and no return value
        /// </summary>
        [Test]
        public void HandleRequestNoArgsReturn ()
        {
            // Create response
            var expectedResponse = Response.CreateBuilder ()
                .SetError ("foo").SetTime (42).Build ();
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureNoArgsReturns ())
                .Returns (expectedResponse);
            TestService.Service = mock.Object;
            // Create request
            var request = Request.CreateBuilder ()
                .SetService ("TestService")
                .SetProcedure ("ProcedureNoArgsReturns")
                .Build ();
            // Run the request
            Response.Builder responseBuilder = KRPC.Service.Services.Instance.HandleRequest (request);
            responseBuilder.SetTime (42);
            Response response = responseBuilder.Build ();
            mock.Verify (x => x.ProcedureNoArgsReturns (), Times.Once ());
            // Check the return value
            Response innerResponse = Response.CreateBuilder ().MergeFrom (response.ReturnValue).Build ();
            Assert.AreEqual (expectedResponse.Error, innerResponse.Error);
        }

        /// <summary>
        /// Test calling a service method with an argument and return value
        /// </summary>
        [Test]
        public void HandleRequestArgsReturn ()
        {
            // Create resonse
            var expectedResponse = Response.CreateBuilder ()
                .SetTime (42).SetError ("bar").Build ();
            byte[] expectedResponseBytes;
            using (var stream = new MemoryStream ()) {
                expectedResponse.WriteTo (stream);
                expectedResponseBytes = stream.ToArray ();
            }
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureSingleArgReturns (It.IsAny<Response> ()))
                .Returns ((Response x) => Response.CreateBuilder ().MergeFrom (x).Build ());
            TestService.Service = mock.Object;
            // Create request
            var request = Request.CreateBuilder ()
                .SetService ("TestService")
                .SetProcedure ("ProcedureSingleArgReturns")
                .AddParameters (ByteString.CopyFrom (expectedResponseBytes))
                .Build ();
            // Run the request
            Response.Builder responseBuilder = KRPC.Service.Services.Instance.HandleRequest (request);
            responseBuilder.Time = 42;
            Response response = responseBuilder.Build ();
            mock.Verify (x => x.ProcedureSingleArgReturns (It.IsAny<Response> ()), Times.Once ());
            // Check the return value
            Response innerResponse = Response.CreateBuilder ().MergeFrom (response.ReturnValue).Build ();
            Assert.AreEqual (expectedResponse.Error, innerResponse.Error);
        }

        /// <summary>
        /// Test calling a service method with value types for parameters
        /// </summary>
        [Test]
        public void HandleRequestWithValueTypes ()
        {
            // Create arguments
            float expectedX = 3.14159f;
            string expectedY = "foo";
            byte[] expectedZ = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
            byte[] xBytes, yBytes, zBytes;
            using (var stream = new MemoryStream ()) {
                var codedStream = CodedOutputStream.CreateInstance (stream);
                codedStream.WriteFloatNoTag (expectedX);
                codedStream.Flush ();
                xBytes = stream.ToArray ();
            }
            using (var stream = new MemoryStream ()) {
                var codedStream = CodedOutputStream.CreateInstance (stream);
                codedStream.WriteStringNoTag (expectedY);
                codedStream.Flush ();
                yBytes = stream.ToArray ();
            }
            using (var stream = new MemoryStream ()) {
                var codedStream = CodedOutputStream.CreateInstance (stream);
                codedStream.WriteBytesNoTag (ByteString.CopyFrom (expectedZ));
                codedStream.Flush ();
                zBytes = stream.ToArray ();
            }
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureWithValueTypes (
                It.IsAny<float> (),
                It.IsAny<string> (),
                It.IsAny<byte[]> ()))
                .Callback ((float x, string y, byte[] z) => {
                // Check the argument
                Assert.AreEqual (expectedX, x);
                Assert.AreEqual (expectedY, y);
                Assert.AreEqual (expectedZ, z);
            }).Returns (42);
            TestService.Service = mock.Object;
            // Create request
            var request = Request.CreateBuilder ()
                .SetService ("TestService")
                .SetProcedure ("ProcedureWithValueTypes")
                .AddParameters (ByteString.CopyFrom (xBytes))
                .AddParameters (ByteString.CopyFrom (yBytes))
                .AddParameters (ByteString.CopyFrom (zBytes))
                .Build ();
            // Run the request
            KRPC.Service.Services.Instance.HandleRequest (request);
            mock.Verify (x => x.ProcedureWithValueTypes (
                It.IsAny<float> (), It.IsAny<string> (), It.IsAny<byte[]> ()), Times.Once ());
        }

        [Test]
        public void GetServices ()
        {
            var services = KRPC.Service.KRPC.GetServices ();
            Assert.IsNotNull (services);
            Assert.AreEqual (2, services.Services_Count);
            foreach (KRPC.Schema.KRPC.Service service in services.Services_List) {
                if (service.Name == "TestService") {
                    Assert.AreEqual (6, service.ProceduresCount);
                    foreach (KRPC.Schema.KRPC.Procedure method in service.ProceduresList) {
                        if (method.Name == "ProcedureNoArgsNoReturn") {
                            Assert.AreEqual (0, method.ParameterTypesCount);
                            Assert.IsFalse (method.HasReturnType);
                        }
                        if (method.Name == "ProcedureSingleArgNoReturn") {
                            Assert.AreEqual (1, method.ParameterTypesCount);
                            Assert.AreEqual ("KRPC.Response", method.ParameterTypesList [0]);
                            Assert.IsFalse (method.HasReturnType);
                        }
                        if (method.Name == "ProcedureThreeArgsNoReturn") {
                            Assert.AreEqual (3, method.ParameterTypesCount);
                            Assert.AreEqual ("KRPC.Response", method.ParameterTypesList [0]);
                            Assert.AreEqual ("KRPC.Request", method.ParameterTypesList [1]);
                            Assert.AreEqual ("KRPC.Response", method.ParameterTypesList [2]);
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
                            Assert.AreEqual ("KRPC.Response", method.ParameterTypesList [0]);
                            Assert.AreEqual ("KRPC.Response", method.ReturnType);
                        }
                        if (method.Name == "ProcedureWithValueTypes") {
                            Assert.AreEqual (3, method.ParameterTypesCount);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("float", method.ParameterTypesList [0]);
                            Assert.AreEqual ("string", method.ParameterTypesList [1]);
                            Assert.AreEqual ("bytes", method.ParameterTypesList [2]);
                            Assert.AreEqual ("int32", method.ReturnType);
                        }
                    }
                }
            }
        }
    }
}

