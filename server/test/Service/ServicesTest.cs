using System;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf;
using KRPC;
using KRPC.Continuations;
using KRPC.Schema.KRPC;
using KRPC.Service;
using KRPC.Utils;
using Moq;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class ServicesTest
    {
        static Request Req (string service, string procedure, params Argument[] args)
        {
            var request = new Request ();
            request.Service = service;
            request.Procedure = procedure;
            foreach (var arg in args)
                request.Arguments.Add (arg);
            return request;
        }

        static Response Res (string error, int time)
        {
            return new Response {
                Error = error,
                Time = time
            };
        }

        static Response Res (ByteString data)
        {
            var response = new Response ();
            response.MergeFrom (data);
            return response;
        }

        static Response Res (Response response)
        {
            var newResponse = new Response ();
            newResponse.MergeFrom (response);
            return newResponse;
        }

        static Argument Arg (uint position, ByteString value)
        {
            return new Argument {
                Position = position,
                Value = value
            };
        }

        static Response Run (Request request)
        {
            var procedure = KRPC.Service.Services.Instance.GetProcedureSignature (request);
            return KRPC.Service.Services.Instance.HandleRequest (procedure, request);
        }

        static byte[] ToBytes<T> (T x) where T : IMessage
        {
            using (var stream = new MemoryStream ()) {
                x.WriteTo (stream);
                return stream.ToArray ();
            }
        }

        static byte[] ToBytes (float x)
        {
            using (var stream = new MemoryStream ()) {
                var codedStream = new CodedOutputStream (stream);
                codedStream.WriteFloat (x);
                codedStream.Flush ();
                return stream.ToArray ();
            }
        }

        static byte[] ToBytes (string x)
        {
            using (var stream = new MemoryStream ()) {
                var codedStream = new CodedOutputStream (stream);
                codedStream.WriteString (x);
                codedStream.Flush ();
                return stream.ToArray ();
            }
        }

        static byte[] ToBytes (byte[] x)
        {
            using (var stream = new MemoryStream ()) {
                var codedStream = new CodedOutputStream (stream);
                codedStream.WriteBytes (ByteString.CopyFrom (x));
                codedStream.Flush ();
                return stream.ToArray ();
            }
        }

        [SetUp]
        public void SetUp ()
        {
            KRPCServer.Context.SetGameScene (GameScene.Flight);
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
                argBytes [i] = (byte)(argBytes [i] + 2);
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
                .Callback ((Response x) => Assert.AreEqual (argBytes, x.ToByteArray ()));
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
            argBytes.Add (ToBytes (args [0]));
            argBytes.Add (ToBytes (args [1]));
            argBytes.Add (ToBytes (args [2]));
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
            var response = Run (Req ("TestService", "ProcedureNoArgsReturns"));
            response.Time = 42;
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
                .Returns ((Response x) => Res (x));
            TestService.Service = mock.Object;
            var request = Req ("TestService", "ProcedureSingleArgReturns",
                              Arg (0, ByteString.CopyFrom (expectedResponseBytes)));
            Response response = Run (request);
            response.Time = 42;
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
            Response response = Run (Req ("TestService", "get_PropertyWithGet"));
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
            Response response = Run (request);
            Assert.AreEqual ("", response.Error);
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
            Response response = Run (Req ("TestService", "CreateTestObject", Arg (0, argBytes)));
            Assert.AreEqual ("", response.Error);
            response.Time = 42;
            Assert.IsNotNull (response.ReturnValue);
            Assert.AreEqual (guid, ProtocolBuffers.ReadValue (response.ReturnValue, typeof(ulong)));
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
                .Callback ((TestService.TestClass x) => Assert.AreSame (instance, x));
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
                .Callback ((TestService.TestClass x) => Assert.AreEqual (null, x))
                .Returns ((TestService.TestClass x) => x);
            TestService.Service = mock.Object;
            Response response = Run (Req ("TestService", "EchoTestObject", Arg (0, argBytes)));
            Assert.AreEqual ("", response.Error);
            response.Time = 42;
            Assert.IsNotNull (response.ReturnValue);
            Assert.AreEqual (0, ProtocolBuffers.ReadValue (response.ReturnValue, typeof(ulong)));
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
            var response = Run (request);
            response.Time = 42;
            Assert.AreEqual ("jeb3.14159", ProtocolBuffers.ReadValue (response.ReturnValue, typeof(string)));
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
            var response = Run (request);
            response.Time = 42;
            Assert.AreEqual ("billbob", ProtocolBuffers.ReadValue (response.ReturnValue, typeof(string)));
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
            var response = Run (request);
            response.Time = 0;
            Assert.AreEqual ("", response.Error);
            Assert.AreEqual (42, ProtocolBuffers.ReadValue (response.ReturnValue, typeof(int)));
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
            var response = Run (request);
            response.Time = 0;
            Assert.AreEqual ("", response.Error);
            Assert.AreEqual (1337, instance.IntProperty);
        }

        /// <summary>
        /// Test calling the static method of a class
        /// </summary>
        [Test]
        public void HandleRequestForClassStaticMethod ()
        {
            ByteString argBytes = ProtocolBuffers.WriteValue ("bob", typeof(string));
            var request = Req ("TestService", "TestClass_StaticMethod", Arg (0, argBytes));
            var response = Run (request);
            response.Time = 42;
            Assert.AreEqual ("jebbob", ProtocolBuffers.ReadValue (response.ReturnValue, typeof(string)));
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
            var response = Run (request);
            response.Time = 0;
            Assert.AreEqual ("", response.Error);
            Assert.AreEqual (42, ProtocolBuffers.ReadValue (response.ReturnValue, typeof(long)));
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
            var response = Run (request);
            response.Time = 0;
            Assert.AreEqual ("", response.Error);
            var guid = (ulong)ProtocolBuffers.ReadValue (response.ReturnValue, typeof(ulong));
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
                .Callback ((string x) => Assert.AreEqual (x, "foo"));
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
            const float arg0 = 3.14159f;
            const int arg2 = 42;
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
        /// Test calling a service method with an optional null argument
        /// </summary>
        [Test]
        public void HandleRequestOptionalNullArg ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureOptionalNullArg (It.IsAny<TestService.TestClass> ()))
                .Callback ((TestService.TestClass x) => Assert.AreEqual (x, null));
            TestService.Service = mock.Object;
            Run (Req ("TestService", "ProcedureOptionalNullArg"));
            mock.Verify (x => x.ProcedureOptionalNullArg (It.IsAny<TestService.TestClass> ()), Times.Once ());
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
        /// Test calling a service method with an argument that is a C# enumeration
        /// </summary>
        [Test]
        public void HandleRequestSingleEnumArgNoReturn ()
        {
            var arg = TestService.TestEnum.y;
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureEnumArg (It.IsAny<TestService.TestEnum> ()))
                .Callback ((TestService.TestEnum x) => Assert.AreEqual (TestService.TestEnum.y, x));
            TestService.Service = mock.Object;
            var request = Req ("TestService", "ProcedureEnumArg",
                              Arg (0, ProtocolBuffers.WriteValue ((int)arg, typeof(int))));
            Run (request);
            mock.Verify (x => x.ProcedureEnumArg (It.IsAny<TestService.TestEnum> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method that returns a C# enumeration
        /// </summary>
        [Test]
        public void HandleRequestNoArgEnumReturn ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureEnumReturn ()).Returns (TestService.TestEnum.z);
            TestService.Service = mock.Object;
            var response = Run (Req ("TestService", "ProcedureEnumReturn"));
            response.Time = 0;
            Assert.AreEqual ("", response.Error);
            Assert.AreEqual (ProtocolBuffers.WriteValue ((int)TestService.TestEnum.z, typeof(int)), response.ReturnValue);
            mock.Verify (x => x.ProcedureEnumReturn (), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method with an argument that is an invalid value for a C# enumeration
        /// </summary>
        [Test]
        public void HandleRequestSingleInvalidEnumArgNoReturn ()
        {
            const int arg = 9999;
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureEnumArg (It.IsAny<TestService.TestEnum> ()));
            TestService.Service = mock.Object;
            var request = Req ("TestService", "ProcedureTestEnumArg",
                              Arg (0, ProtocolBuffers.WriteValue ((int)arg, typeof(int))));
            Assert.Throws<RPCException> (() => Run (request));
        }

        int BlockingProcedureNoReturnFnCount;

        void BlockingProcedureNoReturnFn (int n)
        {
            BlockingProcedureNoReturnFnCount++;
            if (n == 0)
                return;
            else
                throw new YieldException (new ParameterizedContinuationVoid<int> (BlockingProcedureNoReturnFn, n - 1));
        }

        int BlockingProcedureReturnsFnCount;

        int BlockingProcedureReturnsFn (int n, int sum)
        {
            BlockingProcedureReturnsFnCount++;
            if (n == 0)
                return sum;
            else
                throw new YieldException (new ParameterizedContinuation<int,int,int> (BlockingProcedureReturnsFn, n - 1, sum + n));
        }

        /// <summary>
        /// Test calling a service method that blocks, takes arguments and returns nothing
        /// </summary>
        [Test]
        public void HandleBlockingRequestArgsNoReturn ()
        {
            const int num = 42;
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.BlockingProcedureNoReturn (It.IsAny<int> ()))
                .Callback ((int n) => BlockingProcedureNoReturnFn (n));
            TestService.Service = mock.Object;
            var request = Req ("TestService", "BlockingProcedureNoReturn",
                              Arg (0, ProtocolBuffers.WriteValue (num, typeof(int))));
            BlockingProcedureNoReturnFnCount = 0;
            Response response = null;
            Continuation<Response> continuation = new RequestContinuation (null, request);
            while (response == null) {
                try {
                    response = continuation.Run ();
                } catch (YieldException e) {
                    continuation = (Continuation<Response>)e.Continuation;
                }
            }
            response.Time = 0;
            Assert.AreEqual ("", response.Error);
            // Verify the KRPCProcedure is called once, but the handler function is called multiple times
            mock.Verify (x => x.BlockingProcedureNoReturn (It.IsAny<int> ()), Times.Once ());
            Assert.AreEqual (num + 1, BlockingProcedureNoReturnFnCount);
        }

        /// <summary>
        /// Test calling a service method that blocks, takes arguments and returns a value
        /// </summary>
        [Test]
        public void HandleBlockingRequestArgsReturns ()
        {
            const int num = 10;
            const int expectedResult = 55;
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.BlockingProcedureReturns (It.IsAny<int> (), It.IsAny<int> ()))
                .Returns ((int n, int sum) => BlockingProcedureReturnsFn (n, sum));
            TestService.Service = mock.Object;
            var request = Req ("TestService", "BlockingProcedureReturns",
                              Arg (0, ProtocolBuffers.WriteValue (num, typeof(int))));
            BlockingProcedureReturnsFnCount = 0;
            Response response = null;
            Continuation<Response> continuation = new RequestContinuation (null, request);
            while (response == null) {
                try {
                    response = continuation.Run ();
                } catch (YieldException e) {
                    continuation = (Continuation<Response>)e.Continuation;
                }
            }
            response.Time = 0;
            Assert.AreEqual ("", response.Error);
            Assert.AreEqual (expectedResult, ProtocolBuffers.ReadValue (response.ReturnValue, typeof(int)));
            // Verify the KRPCProcedure is called once, but the handler function is called multiple times
            mock.Verify (x => x.BlockingProcedureReturns (It.IsAny<int> (), It.IsAny<int> ()), Times.Once ());
            Assert.AreEqual (num + 1, BlockingProcedureReturnsFnCount);
        }

        /// <summary>
        /// Test calling a service method that takes a list as an argument and returns the same list
        /// </summary>
        [Test]
        public void HandleEchoList ()
        {
            var list = new KRPC.Schema.KRPC.List ();
            list.Items.Add (ProtocolBuffers.WriteValue ("jeb", typeof(string)));
            list.Items.Add (ProtocolBuffers.WriteValue ("bob", typeof(string)));
            list.Items.Add (ProtocolBuffers.WriteValue ("bill", typeof(string)));
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoList (It.IsAny<IList<string>> ()))
                .Returns ((IList<string> x) => x);
            TestService.Service = mock.Object;
            var response = Run (Req ("TestService", "EchoList",
                               Arg (0, ProtocolBuffers.WriteMessage (list))));
            response.Time = 0;
            Assert.AreEqual ("", response.Error);
            Assert.AreEqual (ProtocolBuffers.WriteMessage (list), response.ReturnValue);
            mock.Verify (x => x.EchoList (It.IsAny<IList<string>> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method that takes a dictionary as an argument and returns the same dictionary
        /// </summary>
        [Test]
        public void HandleEchoDictionary ()
        {
            var dictionary = new Dictionary ();
            dictionary.Entries.Add (new DictionaryEntry {
                Key = ProtocolBuffers.WriteValue (0, typeof(int)),
                Value = ProtocolBuffers.WriteValue ("jeb", typeof(string))
            });
            dictionary.Entries.Add (new DictionaryEntry {
                Key = ProtocolBuffers.WriteValue (1, typeof(int)),
                Value = ProtocolBuffers.WriteValue ("bob", typeof(string))
            });
            dictionary.Entries.Add (new DictionaryEntry {
                Key = ProtocolBuffers.WriteValue (2, typeof(int)),
                Value = ProtocolBuffers.WriteValue ("bill", typeof(string))
            });
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoDictionary (It.IsAny<IDictionary<int,string>> ()))
                .Returns ((IDictionary<int,string> x) => x);
            TestService.Service = mock.Object;
            var response = Run (Req ("TestService", "EchoDictionary",
                               Arg (0, ProtocolBuffers.WriteMessage (dictionary))));
            response.Time = 0;
            Assert.AreEqual ("", response.Error);
            Assert.AreEqual (ProtocolBuffers.WriteMessage (dictionary), response.ReturnValue);
            mock.Verify (x => x.EchoDictionary (It.IsAny<IDictionary<int,string>> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method that takes a set as an argument and returns the same set
        /// </summary>
        [Test]
        public void HandleEchoSet ()
        {
            var set = new Set ();
            set.Items.Add (ProtocolBuffers.WriteValue (345, typeof(int)));
            set.Items.Add (ProtocolBuffers.WriteValue (723, typeof(int)));
            set.Items.Add (ProtocolBuffers.WriteValue (112, typeof(int)));
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoSet (It.IsAny<HashSet<int>> ()))
                .Returns ((HashSet<int> x) => x);
            TestService.Service = mock.Object;
            var response = Run (Req ("TestService", "EchoSet",
                               Arg (0, ProtocolBuffers.WriteMessage (set))));
            response.Time = 0;
            Assert.AreEqual ("", response.Error);
            Assert.AreEqual (ProtocolBuffers.WriteMessage (set), response.ReturnValue);
            mock.Verify (x => x.EchoSet (It.IsAny<HashSet<int>> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method that takes a tuple as an argument and returns the same tuple
        /// </summary>
        [Test]
        public void HandleEchoTuple ()
        {
            var tuple = new KRPC.Schema.KRPC.Tuple ();
            tuple.Items.Add (ProtocolBuffers.WriteValue (42, typeof(int)));
            tuple.Items.Add (ProtocolBuffers.WriteValue (false, typeof(bool)));
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoTuple (It.IsAny<KRPC.Utils.Tuple<int,bool>> ()))
                .Returns ((KRPC.Utils.Tuple<int,bool> x) => x);
            TestService.Service = mock.Object;
            var response = Run (Req ("TestService", "EchoTuple",
                               Arg (0, ProtocolBuffers.WriteMessage (tuple))));
            response.Time = 0;
            Assert.AreEqual ("", response.Error);
            Assert.AreEqual (ProtocolBuffers.WriteMessage (tuple), response.ReturnValue);
            mock.Verify (x => x.EchoTuple (It.IsAny<KRPC.Utils.Tuple<int,bool>> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method that takes a nested collection as an argument and returns the same collection
        /// </summary>
        [Test]
        public void HandleEchoNestedCollection ()
        {
            var list0 = new KRPC.Schema.KRPC.List ();
            list0.Items.Add (ProtocolBuffers.WriteValue ("jeb", typeof(string)));
            list0.Items.Add (ProtocolBuffers.WriteValue ("bob", typeof(string)));
            var list1 = new KRPC.Schema.KRPC.List ();
            var list2 = new KRPC.Schema.KRPC.List ();
            list2.Items.Add (ProtocolBuffers.WriteValue ("bill", typeof(string)));
            list2.Items.Add (ProtocolBuffers.WriteValue ("edzor", typeof(string)));
            var collection = new Dictionary ();
            collection.Entries.Add (new DictionaryEntry {
                Key = ProtocolBuffers.WriteValue (0, typeof(int)),
                Value = ProtocolBuffers.WriteMessage (list0)
            });
            collection.Entries.Add (new DictionaryEntry {
                Key = ProtocolBuffers.WriteValue (1, typeof(int)),
                Value = ProtocolBuffers.WriteMessage (list1)
            });
            collection.Entries.Add (new DictionaryEntry {
                Key = ProtocolBuffers.WriteValue (2, typeof(int)),
                Value = ProtocolBuffers.WriteMessage (list2)
            });
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoNestedCollection (It.IsAny<IDictionary<int,IList<string>>> ()))
                .Returns ((IDictionary<int,IList<string>> x) => x);
            TestService.Service = mock.Object;
            var response = Run (Req ("TestService", "EchoNestedCollection",
                               Arg (0, ProtocolBuffers.WriteMessage (collection))));
            response.Time = 0;
            Assert.AreEqual ("", response.Error);
            Assert.AreEqual (ProtocolBuffers.WriteMessage (collection), response.ReturnValue);
            mock.Verify (x => x.EchoNestedCollection (It.IsAny<IDictionary<int,IList<string>>> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method that takes a list of objects as an argument and returns the same list
        /// </summary>
        [Test]
        public void HandleEchoListOfObjects ()
        {
            var instance0 = new TestService.TestClass ("foo");
            var instance1 = new TestService.TestClass ("bar");
            var guid0 = ObjectStore.Instance.AddInstance (instance0);
            var guid1 = ObjectStore.Instance.AddInstance (instance1);
            var list = new KRPC.Schema.KRPC.List ();
            list.Items.Add (ProtocolBuffers.WriteValue (guid0, typeof(ulong)));
            list.Items.Add (ProtocolBuffers.WriteValue (guid1, typeof(ulong)));
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoListOfObjects (It.IsAny<IList<TestService.TestClass>> ()))
                .Returns ((IList<TestService.TestClass> x) => x);
            TestService.Service = mock.Object;
            var response = Run (Req ("TestService", "EchoListOfObjects",
                               Arg (0, ProtocolBuffers.WriteMessage (list))));
            response.Time = 0;
            Assert.AreEqual ("", response.Error);
            Assert.AreEqual (ProtocolBuffers.WriteMessage (list), response.ReturnValue);
            mock.Verify (x => x.EchoListOfObjects (It.IsAny<IList<TestService.TestClass>> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method that is not active in the current game mode
        /// </summary>
        [Test]
        public void HandleRequestWrongGameMode ()
        {
            KRPCServer.Context.SetGameScene (GameScene.TrackingStation);
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureNoArgsNoReturn ());
            TestService.Service = mock.Object;
            Assert.Throws<RPCException> (() => Run (Req ("TestService", "ProcedureNoArgsNoReturn")));
            mock.Verify (x => x.ProcedureNoArgsNoReturn (), Times.Never ());
        }
    }
}
