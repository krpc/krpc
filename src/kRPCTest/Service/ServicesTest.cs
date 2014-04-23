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
        Request Req (string service, string procedure, params Argument[] args)
        {
            var request = Request.CreateBuilder ()
                .SetService (service)
                .SetProcedure (procedure);
            foreach (var arg in args)
                request.AddArguments (arg);
            return request.Build ();
        }

        Response Res (string error, int time)
        {
            return Response.CreateBuilder ()
                .SetError (error)
                .SetTime (time)
                .Build ();
        }

        Response Res (ByteString data)
        {
            return Response.CreateBuilder ().MergeFrom (data).Build ();
        }

        Response Res (Response response)
        {
            return Response.CreateBuilder ().MergeFrom (response).Build ();
        }

        Argument Arg (uint position, ByteString value)
        {
            return Argument.CreateBuilder ()
                .SetPosition (position)
                .SetValue (value)
                .Build ();
        }

        Response.Builder Run (Request request)
        {
            return KRPC.Service.Services.Instance.HandleRequest (request);
        }

        byte[] ToBytes<T> (T x) where T : IMessage
        {
            using (var stream = new MemoryStream ()) {
                x.WriteTo (stream);
                return stream.ToArray ();
            }
        }

        byte[] ToBytes (float x)
        {
            using (var stream = new MemoryStream ()) {
                var codedStream = CodedOutputStream.CreateInstance (stream);
                codedStream.WriteFloatNoTag (x);
                codedStream.Flush ();
                return stream.ToArray ();
            }
        }

        byte[] ToBytes (string x)
        {
            using (var stream = new MemoryStream ()) {
                var codedStream = CodedOutputStream.CreateInstance (stream);
                codedStream.WriteStringNoTag (x);
                codedStream.Flush ();
                return stream.ToArray ();
            }
        }

        byte[] ToBytes (byte[] x)
        {
            using (var stream = new MemoryStream ()) {
                var codedStream = CodedOutputStream.CreateInstance (stream);
                codedStream.WriteBytesNoTag (ByteString.CopyFrom (x));
                codedStream.Flush ();
                return stream.ToArray ();
            }
        }

        [Test]
        public void NonExistantService ()
        {
            Assert.Throws<RPCException> (() => Run (Req ("NonExistantService", "NonExistantProcedure")));
        }

        [Test]
        public void NonExistantProcedure ()
        {
            Assert.Throws<RPCException> (() => Run (Req ("TestService", "NonExistantProcedure")));
        }

        [Test]
        public void ProcedureWithoutAttribute ()
        {
            Assert.Throws<RPCException> (() => Run (Req ("TestService", "ProcedureWithoutAttribute")));
        }

        /// <summary>
        /// Test service method with no argument and no return value
        /// </summary>
        [Test]
        public void HandleRequestNoArgsNoReturn ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureNoArgsNoReturn ());
            TestService.Service = mock.Object;
            Run (Req ("TestService", "ProcedureNoArgsNoReturn"));
            mock.Verify (x => x.ProcedureNoArgsNoReturn (), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method with a malformed argument
        /// </summary>
        [Test]
        public void HandleRequestSingleMalformedArgNoReturn ()
        {
            var arg = Res ("foo", 42);
            byte[] argBytes = ToBytes (arg);
            // Screw it up!
            for (int i = 0; i < argBytes.Length; i++)
                argBytes [i] = (byte)(argBytes [i] + 1);
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureSingleArgNoReturn (It.IsAny<Response> ()));
            TestService.Service = mock.Object;
            var request = Req ("TestService", "ProcedureSingleArgNoReturn", Arg (0, ByteString.CopyFrom (argBytes)));
            Assert.Throws<RPCException> (() => Run (request));
            mock.Verify (x => x.ProcedureSingleArgNoReturn (It.IsAny<Response> ()), Times.Never ());
        }

        /// <summary>
        /// Test calling a service method that returns null
        /// </summary>
        [Test]
        public void HandleRequestNoArgsReturnsNull ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureNoArgsReturns ()).Returns ((Response)null);
            TestService.Service = mock.Object;
            Assert.Throws<RPCException> (() => Run (Req ("TestService", "ProcedureNoArgsReturns")));
            mock.Verify (x => x.ProcedureNoArgsReturns (), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method that throws an exception
        /// </summary>
        [Test]
        public void HandleRequestNoArgsThrows ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureNoArgsReturns ()).Throws (new ArgumentException ());
            TestService.Service = mock.Object;
            Assert.Throws<RPCException> (() => Run (Req ("TestService", "ProcedureNoArgsReturns")));
            mock.Verify (x => x.ProcedureNoArgsReturns (), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method with an argument and no return value
        /// </summary>
        [Test]
        public void HandleRequestSingleArgNoReturn ()
        {
            var arg = Res ("foo", 42);
            byte[] argBytes = ToBytes (arg);
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureSingleArgNoReturn (It.IsAny<Response> ()))
                .Callback ((Response x) => {
                Assert.AreEqual (argBytes, x.ToByteArray ());
            });
            TestService.Service = mock.Object;
            Run (Req ("TestService", "ProcedureSingleArgNoReturn", Arg (0, ByteString.CopyFrom (argBytes))));
            mock.Verify (x => x.ProcedureSingleArgNoReturn (It.IsAny<Response> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method with multiple parameters and no return
        /// </summary>
        [Test]
        public void HandleRequestThreeArgsNoReturn ()
        {
            var args = new IMessage [3];
            args [0] = Res ("foo", 42);
            args [1] = Req ("bar", "bar");
            args [2] = Res ("baz", 123);
            var argBytes = new List<byte[]> ();
            argBytes.Add (ToBytes (args[0]));
            argBytes.Add (ToBytes (args[1]));
            argBytes.Add (ToBytes (args[2]));
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureThreeArgsNoReturn (
                It.IsAny<Response> (),
                It.IsAny<Request> (),
                It.IsAny<Response> ()))
                .Callback ((Response x,
                            Request y,
                            Response z) => {
                Assert.AreEqual (argBytes [0], x.ToByteArray ());
                Assert.AreEqual (argBytes [1], y.ToByteArray ());
                Assert.AreEqual (argBytes [2], z.ToByteArray ());
            });
            TestService.Service = mock.Object;
            Run (Req ("TestService", "ProcedureThreeArgsNoReturn",
                Arg (0, ByteString.CopyFrom (argBytes [0])),
                Arg (1, ByteString.CopyFrom (argBytes [1])),
                Arg (2, ByteString.CopyFrom (argBytes [2]))));
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
            var expectedResponse = Res ("foo", 42);
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureNoArgsReturns ()).Returns (expectedResponse);
            TestService.Service = mock.Object;
            var responseBuilder = Run (Req ("TestService", "ProcedureNoArgsReturns"));
            var response = responseBuilder.SetTime (42).Build();
            mock.Verify (x => x.ProcedureNoArgsReturns (), Times.Once ());
            Response innerResponse = Res (response.ReturnValue);
            Assert.AreEqual (expectedResponse.Error, innerResponse.Error);
        }

        /// <summary>
        /// Test calling a service method with an argument and return value
        /// </summary>
        [Test]
        public void HandleRequestArgsReturn ()
        {
            var expectedResponse = Res ("bar", 42);
            byte[] expectedResponseBytes = ToBytes (expectedResponse);
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureSingleArgReturns (It.IsAny<Response> ()))
                .Returns ((Response x) => Res(x));
            TestService.Service = mock.Object;
            var request = Req ("TestService", "ProcedureSingleArgReturns",
                Arg (0, ByteString.CopyFrom (expectedResponseBytes)));
            Response.Builder responseBuilder = Run (request);
            Response response = responseBuilder.SetTime (42).Build ();
            mock.Verify (x => x.ProcedureSingleArgReturns (It.IsAny<Response> ()), Times.Once ());
            Response innerResponse = Res (response.ReturnValue);
            Assert.AreEqual (expectedResponse.Error, innerResponse.Error);
        }

        /// <summary>
        /// Test calling a service method with value types for parameters
        /// </summary>
        [Test]
        public void HandleRequestWithValueTypes ()
        {
            const float expectedX = 3.14159f;
            const string expectedY = "foo";
            var expectedZ = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
            byte[] xBytes = ToBytes (expectedX);
            byte[] yBytes = ToBytes (expectedY);
            byte[] zBytes = ToBytes (expectedZ);
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureWithValueTypes (
                It.IsAny<float> (),
                It.IsAny<string> (),
                It.IsAny<byte[]> ()))
                .Callback ((float x, string y, byte[] z) => {
                Assert.AreEqual (expectedX, x);
                Assert.AreEqual (expectedY, y);
                Assert.AreEqual (expectedZ, z);
            }).Returns (42);
            TestService.Service = mock.Object;
            Run (Req ("TestService", "ProcedureWithValueTypes",
                Arg (0, ByteString.CopyFrom (xBytes)),
                Arg (1, ByteString.CopyFrom (yBytes)),
                Arg (2, ByteString.CopyFrom (zBytes))));
            mock.Verify (x => x.ProcedureWithValueTypes (
                It.IsAny<float> (), It.IsAny<string> (), It.IsAny<byte[]> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling the getter for a property
        /// </summary>
        [Test]
        public void HandleRequestForPropertyGetter ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.PropertyWithGet).Returns ("foo");
            TestService.Service = mock.Object;
            Response.Builder response = Run (Req ("TestService", "get_PropertyWithGet"));
            Assert.AreEqual ("foo", ProtocolBuffers.ReadValue (response.ReturnValue, typeof(string)));
            mock.Verify (x => x.PropertyWithGet, Times.Once ());
        }

        /// <summary>
        /// Test calling the setter for a property
        /// </summary>
        [Test]
        public void HandleRequestForPropertySetter ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.SetupSet (x => x.PropertyWithSet = "foo");
            TestService.Service = mock.Object;
            var request = Req ("TestService", "set_PropertyWithSet",
                Arg (0, ProtocolBuffers.WriteValue ("foo", typeof(string))));
            Response.Builder response = Run (request);
            Assert.False (response.HasError);
        }

        /// <summary>
        /// Test calling a procedure that returns a proxy object
        /// </summary>
        [Test]
        public void HandleRequestWithObjectReturn ()
        {
            var instance = new TestService.TestClass ("foo");
            var guid = ObjectStore.Instance.AddInstance (instance);
            var argBytes = ProtocolBuffers.WriteValue ("foo", typeof(string));
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.CreateTestObject ("foo")).Returns (instance);
            TestService.Service = mock.Object;
            Response.Builder response = Run (Req ("TestService", "CreateTestObject", Arg (0, argBytes)));
            Assert.False (response.HasError);
            Response builtResponse = response.SetTime (42).Build ();
            Assert.True (builtResponse.HasReturnValue);
            Assert.AreEqual (guid, ProtocolBuffers.ReadValue (builtResponse.ReturnValue, typeof(ulong)));
        }

        /// <summary>
        /// Test calling a procedure that takes a proxy object as a parameter
        /// </summary>
        [Test]
        public void HandleRequestWithObjectParameter ()
        {
            var instance = new TestService.TestClass ("foo");
            var arg = ObjectStore.Instance.AddInstance (instance);
            ByteString argBytes = ProtocolBuffers.WriteValue (arg, typeof(ulong));
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.DeleteTestObject (It.IsAny<TestService.TestClass> ()))
                .Callback ((TestService.TestClass x) => {
                Assert.AreSame (instance, x);
            });
            TestService.Service = mock.Object;
            Run (Req ("TestService", "DeleteTestObject", Arg (0, argBytes)));
            mock.Verify (x => x.DeleteTestObject (It.IsAny<TestService.TestClass> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a procedure with a null proxy object as a parameter, and a null proxy object return value
        /// </summary>
        [Test]
        public void HandleRequestWithNullObjectParameterAndReturn ()
        {
            ByteString argBytes = ProtocolBuffers.WriteValue (0ul, typeof(ulong));
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoTestObject (It.IsAny<TestService.TestClass> ()))
                .Callback ((TestService.TestClass x) => {
                Assert.AreEqual (null, x);
            }).Returns ((TestService.TestClass x) => x);
            TestService.Service = mock.Object;
            Response.Builder response = Run (Req ("TestService", "EchoTestObject", Arg (0, argBytes)));
            Assert.False (response.HasError);
            Response builtResponse = response.SetTime (42).Build ();
            Assert.True (builtResponse.HasReturnValue);
            Assert.AreEqual (0, ProtocolBuffers.ReadValue (builtResponse.ReturnValue, typeof(ulong)));
        }

        /// <summary>
        /// Test calling the method of a proxy object
        /// </summary>
        [Test]
        public void HandleRequestForObjectMethod ()
        {
            var instance = new TestService.TestClass ("jeb");
            var guid = ObjectStore.Instance.AddInstance (instance);
            ByteString guidBytes = ProtocolBuffers.WriteValue (guid, typeof(ulong));
            ByteString argBytes = ProtocolBuffers.WriteValue (3.14159f, typeof(float));
            var request = Req ("TestService", "TestClass_FloatToString", Arg (0, guidBytes), Arg (1, argBytes));
            Response.Builder response = Run (request);
            var builtResponse = response.SetTime (42).Build ();
            Assert.AreEqual ("jeb3.14159", ProtocolBuffers.ReadValue (builtResponse.ReturnValue, typeof(string)));
        }

        /// <summary>
        /// Test calling the method of a proxy object, and pass a proxy object as a parameter
        /// </summary>
        [Test]
        public void HandleRequestForObjectMethodWithObjectParameter ()
        {
            var instance = new TestService.TestClass ("bill");
            var argInstance = new TestService.TestClass ("bob");
            var guid = ObjectStore.Instance.AddInstance (instance);
            var argGuid = ObjectStore.Instance.AddInstance (argInstance);
            ByteString guidBytes = ProtocolBuffers.WriteValue (guid, typeof(ulong));
            ByteString argBytes = ProtocolBuffers.WriteValue (argGuid, typeof(ulong));
            var request = Req ("TestService", "TestClass_ObjectToString", Arg (0, guidBytes), Arg (1, argBytes));
            Response.Builder response = Run (request);
            var builtResponse = response.SetTime (42).Build ();
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
            var guid = ObjectStore.Instance.AddInstance (instance);
            ByteString guidBytes = ProtocolBuffers.WriteValue (guid, typeof(ulong));
            var request = Req ("TestService", "TestClass_get_IntProperty", Arg (0, guidBytes));
            Response.Builder response = Run (request);
            var builtResponse = response.SetTime (0).Build ();
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
            var guid = ObjectStore.Instance.AddInstance (instance);
            ByteString guidBytes = ProtocolBuffers.WriteValue (guid, typeof(ulong));
            var request = Req ("TestService", "TestClass_set_IntProperty",
                Arg (0, guidBytes), Arg (1, ProtocolBuffers.WriteValue (1337, typeof(int))));
            Response.Builder response = Run (request);
            var builtResponse = response.SetTime (0).Build ();
            Assert.IsFalse (builtResponse.HasError);
            Assert.AreEqual (1337, instance.IntProperty);
        }

        /// <summary>
        /// Test calling a procedure with a class as the parameter,
        /// where the class is defined in a different service
        /// </summary>
        [Test]
        public void HandleRequestWithClassTypeParameterFromDifferentService ()
        {
            var instance = new TestService.TestClass ("jeb");
            instance.IntProperty = 42;
            var guid = ObjectStore.Instance.AddInstance (instance);
            ByteString guidBytes = ProtocolBuffers.WriteValue (guid, typeof(ulong));
            var request = Req ("TestService2", "ClassTypeFromOtherServiceAsParameter", Arg (0, guidBytes));
            Response.Builder response = Run (request);
            var builtResponse = response.SetTime (0).Build ();
            Assert.IsFalse (builtResponse.HasError);
            Assert.AreEqual (42, ProtocolBuffers.ReadValue (builtResponse.ReturnValue, typeof(long)));
        }

        /// <summary>
        /// Test calling a procedure that returns an object,
        /// where the class of the object is defined in a different service
        /// </summary>
        [Test]
        public void HandleRequestWithClassTypeReturnFromDifferentService ()
        {
            var request = Req ("TestService2", "ClassTypeFromOtherServiceAsReturn",
                Arg (0, ProtocolBuffers.WriteValue ("jeb", typeof(string))));
            Response.Builder response = Run (request);
            var builtResponse = response.SetTime (0).Build ();
            Assert.IsFalse (builtResponse.HasError);
            var guid = (ulong)ProtocolBuffers.ReadValue (builtResponse.ReturnValue, typeof(ulong));
            var obj = (TestService.TestClass)ObjectStore.Instance.GetInstance (guid);
            Assert.AreEqual ("jeb", obj.value);
        }

        /// <summary>
        /// Test calling a service method with an optional argument and no return value
        /// </summary>
        [Test]
        public void HandleRequestSingleOptionalArgNoReturn ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureSingleOptionalArgNoReturn (It.IsAny<string> ()))
                .Callback ((string x) => {
                Assert.AreEqual (x, "foo");
            });
            TestService.Service = mock.Object;
            Run (Req ("TestService", "ProcedureSingleOptionalArgNoReturn"));
            mock.Verify (x => x.ProcedureSingleOptionalArgNoReturn (It.IsAny<string> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method with multiple parameters, by name with optional arguments
        /// </summary>
        [Test]
        public void HandleRequestThreeOptionalArgs ()
        {
            var arg0 = 3.14159f;
            var arg2 = 42;
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureThreeOptionalArgsNoReturn (
                It.IsAny<float> (),
                It.IsAny<string> (),
                It.IsAny<int> ()))
                .Callback ((float x,
                            string y,
                            int z) => {
                Assert.AreEqual (arg0, x);
                Assert.AreEqual ("jeb", y);
                Assert.AreEqual (arg2, z);
            });
            TestService.Service = mock.Object;
            var request = Req ("TestService", "ProcedureThreeOptionalArgsNoReturn",
                Arg (2, ProtocolBuffers.WriteValue (arg2, arg2.GetType ())),
                Arg (0, ProtocolBuffers.WriteValue (arg0, arg0.GetType ())));
            Run (request);
            mock.Verify (x => x.ProcedureThreeOptionalArgsNoReturn (
                It.IsAny<float> (),
                It.IsAny<string> (),
                It.IsAny<int> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method with a missing argument
        /// </summary>
        [Test]
        public void HandleRequestMissingArgs ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureThreeOptionalArgsNoReturn (
                It.IsAny<float> (),
                It.IsAny<string> (),
                It.IsAny<int> ()));
            TestService.Service = mock.Object;
            Assert.Throws<RPCException> (() => Run (Req ("TestService", "ProcedureThreeOptionalArgsNoReturn")));
        }

        /// <summary>
        /// Test calling a service method with an argument that is a .proto enumeration
        /// </summary>
        [Test]
        public void HandleRequestSingleEnumArgNoReturn ()
        {
            var arg = KRPC.Schema.Test.TestEnum.b;
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureEnumArg (It.IsAny<KRPC.Schema.Test.TestEnum> ()))
                .Callback ((KRPC.Schema.Test.TestEnum x) => {
                Assert.AreEqual (KRPC.Schema.Test.TestEnum.b, x);
            });
            TestService.Service = mock.Object;
            var request = Req ("TestService", "ProcedureEnumArg",
                Arg (0, ProtocolBuffers.WriteValue ((int)arg, typeof(int))));
            Run (request);
            mock.Verify (x => x.ProcedureEnumArg (It.IsAny<KRPC.Schema.Test.TestEnum> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method that returns a .proto enumeration
        /// </summary>
        [Test]
        public void HandleRequestNoArgEnumReturn ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureEnumReturn ()).Returns(KRPC.Schema.Test.TestEnum.c);
            TestService.Service = mock.Object;
            var response = Run (Req ("TestService", "ProcedureEnumReturn"));
            var builtResponse = response.SetTime (0).Build ();
            Assert.IsFalse (builtResponse.HasError);
            Assert.AreEqual (ProtocolBuffers.WriteValue((int) KRPC.Schema.Test.TestEnum.c, typeof(int)), builtResponse.ReturnValue);
            mock.Verify (x => x.ProcedureEnumReturn (), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method with an argument that is a C# enumeration
        /// </summary>
        [Test]
        public void HandleRequestSingleCSharpEnumArgNoReturn ()
        {
            var arg = TestService.CSharpEnum.y;
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureCSharpEnumArg (It.IsAny<TestService.CSharpEnum> ()))
                .Callback ((TestService.CSharpEnum x) => {
                Assert.AreEqual (TestService.CSharpEnum.y, x);
            });
            TestService.Service = mock.Object;
            var request = Req ("TestService", "ProcedureCSharpEnumArg",
                Arg (0, ProtocolBuffers.WriteValue ((int)arg, typeof(int))));
            Run (request);
            mock.Verify (x => x.ProcedureCSharpEnumArg (It.IsAny<TestService.CSharpEnum> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method that returns a C# enumeration
        /// </summary>
        [Test]
        public void HandleRequestNoArgCSharpEnumReturn ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureCSharpEnumReturn ()).Returns(TestService.CSharpEnum.z);
            TestService.Service = mock.Object;
            var response = Run (Req ("TestService", "ProcedureCSharpEnumReturn"));
            var builtResponse = response.SetTime (0).Build ();
            Assert.IsFalse (builtResponse.HasError);
            Assert.AreEqual (ProtocolBuffers.WriteValue((int) TestService.CSharpEnum.z, typeof(int)), builtResponse.ReturnValue);
            mock.Verify (x => x.ProcedureCSharpEnumReturn (), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method with an argument that is an invalid value for a C# enumeration
        /// </summary>
        [Test]
        public void HandleRequestSingleInvalidCSharpEnumArgNoReturn ()
        {
            int arg = 9999;
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureCSharpEnumArg (It.IsAny<TestService.CSharpEnum> ()));
            TestService.Service = mock.Object;
            var request = Req ("TestService", "ProcedureCSharpEnumArg",
                Arg (0, ProtocolBuffers.WriteValue ((int)arg, typeof(int))));
            Assert.Throws<RPCException> (() => Run (request));
        }
    }
}

