using System;
using System.Collections.Generic;
using KRPC.Service;
using KRPC.Service.KRPC;
using KRPC.Service.Messages;
using NUnit.Framework;
using LinqExpression = System.Linq.Expressions.Expression;
using Type = KRPC.Service.KRPC.Type;

namespace KRPC.Test.Service.KRPC
{
    /// <summary>
    /// Golden tests of the expression trees the function API builds.
    /// </summary>
    [TestFixture]
    public class ExpressionTreePrinterTest
    {
        static ProcedureCall BuildProcedureCall (string procedure, params Argument[] args)
        {
            var call = new ProcedureCall ("TestService", procedure);
            foreach (var arg in args)
                call.Arguments.Add (arg);
            return call;
        }

        static void Check (string expected, Expression expression)
        {
            Assert.AreEqual (expected, ExpressionTreePrinter.Print (expression));
        }

        [Test]
        public void NumericPromotion ()
        {
            Check (
                "Add<System.Double>\n" +
                "  Convert<System.Double>\n" +
                "    Constant<System.Int32> 1\n" +
                "  Constant<System.Double> 2.5\n",
                Expression.Add (Expression.ConstantInt (1), Expression.ConstantDouble (2.5)));
        }

        [Test]
        public void NullConstant ()
        {
            Check ("Constant<System.String> null\n", Expression.ConstantNull (Type.String ()));
            Check ("Constant<System.Nullable<System.Int32>> null\n", Expression.ConstantNull (Type.Int ()));
        }

        [Test]
        public void StateVariable ()
        {
            Check (
                "MemberAccess<System.Int32> state count#0\n",
                Expression.StateVariable ("count", Expression.ConstantInt (0)));
            // Two state variables of the same name are told apart
            var first = Expression.StateVariable ("count", Expression.ConstantInt (0));
            var second = Expression.StateVariable ("count", Expression.ConstantInt (0));
            Check (
                "Assign<System.Int32>\n" +
                "  MemberAccess<System.Int32> state count#0\n" +
                "  MemberAccess<System.Int32> state count#1\n",
                Expression.Assign (first, second));
        }

        [Test]
        public void CallValueReturn ()
        {
            // A value typed return needs no null check after the direct call
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            Check (
                "Block<System.Int32>\n" +
                "  Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene\n" +
                "    Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_get_IntProperty\n" +
                "  Call<System.Int32> KRPC.Test.Service.TestService.TestClass.get_IntProperty\n" +
                "    Constant<KRPC.Test.Service.TestService.TestClass> <KRPC.Test.Service.TestService.TestClass>\n",
                Expression.Call (BuildProcedureCall ("TestClass_get_IntProperty", new Argument (0, obj))));
        }

        [Test]
        public void CallReferenceReturn ()
        {
            // A non-nullable reference typed return is checked for null on every evaluation
            var obj = new global::KRPC.Test.Service.TestService.TestClass ("foo");
            Check (
                "Block<System.String> (returnValue#0: System.String)\n" +
                "  Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene\n" +
                "    Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_FloatToString\n" +
                "  Assign<System.String>\n" +
                "    Parameter<System.String> returnValue#0\n" +
                "    Call<System.String> KRPC.Test.Service.TestService.TestClass.FloatToString\n" +
                "      Constant<KRPC.Test.Service.TestService.TestClass> <KRPC.Test.Service.TestService.TestClass>\n" +
                "      Constant<System.Single> 0.5\n" +
                "  Call<System.Void> static KRPC.Service.Services.CheckExpressionReturnValue\n" +
                "    Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_FloatToString\n" +
                "    Parameter<System.String> returnValue#0\n" +
                "  Parameter<System.String> returnValue#0\n",
                Expression.Call (BuildProcedureCall (
                    "TestClass_FloatToString", new Argument (0, obj), new Argument (1, 0.5f))));
        }

        [Test]
        public void CallVoidReturn ()
        {
            Check (
                "Block<System.Void>\n" +
                "  Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene\n" +
                "    Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.ProcedureSingleOptionalArgNoReturn\n" +
                "  Call<System.Void> static KRPC.Test.Service.TestService.ProcedureSingleOptionalArgNoReturn\n" +
                "    Constant<System.String> \"foo\"\n",
                Expression.Call (BuildProcedureCall ("ProcedureSingleOptionalArgNoReturn")));
        }

        [Test]
        public void CallPerElement ()
        {
            // The function parameter provides the instance argument of the call
            var param = Expression.Parameter ("x", Type.ClassType ("TestService", "TestClass"));
            var call = Expression.CallWithArguments (
                BuildProcedureCall ("TestClass_FloatToString", new Argument (1, 0.5f)),
                new Dictionary<int, Expression> { { 0, param } });
            Check (
                "Lambda<System.Func<KRPC.Test.Service.TestService.TestClass, System.String>> (x#0)\n" +
                "  Label<System.String> label#0\n" +
                "    Block<System.String> (returnValue#1: System.String)\n" +
                "      Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene\n" +
                "        Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_FloatToString\n" +
                "      Assign<System.String>\n" +
                "        Parameter<System.String> returnValue#1\n" +
                "        Call<System.String> KRPC.Test.Service.TestService.TestClass.FloatToString\n" +
                "          Call<KRPC.Test.Service.TestService.TestClass> static KRPC.Service.Services.CheckExpressionArgument\n" +
                "            Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_FloatToString\n" +
                "            Constant<System.Int32> 0\n" +
                "            Parameter<KRPC.Test.Service.TestService.TestClass> x#0\n" +
                "          Constant<System.Single> 0.5\n" +
                "      Call<System.Void> static KRPC.Service.Services.CheckExpressionReturnValue\n" +
                "        Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_FloatToString\n" +
                "        Parameter<System.String> returnValue#1\n" +
                "      Parameter<System.String> returnValue#1\n",
                Expression.Lambda (new List<Expression> { param }, call));
        }

        [Test]
        public void Statements ()
        {
            // While desugars to a loop over a conditional; the break marker in the
            // body is rewritten to a goto targeting the loop's break label
            var i = Expression.Variable ("i", Type.Int ());
            var body = Expression.Block (new List<Expression> {
                Expression.IfThen (
                    Expression.GreaterThan (i, Expression.ConstantInt (10)),
                    Expression.Break ()),
                Expression.Assign (i, Expression.Add (i, Expression.ConstantInt (1)))
            });
            Check (
                "Block<System.Int32> (i#0: System.Int32)\n" +
                "  Assign<System.Int32>\n" +
                "    Parameter<System.Int32> i#0\n" +
                "    Constant<System.Int32> 0\n" +
                "  Loop<System.Void> break:label#0 continue:label#1\n" +
                "    Conditional<System.Void>\n" +
                "      Constant<System.Boolean> true\n" +
                "      Block<System.Void>\n" +
                "        Block<System.Int32>\n" +
                "          Conditional<System.Void>\n" +
                "            GreaterThan<System.Boolean>\n" +
                "              Parameter<System.Int32> i#0\n" +
                "              Constant<System.Int32> 10\n" +
                "            Goto<System.Void> Break label#0\n" +
                "            Default<System.Void>\n" +
                "          Assign<System.Int32>\n" +
                "            Parameter<System.Int32> i#0\n" +
                "            Add<System.Int32>\n" +
                "              Parameter<System.Int32> i#0\n" +
                "              Constant<System.Int32> 1\n" +
                "      Goto<System.Void> Break label#0\n" +
                "  Parameter<System.Int32> i#0\n",
                Expression.BlockWithVariables (
                    new List<Expression> { i },
                    new List<Expression> {
                        Expression.Assign (i, Expression.ConstantInt (0)),
                        Expression.While (Expression.ConstantBool (true), body),
                        i
                    }));
        }

        [Test]
        public void FunctionWithReturn ()
        {
            // An early return becomes a marker, rewritten to a goto. The function's
            // final return statement compiles to a trailing value, which types the
            // label the markers jump to
            var x = Expression.Parameter ("x", Type.Int ());
            var body = Expression.Block (new List<Expression> {
                Expression.IfThen (
                    Expression.LessThan (x, Expression.ConstantInt (0)),
                    Expression.Return (Expression.ConstantInt (0))),
                x
            });
            Check (
                "Lambda<System.Func<System.Int32, System.Int32>> (x#0)\n" +
                "  Label<System.Int32> label#0\n" +
                "    Block<System.Int32>\n" +
                "      Conditional<System.Void>\n" +
                "        LessThan<System.Boolean>\n" +
                "          Parameter<System.Int32> x#0\n" +
                "          Constant<System.Int32> 0\n" +
                "        Block<System.Void>\n" +
                "          Goto<System.Int32> Return label#0\n" +
                "            Constant<System.Int32> 0\n" +
                "        Default<System.Void>\n" +
                "      Parameter<System.Int32> x#0\n",
                Expression.Lambda (new List<Expression> { x }, body));
        }

        [Test]
        public void ForEachLoopOverAList ()
        {
            // ForEach over a list desugars to a loop over its positions
            var x = Expression.Variable ("x", Type.Int ());
            var total = Expression.Variable ("total", Type.Int ());
            var list = Expression.CreateList (new List<Expression> {
                Expression.ConstantInt (1),
                Expression.ConstantInt (2)
            });
            Check (
                "Block<System.Int32> (total#0: System.Int32, x#1: System.Int32)\n" +
                "  Block<System.Void> (list#2: System.Collections.Generic.IList<System.Int32>, index#3: System.Int32)\n" +
                "    Assign<System.Collections.Generic.IList<System.Int32>>\n" +
                "      Parameter<System.Collections.Generic.IList<System.Int32>> list#2\n" +
                "      Convert<System.Collections.Generic.IList<System.Int32>>\n" +
                "        New<System.Collections.Generic.List<System.Int32>>\n" +
                "          NewArrayInit<System.Int32[]>\n" +
                "            Constant<System.Int32> 1\n" +
                "            Constant<System.Int32> 2\n" +
                "    Assign<System.Int32>\n" +
                "      Parameter<System.Int32> index#3\n" +
                "      Constant<System.Int32> 0\n" +
                "    Loop<System.Void> break:label#0 continue:label#1\n" +
                "      Conditional<System.Void>\n" +
                "        LessThan<System.Boolean>\n" +
                "          Parameter<System.Int32> index#3\n" +
                "          MemberAccess<System.Int32> System.Collections.Generic.ICollection<System.Int32>.Count\n" +
                "            Parameter<System.Collections.Generic.IList<System.Int32>> list#2\n" +
                "        Block<System.Void>\n" +
                "          Assign<System.Int32>\n" +
                "            Parameter<System.Int32> x#1\n" +
                "            Index<System.Int32> System.Collections.Generic.IList<System.Int32>.Item\n" +
                "              Parameter<System.Collections.Generic.IList<System.Int32>> list#2\n" +
                "              Parameter<System.Int32> index#3\n" +
                "          PreIncrementAssign<System.Int32>\n" +
                "            Parameter<System.Int32> index#3\n" +
                "          Block<System.Void>\n" +
                "            Assign<System.Int32>\n" +
                "              Parameter<System.Int32> total#0\n" +
                "              Add<System.Int32>\n" +
                "                Parameter<System.Int32> total#0\n" +
                "                Parameter<System.Int32> x#1\n" +
                "        Goto<System.Void> Break label#0\n" +
                "  Parameter<System.Int32> total#0\n",
                Expression.BlockWithVariables (
                    new List<Expression> { total, x },
                    new List<Expression> {
                        Expression.ForEach (x, list, Expression.Assign (total, Expression.Add (total, x))),
                        total
                    }));
        }

        [Test]
        public void ForEachLoopOverASet ()
        {
            // ForEach over a set desugars to an enumerator loop. The enclosing block declares
            // the loop variable
            var x = Expression.Variable ("x", Type.Int ());
            var total = Expression.Variable ("total", Type.Int ());
            var set = Expression.CreateSet (new HashSet<Expression> {
                Expression.ConstantInt (1),
                Expression.ConstantInt (2)
            });
            Check (
                "Block<System.Int32> (total#0: System.Int32, x#1: System.Int32)\n" +
                "  Block<System.Void> (enumerator#2: System.Collections.Generic.IEnumerator<System.Int32>)\n" +
                "    Assign<System.Collections.Generic.IEnumerator<System.Int32>>\n" +
                "      Parameter<System.Collections.Generic.IEnumerator<System.Int32>> enumerator#2\n" +
                "      Call<System.Collections.Generic.IEnumerator<System.Int32>> System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator\n" +
                "        Call<System.Collections.Generic.HashSet<System.Int32>> static KRPC.Service.KRPC.Expression.SetOf\n" +
                "          NewArrayInit<System.Int32[]>\n" +
                "            Constant<System.Int32> 1\n" +
                "            Constant<System.Int32> 2\n" +
                "    Try<System.Void>\n" +
                "      Loop<System.Void> break:label#0 continue:label#1\n" +
                "        Conditional<System.Void>\n" +
                "          Call<System.Boolean> System.Collections.IEnumerator.MoveNext\n" +
                "            Parameter<System.Collections.Generic.IEnumerator<System.Int32>> enumerator#2\n" +
                "          Block<System.Void>\n" +
                "            Assign<System.Int32>\n" +
                "              Parameter<System.Int32> x#1\n" +
                "              MemberAccess<System.Int32> System.Collections.Generic.IEnumerator<System.Int32>.Current\n" +
                "                Parameter<System.Collections.Generic.IEnumerator<System.Int32>> enumerator#2\n" +
                "            Block<System.Void>\n" +
                "              Assign<System.Int32>\n" +
                "                Parameter<System.Int32> total#0\n" +
                "                Add<System.Int32>\n" +
                "                  Parameter<System.Int32> total#0\n" +
                "                  Parameter<System.Int32> x#1\n" +
                "          Goto<System.Void> Break label#0\n" +
                "      Finally\n" +
                "        Call<System.Void> System.IDisposable.Dispose\n" +
                "          Parameter<System.Collections.Generic.IEnumerator<System.Int32>> enumerator#2\n" +
                "  Parameter<System.Int32> total#0\n",
                Expression.BlockWithVariables (
                    new List<Expression> { total, x },
                    new List<Expression> {
                        Expression.ForEach (x, set, Expression.Assign (total, Expression.Add (total, x))),
                        total
                    }));
        }

        [Test]
        public void Conditional ()
        {
            Check (
                "Conditional<System.String>\n" +
                "  Constant<System.Boolean> true\n" +
                "  Constant<System.String> \"a\"\n" +
                "  Constant<System.String> \"b\"\n",
                Expression.Conditional (
                    Expression.ConstantBool (true),
                    Expression.ConstantString ("a"),
                    Expression.ConstantString ("b")));
        }

        [Test]
        public void SetListElement ()
        {
            var values = Expression.Variable ("values", Type.ListType (Type.Int ()));
            Check (
                "Call<System.Void> static KRPC.Service.KRPC.Expression.ListSetHelper\n" +
                "  Parameter<System.Collections.Generic.IList<System.Int32>> values#0\n" +
                "  Constant<System.Int32> 0\n" +
                "  Constant<System.Int32> 7\n",
                Expression.Set (values, Expression.ConstantInt (0), Expression.ConstantInt (7)));
        }

        [Test]
        public void CatchWithAMessage ()
        {
            var message = Expression.Variable ("message", Type.String ());
            Check (
                "Try<System.Void>\n" +
                "  Throw<System.Void>\n" +
                "    New<KRPC.Service.KRPC.InvalidOperationException>\n" +
                "      Constant<System.String> \"x\"\n" +
                "  Catch<KRPC.Service.KRPC.InvalidOperationException> caught#0\n" +
                "    Block<System.Void>\n" +
                "      Conditional<System.Void>\n" +
                "        Not<System.Boolean>\n" +
                "          Call<System.Boolean> static KRPC.Service.Services.ExpressionExceptionIsNamed\n" +
                "            Parameter<KRPC.Service.KRPC.InvalidOperationException> caught#0\n" +
                "            Constant<System.Type> typeof(KRPC.Service.KRPC.InvalidOperationException)\n" +
                "        Throw<System.Void>\n" +
                "        Default<System.Void>\n" +
                "      Assign<System.String>\n" +
                "        Parameter<System.String> message#1\n" +
                "        MemberAccess<System.String> System.Exception.Message\n" +
                "          Parameter<KRPC.Service.KRPC.InvalidOperationException> caught#0\n" +
                "      Block<System.Void>\n" +
                "        Constant<System.Boolean> false\n" +
                "  Catch<System.InvalidOperationException> caught#2\n" +
                "    Block<System.Void>\n" +
                "      Conditional<System.Void>\n" +
                "        Not<System.Boolean>\n" +
                "          Call<System.Boolean> static KRPC.Service.Services.ExpressionExceptionIsNamed\n" +
                "            Parameter<System.InvalidOperationException> caught#2\n" +
                "            Constant<System.Type> typeof(KRPC.Service.KRPC.InvalidOperationException)\n" +
                "        Throw<System.Void>\n" +
                "        Default<System.Void>\n" +
                "      Assign<System.String>\n" +
                "        Parameter<System.String> message#1\n" +
                "        MemberAccess<System.String> System.Exception.Message\n" +
                "          Parameter<System.InvalidOperationException> caught#2\n" +
                "      Block<System.Void>\n" +
                "        Constant<System.Boolean> false\n",
                Expression.TryCatch (
                    Expression.Throw ("KRPC", "InvalidOperationException", Expression.ConstantString ("x")),
                    "KRPC", "InvalidOperationException", message,
                    Expression.ConstantBool (false)));
        }

        [Test]
        public void CatchAll ()
        {
            // A pause is rethrown
            Check (
                "Try<System.Void>\n" +
                "  Block<System.Void>\n" +
                "    Constant<System.Boolean> true\n" +
                "  Catch<KRPC.Service.YieldException> yielded#0\n" +
                "    Throw<System.Void>\n" +
                "  Catch<System.Exception>\n" +
                "    Block<System.Void>\n" +
                "      Constant<System.Boolean> false\n",
                Expression.TryCatchAll (
                    Expression.ConstantBool (true), null, Expression.ConstantBool (false)));
        }

        [Test]
        public void CreateStruct ()
        {
            Check (
                "MemberInit<KRPC.Test.Service.TestService.TestNullableStruct>\n" +
                "  New<KRPC.Test.Service.TestService.TestNullableStruct>\n" +
                "  Assignment KRPC.Test.Service.TestService.TestNullableStruct.IntField\n" +
                "    Constant<System.Int32> 1\n" +
                "  Assignment KRPC.Test.Service.TestService.TestNullableStruct.NullableIntField\n" +
                "    Constant<System.Nullable<System.Int32>> null\n" +
                "  Assignment KRPC.Test.Service.TestService.TestNullableStruct.NullableEnumField\n" +
                "    Constant<System.Nullable<KRPC.Test.Service.TestService.TestEnum>> null\n" +
                "  Assignment KRPC.Test.Service.TestService.TestNullableStruct.NullableStringField\n" +
                "    Constant<System.String> null\n" +
                "  Assignment KRPC.Test.Service.TestService.TestNullableStruct.NullableObjectField\n" +
                "    Constant<KRPC.Test.Service.TestService.TestClass> null\n",
                Expression.CreateStruct (
                    Type.StructType ("TestService", "TestNullableStruct"),
                    new List<Expression> {
                        Expression.ConstantInt (1),
                        Expression.ConstantNull (Type.Int ()),
                        Expression.ConstantNull (Type.EnumerationType ("TestService", "TestEnum")),
                        Expression.ConstantNull (Type.String ()),
                        Expression.ConstantNull (Type.ClassType ("TestService", "TestClass"))
                    }));
        }

        [Test]
        public void Invoke ()
        {
            var x = Expression.Parameter ("x", Type.Int ());
            var function = Expression.Lambda (new List<Expression> { x }, x);
            Check (
                "Invoke<System.Int32>\n" +
                "  Lambda<System.Func<System.Int32, System.Int32>> (x#0)\n" +
                "    Label<System.Int32> label#0\n" +
                "      Parameter<System.Int32> x#0\n" +
                "  Constant<System.Int32> 1\n",
                Expression.Invoke (function, new Dictionary<string, Expression> {
                    { "x", Expression.ConstantInt (1) }
                }));
        }

        [Test]
        public void Constants ()
        {
            Check (
                "Block<System.Void>\n" +
                "  Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene\n" +
                "    Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.ProcedureEnumArg\n" +
                "  Call<System.Void> static KRPC.Test.Service.TestService.ProcedureEnumArg\n" +
                "    Constant<KRPC.Test.Service.TestService.TestEnum> KRPC.Test.Service.TestService.TestEnum.Y\n",
                Expression.Call (BuildProcedureCall (
                    "ProcedureEnumArg",
                    new Argument (0, global::KRPC.Test.Service.TestService.TestEnum.Y))));
            Check ("Constant<System.Byte[]> 0x01AB\n", Expression.ConstantBytes (new byte[] { 0x01, 0xab }));
            Check ("Constant<System.String> \"a\\\"b\\\\c\\n\\r\\t\\u0001\"\n", Expression.ConstantString ("a\"b\\c\n\r\t\u0001"));
        }

        [Test]
        public void NegativeZero ()
        {
            Check ("Constant<System.Double> -0\n", Expression.ConstantDouble (-0.0));
            Check ("Constant<System.Single> -0\n", Expression.ConstantFloat (-0.0f));
            Check ("Constant<System.Double> 0\n", Expression.ConstantDouble (0.0));
        }

        [Test]
        public void CollectionConstants ()
        {
            Assert.AreEqual (
                "Constant<System.Collections.Generic.List<System.Int32>> [1, 2]\n",
                ExpressionTreePrinter.Print (LinqExpression.Constant (new List<int> { 1, 2 })));
            Assert.AreEqual (
                "Constant<System.Collections.Generic.HashSet<System.String>> {\"a\"}\n",
                ExpressionTreePrinter.Print (LinqExpression.Constant (new HashSet<string> { "a" })));
            Assert.AreEqual (
                "Constant<System.Collections.Generic.Dictionary<System.String, System.Int32>> {\"a\": 1}\n",
                ExpressionTreePrinter.Print (LinqExpression.Constant (new Dictionary<string, int> { { "a", 1 } })));
            Assert.AreEqual (
                "Constant<System.Tuple<System.Int32, System.String>> (1, \"b\")\n",
                ExpressionTreePrinter.Print (LinqExpression.Constant (Tuple.Create (1, "b"))));
            var structure = new global::KRPC.Test.Service.TestService.TestStruct {
                IntField = 1, StringField = "s", ListField = new List<string> ()
            };
            Assert.AreEqual (
                "Constant<KRPC.Test.Service.TestService.TestStruct> KRPC.Test.Service.TestService.TestStruct " +
                "{IntField = 1, StringField = \"s\", EnumField = KRPC.Test.Service.TestService.TestEnum.X, " +
                "ObjectField = null, ListField = []}\n",
                ExpressionTreePrinter.Print (LinqExpression.Constant (structure)));
        }

        [Test]
        public void TypeNestedInAGenericType ()
        {
            Assert.AreEqual (
                "Constant<System.Collections.Generic.Dictionary<TKey, TValue>.KeyCollection<System.Int32, System.String>> [1]\n",
                ExpressionTreePrinter.Print (LinqExpression.Constant (
                    new Dictionary<int, string> { { 1, "a" } }.Keys)));
        }

        [Test]
        public void TypeSpecConstants ()
        {
            var nullableString = TypeSpec.Create (typeof (string), true, new TypeSpec [0]);
            Assert.AreEqual (
                "Constant<KRPC.Service.TypeSpec> System.Collections.Generic.List<System.String?>\n",
                ExpressionTreePrinter.Print (LinqExpression.Constant (TypeSpec.Create (
                    typeof (List<string>), false, new [] { nullableString }))));
            Assert.AreEqual (
                "Constant<KRPC.Service.TypeSpec> " +
                "System.Collections.Generic.Dictionary<System.String, System.Int32?>?\n",
                ExpressionTreePrinter.Print (LinqExpression.Constant (TypeSpec.Create (
                    typeof (Dictionary<string, int?>), true, new [] {
                        TypeSpec.Create (typeof (string)), TypeSpec.Create (typeof (int?), false, new TypeSpec [0])
                    }))));
            Assert.AreEqual (
                "Constant<KRPC.Service.TypeSpec> System.Collections.Generic.IEnumerable<System.String?>\n",
                ExpressionTreePrinter.Print (LinqExpression.Constant (TypeSpec.Create (
                    typeof (IEnumerable<string>), false, new [] { nullableString }))));
        }

        [Test]
        public void UnsupportedNode ()
        {
            Assert.Throws<System.NotSupportedException> (() => ExpressionTreePrinter.Print (
                LinqExpression.TypeIs (LinqExpression.Constant ("a"), typeof (string))));
        }

        class Holder
        {
            public List<int> Values = new List<int> ();
            public Holder Inner = null;
        }

        [Test]
        public void UnsupportedParts ()
        {
            var empty = LinqExpression.Empty ();
            var unsupported = new List<LinqExpression> {
                LinqExpression.TryFault (empty, empty),
                LinqExpression.TryCatch (empty, LinqExpression.Catch (
                    typeof (System.Exception), empty, LinqExpression.Constant (true))),
                LinqExpression.Coalesce (
                    LinqExpression.Constant (null, typeof (string)), LinqExpression.Constant ("a"),
                    LinqExpression.Lambda<Func<string, string>> (
                        LinqExpression.Constant ("b"), LinqExpression.Parameter (typeof (string)))),
                LinqExpression.MemberInit (
                    LinqExpression.New (typeof (Holder)),
                    LinqExpression.ListBind (
                        typeof (Holder).GetField ("Values"),
                        LinqExpression.ElementInit (
                            typeof (List<int>).GetMethod ("Add"), LinqExpression.Constant (1)))),
                LinqExpression.MemberInit (
                    LinqExpression.New (typeof (Holder)),
                    LinqExpression.MemberBind (typeof (Holder).GetField ("Inner")))
            };
            foreach (var expression in unsupported)
                Assert.Throws<System.NotSupportedException> (() => ExpressionTreePrinter.Print (expression));
        }

        [Test]
        public void OperatorMethod ()
        {
            // A string equality calls its operator, and a reference equality does not
            var a = LinqExpression.Constant ("a");
            Assert.AreEqual (
                "Equal<System.Boolean> System.String.op_Equality\n" +
                "  Constant<System.String> \"a\"\n" +
                "  Constant<System.String> \"a\"\n",
                ExpressionTreePrinter.Print (LinqExpression.Equal (a, a)));
            Assert.AreEqual (
                "Equal<System.Boolean>\n" +
                "  Constant<System.String> \"a\"\n" +
                "  Constant<System.String> \"a\"\n",
                ExpressionTreePrinter.Print (LinqExpression.ReferenceEqual (a, a)));
        }

        [Test]
        public void NamesThatAreNotIdentifiers ()
        {
            var first = LinqExpression.Parameter (typeof (int), "a#0, b");
            var second = LinqExpression.Parameter (typeof (int), "b");
            var third = LinqExpression.Parameter (typeof (int), string.Empty);
            Assert.AreEqual (
                "Lambda<System.Func<System.Int32, System.Int32, System.Int32, System.Int32>> " +
                "(\"a#0, b\"#0, b#1, \"\"#2)\n" +
                "  Block<System.Int32>\n" +
                "    Label<System.Void> \"end of loop\"#0\n" +
                "    Parameter<System.Int32> \"a#0, b\"#0\n",
                ExpressionTreePrinter.Print (LinqExpression.Lambda (
                    LinqExpression.Block (
                        LinqExpression.Label (LinqExpression.Label ("end of loop")), first),
                    first, second, third)));
        }
    }
}
