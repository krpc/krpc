using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Moq;
using Google.ProtocolBuffers;
using KRPC.Schema.RPC;
using KRPC.Service;

namespace KRPCTest.Service
{
    [TestFixture]
    public class ServicesTest
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        [Test]
        public void NonExistantNamespace ()
        {
            var request = Request.CreateBuilder()
                .SetService ("NonExistantService")
                .SetMethod ("NonExistantMethod")
                .BuildPartial();
            Assert.Throws<NoSuchServiceException>(
                () => { KRPC.Service.Services.HandleRequest (assembly, "NonExistantNamespace", request); });
        }

        [Test]
        public void NonExistantService ()
        {
            var request = Request.CreateBuilder()
                .SetService ("NonExistantService")
                .SetMethod ("NonExistantMethod")
                .BuildPartial();
            Assert.Throws<NoSuchServiceException>(
                () => { KRPC.Service.Services.HandleRequest (assembly, "KRPC.Service", request); });
        }

        [Test]
        public void NonExistantMethod ()
        {
            var request = Request.CreateBuilder()
                .SetService ("TestService")
                .SetMethod ("NonExistantMethod")
                .BuildPartial();
            Assert.Throws<NoSuchServiceMethodException> (
                () => {    KRPC.Service.Services.HandleRequest (assembly, "KRPCTest.Service", request); });
        }

        [Test]
        public void MethodWithoutAttribute ()
        {
            var request = Request.CreateBuilder()
                .SetService ("TestService")
                .SetMethod ("MethodWithoutAttribute")
                .BuildPartial();
            Assert.Throws<NoSuchServiceMethodException>(
                () => { KRPC.Service.Services.HandleRequest (assembly, "KRPCTest.Service", request); });
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
                .BuildPartial();
            // Run the request
            KRPC.Service.Services.HandleRequest(assembly, "KRPCTest.Service", request);
            mock.Verify (x => x.MethodNoArgsNoReturn (), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method with an argument and no return value
        /// </summary>
        [Test]
        public void HandleRequestArgsNoReturn () {
            // Create argument
            var arg = KRPC.Schema.RPC.Response.CreateBuilder ()
                .SetErrorMessage ("foo").BuildPartial ();
            byte[] argBytes;
            using (MemoryStream stream = new MemoryStream()) {
                arg.WriteTo (stream);
                argBytes = stream.ToArray ();
            }
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.MethodArgsNoReturn (It.IsAny<ByteString>()))
                .Callback((ByteString x) => {
                    // Check the argument
                    Assert.AreEqual (argBytes, x.ToByteArray());
                } );
            TestService.service = mock.Object;
            // Create request
            var request = Request.CreateBuilder()
                .SetService ("TestService")
                .SetMethod ("MethodArgsNoReturn")
                .SetRequest_(ByteString.CopyFrom(argBytes))
                .BuildPartial();
            // Run the request
            KRPC.Service.Services.HandleRequest(assembly, "KRPCTest.Service", request);
            mock.Verify (x => x.MethodArgsNoReturn (It.IsAny<ByteString>()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method with an argument and no return value
        /// </summary>
        [Test]
        public void HandleRequestNoArgsReturn () {
            // Create response
            var expectedResponse = KRPC.Schema.RPC.Response.CreateBuilder ()
                .SetErrorMessage ("foo")
                .BuildPartial ();
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.MethodNoArgsReturns ())
                .Returns(expectedResponse);
            TestService.service = mock.Object;
            // Create request
            var request = Request.CreateBuilder()
                .SetService ("TestService")
                .SetMethod ("MethodNoArgsReturns")
                .BuildPartial();
            // Run the request
            Response response = KRPC.Service.Services.HandleRequest(assembly, "KRPCTest.Service", request)
                .BuildPartial();
            mock.Verify (x => x.MethodNoArgsReturns (), Times.Once ());
            // Check the return value
            Response innerResponse = Response.CreateBuilder ().MergeFrom (response.Response_).BuildPartial ();
            Assert.AreEqual (expectedResponse.ErrorMessage, innerResponse.ErrorMessage);
        }

        /// <summary>
        /// Test calling a service method with an argument and return value
        /// </summary>
        [Test]
        public void HandleRequestArgsReturn () {
            // Create resonse
            var expectedResponse = KRPC.Schema.RPC.Response.CreateBuilder ()
                .SetErrorMessage ("bar")
                .BuildPartial ();
            byte[] expectedResponseBytes;
            using (MemoryStream stream = new MemoryStream()) {
                expectedResponse.WriteTo (stream);
                expectedResponseBytes = stream.ToArray ();
            }
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.MethodArgsReturns (It.IsAny<ByteString>()))
                .Returns((ByteString x) => Request.CreateBuilder().MergeFrom(x).BuildPartial());
            TestService.service = mock.Object;
            // Create request
            var request = Request.CreateBuilder()
                .SetService ("TestService")
                .SetMethod ("MethodArgsReturns")
                .SetRequest_(ByteString.CopyFrom(expectedResponseBytes))
                .BuildPartial();
            // Run the request
            Response response = KRPC.Service.Services.HandleRequest(assembly, "KRPCTest.Service", request)
                .BuildPartial();
            mock.Verify (x => x.MethodArgsReturns (It.IsAny<ByteString>()), Times.Once ());
            // Check the return value
            Response innerResponse = Response.CreateBuilder ().MergeFrom (response.Response_).BuildPartial ();
            Assert.AreEqual (expectedResponse.ErrorMessage, innerResponse.ErrorMessage);
        }
    }
}

