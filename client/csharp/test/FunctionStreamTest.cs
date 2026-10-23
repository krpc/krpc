using System.Collections.Generic;
using System.Linq;
using KRPC.Client.Services.TestService;
using NUnit.Framework;

using Expr = KRPC.Client.Services.KRPC.Expression;
using KType = KRPC.Client.Services.KRPC.Type;
using KTypeCode = KRPC.Client.Services.KRPC.TypeCode;

namespace KRPC.Client.Test
{
    [TestFixture]
    public class FunctionStreamTest : ServerTestCase
    {
        [Test]
        public void TestFunctionStream ()
        {
            var testService = Connection.TestService ();
            testService.StringProperty = "foo";
            var expr = Expr.Call (Connection, Connection.GetCall (() => testService.StringProperty));
            var stream = Connection.AddStream<string> (expr);
            Assert.AreEqual ("foo", stream.Get ());
            stream.Remove ();
        }

        [Test]
        public void TestCallbackIsNotPassedAnError ()
        {
            var testService = Connection.TestService ();
            var values = new List<int> { 1 };
            // The first update is a value, and the second an error as the index is out of range
            var stream = Connection.AddStream<object> (
                () => values [(testService.Counter ("FunctionStreamTest.CallbackError", 1) + 1) % 2]);
            var received = new List<object> ();
            stream.AddCallback (value => {
                lock (received)
                    received.Add (value);
            });
            var done = new System.Threading.ManualResetEvent (false);
            stream.AddEndCallback (exn => done.Set ());
            Assert.IsTrue (done.WaitOne (30000));
            Assert.Throws<System.IndexOutOfRangeException> (() => stream.Get ());
            stream.Remove ();
            lock (received)
                CollectionAssert.AreEqual (new object [] { 1 }, received);
        }

        [Test]
        public void TestConvertedKRPCCallStream ()
        {
            // A KRPC procedure cannot be called in a function, so the call is streamed
            var krpc = Services.KRPC.ExtensionMethods.KRPC (Connection);
            var stream = Connection.AddStream (() => (int)krpc.GameScene);
            Assert.AreEqual ((int)Services.KRPC.GameScene.SpaceCenter, stream.Get ());
            Assert.AreEqual (
                (double)Services.KRPC.GameScene.SpaceCenter,
                Connection.AddStream (() => (double)(int)krpc.GameScene).Get ());
            stream.Remove ();
        }

        [Test]
        public void TestNullIntoANonNullableType ()
        {
            var testService = Connection.TestService ();
            var expr = Expr.Call (Connection, Connection.GetCall (() => testService.EchoNullableInt (null)));
            var stream = Connection.AddStream<int> (expr);
            var exn = Assert.Throws<System.InvalidOperationException> (() => stream.Get ());
            StringAssert.Contains ("null", exn.Message);
            stream.Remove ();
            Assert.IsNull (Connection.AddStream<int?> (expr).Get ());
        }

        [Test]
        public void TestNullableElements ()
        {
            var testService = Connection.TestService ();
            var expr = Expr.Call (Connection, Connection.GetCall (
                () => testService.EchoListOfNullableInts (new List<int?> { 5, null })));
            CollectionAssert.AreEqual (
                new int? [] { 5, null }, Connection.Run<IList<int?>> (expr));
            Assert.Throws<System.InvalidOperationException> (
                () => Connection.Run<IList<int>> (expr));
        }

        [Test]
        public void TestNullableObjectElements ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("nullable-elements");
            var list = Expr.Call (Connection, Connection.GetCall (
                () => testService.EchoListOfNullableObjects (new List<TestClass> { obj, null })));
            CollectionAssert.AreEqual (
                new [] { obj, null }, Connection.Run<IList<TestClass>> (list));
            var stream = Connection.AddStream<IList<TestClass>> (list);
            CollectionAssert.AreEqual (new [] { obj, null }, stream.Get ());
            stream.Remove ();
            var dictionary = Expr.Call (Connection, Connection.GetCall (
                () => testService.EchoDictionaryOfNullableObjects (
                    new Dictionary<string, TestClass> { { "a", obj }, { "b", null } })));
            var echoed = Connection.Run<IDictionary<string, TestClass>> (dictionary);
            Assert.AreEqual (obj, echoed ["a"]);
            Assert.IsNull (echoed ["b"]);
            var tuple = Expr.Call (Connection, Connection.GetCall (
                () => testService.EchoTupleWithANullableObject (
                    new System.Tuple<int, TestClass> (1, null))));
            Assert.AreEqual (
                new System.Tuple<int, TestClass> (1, null),
                Connection.Run<System.Tuple<int, TestClass>> (tuple));
        }

        [Test]
        public void TestComputedValue ()
        {
            var testService = Connection.TestService ();
            var counter = Expr.Call (
                Connection, Connection.GetCall (() => testService.Counter ("CSharpFunctionStream.Computed", 1)));
            // int RPC result * double constant promotes to double
            var expr = Expr.Multiply (Connection, counter, Expr.ConstantDouble (Connection, 0.5));
            var stream = Connection.AddStream<double> (expr);
            var value = stream.Get ();
            Assert.Greater (value, 0);
            stream.Remove ();
        }

        [Test]
        public void TestPerElementCall ()
        {
            var testService = Connection.TestService ();
            var objs = new List<TestClass> ();
            for (int i = 0; i < 3; i++) {
                var obj = testService.CreateTestObject ("expr" + i);
                obj.IntProperty = i + 1;
                objs.Add (obj);
            }
            var objects = Expr.CreateList (
                Connection,
                objs.Select (x => Expr.ConstantObject (Connection, RemoteObject.GetObjectId (x))).ToList ());
            var param = Expr.Parameter (
                Connection, "x", KType.ClassType (Connection, "TestService", "TestClass"));
            var getProperty = Expr.CallWithArguments (
                Connection,
                Connection.GetCall (() => objs [0].IntProperty),
                new Dictionary<int, Expr> { { 0, param } });
            var selected = Expr.ToList (
                Connection,
                Expr.Select (
                    Connection, objects,
                    Expr.Lambda (Connection, new List<Expr> { param }, getProperty)));
            var stream = Connection.AddStream<IList<int>> (selected);
            CollectionAssert.AreEqual (new [] { 1, 2, 3 }, stream.Get ());
            stream.Remove ();
        }

        [Test]
        public void TestReturnType ()
        {
            var expr = Expr.ConstantDouble (Connection, 1.5);
            Assert.AreEqual (KTypeCode.Double, expr.ReturnType.Code);
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("expr-return-type");
            var objType = Expr.ConstantObject (Connection, RemoteObject.GetObjectId (obj)).ReturnType;
            Assert.AreEqual (KTypeCode.Class, objType.Code);
            Assert.AreEqual ("TestService", objType.Service);
            Assert.AreEqual ("TestClass", objType.Name);
        }

        [Test]
        public void TestTypeParameterHoldingTheValue ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("held");
            obj.IntProperty = 3;
            var value = Connection.Compile (() => obj.IntProperty + 1);
            Assert.AreEqual (4, Connection.Run<object> (value));
            var list = Connection.Compile (() => testService.IncrementList (new List<int> { 1, 2 }));
            CollectionAssert.AreEqual (new [] { 2, 3 }, Connection.Run<IEnumerable<int>> (list));
            CollectionAssert.AreEqual (new [] { 2, 3 }, Connection.Run<ICollection<int>> (list));
            Assert.AreEqual (obj, Connection.Run<object> (Connection.Compile (() => obj)));
        }

        [Test]
        public void TestEventFromAFunction ()
        {
            var testService = Connection.TestService ();
            var expr = Connection.Compile (
                () => testService.Counter ("CSharpFunctionStream.Event", 1) > 5);
            var evnt = Connection.AddEvent (expr);
            lock (evnt.Condition) {
                evnt.Wait ();
            }
            evnt.Remove ();
        }

        [Test]
        public void TestEventsOverOneFunction ()
        {
            var testService = Connection.TestService ();
            var expr = Connection.Compile (() => testService.StringProperty == "never");
            var first = Connection.AddEvent (expr);
            var second = Connection.AddEvent (expr);
            Assert.AreNotEqual (first.Stream.stream.Id, second.Stream.stream.Id);
            Assert.AreNotEqual (first, second);
            first.Remove ();
            lock (first.Condition)
                Assert.Throws<KRPC.Client.Services.KRPC.StreamRemovedException> (() => first.Wait ());
            lock (second.Condition)
                Assert.IsFalse (second.Wait (0.1));
            second.Remove ();
        }

        [Test]
        public void TestStreamsOverOneFunction ()
        {
            var testService = Connection.TestService ();
            testService.StringProperty = "foo";
            var expr = Connection.Compile (() => testService.StringProperty + "x");
            var first = Connection.AddStream<string> (expr);
            var second = Connection.AddStream<string> (expr);
            Assert.AreNotEqual (first.stream.Id, second.stream.Id);
            Assert.AreNotEqual (first, second);
            first.Remove ();
            Assert.Throws<KRPC.Client.Services.KRPC.StreamRemovedException> (() => first.Get ());
            Assert.AreEqual ("foox", second.Get ());
            second.Remove ();
        }

        [Test]
        public void TestTypeParameterIsChecked ()
        {
            var obj = Connection.TestService ().CreateTestObject ("checked");
            obj.IntProperty = 3;
            var value = Expr.Call (Connection, Connection.GetCall (() => obj.IntProperty));
            Assert.Throws<System.InvalidOperationException> (() => Connection.Run<string> (value));
            Assert.Throws<System.InvalidOperationException> (() => Connection.AddStream<IList<int>> (value));
            Assert.AreEqual (3, Connection.Run<int> (value));
            var testService = Connection.TestService ();
            var effect = Connection.Compile (
                () => testService.ResetInvalidOperationExceptionLater ());
            var exn = Assert.Throws<System.InvalidOperationException> (
                () => Connection.Run<int> (effect));
            StringAssert.Contains ("without a type parameter", exn.Message);
            exn = Assert.Throws<System.InvalidOperationException> (() => Connection.AddStream<int> (effect));
            StringAssert.Contains ("cannot be streamed", exn.Message);
        }

        [Test]
        public void TestRunFunctionWithArguments ()
        {
            var x = Expr.Parameter (Connection, "x", KType.Double (Connection));
            var n = Expr.Parameter (Connection, "n", KType.Int (Connection));
            var function = Expr.Lambda (Connection, new List<Expr> { x, n }, Expr.Multiply (Connection, x, n));
            Assert.AreEqual (7.5, Connection.Run<double> (function, 2.5, 3));
            // An int widens to a double parameter
            Assert.AreEqual (-8.0, Connection.Run<double> (function, 2, -4));
            var exn = Assert.Throws<System.ArgumentException> (
                () => Connection.Run<double> (function, 1.0));
            StringAssert.Contains ("takes 2 arguments, got 1", exn.Message);
            exn = Assert.Throws<System.ArgumentException> (
                () => Connection.Run<double> (function, 1.0, 2.5));
            StringAssert.Contains ("Argument 2 is a System.Double", exn.Message);
            // An int that is not negative converts to an unsigned parameter, as a constant does
            var u = Expr.Parameter (Connection, "u", KType.UInt (Connection));
            var identity = Expr.Lambda (Connection, new List<Expr> { u }, u);
            Assert.AreEqual (3u, Connection.Run<uint> (identity, 3));
            Assert.Throws<System.ArgumentException> (() => Connection.Run<uint> (identity, -3));
            // An array passed as the only argument is that argument
            var names = Expr.Parameter (Connection, "names", KType.ListType (Connection, KType.String (Connection)));
            var count = Expr.Lambda (Connection, new List<Expr> { names }, Expr.Count (Connection, names));
            Assert.AreEqual (2, Connection.Run<int> (count, new [] { "a", "b" }));
            Assert.AreEqual (0, Connection.Run<int> (count, new string [0]));
            // A function with parameters given no arguments is checked on the client
            exn = Assert.Throws<System.ArgumentException> (() => Connection.Run<int> (count));
            StringAssert.Contains ("takes 1 argument, got 0", exn.Message);
            exn = Assert.Throws<System.ArgumentException> (() => Connection.Run (count));
            StringAssert.Contains ("takes 1 argument, got 0", exn.Message);
            // A null element of a list of strings, which cannot hold one
            exn = Assert.Throws<System.ArgumentException> (
                () => Connection.Run<int> (count, new List<string> { "a", null }));
            StringAssert.Contains ("Argument 1 cannot be encoded", exn.Message);
        }

        [Test]
        public void TestRunFunctionArgumentConversions ()
        {
            var types = new Dictionary<string, KType> {
                { "int", KType.Int (Connection) }, { "long", KType.Long (Connection) },
                { "uint", KType.UInt (Connection) }, { "ulong", KType.ULong (Connection) },
                { "float", KType.Float (Connection) }, { "double", KType.Double (Connection) }
            };
            var functions = new Dictionary<string, Expr> ();
            foreach (var type in types) {
                var x = Expr.Parameter (Connection, "x", type.Value);
                functions [type.Key] = Expr.Lambda (Connection, new List<Expr> { x }, x);
            }
            // The implicit numeric conversions of C#
            Assert.AreEqual (3, Connection.Run<int> (functions ["int"], (short)3));
            Assert.AreEqual (3, Connection.Run<int> (functions ["int"], (sbyte)3));
            Assert.AreEqual (3, Connection.Run<int> (functions ["int"], (byte)3));
            Assert.AreEqual (3, Connection.Run<int> (functions ["int"], (ushort)3));
            Assert.AreEqual (97, Connection.Run<int> (functions ["int"], 'a'));
            Assert.AreEqual (3L, Connection.Run<long> (functions ["long"], (short)3));
            Assert.AreEqual (3u, Connection.Run<uint> (functions ["uint"], (byte)3));
            Assert.AreEqual (3ul, Connection.Run<ulong> (functions ["ulong"], (ushort)3));
            Assert.AreEqual (97.0, Connection.Run<double> (functions ["double"], 'a'));
            Assert.AreEqual (3.0f, Connection.Run<float> (functions ["float"], (short)3));
            // A signed integer that is not negative converts to an unsigned type at least as wide
            Assert.AreEqual (3u, Connection.Run<uint> (functions ["uint"], (short)3));
            Assert.AreEqual (3ul, Connection.Run<ulong> (functions ["ulong"], (sbyte)3));
            Assert.AreEqual (3ul, Connection.Run<ulong> (functions ["ulong"], 3L));
            Assert.Throws<System.ArgumentException> (
                () => Connection.Run<uint> (functions ["uint"], 3L));
            Assert.Throws<System.ArgumentException> (
                () => Connection.Run<uint> (functions ["uint"], (short)-3));
            Assert.Throws<System.ArgumentException> (
                () => Connection.Run<int> (functions ["int"], 3L));
            Assert.Throws<System.ArgumentException> (
                () => Connection.Run<int> (functions ["int"], 3.0));
        }

        [Test]
        public void TestRunFunctionWithObjectAndNullArguments ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("argument");
            obj.IntProperty = 4;
            var param = Expr.Parameter (Connection, "obj", KType.ClassType (Connection, "TestService", "TestClass"));
            var label = Expr.Parameter (Connection, "label", KType.NullableType (Connection, KType.String (Connection)));
            var getProperty = Expr.CallWithArguments (
                Connection, Connection.GetCall (() => obj.IntProperty),
                new Dictionary<int, Expr> { { 0, param } });
            var function = Expr.Lambda (Connection, new List<Expr> { param, label },
                Expr.Conditional (Connection, Expr.IsNull (Connection, label), getProperty, Expr.ConstantInt (Connection, 0)));
            Assert.AreEqual (4, Connection.Run<int> (function, obj, null));
            Assert.AreEqual (0, Connection.Run<int> (function, obj, "label"));
        }

        [Test]
        public void TestFunctionWithParametersCannotBeStreamed ()
        {
            var flag = Expr.Parameter (Connection, "b", KType.Bool (Connection));
            var function = Expr.Lambda (Connection, new List<Expr> { flag }, flag);
            var exn = Assert.Throws<System.ArgumentException> (
                () => Connection.AddStream<bool> (function));
            StringAssert.Contains ("cannot take parameters", exn.Message);
            exn = Assert.Throws<System.ArgumentException> (
                () => Connection.AddEvent (function));
            StringAssert.Contains ("cannot take parameters", exn.Message);
            // A lambda that is a single call is rejected before it is streamed as the call
            var obj = Connection.TestService ().CreateTestObject ("streamedparameter");
            System.Linq.Expressions.Expression<System.Func<int, int>> single = x => obj.IntProperty;
            Assert.Throws<FunctionCompilationException> (
                () => Connection.AddStream<int> ((System.Linq.Expressions.LambdaExpression)single));
        }
    }
}
