using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using KRPC.Client.Services.TestService;
using NUnit.Framework;

using Expr = KRPC.Client.Services.KRPC.Expression;
using KType = KRPC.Client.Services.KRPC.Type;

namespace KRPC.Client.Test
{
    [TestFixture]
    public class ExpressionCompilerTest : ServerTestCase
    {
        T Evaluate<T> (Expression<Func<T>> expression)
        {
            var stream = Connection.AddStream (expression);
            var value = stream.Get ();
            stream.Remove ();
            return value;
        }

        [Test]
        public void TestConstantFolding ()
        {
            int threshold = 5;
            Assert.AreEqual (11, Evaluate (() => threshold * 2 + 1));
            Assert.AreEqual ("foo", Evaluate (() => "fo" + "o"));
        }

        [Test]
        public void TestRemoteProperty ()
        {
            var testService = Connection.TestService ();
            testService.StringProperty = "foo";
            Assert.IsTrue (Evaluate (() => testService.StringProperty == "foo"));
            Assert.IsFalse (Evaluate (() => testService.StringProperty != "foo"));
        }

        [Test]
        public void TestMixedTypeArithmetic ()
        {
            var testService = Connection.TestService ();
            var value = Evaluate (() => testService.Counter ("CSharpCompiler.Mixed", 1) * 0.5 + 1);
            Assert.Greater (value, 1);
        }

        [Test]
        public void TestRemoteMethodWithArguments ()
        {
            var obj = Connection.TestService ().CreateTestObject ("compiled");
            var expected = obj.FloatToString (0.5f);
            Assert.AreEqual (expected, Evaluate (() => obj.FloatToString (0.5f)));
        }

        [Test]
        public void TestMethodWithMultipleArguments ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("args");
            var other = testService.CreateTestObject ("other");
            var expected = obj.OptionalArguments ("1", "2", "3", other);
            Assert.AreEqual (expected, Evaluate (() => obj.OptionalArguments ("1", "2", "3", other)));
        }

        [Test]
        public void TestStaticMethod ()
        {
            var expected = TestClass.StaticMethod (Connection, "bob", "");
            Assert.AreEqual (expected, Evaluate (() => TestClass.StaticMethod (Connection, "bob", "")));
        }

        [Test]
        public void TestChainedCalls ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("outer");
            var inner = testService.CreateTestObject ("inner");
            obj.ObjectProperty = inner;
            inner.IntProperty = 7;
            Assert.IsTrue (Evaluate (() => obj.ObjectProperty.IntProperty == 7));
            Assert.AreEqual ("value=inner", Evaluate (() => obj.ObjectProperty.GetValue ()));
        }

        [Test]
        public void TestObjectEquality ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("eq");
            var other = testService.CreateTestObject ("eq2");
            testService.ObjectProperty = obj;
            Assert.IsTrue (Evaluate (() => testService.ObjectProperty == obj));
            Assert.IsFalse (Evaluate (() => testService.ObjectProperty == other));
        }

        [Test]
        public void TestEnumValues ()
        {
            var testService = Connection.TestService ();
            Assert.IsTrue (Evaluate (() => testService.EnumEcho (TestEnum.ValueB) == TestEnum.ValueB));
        }

        [Test]
        public void TestTernary ()
        {
            var obj = Connection.TestService ().CreateTestObject ("ternary");
            obj.IntProperty = 1;
            Assert.AreEqual ("one", Evaluate (() => obj.IntProperty == 1 ? "one" : "other"));
            Assert.AreEqual (2.5, Evaluate (() => obj.IntProperty == 1 ? 2.5 : 2));
        }

        [Test]
        public void TestBooleanOperators ()
        {
            var obj = Connection.TestService ().CreateTestObject ("bool");
            obj.IntProperty = 5;
            Assert.IsTrue (Evaluate (() => obj.IntProperty > 1 && obj.IntProperty < 10));
            Assert.IsTrue (Evaluate (() => obj.IntProperty > 10 || !(obj.IntProperty == 4)));
        }

        [Test]
        public void TestMathPow ()
        {
            var obj = Connection.TestService ().CreateTestObject ("pow");
            obj.IntProperty = 3;
            Assert.AreEqual (9.0, Evaluate (() => Math.Pow (obj.IntProperty, 2)));
        }

        IList<TestClass> MakeObjects (string prefix)
        {
            var testService = Connection.TestService ();
            var objs = new List<TestClass> ();
            for (int i = 0; i < 3; i++) {
                var obj = testService.CreateTestObject (prefix + i);
                obj.IntProperty = i + 1;
                objs.Add (obj);
            }
            return objs;
        }

        [Test]
        public void TestSelect ()
        {
            var objs = MakeObjects ("select");
            CollectionAssert.AreEqual (
                new [] { 2, 4, 6 },
                Evaluate (() => objs.Select (o => o.IntProperty * 2).ToList ()));
        }

        [Test]
        public void TestSelectWithoutToList ()
        {
            var objs = MakeObjects ("lazy");
            // The lazily evaluated sequence is implicitly converted to a list
            var expression = Connection.CompileExpression (() => objs.Select (o => o.IntProperty));
            var stream = Connection.AddStream<IList<int>> (expression);
            CollectionAssert.AreEqual (new [] { 1, 2, 3 }, stream.Get ());
            stream.Remove ();
        }

        [Test]
        public void TestWhere ()
        {
            var objs = MakeObjects ("where");
            CollectionAssert.AreEqual (
                new [] { 2, 3 },
                Evaluate (() => objs.Where (o => o.IntProperty > 1).Select (o => o.IntProperty).ToList ()));
        }

        [Test]
        public void TestAggregations ()
        {
            var objs = MakeObjects ("agg");
            Assert.AreEqual (6, Evaluate (() => objs.Select (o => o.IntProperty).Sum ()));
            Assert.AreEqual (1, Evaluate (() => objs.Select (o => o.IntProperty).Min ()));
            Assert.AreEqual (3, Evaluate (() => objs.Select (o => o.IntProperty).Max ()));
            Assert.AreEqual (3, Evaluate (() => objs.Select (o => o.IntProperty).Count ()));
        }

        [Test]
        public void TestAnyAll ()
        {
            var objs = MakeObjects ("anyall");
            Assert.IsTrue (Evaluate (() => objs.Any (o => o.IntProperty == 2)));
            Assert.IsFalse (Evaluate (() => objs.Any (o => o.IntProperty == 4)));
            Assert.IsTrue (Evaluate (() => objs.All (o => o.IntProperty < 4)));
            Assert.IsFalse (Evaluate (() => objs.All (o => o.IntProperty > 1)));
        }

        [Test]
        public void TestContains ()
        {
            var objs = MakeObjects ("contains");
            Assert.IsTrue (Evaluate (() => objs.Select (o => o.IntProperty).Contains (2)));
            Assert.IsFalse (Evaluate (() => objs.Select (o => o.IntProperty).Contains (4)));
        }

        [Test]
        public void TestOrderBy ()
        {
            var objs = MakeObjects ("orderby");
            var expression = Connection.CompileExpression (
                () => objs.Select (o => o.IntProperty).OrderBy (x => -x));
            var stream = Connection.AddStream<IList<int>> (expression);
            CollectionAssert.AreEqual (new [] { 3, 2, 1 }, stream.Get ());
            stream.Remove ();
        }

        [Test]
        public void TestIndexing ()
        {
            var objs = MakeObjects ("index");
            Assert.AreEqual (1, Evaluate (() => objs.Select (o => o.IntProperty).ToList () [0]));
        }

        [Test]
        public void TestEvent ()
        {
            var testService = Connection.TestService ();
            var evnt = Connection.AddEvent (() => testService.Counter ("CSharpCompiler.Event", 1) > 5);
            lock (evnt.Condition) {
                evnt.Wait (5);
                Assert.IsTrue (evnt.Stream.Get ());
            }
        }

        [Test]
        public void TestSingleCallStreamStillWorks ()
        {
            var testService = Connection.TestService ();
            testService.StringProperty = "plain";
            var stream = Connection.AddStream (() => testService.StringProperty);
            Assert.AreEqual ("plain", stream.Get ());
            stream.Remove ();
        }

        [Test]
        public void TestMathFunctions ()
        {
            var obj = Connection.TestService ().CreateTestObject ("mathfns");
            obj.IntProperty = 16;
            Assert.AreEqual (4.0, Evaluate (() => Math.Sqrt (obj.IntProperty)));
            Assert.AreEqual (16, Evaluate (() => Math.Abs (-obj.IntProperty)));
            Assert.AreEqual (16.0, Evaluate (() => Math.Max ((double)obj.IntProperty, 3.0)));
            // Client side arguments are still evaluated on the client
            Assert.AreEqual (3.0, Evaluate (() => Math.Sqrt (9) * 1.0));
        }

        [Test]
        public void TestStdLibService ()
        {
            var stdlib = KRPC.Client.Services.StdLib.ExtensionMethods.StdLib (Connection);
            Assert.AreEqual (3.0, stdlib.Sqrt (9));
            Assert.AreEqual (
                Tuple.Create (0.0, 0.0, 1.0),
                stdlib.VectorCross (
                    Tuple.Create (1.0, 0.0, 0.0), Tuple.Create (0.0, 1.0, 0.0)));
        }

        [Test]
        public void TestStringConcatenation ()
        {
            var obj = Connection.TestService ().CreateTestObject ("strconcat");
            obj.IntProperty = 42;
            Assert.AreEqual ("value is 42!", Evaluate (() => "value is " + obj.IntProperty + "!"));
            Assert.AreEqual ("42", Evaluate (() => obj.IntProperty.ToString () + ""));
        }

        [Test]
        public void TestBitwiseComplement ()
        {
            var obj = Connection.TestService ().CreateTestObject ("bitwise");
            obj.IntProperty = 12;
            Assert.AreEqual (-13, Evaluate (() => ~obj.IntProperty));
            Assert.AreEqual (8, Evaluate (() => obj.IntProperty & 10));
        }

        [Test]
        public void TestSkipAndTake ()
        {
            var objs = MakeObjects ("skiptake");
            CollectionAssert.AreEqual (
                new [] { 2, 3 },
                Evaluate (() => objs.Select (o => o.IntProperty).Skip (1).Take (2).ToList ()));
        }

        [Test]
        public void TestToDictionary ()
        {
            var objs = MakeObjects ("todict");
            var result = Evaluate (() => objs.ToDictionary (
                o => o.IntProperty.ToString (), o => o.IntProperty * 2));
            Assert.AreEqual (3, result.Count);
            Assert.AreEqual (2, result ["1"]);
            Assert.AreEqual (6, result ["3"]);
        }

        [Test]
        public void TestRunFunction ()
        {
            var obj = Connection.TestService ().CreateTestObject ("run");
            obj.IntProperty = 20;
            Assert.AreEqual (41, Connection.RunFunction (() => obj.IntProperty * 2 + 1));
        }

        [Test]
        public void TestRunFunctionActionLambda ()
        {
            var testService = Connection.TestService ();
            Connection.RunFunction (() => testService.ResetCustomExceptionLater ());
        }

        [Test]
        public void TestRunFunctionSideEffects ()
        {
            var obj = Connection.TestService ().CreateTestObject ("runeffect");
            obj.IntProperty = 1;
            var call = new KRPC.Schema.KRPC.ProcedureCall ();
            call.Service = "TestService";
            call.Procedure = "TestClass_set_IntProperty";
            var expression = Expr.CallWithArguments (
                Connection, call,
                new Dictionary<int, Expr> {
                    { 0, Expr.ConstantObject (Connection, obj.id) },
                    { 1, Expr.ConstantInt (Connection, 42) }
                });
            Connection.RunFunction (expression);
            Assert.AreEqual (42, obj.IntProperty);
        }

        [Test]
        public void TestRunFunctionWithStatements ()
        {
            // Built with the factory API: total = 0; foreach x in [1, 2, 3]: total += x
            var total = Expr.Variable (Connection, "total", KType.Int (Connection));
            var x = Expr.Variable (Connection, "x", KType.Int (Connection));
            var values = Expr.CreateList (
                Connection,
                new List<Expr> {
                    Expr.ConstantInt (Connection, 1),
                    Expr.ConstantInt (Connection, 2),
                    Expr.ConstantInt (Connection, 3)
                });
            var program = Expr.BlockWithVariables (
                Connection,
                new List<Expr> { total, x },
                new List<Expr> {
                    Expr.Assign (Connection, total, Expr.ConstantInt (Connection, 0)),
                    Expr.ForEach (
                        Connection, x, values,
                        Expr.Assign (Connection, total, Expr.Add (Connection, total, x))),
                    total
                });
            Assert.AreEqual (6, Connection.RunFunction<int> (program));
        }

        [Test]
        public void TestStructField ()
        {
            var testService = Connection.TestService ();
            Assert.AreEqual (
                "CSharpCompiler.Field",
                Evaluate (() => testService.CounterStruct ("CSharpCompiler.Field").StringField));
            Assert.IsTrue (
                Evaluate (() => testService.CounterStruct ("CSharpCompiler.Field2").IntField >= 0));
        }

        [Test]
        public void TestNestedStructField ()
        {
            var testService = Connection.TestService ();
            var obj = testService.CreateTestObject ("nested");
            var nested = new TestNestedStruct (
                new TestStruct (1, "inner", TestEnum.ValueA, new List<int> { 2 }), obj, "outer");
            Assert.AreEqual (
                "inner",
                Evaluate (() => testService.NestedStructEcho (nested).StructField.StringField));
        }

        [Test]
        public void TestCreateStruct ()
        {
            var testService = Connection.TestService ();
            var value = Evaluate (
                () => testService.StructEcho (
                    new TestStruct (
                        testService.Counter ("CSharpCompiler.Struct", 1),
                        "built", TestEnum.ValueC, new List<int> { 1, 2 })));
            Assert.AreEqual ("built", value.StringField);
            Assert.AreEqual (TestEnum.ValueC, value.EnumField);
            Assert.AreEqual (new List<int> { 1, 2 }, value.ListField);
        }

        [Test]
        public void TestUnsupportedConstructs ()
        {
            var testService = Connection.TestService ();
            Assert.Throws<ExpressionCompilationException> (
                () => Connection.CompileExpression (() => testService.StringProperty.Length));
        }
    }
}
