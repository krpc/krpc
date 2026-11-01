using System.Collections.Generic;
using System.Linq;
using KRPC.Service.KRPC;
using KRPC.Service.Messages;
using Moq;
using NUnit.Framework;
using LinqExpression = System.Linq.Expressions.Expression;

namespace KRPC.Test.Service.KRPC
{
    [TestFixture]
    public partial class ExpressionTest
    {
        static T Eval<T> (Expression expression)
        {
            return LinqExpression.Lambda<System.Func<T>> (expression).Compile () ();
        }

        [Test]
        public void Constant ()
        {
            Assert.AreEqual (1.2, Eval<double> (Expression.ConstantDouble (1.2)));
            Assert.AreEqual (3.4f, Eval<float> (Expression.ConstantFloat (3.4f)));
            Assert.AreEqual (5, Eval<int> (Expression.ConstantInt (5)));
            Assert.AreEqual (5000000000L, Eval<long> (Expression.ConstantLong (5000000000L)));
            Assert.AreEqual (4000000000u, Eval<uint> (Expression.ConstantUInt (4000000000u)));
            Assert.AreEqual (
                18000000000000000000ul, Eval<ulong> (Expression.ConstantULong (18000000000000000000ul)));
            Assert.IsFalse (Eval<bool> (Expression.ConstantBool (false)));
            Assert.IsTrue (Eval<bool> (Expression.ConstantBool (true)));
            Assert.AreEqual ("foo", Eval<string> (Expression.ConstantString ("foo")));
        }

        static ulong AddInstance (object obj)
        {
            return global::KRPC.Service.ObjectStore.Instance.AddInstance (obj);
        }

        static ProcedureCall BuildProcedureCall (string procedure, params Argument[] args)
        {
            var call = new ProcedureCall ("TestService", procedure);
            foreach (var arg in args)
                call.Arguments.Add (arg);
            return call;
        }

        [SetUp]
        public void SetUpGameScene ()
        {
            global::KRPC.Service.CallContext.GameScene = global::KRPC.Service.GameScene.Flight;
        }

        [Test]
        public void ConstantBytes ()
        {
            CollectionAssert.AreEqual (
                new byte [] { 1, 2, 3 }, Eval<byte[]> (Expression.ConstantBytes (new byte [] { 1, 2, 3 })));
            Assert.AreSame (
                Expression.ConstantBytes (new byte [] { 1, 2 }),
                Expression.ConstantBytes (new byte [] { 1, 2 }));
            Assert.AreNotSame (
                Expression.ConstantBytes (new byte [] { 1, 2 }),
                Expression.ConstantBytes (new byte [] { 2, 1 }));
        }

        [Test]
        public void Call ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            obj.IntProperty = 42;
            var expr = Expression.Call (BuildProcedureCall ("TestClass_get_IntProperty", new Argument (0, obj)));
            Assert.AreEqual (42, Eval<int> (expr));
            var method = Expression.Call (BuildProcedureCall (
                "TestClass_FloatToString", new Argument (0, obj), new Argument (1, 0.5f)));
            Assert.AreEqual ("foo0.5", Eval<string> (method));
        }

        [Test]
        public void CallDefaultArgument ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var expr = Expression.Call (BuildProcedureCall ("TestClass_IntToString", new Argument (0, obj)));
            Assert.AreEqual ("foo42", Eval<string> (expr));
        }

        [Test]
        public void CallGivesACopyOfACollection ()
        {
            // The procedures return collections they hold
            var held = new List<string> { "a" };
            var nested = new Dictionary<int, IList<string>> { { 1, held } };
            var structure = new global::KRPC.Test.Service.TestService.TestStruct { ListField = held };
            var mock = new Mock<global::KRPC.Test.Service.ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoList (It.IsAny<IList<string>> ())).Returns (held);
            mock.Setup (x => x.EchoNestedCollection (It.IsAny<IDictionary<int, IList<string>>> ()))
                .Returns (nested);
            mock.Setup (x => x.EchoStruct (It.IsAny<global::KRPC.Test.Service.TestService.TestStruct> ()))
                .Returns (structure);
            global::KRPC.Test.Service.TestService.Service = mock.Object;
            var list = Eval<IList<string>> (Expression.Call (BuildProcedureCall (
                "EchoList", new Argument (0, new List<string> ()))));
            Assert.AreNotSame (held, list);
            CollectionAssert.AreEqual (held, list);
            var dictionary = Eval<IDictionary<int, IList<string>>> (Expression.Call (BuildProcedureCall (
                "EchoNestedCollection", new Argument (0, new Dictionary<int, IList<string>> ()))));
            Assert.AreNotSame (nested, dictionary);
            Assert.AreNotSame (held, dictionary [1]);
            CollectionAssert.AreEqual (held, dictionary [1]);
            var copy = Eval<global::KRPC.Test.Service.TestService.TestStruct> (Expression.Call (BuildProcedureCall (
                "EchoStruct", new Argument (0, new global::KRPC.Test.Service.TestService.TestStruct ()))));
            Assert.AreNotSame (held, copy.ListField);
            CollectionAssert.AreEqual (held, copy.ListField);
        }

        [Test]
        public void CallMissingArgument ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.Call (BuildProcedureCall ("TestClass_FloatToString", new Argument (0, obj))));
        }

        [Test]
        public void CallWrongGameScene ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var expr = Expression.Call (BuildProcedureCall (
                "TestClass_MethodAvailableInSpecifiedGameScene", new Argument (0, obj)));
            Assert.Throws<global::KRPC.Service.RPCException> (() => Eval<string> (expr));
        }

        [Test]
        public void CallNullReturnAllowed ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var expr = Expression.Call (BuildProcedureCall (
                "TestClass_get_ObjectProperty", new Argument (0, obj)));
            Assert.IsNull (Eval<global::KRPC.Test.Service.TestService.TestClass> (expr));
        }

        [Test]
        public void CallNullReturnNotAllowed ()
        {
            var mock = new Mock<global::KRPC.Test.Service.ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ReturnNullWhenNotAllowed ())
                .Returns ((global::KRPC.Test.Service.TestService.TestClass)null);
            global::KRPC.Test.Service.TestService.Service = mock.Object;
            var expr = Expression.Call (BuildProcedureCall ("ReturnNullWhenNotAllowed"));
            Assert.Throws<global::KRPC.Service.RPCException> (
                () => Eval<global::KRPC.Test.Service.TestService.TestClass> (expr));
        }

        [Test]
        public void CallWithANullForANonNullableParameter ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var objId = AddInstance (obj);
            var nullObject = Expression.CallWithArguments (
                BuildProcedureCall ("TestClass_EchoNullableObject", new Argument (1, null)),
                new Dictionary<int, Expression> { { 0, Expression.ConstantObject (objId) } });
            var asArgument = Expression.CallWithArguments (
                BuildProcedureCall ("TestClass_ObjectToString"),
                new Dictionary<int, Expression> {
                    { 0, Expression.ConstantObject (objId) },
                    { 1, nullObject }
                });
            var exn = Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => Eval<string> (asArgument));
            StringAssert.Contains ("parameter other", exn.Message);
            var asInstance = Expression.CallWithArguments (
                BuildProcedureCall ("TestClass_FloatToString", new Argument (1, 1f)),
                new Dictionary<int, Expression> { { 0, nullObject } });
            exn = Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => Eval<string> (asInstance));
            StringAssert.Contains ("got null", exn.Message);
        }

        [Test]
        public void CallWithANullConstantForANonNullableParameter ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var objId = AddInstance (obj);
            var nullObject = Expression.ConstantNull (Type.ClassType ("TestService", "TestClass"));
            var asArgument = Expression.CallWithArguments (
                BuildProcedureCall ("TestClass_ObjectToString"),
                new Dictionary<int, Expression> {
                    { 0, Expression.ConstantObject (objId) },
                    { 1, nullObject }
                });
            var exn = Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => Eval<string> (asArgument));
            StringAssert.Contains ("parameter other", exn.Message);
            var asInstance = Expression.CallWithArguments (
                BuildProcedureCall ("TestClass_FloatToString", new Argument (1, 1f)),
                new Dictionary<int, Expression> { { 0, nullObject } });
            exn = Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => Eval<string> (asInstance));
            StringAssert.Contains ("got null", exn.Message);
        }

        [Test]
        public void CallWithANullNumberForANonNullableParameter ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var call = Expression.CallWithArguments (
                BuildProcedureCall ("TestClass_FloatToString", new Argument (0, obj)),
                new Dictionary<int, Expression> { { 1, Expression.ConstantNull (Type.Int ()) } });
            var exn = Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (() => Eval<string> (call));
            StringAssert.Contains ("parameter x", exn.Message);
            StringAssert.Contains ("got null", exn.Message);
            var value = Expression.CallWithArguments (
                BuildProcedureCall ("TestClass_FloatToString", new Argument (0, obj)),
                new Dictionary<int, Expression> {
                    { 1, Expression.Cast (Expression.ConstantInt (2), Type.NullableType (Type.Int ())) } });
            Assert.AreEqual ("foo2", Eval<string> (value));
        }

        [Test]
        public void CallWithArguments ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var expr = Expression.CallWithArguments (
                BuildProcedureCall ("TestClass_FloatToString"),
                new Dictionary<int, Expression> {
                    { 0, Expression.ConstantObject (AddInstance (obj)) },
                    { 1, Expression.ConstantFloat (3.5f) }
                });
            Assert.AreEqual ("foo3.5", Eval<string> (expr));
        }

        [Test]
        public void CallWithArgumentsPartial ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            // Instance provided by an expression; float argument from the encoded call
            var expr = Expression.CallWithArguments (
                BuildProcedureCall ("TestClass_FloatToString", new Argument (1, 0.5f)),
                new Dictionary<int, Expression> {
                    { 0, Expression.ConstantObject (AddInstance (obj)) }
                });
            Assert.AreEqual ("foo0.5", Eval<string> (expr));
            // Instance from the encoded call; float argument from an expression
            var expr2 = Expression.CallWithArguments (
                BuildProcedureCall ("TestClass_FloatToString", new Argument (0, obj)),
                new Dictionary<int, Expression> {
                    { 1, Expression.ConstantFloat (1.5f) }
                });
            Assert.AreEqual ("foo1.5", Eval<string> (expr2));
        }

        [Test]
        public void CallWithArgumentsDefaultArgument ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var expr = Expression.CallWithArguments (
                BuildProcedureCall ("TestClass_IntToString"),
                new Dictionary<int, Expression> {
                    { 0, Expression.ConstantObject (AddInstance (obj)) }
                });
            Assert.AreEqual ("foo42", Eval<string> (expr));
        }

        [Test]
        public void CallWithArgumentsNumericConversion ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            // int expression implicitly converted to the float parameter
            var expr = Expression.CallWithArguments (
                BuildProcedureCall ("TestClass_FloatToString"),
                new Dictionary<int, Expression> {
                    { 0, Expression.ConstantObject (AddInstance (obj)) },
                    { 1, Expression.ConstantInt (3) }
                });
            Assert.AreEqual ("foo3", Eval<string> (expr));
        }

        [Test]
        public void CallWithArgumentsWrongType ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.CallWithArguments (
                    BuildProcedureCall ("TestClass_FloatToString"),
                    new Dictionary<int, Expression> {
                        { 0, Expression.ConstantObject (AddInstance (obj)) },
                        { 1, Expression.ConstantString ("nope") }
                    }));
        }

        [Test]
        public void FunctionParametersHaveDistinctNames ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var other = Expression.Parameter ("x", Type.Int ());
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.Lambda (new List<Expression> { x, other }, x));
        }

        [Test]
        public void FunctionReusedAcrossExpressions ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var function = Expression.Lambda (
                new List<Expression> { x },
                Expression.Multiply (x, Expression.ConstantInt (3)));
            var first = Expression.Invoke (
                function, new Dictionary<string, Expression> { { "x", Expression.ConstantInt (1) } });
            var second = Expression.Add (
                Expression.Invoke (
                    function, new Dictionary<string, Expression> { { "x", Expression.ConstantInt (2) } }),
                Expression.ConstantInt (1));
            Assert.AreEqual (3, Eval<int> (first));
            Assert.AreEqual (7, Eval<int> (second));
        }

        [Test]
        public void InvokeWithAnArgumentOfAnUnrelatedType ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var function = Expression.Lambda (new List<Expression> { x }, x);
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Invoke (function, new Dictionary<string, Expression> {
                    { "x", Expression.ConstantString ("a") }
                }));
            Assert.AreEqual (
                "Incorrect expression type for parameter x. " +
                "Expected an expression of type System.Int32, got System.String", exn.Message);
        }

        /// <summary>
        /// A chain where each step uses the one before twice, doubling the nodes it
        /// compiles to.
        /// </summary>
        static Expression ChainWithTooManyNodes ()
        {
            var expr = Expression.Add (Expression.ConstantInt (1), Expression.ConstantInt (2));
            for (int i = 0; i < 22; i++)
                expr = Expression.Add (expr, expr);
            return expr;
        }

        [Test]
        public void FunctionWithTooManyNodesIsRejected ()
        {
            var expr = Expression.Equal (ChainWithTooManyNodes (), Expression.ConstantInt (0));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => global::KRPC.Service.KRPC.KRPC.AddEvent (expr));
            StringAssert.Contains ("more than 1000000 nodes", exn.Message);
        }

        [Test]
        public void CallReturningAMessageIsRejected ()
        {
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Call (BuildProcedureCall ("CreateEvent")));
            StringAssert.Contains ("protocol buffer message of type Event", exn.Message);
        }

        [Test]
        public void ConstantEnum ()
        {
            var value = Eval<global::KRPC.Test.Service.TestService.TestEnum> (
                Expression.ConstantEnum ("TestService", "TestEnum", 1));
            Assert.AreEqual (global::KRPC.Test.Service.TestService.TestEnum.Y, value);
            // Constants of the same member are shared
            Assert.AreSame (
                Expression.ConstantEnum ("TestService", "TestEnum", 1),
                Expression.ConstantEnum ("TestService", "TestEnum", 1));
        }

        [Test]
        public void ConstantEnumErrors ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.ConstantEnum ("NoSuchService", "TestEnum", 0));
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.ConstantEnum ("TestService", "NoSuchEnum", 0));
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.ConstantEnum ("TestService", "TestEnum", 99));
        }

        [Test]
        public void IsNull ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var property = Expression.Call (BuildProcedureCall (
                "TestClass_get_ObjectProperty", new Argument (0, obj)));
            Assert.IsTrue (Eval<bool> (Expression.IsNull (property)));
            obj.ObjectProperty = obj;
            Assert.IsFalse (Eval<bool> (Expression.IsNull (property)));

            var mock = new Mock<global::KRPC.Test.Service.ITestService> (MockBehavior.Strict);
            // Echoes its argument, with 0 standing for null
            mock.Setup (x => x.EchoNullableInt (It.IsAny<int?> ()))
                .Returns ((int? x) => x == 0 ? null : x);
            global::KRPC.Test.Service.TestService.Service = mock.Object;
            var echo = Expression.CallWithArguments (
                BuildProcedureCall ("EchoNullableInt"),
                new Dictionary<int, Expression> { { 0, Expression.ConstantInt (3) } });
            Assert.IsFalse (Eval<bool> (Expression.IsNull (echo)));
            var echoNull = Expression.Call (BuildProcedureCall ("EchoNullableInt", new Argument (0, 0)));
            Assert.IsTrue (Eval<bool> (Expression.IsNull (echoNull)));
        }

        [Test]
        public void IsNullOfAValueThatCannotBeNull ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.IsNull (Expression.ConstantInt (1)));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.IsNull (Expression.ConstantString ("a")));
        }

        // A string from a procedure that can return null
        static Expression NullableString (string value)
        {
            return Expression.Call (BuildProcedureCall (
                "TestClass_static_StaticNullableMethod", new Argument (0, value)));
        }

        [Test]
        public void NullableReferenceReturn ()
        {
            Assert.IsTrue (NullableString ("a").ReturnType.Nullable);
            Assert.IsFalse (Expression.ConstantString ("a").ReturnType.Nullable);
            Assert.IsTrue (Eval<bool> (Expression.IsNull (NullableString (null))));
            Assert.IsFalse (Eval<bool> (Expression.IsNull (NullableString ("a"))));
        }

        [Test]
        public void ConstantNull ()
        {
            Assert.IsNull (Eval<int?> (Expression.ConstantNull (Type.Int ())));
            Assert.IsNull (Eval<string> (Expression.ConstantNull (Type.String ())));
            Assert.IsNull (Eval<IList<string>> (Expression.ConstantNull (Type.ListType (Type.String ()))));
            Assert.IsNull (Eval<global::KRPC.Test.Service.TestService.TestClass> (
                Expression.ConstantNull (Type.ClassType ("TestService", "TestClass"))));
            Assert.IsTrue (Expression.ConstantNull (Type.Int ()).ReturnType.Nullable);
            Assert.IsTrue (Expression.ConstantNull (Type.String ()).ReturnType.Nullable);
            Assert.AreEqual (
                Type.NullableType (Type.String ()), Expression.ConstantNull (Type.String ()).ReturnType);
            Assert.IsTrue (Eval<bool> (Expression.IsNull (Expression.ConstantNull (Type.String ()))));
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentNullException> (() => Expression.ConstantNull (null));
        }

        [Test]
        public void ConstantNullsAreShared ()
        {
            Assert.AreSame (
                Expression.ConstantNull (Type.String ()),
                Expression.ConstantNull (Type.NullableType (Type.String ())));
            var list = Type.ListType (Type.String ());
            var listOfNullable = Type.ListType (Type.NullableType (Type.String ()));
            Assert.AreNotSame (Expression.ConstantNull (list), Expression.ConstantNull (listOfNullable));
            Assert.IsTrue (Expression.ConstantNull (listOfNullable).ReturnType.Types [0].Nullable);
        }

        [Test]
        public void ConstantNullInAPositionThatIsNotNullable ()
        {
            var cast = Expression.Cast (Expression.ConstantNull (Type.String ()), Type.String ());
            var exn = Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => Eval<string> (cast));
            StringAssert.Contains ("is null", exn.Message);
            var conditional = Expression.Conditional (
                Expression.ConstantBool (true), Expression.ConstantString ("a"),
                Expression.ConstantNull (Type.String ()));
            Assert.IsTrue (conditional.ReturnType.Nullable);
            Assert.AreEqual ("a", Eval<string> (conditional));
        }

        [Test]
        public void NullableElementsOfAReturnValue ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var mock = new Mock<global::KRPC.Test.Service.ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoListOfNullableObjects (
                It.IsAny<IList<global::KRPC.Test.Service.TestService.TestClass>> ()))
                .Returns ((IList<global::KRPC.Test.Service.TestService.TestClass> x) => x);
            global::KRPC.Test.Service.TestService.Service = mock.Object;
            var call = Expression.Call (BuildProcedureCall (
                "EchoListOfNullableObjects",
                new Argument (0, new List<global::KRPC.Test.Service.TestService.TestClass> { obj, null })));
            var type = call.ReturnType;
            Assert.IsFalse (type.Nullable);
            Assert.IsTrue (type.Types [0].Nullable);
        }

        [Test]
        public void CastToATypeThatIsNotNullable ()
        {
            var cast = Expression.Cast (NullableString (null), Type.String ());
            Assert.IsFalse (cast.ReturnType.Nullable);
            var exn = Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => Eval<string> (cast));
            StringAssert.Contains ("is null", exn.Message);
            Assert.AreEqual ("a", Eval<string> (Expression.Cast (NullableString ("a"), Type.String ())));
            Assert.IsTrue (Expression.Cast (
                Expression.ConstantString ("a"), Type.NullableType (Type.String ())).ReturnType.Nullable);
        }

        [Test]
        public void ConditionalIsNullableWhenABranchIs ()
        {
            Assert.IsTrue (Expression.Conditional (
                Expression.ConstantBool (true), Expression.ConstantString ("a"),
                NullableString ("b")).ReturnType.Nullable);
            Assert.IsFalse (Expression.Conditional (
                Expression.ConstantBool (true), Expression.ConstantString ("a"),
                Expression.ConstantString ("b")).ReturnType.Nullable);
        }

        [Test]
        public void NullableBooleans ()
        {
            Expression nullBool = LinqExpression.Constant (null, typeof (bool?));
            Expression trueBool = LinqExpression.Constant (true, typeof (bool?));
            Assert.IsFalse (Eval<bool> (Expression.Not (trueBool)));
            Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (() => Eval<bool> (Expression.Not (nullBool)));
            Assert.IsTrue (Eval<bool> (Expression.ConditionalAnd (trueBool, Expression.ConstantBool (true))));
            Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => Eval<bool> (Expression.ConditionalOr (nullBool, Expression.ConstantBool (true))));
            Assert.AreEqual (1, Eval<int> (Expression.Conditional (
                trueBool, Expression.ConstantInt (1), Expression.ConstantInt (2))));
            Assert.IsTrue (Eval<bool> (Expression.Equal (nullBool, nullBool)));
            Assert.IsFalse (Eval<bool> (Expression.Equal (nullBool, Expression.ConstantBool (true))));
            Assert.IsTrue (Eval<bool> (Expression.IsNull (nullBool)));
        }

        [Test]
        public void NullableStructuresCompare ()
        {
            var value = new global::KRPC.Test.Service.TestService.TestStruct { IntField = 1 };
            Expression nullStruct = LinqExpression.Constant (
                null, typeof (global::KRPC.Test.Service.TestService.TestStruct?));
            Expression someStruct = LinqExpression.Constant (
                value, typeof (global::KRPC.Test.Service.TestService.TestStruct?));
            Assert.IsTrue (Eval<bool> (Expression.Equal (nullStruct, nullStruct)));
            Assert.IsTrue (Eval<bool> (Expression.Equal (someStruct, someStruct)));
            Assert.IsFalse (Eval<bool> (Expression.Equal (nullStruct, someStruct)));
        }

        [Test]
        public void InvokeWithANullableArgument ()
        {
            var parameter = Expression.Parameter ("x", Type.String ());
            var function = Expression.Lambda (new List<Expression> { parameter }, parameter);
            Assert.IsFalse (Expression.Invoke (function, new Dictionary<string, Expression> {
                { "x", NullableString ("a") } }).ReturnType.Nullable);
            var invoke = Expression.Invoke (function, new Dictionary<string, Expression> {
                { "x", NullableString (null) } });
            var exn = Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => Eval<string> (invoke));
            StringAssert.Contains ("parameter x", exn.Message);
            var nullableParameter = Expression.Parameter ("y", Type.NullableType (Type.String ()));
            var nullableFunction = Expression.Lambda (
                new List<Expression> { nullableParameter }, nullableParameter);
            var nullableInvoke = Expression.Invoke (nullableFunction, new Dictionary<string, Expression> {
                { "y", NullableString (null) } });
            Assert.IsTrue (nullableInvoke.ReturnType.Nullable);
            Assert.IsNull (Eval<string> (nullableInvoke));
        }

        [Test]
        public void ConstantObject ()
        {
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var id = global::KRPC.Service.ObjectStore.Instance.AddInstance (obj);
            var expr = Expression.ConstantObject (id);
            Assert.AreEqual (typeof (global::KRPC.Test.Service.TestService.TestClass), ((LinqExpression)expr).Type);
            Assert.AreSame (obj, Eval<global::KRPC.Test.Service.TestService.TestClass> (expr));
            Assert.IsTrue (Eval<bool> (Expression.Equal (
                Expression.ConstantObject (id), Expression.ConstantObject (id))));
        }

        [Test]
        public void ConstantObjectInvalid ()
        {
            // The object store issues identifiers from one, and reports both an
            // identifier it never issued and one it has not reached
            var zero = Assert.Throws<System.ArgumentException> (
                () => Expression.ConstantObject (0));
            StringAssert.Contains ("0 is not an object identifier", zero.Message);
            Assert.Throws<System.ArgumentException> (
                () => Expression.ConstantObject (ulong.MaxValue));
            var id = global::KRPC.Service.ObjectStore.Instance.AddInstance (new object ());
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.ConstantObject (id));
        }

        [Test]
        public void ReturnType ()
        {
            Assert.AreEqual (TypeCode.Double, Expression.ConstantDouble (1.2).ReturnType.Code);
            Assert.AreEqual (TypeCode.Bool, Expression.Equal (
                Expression.ConstantInt (1), Expression.ConstantInt (2)).ReturnType.Code);
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            var call = Expression.Call (BuildProcedureCall ("TestClass_get_IntProperty", new Argument (0, obj)));
            Assert.AreEqual (TypeCode.SInt32, call.ReturnType.Code);
            var objConstant = Expression.ConstantObject (AddInstance (obj));
            var objType = objConstant.ReturnType;
            Assert.AreEqual (TypeCode.Class, objType.Code);
            Assert.AreEqual ("TestService", objType.Service);
            Assert.AreEqual ("TestClass", objType.Name);
            var echoed = Expression.Call (BuildProcedureCall (
                "EchoList", new Argument (0, new List<string> ())));
            Assert.AreEqual (TypeCode.List, echoed.ReturnType.Code);
            Assert.AreEqual (TypeCode.String, echoed.ReturnType.Types [0].Code);
        }

        [Test]
        public void HasReturnType ()
        {
            var value = Expression.Call (BuildProcedureCall ("ProcedureNoArgsReturns"));
            Assert.IsTrue (value.HasReturnType);
            var valueless = Expression.Call (BuildProcedureCall ("ProcedureNoArgsNoReturn"));
            Assert.IsFalse (valueless.HasReturnType);
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => { var unused = valueless.ReturnType; });
            StringAssert.Contains ("does not evaluate to a value", exn.Message);
        }

        [Test]
        public void ReturnTypeOfNullableValue ()
        {
            var expr = Expression.Call (BuildProcedureCall (
                "EchoNullableInt", new Argument (0, 3)));
            Assert.AreEqual (TypeCode.SInt32, expr.ReturnType.Code);
            var values = Expression.Call (BuildProcedureCall (
                "EchoListOfNullableInts",
                new Argument (0, new List<int?> { 1, null })));
            Assert.AreEqual (TypeCode.List, values.ReturnType.Code);
            Assert.AreEqual (TypeCode.SInt32, values.ReturnType.Types.Single ().Code);
        }

        [Test]
        public void ReturnTypeOfAMessage ()
        {
            var stream = new Expression (
                System.Linq.Expressions.Expression.Constant (new global::KRPC.Service.Messages.Stream (1)));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => { var type = stream.ReturnType; });
            StringAssert.Contains ("protocol buffer message of type Stream", exn.Message);
        }

        sealed class EquatableObject : global::KRPC.Utils.Equatable<EquatableObject>
        {
            readonly int value;

            public EquatableObject (int value)
            {
                this.value = value;
            }

            public override bool Equals (EquatableObject other)
            {
                return !ReferenceEquals (other, null) && value == other.value;
            }

            public override int GetHashCode ()
            {
                return value;
            }
        }

        [Test]
        public void EqualityOfObjects ()
        {
            Expression a = LinqExpression.Constant (new EquatableObject (1));
            Expression b = LinqExpression.Constant (new EquatableObject (1));
            Expression c = LinqExpression.Constant (new EquatableObject (2));
            Expression none = LinqExpression.Constant (null, typeof (EquatableObject));
            Assert.IsTrue (Eval<bool> (Expression.Equal (a, b)));
            Assert.IsFalse (Eval<bool> (Expression.NotEqual (a, b)));
            Assert.IsFalse (Eval<bool> (Expression.Equal (a, c)));
            Assert.IsTrue (Eval<bool> (Expression.NotEqual (a, c)));
            Assert.IsFalse (Eval<bool> (Expression.Equal (a, none)));
            Assert.IsTrue (Eval<bool> (Expression.Equal (none, none)));
        }

        [Test]
        public void NullableNumbersKeepTheirNull ()
        {
            Expression none = LinqExpression.Constant (null, typeof (int?));
            Expression three = LinqExpression.Constant (3, typeof (int?));
            Expression noneLong = LinqExpression.Constant (null, typeof (long?));
            Assert.IsTrue (Eval<bool> (Expression.Equal (none, none)));
            Assert.IsTrue (Eval<bool> (Expression.Equal (none, noneLong)));
            Assert.IsFalse (Eval<bool> (Expression.Equal (none, Expression.ConstantInt (3))));
            Assert.IsTrue (Eval<bool> (Expression.NotEqual (none, three)));
            Assert.IsTrue (Eval<bool> (Expression.Equal (three, Expression.ConstantDouble (3))));
            Assert.IsNull (Eval<int?> (Expression.Conditional (
                Expression.ConstantBool (true), none, three)));
            Assert.AreEqual (3L, Eval<long?> (Expression.Conditional (
                Expression.ConstantBool (false), noneLong, three)));
        }

        [Test]
        public void NullOperandIsNamed ()
        {
            var add = Expression.Add (Expression.ConstantInt (1), Expression.ConstantNull (Type.Int ()));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (() => Eval<int> (add));
            Assert.AreEqual ("The second operand of Add is null, which its type does not allow", exn.Message);
        }

        [Test]
        public void NullableNumbersAreUsedAsNumbers ()
        {
            var mock = new Mock<global::KRPC.Test.Service.ITestService> (MockBehavior.Strict);
            // Echoes its argument, with 0 standing for null
            mock.Setup (x => x.EchoNullableInt (It.IsAny<int?> ()))
                .Returns ((int? x) => x == 0 ? null : x);
            global::KRPC.Test.Service.TestService.Service = mock.Object;
            var echo = Expression.Call (BuildProcedureCall ("EchoNullableInt", new Argument (0, 3)));
            Assert.AreEqual (4.5, Eval<double> (Expression.Add (echo, Expression.ConstantDouble (1.5))));
            Assert.IsTrue (Eval<bool> (Expression.Equal (echo, Expression.ConstantInt (3))));
            Assert.AreEqual (-3, Eval<int> (Expression.Negate (echo)));
            // Passed on to a nullable parameter as it is, including its nulls
            var echoNull = Expression.Call (BuildProcedureCall ("EchoNullableInt", new Argument (0, 0)));
            var echoAgain = Expression.CallWithArguments (
                BuildProcedureCall ("EchoNullableInt"),
                new Dictionary<int, Expression> { { 0, echoNull } });
            Assert.IsTrue (Eval<bool> (Expression.IsNull (echoAgain)));
            // Widened to a nullable parameter of another number type
            mock.Setup (x => x.EchoNullableDouble (It.IsAny<double?> ()))
                .Returns ((double? x) => x);
            var widened = Expression.CallWithArguments (
                BuildProcedureCall ("EchoNullableDouble"),
                new Dictionary<int, Expression> { { 0, echoNull } });
            Assert.IsTrue (Eval<bool> (Expression.IsNull (widened)));
            widened = Expression.CallWithArguments (
                BuildProcedureCall ("EchoNullableDouble"),
                new Dictionary<int, Expression> { { 0, echo } });
            Assert.AreEqual (3.0, Eval<double?> (widened));
            // A null number throws when it is used as a number
            Assert.Throws<global::KRPC.Service.KRPC.NullReferenceException> (
                () => Eval<int> (Expression.Add (echoNull, Expression.ConstantInt (1))));
        }

        [Test]
        public void NullableEnumerationsKeepTheirNull ()
        {
            var nullableType = typeof (global::KRPC.Test.Service.TestService.TestEnum?);
            Expression none = LinqExpression.Constant (null, nullableType);
            Expression y = LinqExpression.Constant (
                global::KRPC.Test.Service.TestService.TestEnum.Y, nullableType);
            var constantY = Expression.ConstantEnum ("TestService", "TestEnum", 1);
            Assert.IsTrue (Eval<bool> (Expression.Equal (y, constantY)));
            Assert.IsFalse (Eval<bool> (Expression.Equal (none, constantY)));
            Assert.IsTrue (Eval<bool> (Expression.NotEqual (constantY, none)));
            Assert.IsNull (Eval<global::KRPC.Test.Service.TestService.TestEnum?> (
                Expression.Conditional (Expression.ConstantBool (true), none, constantY)));
            // A position of the enumeration type takes the value
            var parameter = Expression.Parameter (
                "x", Type.EnumerationType ("TestService", "TestEnum"));
            var function = Expression.Lambda (new List<Expression> { parameter }, parameter);
            Assert.AreEqual (
                global::KRPC.Test.Service.TestService.TestEnum.Y,
                Eval<global::KRPC.Test.Service.TestService.TestEnum> (Expression.Invoke (
                    function, new Dictionary<string, Expression> { { "x", y } })));
        }

        [Test]
        public void PlainValueConvertsToANullablePosition ()
        {
            var parameter = Expression.Parameter ("x", new Type (typeof (long?)));
            var function = Expression.Lambda (new List<Expression> { parameter }, parameter);
            Assert.AreEqual (5L, Eval<long?> (Expression.Invoke (
                function, new Dictionary<string, Expression> { { "x", Expression.ConstantInt (5) } })));
            Expression none = LinqExpression.Constant (null, typeof (int?));
            Assert.IsNull (Eval<long?> (Expression.Invoke (
                function, new Dictionary<string, Expression> { { "x", none } })));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Invoke (function, new Dictionary<string, Expression> {
                    { "x", Expression.ConstantDouble (1) } }));
        }

        [Test]
        public void EqualityOfCollectionsWithDifferingNumericElements ()
        {
            Expression ints = LinqExpression.Constant (new List<int> { 1, 2 }, typeof (IList<int>));
            Expression doubles = LinqExpression.Constant (
                new List<double> { 1, 2 }, typeof (IList<double>));
            Expression ulongs = LinqExpression.Constant (
                new List<ulong> { 1, 2 }, typeof (IList<ulong>));
            Assert.IsTrue (Eval<bool> (Expression.Equal (ints, doubles)));
            Assert.IsTrue (Eval<bool> (Expression.Equal (ints, ulongs)));
            Assert.IsFalse (Eval<bool> (Expression.NotEqual (doubles, ints)));
            Expression intTuple = LinqExpression.Constant (System.Tuple.Create (1, 0));
            Assert.IsFalse (Eval<bool> (Expression.Equal (
                intTuple, LinqExpression.Constant (System.Tuple.Create (1.0, 0.5)))));
            Assert.IsTrue (Eval<bool> (Expression.Equal (
                intTuple, LinqExpression.Constant (System.Tuple.Create (1.0, 0.0)))));
            Assert.IsTrue (Eval<bool> (Expression.Equal (
                LinqExpression.Constant (System.Tuple.Create (1, 2.0)),
                LinqExpression.Constant (System.Tuple.Create (1.0, 2)))));
            Assert.IsTrue (Eval<bool> (Expression.Equal (
                LinqExpression.Constant (new HashSet<int> { 1, 2 }),
                LinqExpression.Constant (new HashSet<long> { 2, 1 }))));
            Assert.IsTrue (Eval<bool> (Expression.Equal (
                LinqExpression.Constant (
                    new Dictionary<int, int> { { 1, 2 } }, typeof (IDictionary<int, int>)),
                LinqExpression.Constant (
                    new Dictionary<long, double> { { 1, 2 } }, typeof (IDictionary<long, double>)))));
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.Equal (ints, Expression.ConstantInt (1)));
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.Equal (Expression.ConstantInt (1), Expression.ConstantBool (true)));
        }

        [Test]
        public void EqualityOfCollectionsWithElementsThatCannotBeEqual ()
        {
            Expression ints = LinqExpression.Constant (new List<int> { 1 }, typeof (IList<int>));
            Expression strings = LinqExpression.Constant (new List<string> { "a" }, typeof (IList<string>));
            Expression intSet = LinqExpression.Constant (new HashSet<int> { 1 });
            Expression intDictionary = LinqExpression.Constant (
                new Dictionary<int, int> { { 1, 1 } }, typeof (IDictionary<int, int>));
            Expression stringDictionary = LinqExpression.Constant (
                new Dictionary<int, string> { { 1, "a" } }, typeof (IDictionary<int, string>));
            Expression bytes = LinqExpression.Constant (new byte [] { 1 });
            foreach (var pair in new [] {
                    System.Tuple.Create (ints, strings),
                    System.Tuple.Create (ints, intSet),
                    System.Tuple.Create (ints, intDictionary),
                    System.Tuple.Create (intDictionary, stringDictionary),
                    System.Tuple.Create (bytes, ints),
                    System.Tuple.Create<Expression, Expression> (
                        LinqExpression.Constant (System.Tuple.Create (1)),
                        LinqExpression.Constant (System.Tuple.Create ("a"))),
                    System.Tuple.Create<Expression, Expression> (
                        LinqExpression.Constant (System.Tuple.Create (1)),
                        LinqExpression.Constant (System.Tuple.Create (1, 2))) }) {
                Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                    () => Expression.Equal (pair.Item1, pair.Item2));
                Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                    () => Expression.NotEqual (pair.Item2, pair.Item1));
            }
            Expression nullableInts = LinqExpression.Constant (
                new List<int?> { 1 }, typeof (IList<int?>));
            Assert.IsTrue (Eval<bool> (Expression.Equal (ints, nullableInts)));
        }

        [Test]
        public void EqualityOfBytesAndCollectionsComparesContents ()
        {
            Expression bytes = LinqExpression.Constant (new byte [] { 1, 2 });
            Assert.IsTrue (Eval<bool> (Expression.Equal (
                bytes, LinqExpression.Constant (new byte [] { 1, 2 }))));
            Assert.IsTrue (Eval<bool> (Expression.Equal (
                bytes, Expression.ConstantBytes (new byte [] { 1, 2 }))));
            Assert.IsTrue (Eval<bool> (Expression.NotEqual (
                bytes, Expression.ConstantBytes (new byte [] { 2, 1 }))));
            // Each call returns a new collection
            var mock = new Mock<global::KRPC.Test.Service.ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.EchoList (It.IsAny<IList<string>> ()))
                .Returns ((IList<string> x) => new List<string> (x));
            mock.Setup (x => x.EchoSet (It.IsAny<HashSet<int>> ()))
                .Returns ((HashSet<int> x) => new HashSet<int> (x));
            mock.Setup (x => x.EchoDictionary (It.IsAny<IDictionary<int, string>> ()))
                .Returns ((IDictionary<int, string> x) => new Dictionary<int, string> (x));
            mock.Setup (x => x.EchoTuple (It.IsAny<System.Tuple<int, bool>> ()))
                .Returns ((System.Tuple<int, bool> x) => System.Tuple.Create (x.Item1, x.Item2));
            global::KRPC.Test.Service.TestService.Service = mock.Object;
            System.Func<string, object, Expression> echo = (procedure, value) =>
                Expression.Call (BuildProcedureCall (procedure, new Argument (0, value)));
            var list = new List<string> { "a", "b" };
            Assert.IsTrue (Eval<bool> (Expression.Equal (
                echo ("EchoList", list), echo ("EchoList", list))));
            Assert.IsTrue (Eval<bool> (Expression.NotEqual (
                echo ("EchoList", list), echo ("EchoList", new List<string> { "b", "a" }))));
            Assert.IsTrue (Eval<bool> (Expression.Equal (
                echo ("EchoSet", new HashSet<int> { 1, 2 }),
                echo ("EchoSet", new HashSet<int> { 2, 1 }))));
            var dictionary = new Dictionary<int, string> { { 1, "a" } };
            Assert.IsTrue (Eval<bool> (Expression.Equal (
                echo ("EchoDictionary", dictionary), echo ("EchoDictionary", dictionary))));
            Assert.IsFalse (Eval<bool> (Expression.Equal (
                echo ("EchoDictionary", dictionary),
                echo ("EchoDictionary", new Dictionary<int, string> { { 1, "b" } }))));
            var tuple = System.Tuple.Create (1, true);
            Assert.IsTrue (Eval<bool> (Expression.Equal (
                echo ("EchoTuple", tuple), echo ("EchoTuple", tuple))));
        }

        static global::KRPC.Test.Service.TestService.TestStruct TestStruct (int value)
        {
            return new global::KRPC.Test.Service.TestService.TestStruct {
                IntField = value,
                StringField = "foo",
                ListField = new List<string> { "a" }
            };
        }

        [Test]
        public void EqualityOfStructures ()
        {
            // Each structure holds its own list, equal in content
            Expression a = LinqExpression.Constant (TestStruct (1));
            Expression b = LinqExpression.Constant (TestStruct (1));
            Expression c = LinqExpression.Constant (TestStruct (2));
            Assert.IsTrue (Eval<bool> (Expression.Equal (a, a)));
            Assert.IsTrue (Eval<bool> (Expression.Equal (a, b)));
            Assert.IsFalse (Eval<bool> (Expression.Equal (a, c)));
            Assert.IsTrue (Eval<bool> (Expression.NotEqual (a, c)));
        }

        [Test]
        public void ConstantsAreShared ()
        {
            // A compiled function mentions the same literals over and over, so each
            // value gets one entry in the object store rather than one per mention
            var store = global::KRPC.Service.ObjectStore.Instance;
            Assert.AreEqual (store.AddInstance (Expression.ConstantInt (1)),
                             store.AddInstance (Expression.ConstantInt (1)));
            Assert.AreEqual (store.AddInstance (Expression.ConstantString ("a")),
                             store.AddInstance (Expression.ConstantString ("a")));
            Assert.AreNotEqual (store.AddInstance (Expression.ConstantInt (1)),
                                store.AddInstance (Expression.ConstantInt (2)));
            // Constants of equal value but differing type stay distinct
            Assert.AreNotEqual (store.AddInstance (Expression.ConstantInt (1)),
                                store.AddInstance (Expression.ConstantDouble (1)));
            Assert.AreNotEqual (store.AddInstance (Expression.ConstantDouble (1)),
                                store.AddInstance (Expression.ConstantFloat (1)));
            // Negative zero is a distinct constant, though it compares equal to zero
            Assert.AreNotEqual (store.AddInstance (Expression.ConstantDouble (0)),
                                store.AddInstance (Expression.ConstantDouble (-0.0)));
            Assert.AreNotEqual (store.AddInstance (Expression.ConstantFloat (0)),
                                store.AddInstance (Expression.ConstantFloat (-0.0f)));
        }

        [Test]
        public void Equal ()
        {
            Assert.IsTrue (Eval<bool> (Expression.Equal (
                Expression.ConstantInt (1), Expression.ConstantInt (1))));
            Assert.IsFalse (Eval<bool> (Expression.Equal (
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
            Assert.IsTrue (Eval<bool> (Expression.Equal (
                Expression.ConstantString ("foo"), Expression.ConstantString ("foo"))));
            Assert.IsFalse (Eval<bool> (Expression.Equal (
                Expression.ConstantString ("foo"), Expression.ConstantString ("bar"))));
        }

        [Test]
        public void NotEqual ()
        {
            Assert.IsFalse (Eval<bool> (Expression.NotEqual (
                Expression.ConstantInt (1), Expression.ConstantInt (1))));
            Assert.IsTrue (Eval<bool> (Expression.NotEqual (
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
            Assert.IsFalse (Eval<bool> (Expression.NotEqual (
                Expression.ConstantString ("foo"), Expression.ConstantString ("foo"))));
            Assert.IsTrue (Eval<bool> (Expression.NotEqual (
                Expression.ConstantString ("foo"), Expression.ConstantString ("bar"))));
        }

        [Test]
        public void GreaterThan ()
        {
            Assert.IsFalse (Eval<bool> (Expression.GreaterThan (
                Expression.ConstantInt (1), Expression.ConstantInt (1))));
            Assert.IsFalse (Eval<bool> (Expression.GreaterThan (
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
            Assert.IsTrue (Eval<bool> (Expression.GreaterThan (
                Expression.ConstantInt (2), Expression.ConstantInt (1))));
        }

        [Test]
        public void GreaterThanOrEqual ()
        {
            Assert.IsTrue (Eval<bool> (Expression.GreaterThanOrEqual (
                Expression.ConstantInt (1), Expression.ConstantInt (1))));
            Assert.IsFalse (Eval<bool> (Expression.GreaterThanOrEqual (
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
            Assert.IsTrue (Eval<bool> (Expression.GreaterThanOrEqual (
                Expression.ConstantInt (2), Expression.ConstantInt (1))));
        }

        [Test]
        public void LessThan ()
        {
            Assert.IsFalse (Eval<bool> (Expression.LessThan (
                Expression.ConstantInt (1), Expression.ConstantInt (1))));
            Assert.IsTrue (Eval<bool> (Expression.LessThan (
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
            Assert.IsFalse (Eval<bool> (Expression.LessThan (
                Expression.ConstantInt (2), Expression.ConstantInt (1))));
        }

        [Test]
        public void LessThanOrEqual ()
        {
            Assert.IsTrue (Eval<bool> (Expression.LessThanOrEqual (
                Expression.ConstantInt (1), Expression.ConstantInt (1))));
            Assert.IsTrue (Eval<bool> (Expression.LessThanOrEqual (
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
            Assert.IsFalse (Eval<bool> (Expression.LessThanOrEqual (
                Expression.ConstantInt (2), Expression.ConstantInt (1))));
        }

        [Test]
        public void And ()
        {
            Assert.IsTrue (Eval<bool> (Expression.And (
                Expression.ConstantBool (true), Expression.ConstantBool (true))));
            Assert.IsFalse (Eval<bool> (Expression.And (
                Expression.ConstantBool (true), Expression.ConstantBool (false))));
            Assert.IsFalse (Eval<bool> (Expression.And (
                Expression.ConstantBool (false), Expression.ConstantBool (true))));
            Assert.IsFalse (Eval<bool> (Expression.And (
                Expression.ConstantBool (false), Expression.ConstantBool (false))));
        }

        [Test]
        public void Or ()
        {
            Assert.IsTrue (Eval<bool> (Expression.Or (
                Expression.ConstantBool (true), Expression.ConstantBool (true))));
            Assert.IsTrue (Eval<bool> (Expression.Or (
                Expression.ConstantBool (true), Expression.ConstantBool (false))));
            Assert.IsTrue (Eval<bool> (Expression.Or (
                Expression.ConstantBool (false), Expression.ConstantBool (true))));
            Assert.IsFalse (Eval<bool> (Expression.Or (
                Expression.ConstantBool (false), Expression.ConstantBool (false))));
        }

        [Test]
        public void ConditionalOperatorsShortCircuit ()
        {
            // Dividing by zero throws, so the second operand shows whether it was
            // evaluated at all
            var fails = Expression.Equal (
                Expression.Divide (Expression.ConstantInt (1), Expression.ConstantInt (0)),
                Expression.ConstantInt (0));
            Assert.IsFalse (Eval<bool> (
                Expression.ConditionalAnd (Expression.ConstantBool (false), fails)));
            Assert.IsTrue (Eval<bool> (
                Expression.ConditionalOr (Expression.ConstantBool (true), fails)));
            Assert.Throws<System.DivideByZeroException> (() => Eval<bool> (
                Expression.And (Expression.ConstantBool (false), fails)));
            Assert.Throws<System.DivideByZeroException> (() => Eval<bool> (
                Expression.Or (Expression.ConstantBool (true), fails)));
            Assert.IsTrue (Eval<bool> (Expression.ConditionalAnd (
                Expression.ConstantBool (true), Expression.ConstantBool (true))));
            Assert.IsFalse (Eval<bool> (Expression.ConditionalOr (
                Expression.ConstantBool (false), Expression.ConstantBool (false))));
        }

        [Test]
        public void ExclusiveOr ()
        {
            Assert.IsFalse (Eval<bool> (Expression.ExclusiveOr (
                Expression.ConstantBool (true), Expression.ConstantBool (true))));
            Assert.IsTrue (Eval<bool> (Expression.ExclusiveOr (
                Expression.ConstantBool (true), Expression.ConstantBool (false))));
            Assert.IsTrue (Eval<bool> (Expression.ExclusiveOr (
                Expression.ConstantBool (false), Expression.ConstantBool (true))));
            Assert.IsFalse (Eval<bool> (Expression.ExclusiveOr (
                Expression.ConstantBool (false), Expression.ConstantBool (false))));
        }

        [Test]
        public void Not ()
        {
            Assert.IsFalse (Eval<bool> (Expression.Not (Expression.ConstantBool (true))));
            Assert.IsTrue (Eval<bool> (Expression.Not (Expression.ConstantBool (false))));
        }

        [Test]
        public void Negate ()
        {
            Assert.AreEqual (-5, Eval<int> (Expression.Negate (Expression.ConstantInt (5))));
            Assert.AreEqual (-1.5, Eval<double> (Expression.Negate (Expression.ConstantDouble (1.5))));
            Assert.AreEqual (-5L, Eval<long> (Expression.Negate (
                Expression.Cast (Expression.ConstantInt (5), Type.Long ()))));
            Assert.AreEqual (-5L, Eval<long> (Expression.Negate (
                Expression.Cast (Expression.ConstantInt (5), Type.UInt ()))));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Negate (Expression.Cast (Expression.ConstantInt (1), Type.ULong ())));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Negate (Expression.ConstantString ("a")));
        }

        [Test]
        public void Add ()
        {
            Assert.AreEqual (3, Eval<int> (Expression.Add (
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
        }

        [Test]
        public void Subtract ()
        {
            Assert.AreEqual (-1, Eval<int> (Expression.Subtract (
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
        }

        [Test]
        public void Multiply ()
        {
            Assert.AreEqual (6, Eval<int> (Expression.Multiply (
                Expression.ConstantInt (2), Expression.ConstantInt (3))));
            Assert.AreEqual (2.1f * 3.2f, Eval<float> (Expression.Multiply (
                Expression.ConstantFloat (2.1f), Expression.ConstantFloat (3.2f))));
        }

        [Test]
        public void Divide ()
        {
            Assert.AreEqual (0, Eval<int> (Expression.Divide (
                Expression.ConstantInt (2), Expression.ConstantInt (3))));
            Assert.AreEqual (2f / 3f, Eval<float> (Expression.Divide (
                Expression.ConstantFloat (2), Expression.ConstantFloat (3))));
        }

        [Test]
        public void Modulo ()
        {
            Assert.AreEqual (2, Eval<int> (Expression.Modulo (
                Expression.ConstantInt (2), Expression.ConstantInt (3))));
            Assert.AreEqual (0, Eval<int> (Expression.Modulo (
                Expression.ConstantInt (2), Expression.ConstantInt (1))));
            Assert.AreEqual (0, Eval<int> (Expression.Modulo (
                Expression.ConstantInt (6), Expression.ConstantInt (3))));
            Assert.AreEqual (1, Eval<int> (Expression.Modulo (
                Expression.ConstantInt (6), Expression.ConstantInt (5))));
        }

        [Test]
        public void Power ()
        {
            Assert.AreEqual (8, Eval<int> (Expression.Power (
                Expression.ConstantInt (2), Expression.ConstantInt (3))));
            // Integer powers are exact and wrap on overflow
            Assert.AreEqual (0, Eval<int> (Expression.Power (
                Expression.ConstantInt (2), Expression.ConstantInt (32))));
            Assert.AreEqual (-8, Eval<int> (Expression.Power (
                Expression.ConstantInt (-2), Expression.ConstantInt (3))));
            Assert.AreEqual (7450580596923828125L, Eval<long> (Expression.Power (
                Expression.ConstantLong (5), Expression.ConstantLong (27))));
            Assert.AreEqual (12157665459056928801UL, Eval<ulong> (Expression.Power (
                Expression.ConstantULong (3), Expression.ConstantULong (40))));
            Assert.AreEqual (0, Eval<int> (Expression.Power (
                Expression.ConstantInt (2), Expression.ConstantInt (-1))));
            Assert.AreEqual (System.Math.Pow (2.1, 1.2), Eval<double> (Expression.Power (
                Expression.ConstantDouble (2.1), Expression.ConstantDouble (1.2))));
            Assert.AreEqual (System.Math.Pow (2.1, 1.2f), Eval<double> (Expression.Power (
                Expression.ConstantDouble (2.1), Expression.ConstantFloat (1.2f))));
            Assert.AreEqual ((float)System.Math.Pow (2.1f, 1.2f), Eval<float> (Expression.Power (
                Expression.ConstantFloat (2.1f), Expression.ConstantFloat (1.2f))));
            // Mixed-type operands promote, so a fractional exponent of an int gives
            // a double
            Assert.AreEqual (System.Math.Pow (2, 0.5), Eval<double> (Expression.Power (
                Expression.ConstantInt (2), Expression.ConstantDouble (0.5))));
            Assert.AreEqual (System.Math.Pow (2.5, 2), Eval<double> (Expression.Power (
                Expression.ConstantDouble (2.5), Expression.ConstantInt (2))));
        }

        [Test]
        public void Conditional ()
        {
            Assert.AreEqual (1, Eval<int> (Expression.Conditional (
                Expression.ConstantBool (true),
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
            Assert.AreEqual (2, Eval<int> (Expression.Conditional (
                Expression.ConstantBool (false),
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
            // Branches of differing numeric types promote to a common type
            Assert.AreEqual (2.5, Eval<double> (Expression.Conditional (
                Expression.ConstantBool (false),
                Expression.ConstantInt (1), Expression.ConstantDouble (2.5))));
            Assert.AreEqual ("a", Eval<string> (Expression.Conditional (
                Expression.ConstantBool (true),
                Expression.ConstantString ("a"), Expression.ConstantString ("b"))));
        }

        [Test]
        public void ConditionalWithBranchesOfNoCommonType ()
        {
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Conditional (
                    Expression.ConstantBool (true),
                    Expression.ConstantString ("a"), Expression.ConstantInt (1)));
            Assert.AreEqual (
                "The branches of Conditional must be of the same type, or numbers with a " +
                "common type, got System.String and System.Int32", exn.Message);
            // A branch with no value has no type in common with a value
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Conditional (
                    Expression.ConstantBool (true),
                    Expression.Call (BuildProcedureCall ("ProcedureNoArgsNoReturn")),
                    Expression.ConstantInt (1)));
        }

        [Test]
        public void NumericPromotion ()
        {
            // Mixed-type arithmetic promotes to the wider operand type
            Assert.AreEqual (5.5, Eval<double> (Expression.Multiply (
                Expression.ConstantDouble (2.75), Expression.ConstantInt (2))));
            Assert.AreEqual (5.5, Eval<double> (Expression.Multiply (
                Expression.ConstantInt (2), Expression.ConstantDouble (2.75))));
            Assert.AreEqual (3.5f, Eval<float> (Expression.Add (
                Expression.ConstantFloat (1.5f), Expression.ConstantInt (2))));
            Assert.AreEqual (2.5, Eval<double> (Expression.Divide (
                Expression.ConstantDouble (5), Expression.ConstantFloat (2f))));
            Assert.AreEqual (1.5, Eval<double> (Expression.Modulo (
                Expression.ConstantDouble (7.5), Expression.ConstantInt (2))));
            Assert.AreEqual (-1.5, Eval<double> (Expression.Subtract (
                Expression.ConstantInt (1), Expression.ConstantDouble (2.5))));
        }

        [Test]
        public void NumericPromotionComparison ()
        {
            Assert.IsTrue (Eval<bool> (Expression.GreaterThan (
                Expression.ConstantDouble (2.5), Expression.ConstantInt (2))));
            Assert.IsFalse (Eval<bool> (Expression.GreaterThan (
                Expression.ConstantInt (2), Expression.ConstantDouble (2.5))));
            Assert.IsTrue (Eval<bool> (Expression.LessThanOrEqual (
                Expression.ConstantInt (2), Expression.ConstantFloat (2f))));
            Assert.IsTrue (Eval<bool> (Expression.Equal (
                Expression.ConstantInt (2), Expression.ConstantDouble (2))));
            Assert.IsTrue (Eval<bool> (Expression.NotEqual (
                Expression.ConstantFloat (2.5f), Expression.ConstantInt (2))));
        }

        [Test]
        public void NumericPromotionIntegers ()
        {
            var longValue = Expression.Cast (Expression.ConstantInt (2), Type.Long ());
            var uintValue = Expression.Cast (Expression.ConstantInt (3), Type.UInt ());
            var ulongValue = Expression.Cast (Expression.ConstantInt (4), Type.ULong ());
            Assert.AreEqual (5L, Eval<long> (Expression.Add (
                longValue, Expression.ConstantInt (3))));
            // An unsigned integer and a signed one of the same width promote to the
            // wider signed type
            Assert.AreEqual (5L, Eval<long> (Expression.Add (
                uintValue, Expression.ConstantInt (2))));
            Assert.AreEqual (7UL, Eval<ulong> (Expression.Add (ulongValue, uintValue)));
        }

        [Test]
        public void NumericPromotionWithoutCommonType ()
        {
            var longValue = Expression.Cast (Expression.ConstantInt (2), Type.Long ());
            var ulongValue = Expression.Cast (Expression.ConstantInt (4), Type.ULong ());
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Add (ulongValue, Expression.ConstantInt (1)));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Add (ulongValue, longValue));
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.LessThan (longValue, ulongValue));
        }

        [Test]
        public void LeftShift ()
        {
            Assert.AreEqual (1, Eval<int> (Expression.LeftShift (
                Expression.ConstantInt (1), Expression.ConstantInt (0))));
            Assert.AreEqual (2, Eval<int> (Expression.LeftShift (
                Expression.ConstantInt (1), Expression.ConstantInt (1))));
            Assert.AreEqual (4, Eval<int> (Expression.LeftShift (
                Expression.ConstantInt (1), Expression.ConstantInt (2))));
        }

        [Test]
        public void RightShift ()
        {
            Assert.AreEqual (1, Eval<int> (Expression.RightShift (
                Expression.ConstantInt (1), Expression.ConstantInt (0))));
            Assert.AreEqual (1, Eval<int> (Expression.RightShift (
                Expression.ConstantInt (2), Expression.ConstantInt (1))));
            Assert.AreEqual (1, Eval<int> (Expression.RightShift (
                Expression.ConstantInt (4), Expression.ConstantInt (2))));
        }

        [Test]
        public void IntegerDivisionOfTheSmallestIntegerByMinusOneWraps ()
        {
            Assert.AreEqual (int.MinValue, Eval<int> (Expression.Divide (
                Expression.ConstantInt (int.MinValue), Expression.ConstantInt (-1))));
            Assert.AreEqual (0, Eval<int> (Expression.Modulo (
                Expression.ConstantInt (int.MinValue), Expression.ConstantInt (-1))));
            Assert.AreEqual (long.MinValue, Eval<long> (Expression.Divide (
                Expression.ConstantLong (long.MinValue), Expression.ConstantInt (-1))));
            Assert.AreEqual (-3, Eval<int> (Expression.Divide (
                Expression.ConstantInt (7), Expression.ConstantInt (-2))));
            Assert.AreEqual (1, Eval<int> (Expression.Modulo (
                Expression.ConstantInt (7), Expression.ConstantInt (-2))));
        }

        [Test]
        public void BitwiseOperatorsPromoteTheirOperands ()
        {
            Assert.AreEqual (3L, Eval<long> (Expression.Or (
                Expression.ConstantInt (1), Expression.ConstantLong (2))));
            Assert.AreEqual (1L, Eval<long> (Expression.And (
                Expression.ConstantLong (3), Expression.ConstantInt (1))));
            Assert.AreEqual (2L, Eval<long> (Expression.ExclusiveOr (
                Expression.ConstantInt (1), Expression.ConstantLong (3))));
            Assert.AreEqual (1L << 40, Eval<long> (Expression.LeftShift (
                Expression.ConstantLong (1), Expression.ConstantLong (40))));
            Assert.AreEqual (1UL, Eval<ulong> (Expression.RightShift (
                Expression.ConstantULong (2), Expression.ConstantUInt (1))));
        }

        [Test]
        public void Cast ()
        {
            Assert.AreEqual ((double)1, Eval<double> (Expression.Cast (Expression.ConstantInt (1), Type.Double ())));
            Assert.AreEqual ((float)1, Eval<float> (Expression.Cast (Expression.ConstantInt (1), Type.Float ())));
            Assert.AreEqual (1, Eval<int> (Expression.Cast (Expression.ConstantDouble (1.1), Type.Int ())));
            Assert.AreEqual (1, Eval<int> (Expression.Cast (Expression.ConstantFloat (1.1f), Type.Int ())));
        }

        static Expression ListOfInts (params int[] values)
        {
            var mock = new Mock<global::KRPC.Test.Service.ITestService> (MockBehavior.Strict);
            mock.Setup (x => x.ListDefault (It.IsAny<IList<int>> ())).Returns ((IList<int> x) => x);
            global::KRPC.Test.Service.TestService.Service = mock.Object;
            return Expression.Call (BuildProcedureCall ("ListDefault", new Argument (0, values.ToList ())));
        }

        [Test]
        public void CastCollection ()
        {
            var widened = Expression.Cast (ListOfInts (1, 2), Type.ListType (Type.Double ()));
            CollectionAssert.AreEqual (new [] { 1.0, 2.0 }, Eval<IList<double>> (widened));
            // The elements narrow as a cast of each would
            Expression doubles = LinqExpression.Constant (new List<double> { 1.5, -2.5 }, typeof (IList<double>));
            CollectionAssert.AreEqual (new [] { 1, -2 }, Eval<IList<int>> (
                Expression.Cast (doubles, Type.ListType (Type.Int ()))));
            Expression tuple = LinqExpression.Constant (System.Tuple.Create (1.5, 3L));
            Assert.AreEqual (System.Tuple.Create (1, 3u), Eval<System.Tuple<int, uint>> (
                Expression.Cast (tuple, Type.TupleType (new List<Type> { Type.Int (), Type.UInt () }))));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Cast (ListOfInts (1, 2), Type.SetType (Type.Int ())));
            StringAssert.Contains ("Cannot cast", exn.Message);
        }

        [Test]
        public void CastLazySequence ()
        {
            Expression lazy = LinqExpression.Constant (Enumerable.Range (1, 2), typeof (IEnumerable<int>));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Cast (lazy, Type.ListType (Type.Int ())));
            StringAssert.Contains ("Use ToList or ToSet", exn.Message);
        }

        [Test]
        public void NestedNullableValueTypesMustMatch ()
        {
            Expression nullables = LinqExpression.Constant (
                new List<int?> { 1, null }, typeof (IList<int?>));
            const string hint = "The nullability of a value type inside a collection or tuple must match";
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Cast (nullables, Type.ListType (Type.Int ())));
            StringAssert.Contains (hint, exn.Message);
            var x = Expression.Parameter ("x", Type.ListType (Type.Int ()));
            var function = Expression.Lambda (new List<Expression> { x }, x);
            exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Invoke (function, new Dictionary<string, Expression> { { "x", nullables } }));
            StringAssert.Contains (hint, exn.Message);
            // A plain value widens to a nullable one
            var widened = Expression.Cast (ListOfInts (1, 2), Type.ListType (Type.NullableType (Type.Int ())));
            CollectionAssert.AreEqual (new int? [] { 1, 2 }, Eval<IList<int?>> (widened));
            // A mismatch of another kind gives no hint
            exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Cast (ListOfInts (1, 2), Type.ListType (Type.String ())));
            StringAssert.DoesNotContain (hint, exn.Message);
        }

        [Test]
        public void NullTupleIsWidened ()
        {
            var ints = Type.TupleType (new List<Type> { Type.Int () });
            var longs = Type.TupleType (new List<Type> { Type.Long () });
            Assert.IsTrue (Eval<bool> (Expression.Equal (
                Expression.ConstantNull (ints), Expression.ConstantNull (longs))));
            var y = Expression.Parameter ("y", Type.NullableType (
                Type.TupleType (new List<Type> { Type.Double () })));
            var isNull = Expression.Lambda (new List<Expression> { y }, Expression.IsNull (y));
            Assert.IsTrue (Eval<bool> (Expression.Invoke (
                isNull, new Dictionary<string, Expression> { { "y", Expression.ConstantNull (ints) } })));
        }

        [Test]
        public void Invoke ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var y = Expression.Parameter ("y", Type.Int ());
            var func = Expression.Lambda (
                new List<Expression> { x, y },
                Expression.Divide (x, y));
            var call = Expression.Invoke (func, new Dictionary<string, Expression> {
                { "x", Expression.ConstantInt (6) }, { "y", Expression.ConstantInt (3) }});
            Assert.AreEqual (2, Eval<int> (call));
        }

        [Test]
        public void InvokeArgumentMustHaveAValue ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var function = Expression.Lambda (new List<Expression> { x }, x);
            var statement = Expression.Call (BuildProcedureCall ("ProcedureNoArgsNoReturn"));
            var exn = Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.Invoke (function, new Dictionary<string, Expression> { { "x", statement } }));
            Assert.AreEqual ("The argument for parameter x is an expression with no value", exn.Message);
        }

        [Test]
        public void InvokeArgumentsAreWidenedNotNarrowed ()
        {
            var x = Expression.Parameter ("x", Type.Double ());
            var widening = Expression.Lambda (
                new List<Expression> { x },
                Expression.Multiply (x, Expression.ConstantDouble (2)));
            Assert.AreEqual (6.0, Eval<double> (Expression.Invoke (
                widening, new Dictionary<string, Expression> {
                    { "x", Expression.ConstantInt (3) }})));

            var count = Expression.Parameter ("count", Type.Int ());
            var narrowing = Expression.Lambda (new List<Expression> { count }, count);
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Invoke (narrowing, new Dictionary<string, Expression> {
                    { "count", Expression.ConstantDouble (1.5) }}));
            StringAssert.Contains ("No implicit conversion", exn.Message);
        }

        [Test]
        public void InvokeArgumentCollectionsAreWidened ()
        {
            var x = Expression.Parameter ("x", Type.ListType (Type.Long ()));
            var function = Expression.Lambda (new List<Expression> { x }, x);
            var call = Expression.Invoke (
                function, new Dictionary<string, Expression> { { "x", ListOfInts (1, 2) } });
            CollectionAssert.AreEqual (new [] { 1L, 2L }, Eval<IList<long>> (call));
        }

        [Test]
        public void ParameterOutsideItsFunctionIsRejected ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var exn = Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => global::KRPC.Service.KRPC.KRPC.EventCondition (
                    Expression.GreaterThan (x, Expression.ConstantInt (0))));
            StringAssert.Contains ("x is used outside the function or block that declares it", exn.Message);
            var function = Expression.Lambda (
                new List<Expression> { x }, Expression.GreaterThan (x, Expression.ConstantInt (0)));
            var condition = global::KRPC.Service.KRPC.KRPC.EventCondition (Expression.Invoke (
                function, new Dictionary<string, Expression> { { "x", Expression.ConstantInt (1) } }));
            Assert.IsTrue (condition ());
        }

        [Test]
        public void KRPCServiceIsNotAvailableToAFunction ()
        {
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Call (new ProcedureCall ("KRPC", "GetStatus")));
            var byId = new ProcedureCall (
                string.Empty, global::KRPC.Service.Services.Instance.Signatures ["KRPC"].Id, "GetStatus", 0);
            Assert.Throws<global::KRPC.Service.KRPC.InvalidOperationException> (
                () => Expression.Call (byId));
            var function = Expression.ConstantInt (1);
            var id = global::KRPC.Service.ObjectStore.Instance.AddInstance (function);
            Assert.Throws<global::KRPC.Service.KRPC.ArgumentException> (
                () => Expression.ConstantObject (id));
        }
    }
}
