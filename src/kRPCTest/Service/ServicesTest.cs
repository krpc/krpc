using System;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using Moq;
using Google.ProtocolBuffers;
using KRPC.Schema.KRPC;
using KRPC.Service;
using KRPC.Utils;

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
            const float expectedX = 3.14159f;
            const string expectedY = "foo";
            var expectedZ = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
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

        /// <summary>
        /// Test calling the getter for a property
        /// </summary>
        [Test]
        public void HandleRequestForPropertyGetter ()
        {
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.PropertyWithGet).Returns ("foo");
            TestService.Service = mock.Object;
            // Create request
            var request = Request.CreateBuilder ()
                .SetService ("TestService")
                .SetProcedure ("get_PropertyWithGet")
                .Build ();
            // Run the request
            Response.Builder response = KRPC.Service.Services.Instance.HandleRequest (request);
            Assert.AreEqual ("foo", ProtocolBuffers.ReadValue (response.ReturnValue, typeof(string)));
            mock.Verify (x => x.PropertyWithGet, Times.Once ());
        }

        /// <summary>
        /// Test calling the setter for a property
        /// </summary>
        [Test]
        public void HandleRequestForPropertySetter ()
        {
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.SetupSet (x => x.PropertyWithSet = "foo");
            TestService.Service = mock.Object;
            // Create request
            var request = Request.CreateBuilder ()
                .SetService ("TestService")
                .SetProcedure ("set_PropertyWithSet")
                .AddParameters (ProtocolBuffers.WriteValue ("foo", typeof(string)))
                .Build ();
            // Run the request
            Response.Builder response = KRPC.Service.Services.Instance.HandleRequest (request);
            Assert.False (response.HasError);
        }

        /// <summary>
        /// Test calling a procedure that returns a proxy object
        /// </summary>
        [Test]
        public void HandleRequestWithObjectReturn ()
        {
            var instance = new TestService.TestClass ("foo");
            var guid = ObjectStore.AddInstance (instance);
            var argBytes = ProtocolBuffers.WriteValue ("foo", typeof(string));
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.CreateTestObject ("foo")).Returns (instance);
            TestService.Service = mock.Object;
            // Create request
            var request = Request.CreateBuilder ()
                .SetService ("TestService")
                .SetProcedure ("CreateTestObject")
                .AddParameters (argBytes)
                .Build ();
            // Run the request
            Response.Builder response = KRPC.Service.Services.Instance.HandleRequest (request);
            Assert.False (response.HasError);
            response.Time = 42;
            Response builtResponse = response.Build ();
            Assert.True (builtResponse.HasReturnValue);
            Assert.AreEqual (guid, ProtocolBuffers.ReadValue (builtResponse.ReturnValue, typeof(ulong)));
        }

        /// <summary>
        /// Test calling a procedure that takes a proxy object as a parameter
        /// </summary>
        [Test]
        public void HandleRequestWithObjectParameter ()
        {
            // Create argument
            var instance = new TestService.TestClass ("foo");
            var arg = ObjectStore.AddInstance (instance);
            ByteString argBytes = ProtocolBuffers.WriteValue (arg, typeof(ulong));
            // Create mock service
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.DeleteTestObject (It.IsAny<TestService.TestClass> ()))
                .Callback ((TestService.TestClass x) => {
                // Check the argument
                Assert.AreSame (instance, x);
            });
            TestService.Service = mock.Object;
            // Create request
            var request = Request.CreateBuilder ()
                .SetService ("TestService")
                .SetProcedure ("DeleteTestObject")
                .AddParameters (argBytes)
                .Build ();
            // Run the request
            KRPC.Service.Services.Instance.HandleRequest (request);
            mock.Verify (x => x.DeleteTestObject (It.IsAny<TestService.TestClass> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling the method of a proxy object
        /// </summary>
        [Test]
        public void HandleRequestForObjectMethod ()
        {
            // Create argument
            var instance = new TestService.TestClass ("jeb");
            var guid = ObjectStore.AddInstance (instance);
            ByteString guidBytes = ProtocolBuffers.WriteValue (guid, typeof(ulong));
            ByteString argBytes = ProtocolBuffers.WriteValue (3.14159f, typeof(float));
            // Create request
            var request = Request.CreateBuilder ()
                .SetService ("TestService")
                .SetProcedure ("TestClass_FloatToString")
                .AddParameters (guidBytes)
                .AddParameters (argBytes)
                .Build ();
            // Run the request
            Response.Builder response = KRPC.Service.Services.Instance.HandleRequest (request);
            response.Time = 42;
            var builtResponse = response.Build ();
            Assert.AreEqual ("jeb3.14159", ProtocolBuffers.ReadValue (builtResponse.ReturnValue, typeof(string)));
        }

        /// <summary>
        /// Test calling the method of a proxy object, and pass a proxy object as a parameter
        /// </summary>
        [Test]
        public void HandleRequestForObjectMethodWithObjectParameter ()
        {
            // Create argument
            var instance = new TestService.TestClass ("bill");
            var argInstance = new TestService.TestClass ("bob");
            var guid = ObjectStore.AddInstance (instance);
            var argGuid = ObjectStore.AddInstance (argInstance);
            ByteString guidBytes = ProtocolBuffers.WriteValue (guid, typeof(ulong));
            ByteString argBytes = ProtocolBuffers.WriteValue (argGuid, typeof(ulong));
            // Create request
            var request = Request.CreateBuilder ()
                .SetService ("TestService")
                .SetProcedure ("TestClass_ObjectToString")
                .AddParameters (guidBytes)
                .AddParameters (argBytes)
                .Build ();
            // Run the request
            Response.Builder response = KRPC.Service.Services.Instance.HandleRequest (request);
            response.Time = 42;
            var builtResponse = response.Build ();
            Assert.AreEqual ("billbob", ProtocolBuffers.ReadValue (builtResponse.ReturnValue, typeof(string)));
        }

        /// <summary>
        /// Test the getting a property value in a proxy object
        /// </summary>
        [Test]
        public void HandleRequestForClassPropertyGetter ()
        {
            var instance = new TestService.TestClass ("jeb");
            instance.IntProperty = 42;
            var guid = ObjectStore.AddInstance (instance);
            ByteString guidBytes = ProtocolBuffers.WriteValue (guid, typeof(ulong));
            // Create request
            var request = Request.CreateBuilder ()
                .SetService ("TestService")
                .SetProcedure ("TestClass_get_IntProperty")
                .AddParameters (guidBytes)
                .Build ();
            // Run the request
            Response.Builder response = KRPC.Service.Services.Instance.HandleRequest (request);
            response.Time = 0;
            var builtResponse = response.Build ();
            Assert.IsFalse (builtResponse.HasError);
            Assert.AreEqual (42, ProtocolBuffers.ReadValue (builtResponse.ReturnValue, typeof(int)));
        }

        /// <summary>
        /// Test setting a property value in a proxy object
        /// </summary>
        [Test]
        public void HandleRequestForClassPropertySetter ()
        {
            var instance = new TestService.TestClass ("jeb");
            instance.IntProperty = 42;
            var guid = ObjectStore.AddInstance (instance);
            ByteString guidBytes = ProtocolBuffers.WriteValue (guid, typeof(ulong));
            // Create request
            var request = Request.CreateBuilder ()
                .SetService ("TestService")
                .SetProcedure ("TestClass_set_IntProperty")
                .AddParameters (guidBytes)
                .AddParameters (ProtocolBuffers.WriteValue (1337, typeof(int)))
                .Build ();
            // Run the request
            Response.Builder response = KRPC.Service.Services.Instance.HandleRequest (request);
            response.Time = 0;
            var builtResponse = response.Build ();
            Assert.IsFalse (builtResponse.HasError);
            Assert.AreEqual (1337, instance.IntProperty);
        }

        /// <summary>
        /// Check the output of the service scanner
        /// </summary>
        [Test]
        public void GetServices ()
        {
            var services = KRPC.Service.KRPC.GetServices ();
            Assert.IsNotNull (services);
            Assert.AreEqual (2, services.Services_Count);
            foreach (KRPC.Schema.KRPC.Service service in services.Services_List) {
                if (service.Name == "TestService") {
                    Assert.AreEqual (18, service.ProceduresCount);
                    int found = 0;
                    foreach (var method in service.ProceduresList) {
                        if (method.Name == "ProcedureNoArgsNoReturn") {
                            Assert.AreEqual (0, method.ParameterTypesCount);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            found++;
                        }
                        if (method.Name == "ProcedureSingleArgNoReturn") {
                            Assert.AreEqual (1, method.ParameterTypesCount);
                            Assert.AreEqual ("KRPC.Response", method.ParameterTypesList [0]);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            found++;
                        }
                        if (method.Name == "ProcedureThreeArgsNoReturn") {
                            Assert.AreEqual (3, method.ParameterTypesCount);
                            Assert.AreEqual ("KRPC.Response", method.ParameterTypesList [0]);
                            Assert.AreEqual ("KRPC.Request", method.ParameterTypesList [1]);
                            Assert.AreEqual ("KRPC.Response", method.ParameterTypesList [2]);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            found++;
                        }
                        if (method.Name == "ProcedureNoArgsReturns") {
                            Assert.AreEqual (0, method.ParameterTypesCount);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("KRPC.Response", method.ReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            found++;
                        }
                        if (method.Name == "ProcedureSingleArgReturns") {
                            Assert.AreEqual (1, method.ParameterTypesCount);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("KRPC.Response", method.ParameterTypesList [0]);
                            Assert.AreEqual ("KRPC.Response", method.ReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            found++;
                        }
                        if (method.Name == "ProcedureWithValueTypes") {
                            Assert.AreEqual (3, method.ParameterTypesCount);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("float", method.ParameterTypesList [0]);
                            Assert.AreEqual ("string", method.ParameterTypesList [1]);
                            Assert.AreEqual ("bytes", method.ParameterTypesList [2]);
                            Assert.AreEqual ("int32", method.ReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            found++;
                        }
                        if (method.Name == "get_PropertyWithGetAndSet") {
                            Assert.AreEqual (0, method.ParameterTypesCount);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("string", method.ReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("Property.Get(PropertyWithGetAndSet)", method.AttributesList [0]);
                            found++;
                        }
                        if (method.Name == "set_PropertyWithGetAndSet") {
                            Assert.AreEqual (1, method.ParameterTypesCount);
                            Assert.AreEqual ("string", method.ParameterTypesList [0]);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("Property.Set(PropertyWithGetAndSet)", method.AttributesList [0]);
                            found++;
                        }
                        if (method.Name == "get_PropertyWithGet") {
                            Assert.AreEqual (0, method.ParameterTypesCount);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("string", method.ReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("Property.Get(PropertyWithGet)", method.AttributesList [0]);
                            found++;
                        }
                        if (method.Name == "set_PropertyWithSet") {
                            Assert.AreEqual (1, method.ParameterTypesCount);
                            Assert.AreEqual ("string", method.ParameterTypesList [0]);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("Property.Set(PropertyWithSet)", method.AttributesList [0]);
                            found++;
                        }
                        if (method.Name == "CreateTestObject") {
                            Assert.AreEqual (1, method.ParameterTypesCount);
                            Assert.AreEqual ("string", method.ParameterTypesList [0]);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("uint64", method.ReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("ReturnType.Class(TestClass)", method.AttributesList [0]);
                            found++;
                        }
                        if (method.Name == "DeleteTestObject") {
                            Assert.AreEqual (1, method.ParameterTypesCount);
                            Assert.AreEqual ("uint64", method.ParameterTypesList [0]);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("ParameterType(0).Class(TestClass)", method.AttributesList [0]);
                            found++;
                        }
                        if (method.Name == "TestClass_FloatToString") {
                            Assert.AreEqual (2, method.ParameterTypesCount);
                            Assert.AreEqual ("uint64", method.ParameterTypesList [0]);
                            Assert.AreEqual ("float", method.ParameterTypesList [1]);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("string", method.ReturnType);
                            Assert.AreEqual (2, method.AttributesCount);
                            Assert.AreEqual ("Class.Method(FloatToString)", method.AttributesList [0]);
                            Assert.AreEqual ("ParameterType(0).Class(TestClass)", method.AttributesList [1]);
                            found++;
                        }
                        if (method.Name == "TestClass_ObjectToString") {
                            Assert.AreEqual (2, method.ParameterTypesCount);
                            Assert.AreEqual ("uint64", method.ParameterTypesList [0]);
                            Assert.AreEqual ("uint64", method.ParameterTypesList [1]);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("string", method.ReturnType);
                            Assert.AreEqual (3, method.AttributesCount);
                            Assert.AreEqual ("Class.Method(ObjectToString)", method.AttributesList [0]);
                            Assert.AreEqual ("ParameterType(0).Class(TestClass)", method.AttributesList [1]);
                            Assert.AreEqual ("ParameterType(1).Class(TestClass)", method.AttributesList [2]);
                            found++;
                        }
                        if (method.Name == "TestClass_get_IntProperty") {
                            Assert.AreEqual (1, method.ParameterTypesCount);
                            Assert.AreEqual ("uint64", method.ParameterTypesList [0]);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("int32", method.ReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("Class.Property.Get(TestClass,IntProperty)", method.AttributesList [0]);
                            found++;
                        }
                        if (method.Name == "TestClass_set_IntProperty") {
                            Assert.AreEqual (2, method.ParameterTypesCount);
                            Assert.AreEqual ("uint64", method.ParameterTypesList [0]);
                            Assert.AreEqual ("int32", method.ParameterTypesList [1]);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("Class.Property.Set(TestClass,IntProperty)", method.AttributesList [0]);
                            found++;
                        }
                        if (method.Name == "TestClass_get_ObjectProperty") {
                            Assert.AreEqual (1, method.ParameterTypesCount);
                            Assert.AreEqual ("uint64", method.ParameterTypesList [0]);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("uint64", method.ReturnType);
                            Assert.AreEqual (2, method.AttributesCount);
                            Assert.AreEqual ("Class.Property.Get(TestClass,ObjectProperty)", method.AttributesList [0]);
                            Assert.AreEqual ("ReturnType.Class(TestClass)", method.AttributesList [1]);
                            found++;
                        }
                        if (method.Name == "TestClass_set_ObjectProperty") {
                            Assert.AreEqual (2, method.ParameterTypesCount);
                            Assert.AreEqual ("uint64", method.ParameterTypesList [0]);
                            Assert.AreEqual ("uint64", method.ParameterTypesList [1]);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (2, method.AttributesCount);
                            Assert.AreEqual ("Class.Property.Set(TestClass,ObjectProperty)", method.AttributesList [0]);
                            Assert.AreEqual ("ParameterType(1).Class(TestClass)", method.AttributesList [1]);
                            found++;
                        }
                    }
                    Assert.AreEqual (18, found);
                }
            }
        }

        [Test]
        public void IsAValidType ()
        {
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(string)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(long)));
            Assert.IsTrue (TypeUtils.IsAValidType (typeof(TestService.TestClass)));
            Assert.IsFalse (TypeUtils.IsAValidType (typeof(TestService)));
        }

        [Test]
        public void IsAClassType ()
        {
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(string)));
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(long)));
            Assert.IsTrue (TypeUtils.IsAClassType (typeof(TestService.TestClass)));
            Assert.IsFalse (TypeUtils.IsAClassType (typeof(TestService)));
        }

        [Test]
        public void GetTypeName ()
        {
            Assert.AreEqual ("string", TypeUtils.GetTypeName (typeof(string)));
            Assert.AreEqual ("int64", TypeUtils.GetTypeName (typeof(long)));
            Assert.AreEqual ("uint64", TypeUtils.GetTypeName (typeof(TestService.TestClass)));
            Assert.Throws<ArgumentException> (() => TypeUtils.GetTypeName (typeof(TestService)));
        }

        [Test]
        public void ParameterTypeAttributes ()
        {
            Assert.AreEqual (new string[]{ }, TypeUtils.ParameterTypeAttributes (0, typeof(string)));
            Assert.AreEqual (new string[]{ }, TypeUtils.ParameterTypeAttributes (3, typeof(long)));
            Assert.AreEqual (new []{ "ParameterType(1).Class(TestClass)" }, TypeUtils.ParameterTypeAttributes (1, typeof(TestService.TestClass)));
            Assert.Throws<ArgumentException> (() => TypeUtils.ParameterTypeAttributes (0, typeof(TestService)));
        }

        [Test]
        public void ReturnTypeAttributes ()
        {
            Assert.AreEqual (new string[]{ }, TypeUtils.ReturnTypeAttributes (typeof(string)));
            Assert.AreEqual (new string[]{ }, TypeUtils.ReturnTypeAttributes (typeof(long)));
            Assert.AreEqual (new []{ "ReturnType.Class(TestClass)" }, TypeUtils.ReturnTypeAttributes (typeof(TestService.TestClass)));
            Assert.Throws<ArgumentException> (() => TypeUtils.ReturnTypeAttributes (typeof(TestService)));
        }
    }
}

