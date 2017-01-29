using System;
using System.Collections.Generic;
using KRPC.Continuations;
using KRPC.Service;
using KRPC.Service.Messages;
using Moq;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class ServicesTest
    {
        static Request Req (string service, string procedure, params Argument[] args)
        {
            var request = new Request (service, procedure);
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

        static Argument Arg (uint position, object value)
        {
            return new Argument (position, value);
        }

        static Response Run (Request request)
        {
            var procedure = KRPC.Service.Services.Instance.GetProcedureSignature (request.Service, request.Procedure);
            return KRPC.Service.Services.Instance.HandleRequest (procedure, request);
        }

        [SetUp]
        public void SetUp ()
        {
            CallContext.SetGameScene (GameScene.Flight);
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
        /// Test calling a service method with an invalid argument
        /// </summary>
        [Test]
        public void HandleRequestSingleInvalidArgNoReturn ()
        {
            // should pass a string, not an int
            var request = Req ("TestService", "CreateTestObject", Arg (0, 42));
            Assert.Throws<RPCException> (() => Run (request));
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
            mock.Setup (x => x.ProcedureNoArgsReturns ()).Throws (new ArgumentException ("test exception"));
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
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureSingleArgNoReturn (It.IsAny<Response> ()))
                .Callback ((Response x) => Assert.AreEqual (arg, x));
            TestService.Service = mock.Object;
            Run (Req ("TestService", "ProcedureSingleArgNoReturn", Arg (0, arg)));
            mock.Verify (x => x.ProcedureSingleArgNoReturn (It.IsAny<Response> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method with multiple parameters and no return
        /// </summary>
        [Test]
        public void HandleRequestThreeArgsNoReturn ()
        {
            var arg0 = Res ("foo", 42);
            var arg1 = Req ("bar", "bar");
            var arg2 = Res ("baz", 123);
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureThreeArgsNoReturn (
                It.IsAny<Response> (),
                It.IsAny<Request> (),
                It.IsAny<Response> ()))
                        .Callback ((Response x,
                                    Request y,
                                    Response z) => {
                Assert.AreEqual (arg0, x);
                Assert.AreEqual (arg1, y);
                Assert.AreEqual (arg2, z);
            });
            TestService.Service = mock.Object;
            Run (Req ("TestService", "ProcedureThreeArgsNoReturn",
                Arg (0, arg0),
                Arg (1, arg1),
                Arg (2, arg2)));
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
            var innerResponse = (Response)response.ReturnValue;
            Assert.AreEqual (expectedResponse.Error, innerResponse.Error);
        }

        /// <summary>
        /// Test calling a service method with an argument and return value
        /// </summary>
        [Test]
        public void HandleRequestArgsReturn ()
        {
            var expectedResponse = new Response { Error = "bar", Time = 42 };
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureSingleArgReturns (It.IsAny<Response> ()))
                .Returns ((Response x) => x);
            TestService.Service = mock.Object;
            var request = Req ("TestService", "ProcedureSingleArgReturns", Arg (0, expectedResponse));
            Response response = Run (request);
            response.Time = 42;
            mock.Verify (x => x.ProcedureSingleArgReturns (It.IsAny<Response> ()), Times.Once ());
            var innerResponse = (Response)response.ReturnValue;
            Assert.AreEqual (expectedResponse.Error, innerResponse.Error);
        }

        /// <summary>
        /// Test calling a service method with value types for parameters
        /// </summary>
        [Test]
        public void HandleRequestWithValueTypes ()
        {
            const float arg0 = 3.14159f;
            const string arg1 = "foo";
            var arg2 = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureWithValueTypes (
                It.IsAny<float> (),
                It.IsAny<string> (),
                It.IsAny<byte[]> ()))
                .Callback ((float x, string y, byte[] z) => {
                Assert.AreEqual (arg0, x);
                Assert.AreEqual (arg1, y);
                Assert.AreEqual (arg2, z);
            }).Returns (42);
            TestService.Service = mock.Object;
            Run (Req ("TestService", "ProcedureWithValueTypes",
                Arg (0, arg0),
                Arg (1, arg1),
                Arg (2, arg2)));
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
            Assert.AreEqual ("foo", (string)response.ReturnValue);
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
            var request = Req ("TestService", "set_PropertyWithSet", Arg (0, "foo"));
            Response response = Run (request);
            Assert.AreEqual (string.Empty, response.Error);
        }

        /// <summary>
        /// Test calling a procedure that returns a proxy object
        /// </summary>
        [Test]
        public void HandleRequestWithObjectReturn ()
        {
            var instance = new TestService.TestClass ("foo");
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.CreateTestObject ("foo")).Returns (instance);
            TestService.Service = mock.Object;
            Response response = Run (Req ("TestService", "CreateTestObject", Arg (0, "foo")));
            Assert.AreEqual (string.Empty, response.Error);
            response.Time = 42;
            Assert.IsNotNull (response.ReturnValue);
            Assert.AreEqual (instance, (TestService.TestClass)response.ReturnValue);
        }

        /// <summary>
        /// Test calling a procedure that takes a proxy object as a parameter
        /// </summary>
        [Test]
        public void HandleRequestWithObjectParameter ()
        {
            var instance = new TestService.TestClass ("foo");
            ObjectStore.Instance.AddInstance (instance);
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.DeleteTestObject (It.IsAny<TestService.TestClass> ()))
                .Callback ((TestService.TestClass x) => Assert.AreSame (instance, x));
            TestService.Service = mock.Object;
            Run (Req ("TestService", "DeleteTestObject", Arg (0, instance)));
            mock.Verify (x => x.DeleteTestObject (It.IsAny<TestService.TestClass> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a procedure with a null proxy object as a parameter, and a null proxy object return value
        /// </summary>
        [Test]
        public void HandleRequestWithNullObjectParameterAndReturn ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoTestObject (It.IsAny<TestService.TestClass> ()))
                .Callback ((TestService.TestClass x) => Assert.AreEqual (null, x))
                .Returns ((TestService.TestClass x) => x);
            TestService.Service = mock.Object;
            Response response = Run (Req ("TestService", "EchoTestObject", Arg (0, null)));
            Assert.AreEqual (string.Empty, response.Error);
            response.Time = 42;
            Assert.IsNull (response.ReturnValue);
        }

        /// <summary>
        /// Test calling the method of a proxy object
        /// </summary>
        [Test]
        public void HandleRequestForObjectMethod ()
        {
            var instance = new TestService.TestClass ("jeb");
            var guid = ObjectStore.Instance.AddInstance (instance);
            const float arg = 3.14159f;
            var request = Req ("TestService", "TestClass_FloatToString", Arg (0, guid), Arg (1, arg));
            var response = Run (request);
            response.Time = 42;
            Assert.AreEqual ("jeb3.14159", (string)response.ReturnValue);
        }

        /// <summary>
        /// Test calling the method of a proxy object, and pass a proxy object as a parameter
        /// </summary>
        [Test]
        public void HandleRequestForObjectMethodWithObjectParameter ()
        {
            var instance = new TestService.TestClass ("bill");
            var arg = new TestService.TestClass ("bob");
            var guid = ObjectStore.Instance.AddInstance (instance);
            var request = Req ("TestService", "TestClass_ObjectToString", Arg (0, guid), Arg (1, arg));
            var response = Run (request);
            response.Time = 42;
            Assert.AreEqual ("billbob", (string)(response.ReturnValue));
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
            var request = Req ("TestService", "TestClass_get_IntProperty", Arg (0, guid));
            var response = Run (request);
            response.Time = 0;
            Assert.AreEqual (string.Empty, response.Error);
            Assert.AreEqual (42, (int)response.ReturnValue);
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
            var request = Req ("TestService", "TestClass_set_IntProperty",
                              Arg (0, guid), Arg (1, 1337));
            var response = Run (request);
            response.Time = 0;
            Assert.AreEqual (string.Empty, response.Error);
            Assert.AreEqual (1337, instance.IntProperty);
        }

        /// <summary>
        /// Test calling the static method of a class
        /// </summary>
        [Test]
        public void HandleRequestForClassStaticMethod ()
        {
            var request = Req ("TestService", "TestClass_StaticMethod", Arg (0, "bob"));
            var response = Run (request);
            response.Time = 42;
            Assert.AreEqual ("jebbob", (string)response.ReturnValue);
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
            ObjectStore.Instance.AddInstance (instance);
            var request = Req ("TestService2", "ClassTypeFromOtherServiceAsParameter", Arg (0, instance));
            var response = Run (request);
            response.Time = 0;
            Assert.AreEqual (string.Empty, response.Error);
            Assert.AreEqual (42, (int)response.ReturnValue);
        }

        /// <summary>
        /// Test calling a procedure that returns an object,
        /// where the class of the object is defined in a different service
        /// </summary>
        [Test]
        public void HandleRequestWithClassTypeReturnFromDifferentService ()
        {
            var request = Req ("TestService2", "ClassTypeFromOtherServiceAsReturn", Arg (0, "jeb"));
            var response = Run (request);
            response.Time = 0;
            Assert.AreEqual (string.Empty, response.Error);
            var obj = (TestService.TestClass)response.ReturnValue;
            Assert.AreEqual ("jeb", obj.Value);
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
                              Arg (2, arg2),
                              Arg (0, arg0));
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
            var arg = TestService.TestEnum.Y;
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureEnumArg (It.IsAny<TestService.TestEnum> ()))
                .Callback ((TestService.TestEnum x) => Assert.AreEqual (TestService.TestEnum.Y, x));
            TestService.Service = mock.Object;
            var request = Req ("TestService", "ProcedureEnumArg", Arg (0, arg));
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
            mock.Setup (x => x.ProcedureEnumReturn ()).Returns (TestService.TestEnum.Z);
            TestService.Service = mock.Object;
            var response = Run (Req ("TestService", "ProcedureEnumReturn"));
            response.Time = 0;
            Assert.AreEqual (string.Empty, response.Error);
            Assert.AreEqual (TestService.TestEnum.Z, response.ReturnValue);
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
            var request = Req ("TestService", "ProcedureTestEnumArg", Arg (0, arg));
            Assert.Throws<RPCException> (() => Run (request));
        }

        int BlockingProcedureNoReturnFnCount;

        void BlockingProcedureNoReturnFn (int n)
        {
            BlockingProcedureNoReturnFnCount++;
            if (n == 0)
                return;
            throw new YieldException (new ParameterizedContinuationVoid<int> (BlockingProcedureNoReturnFn, n - 1));
        }

        int BlockingProcedureReturnsFnCount;

        int BlockingProcedureReturnsFn (int n, int sum)
        {
            BlockingProcedureReturnsFnCount++;
            if (n == 0)
                return sum;
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
            var request = Req ("TestService", "BlockingProcedureNoReturn", Arg (0, num));
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
            Assert.AreEqual (string.Empty, response.Error);
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
            var request = Req ("TestService", "BlockingProcedureReturns", Arg (0, num));
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
            Assert.AreEqual (string.Empty, response.Error);
            Assert.AreEqual (expectedResult, (int)response.ReturnValue);
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
            var list = new List<string> { "jeb", "bob", "bill" };
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoList (It.IsAny<IList<string>> ()))
                .Returns ((IList<string> x) => x);
            TestService.Service = mock.Object;
            var response = Run (Req ("TestService", "EchoList", Arg (0, list)));
            response.Time = 0;
            Assert.AreEqual (string.Empty, response.Error);
            CollectionAssert.AreEqual (list, (IList<string>)response.ReturnValue);
            mock.Verify (x => x.EchoList (It.IsAny<IList<string>> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method that takes a dictionary as an argument and returns the same dictionary
        /// </summary>
        [Test]
        public void HandleEchoDictionary ()
        {
            var dictionary = new Dictionary<int,string> { { 0, "jeb" }, { 1, "bob" }, { 2, "bill" } };
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoDictionary (It.IsAny<IDictionary<int,string>> ()))
                .Returns ((IDictionary<int,string> x) => x);
            TestService.Service = mock.Object;
            var response = Run (Req ("TestService", "EchoDictionary", Arg (0, dictionary)));
            response.Time = 0;
            Assert.AreEqual (string.Empty, response.Error);
            CollectionAssert.AreEquivalent (dictionary, (IDictionary<int,string>)response.ReturnValue);
            mock.Verify (x => x.EchoDictionary (It.IsAny<IDictionary<int,string>> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method that takes a set as an argument and returns the same set
        /// </summary>
        [Test]
        public void HandleEchoSet ()
        {
            var set = new HashSet<int> { 345, 723, 112 };
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoSet (It.IsAny<HashSet<int>> ()))
                .Returns ((HashSet<int> x) => x);
            TestService.Service = mock.Object;
            var response = Run (Req ("TestService", "EchoSet", Arg (0, set)));
            response.Time = 0;
            Assert.AreEqual (string.Empty, response.Error);
            CollectionAssert.AreEqual (set, (HashSet<int>)response.ReturnValue);
            mock.Verify (x => x.EchoSet (It.IsAny<HashSet<int>> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method that takes a tuple as an argument and returns the same tuple
        /// </summary>
        [Test]
        public void HandleEchoTuple ()
        {
            var tuple = KRPC.Utils.Tuple.Create (42, false);
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoTuple (It.IsAny<KRPC.Utils.Tuple<int,bool>> ()))
                .Returns ((KRPC.Utils.Tuple<int,bool> x) => x);
            TestService.Service = mock.Object;
            var response = Run (Req ("TestService", "EchoTuple", Arg (0, tuple)));
            response.Time = 0;
            Assert.AreEqual (string.Empty, response.Error);
            Assert.AreEqual (tuple, (KRPC.Utils.Tuple<int,bool>)response.ReturnValue);
            mock.Verify (x => x.EchoTuple (It.IsAny<KRPC.Utils.Tuple<int,bool>> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method that takes a nested collection as an argument and returns the same collection
        /// </summary>
        [Test]
        public void HandleEchoNestedCollection ()
        {
            var list0 = new List<string> { "jeb", "bob" };
            var list1 = new List<string> ();
            var list2 = new List<string> { "bill", "edzor" };
            var collection = new Dictionary<int, IList<string>> { { 0, list0 }, { 1, list1 }, { 2, list2 } };
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoNestedCollection (It.IsAny<IDictionary<int,IList<string>>> ()))
                .Returns ((IDictionary<int,IList<string>> x) => x);
            TestService.Service = mock.Object;
            var response = Run (Req ("TestService", "EchoNestedCollection", Arg (0, collection)));
            response.Time = 0;
            Assert.AreEqual (string.Empty, response.Error);
            CollectionAssert.AreEqual (collection, (IDictionary<int, IList<string>>)response.ReturnValue);
            mock.Verify (x => x.EchoNestedCollection (It.IsAny<IDictionary<int,IList<string>>> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method that takes a list of objects as an argument and returns the same list
        /// </summary>
        [Test]
        public void HandleEchoListOfObjects ()
        {
            var list = new List<TestService.TestClass> {
                new TestService.TestClass ("foo"),
                new TestService.TestClass ("bar")
            };
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoListOfObjects (It.IsAny<IList<TestService.TestClass>> ()))
                .Returns ((IList<TestService.TestClass> x) => x);
            TestService.Service = mock.Object;
            var response = Run (Req ("TestService", "EchoListOfObjects", Arg (0, list)));
            response.Time = 0;
            Assert.AreEqual (string.Empty, response.Error);
            CollectionAssert.AreEqual (list, (IList<TestService.TestClass>)response.ReturnValue);
            mock.Verify (x => x.EchoListOfObjects (It.IsAny<IList<TestService.TestClass>> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method that takes an optional tuple as an argument
        /// </summary>
        [Test]
        public void HandleOptionalTupleNotSpecified ()
        {
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.TupleDefault (It.IsAny<KRPC.Utils.Tuple<int,bool>> ()))
                .Returns ((KRPC.Utils.Tuple<int,bool> x) => x);
            TestService.Service = mock.Object;
            var response = Run (Req ("TestService", "TupleDefault"));
            response.Time = 0;
            Assert.AreEqual (string.Empty, response.Error);
            Assert.AreEqual (TestService.CreateTupleDefault.Create (), response.ReturnValue);
            mock.Verify (x => x.TupleDefault (It.IsAny<KRPC.Utils.Tuple<int,bool>> ()), Times.Once ());
        }

        /// <summary>
        /// Test calling a service method that is not active in the current game mode
        /// </summary>
        [Test]
        public void HandleRequestWrongGameMode ()
        {
            CallContext.SetGameScene (GameScene.TrackingStation);
            var mock = new Mock<ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ProcedureNoArgsNoReturn ());
            TestService.Service = mock.Object;
            Assert.Throws<RPCException> (() => Run (Req ("TestService", "ProcedureNoArgsNoReturn")));
            mock.Verify (x => x.ProcedureNoArgsNoReturn (), Times.Never ());
        }
    }
}
