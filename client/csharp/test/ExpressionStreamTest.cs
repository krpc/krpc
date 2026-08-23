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
    public class ExpressionStreamTest : ServerTestCase
    {
        [Test]
        public void TestExpressionStream ()
        {
            var testService = Connection.TestService ();
            testService.StringProperty = "foo";
            var expr = Expr.Call (Connection, Connection.GetCall (() => testService.StringProperty));
            var stream = Connection.AddStream<string> (expr);
            Assert.AreEqual ("foo", stream.Get ());
            stream.Remove ();
        }

        [Test]
        public void TestComputedValue ()
        {
            var testService = Connection.TestService ();
            var counter = Expr.Call (
                Connection, Connection.GetCall (() => testService.Counter ("CSharpExpressionStream.Computed", 1)));
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
                objs.Select (x => Expr.ConstantObject (Connection, x.id)).ToList ());
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
                    Expr.Function (Connection, new List<Expr> { param }, getProperty)));
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
            var objType = Expr.ConstantObject (Connection, obj.id).ReturnType;
            Assert.AreEqual (KTypeCode.Class, objType.Code);
            Assert.AreEqual ("TestService", objType.Service);
            Assert.AreEqual ("TestClass", objType.Name);
        }
    }
}
