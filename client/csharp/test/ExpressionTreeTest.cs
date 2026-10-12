using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using KRPC.Client.Services.TestService;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    /// <summary>
    /// Golden tests for the expression trees the C# compiler generates, using
    /// the server's ExpressionTreePrinter dump. These verify the exact tree
    /// structure produced by compiling LINQ expressions, without depending on
    /// the values the trees evaluate to.
    /// </summary>
    [TestFixture]
    public class ExpressionTreeTest : ServerTestCase
    {
        string Dump<T> (Expression<Func<T>> expression)
        {
            return Connection.TestService ().DumpExpressionTree (
                Connection.Compile (expression));
        }

        void Check<T> (string expected, Expression<Func<T>> expression)
        {
            Assert.AreEqual (expected, Dump (expression));
        }

        string Dump (Expression<Action> expression)
        {
            return Connection.TestService ().DumpExpressionTree (
                Connection.Compile (expression));
        }

        void Check (string expected, Expression<Action> expression)
        {
            Assert.AreEqual (expected, Dump (expression));
        }

        [Test]
        public void TestLambdaWithParameters ()
        {
            var testService = Connection.TestService ();
            Assert.AreEqual (
                "Lambda<System.Func<TestServer.TestService.TestClass, System.Double, System.Double>> (target#0, factor#1)\n" +
                "  Label<System.Double> label#0\n" +
                "    Multiply<System.Double>\n" +
                "      Convert<System.Double>\n" +
                "        Block<System.Int32>\n" +
                "          Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene\n" +
                "            Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_get_IntProperty\n" +
                "          Call<System.Int32> TestServer.TestService.TestClass.get_IntProperty\n" +
                "            Call<TestServer.TestService.TestClass> static KRPC.Service.Services.CheckExpressionArgument\n" +
                "              Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_get_IntProperty\n" +
                "              Constant<System.Int32> 0\n" +
                "              Parameter<TestServer.TestService.TestClass> target#0\n" +
                "      Parameter<System.Double> factor#1\n",
                testService.DumpExpressionTree (Connection.Compile (
                    (TestClass target, double factor) => target.IntProperty * factor)));
        }

        [Test]
        public void TestRemotePropertyComparison ()
        {
            var testService = Connection.TestService ();
            Check (
                "Equal<System.Boolean> System.String.op_Equality\n" +
                "  Block<System.String> (returnValue#0: System.String)\n" +
                "    Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene\n" +
                "      Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.get_StringProperty\n" +
                "    Assign<System.String>\n" +
                "      Parameter<System.String> returnValue#0\n" +
                "      Call<System.String> static TestServer.TestService.get_StringProperty\n" +
                "    Call<System.Void> static KRPC.Service.Services.CheckExpressionReturnValue\n" +
                "      Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.get_StringProperty\n" +
                "      Parameter<System.String> returnValue#0\n" +
                "    Parameter<System.String> returnValue#0\n" +
                "  Constant<System.String> \"foo\"\n",
                () => testService.StringProperty == "foo");
        }

        [Test]
        public void TestNullComparisonOfANonNullableValue ()
        {
            var testService = Connection.TestService ();
            // The server tracks that the property is not null, so the test is a constant
            Check (
                "Block<System.Boolean>\n" +
                "  Block<System.String> (returnValue#0: System.String)\n" +
                "    Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene\n" +
                "      Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.get_StringProperty\n" +
                "    Assign<System.String>\n" +
                "      Parameter<System.String> returnValue#0\n" +
                "      Call<System.String> static TestServer.TestService.get_StringProperty\n" +
                "    Call<System.Void> static KRPC.Service.Services.CheckExpressionReturnValue\n" +
                "      Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.get_StringProperty\n" +
                "      Parameter<System.String> returnValue#0\n" +
                "    Parameter<System.String> returnValue#0\n" +
                "  Constant<System.Boolean> false\n",
                () => testService.StringProperty == null);
        }

        [Test]
        public void TestMathFunction ()
        {
            var obj = Connection.TestService ().CreateTestObject ("golden");
            Check (
                "Block<System.Double>\n" +
                "  Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene\n" +
                "    Constant<KRPC.Service.Scanner.ProcedureSignature> StdLib.Sqrt\n" +
                "  Call<System.Double> static KRPC.Service.StdLib.StdLib.Sqrt\n" +
                "    Convert<System.Double>\n" +
                "      Block<System.Int32>\n" +
                "        Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene\n" +
                "          Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_get_IntProperty\n" +
                "        Call<System.Int32> TestServer.TestService.TestClass.get_IntProperty\n" +
                "          Constant<TestServer.TestService.TestClass> <TestServer.TestService.TestClass>\n",
                () => Math.Sqrt (obj.IntProperty));
        }

        [Test]
        public void TestWriteLine ()
        {
            var obj = Connection.TestService ().CreateTestObject ("golden");
            Check (
                "Block<System.Void>\n" +
                "  Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene\n" +
                "    Constant<KRPC.Service.Scanner.ProcedureSignature> StdLib.Print\n" +
                "  Call<System.Void> static KRPC.Service.StdLib.StdLib.Print\n" +
                "    Call<System.String> static KRPC.Service.Services.CheckExpressionArgument\n" +
                "      Constant<KRPC.Service.Scanner.ProcedureSignature> StdLib.Print\n" +
                "      Constant<System.Int32> 0\n" +
                "      Block<System.String> (returnValue#0: System.String)\n" +
                "        Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene\n" +
                "          Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_GetValue\n" +
                "        Assign<System.String>\n" +
                "          Parameter<System.String> returnValue#0\n" +
                "          Call<System.String> TestServer.TestService.TestClass.GetValue\n" +
                "            Constant<TestServer.TestService.TestClass> <TestServer.TestService.TestClass>\n" +
                "        Call<System.Void> static KRPC.Service.Services.CheckExpressionReturnValue\n" +
                "          Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_GetValue\n" +
                "          Parameter<System.String> returnValue#0\n" +
                "        Parameter<System.String> returnValue#0\n",
                () => Console.WriteLine (obj.GetValue ()));
        }

        [Test]
        public void TestSelectWithRemoteCall ()
        {
            var obj = Connection.TestService ().CreateTestObject ("golden");
            var values = new List<float> { 1f, 2f };
            Check (
                "Convert<System.Collections.Generic.IList<System.String>>\n" +
                "  Call<System.Collections.Generic.List<System.String>> static System.Linq.Enumerable.ToList\n" +
                "    Call<System.Collections.Generic.IEnumerable<System.String>> static System.Linq.Enumerable.Select\n" +
                "      Convert<System.Collections.Generic.IList<System.Single>>\n" +
                "        New<System.Collections.Generic.List<System.Single>>\n" +
                "          NewArrayInit<System.Single[]>\n" +
                "            Constant<System.Single> 1\n" +
                "            Constant<System.Single> 2\n" +
                "      Lambda<System.Func<System.Single, System.String>> (x#0)\n" +
                "        Label<System.String> label#0\n" +
                "          Block<System.String> (returnValue#1: System.String)\n" +
                "            Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene\n" +
                "              Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_FloatToString\n" +
                "            Assign<System.String>\n" +
                "              Parameter<System.String> returnValue#1\n" +
                "              Call<System.String> TestServer.TestService.TestClass.FloatToString\n" +
                "                Constant<TestServer.TestService.TestClass> <TestServer.TestService.TestClass>\n" +
                "                Parameter<System.Single> x#0\n" +
                "            Call<System.Void> static KRPC.Service.Services.CheckExpressionReturnValue\n" +
                "              Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_FloatToString\n" +
                "              Parameter<System.String> returnValue#1\n" +
                "            Parameter<System.String> returnValue#1\n",
                () => values.Select (x => obj.FloatToString (x)).ToList ());
        }

        [Test]
        public void TestStringConcat ()
        {
            var testService = Connection.TestService ();
            Check (
                "Call<System.String> static System.String.Concat\n" +
                "  NewArrayInit<System.String[]>\n" +
                "    Block<System.String> (returnValue#0: System.String)\n" +
                "      Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene\n" +
                "        Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.get_StringProperty\n" +
                "      Assign<System.String>\n" +
                "        Parameter<System.String> returnValue#0\n" +
                "        Call<System.String> static TestServer.TestService.get_StringProperty\n" +
                "      Call<System.Void> static KRPC.Service.Services.CheckExpressionReturnValue\n" +
                "        Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.get_StringProperty\n" +
                "        Parameter<System.String> returnValue#0\n" +
                "      Parameter<System.String> returnValue#0\n" +
                "    Constant<System.String> \"!\"\n",
                () => testService.StringProperty + "!");
        }

        [Test]
        public void TestStructConstructionAndField ()
        {
            var testService = Connection.TestService ();
            Check (
                "MemberAccess<System.String> TestServer.TestService.TestStruct.StringField\n" +
                "  Block<TestServer.TestService.TestStruct>\n" +
                "    Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene\n" +
                "      Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.StructEcho\n" +
                "    Block<TestServer.TestService.TestStruct> (param#0: TestServer.TestService.TestStruct)\n" +
                "      Assign<TestServer.TestService.TestStruct>\n" +
                "        Parameter<TestServer.TestService.TestStruct> param#0\n" +
                "        Call<TestServer.TestService.TestStruct> static TestServer.TestService.StructEcho\n" +
                "          MemberInit<TestServer.TestService.TestStruct>\n" +
                "            New<TestServer.TestService.TestStruct>\n" +
                "            Assignment TestServer.TestService.TestStruct.IntField\n" +
                "              Constant<System.Int32> 1\n" +
                "            Assignment TestServer.TestService.TestStruct.StringField\n" +
                "              Constant<System.String> \"golden\"\n" +
                "            Assignment TestServer.TestService.TestStruct.EnumField\n" +
                "              Convert<TestServer.TestService.TestEnum>\n" +
                "                Constant<System.Int32> 0\n" +
                "            Assignment TestServer.TestService.TestStruct.ListField\n" +
                "              Convert<System.Collections.Generic.IList<System.Int32>>\n" +
                "                New<System.Collections.Generic.List<System.Int32>>\n" +
                "                  NewArrayInit<System.Int32[]>\n" +
                "                    Constant<System.Int32> 2\n" +
                "      MemberInit<TestServer.TestService.TestStruct>\n" +
                "        New<TestServer.TestService.TestStruct>\n" +
                "        Assignment TestServer.TestService.TestStruct.IntField\n" +
                "          MemberAccess<System.Int32> TestServer.TestService.TestStruct.IntField\n" +
                "            Parameter<TestServer.TestService.TestStruct> param#0\n" +
                "        Assignment TestServer.TestService.TestStruct.StringField\n" +
                "          MemberAccess<System.String> TestServer.TestService.TestStruct.StringField\n" +
                "            Parameter<TestServer.TestService.TestStruct> param#0\n" +
                "        Assignment TestServer.TestService.TestStruct.EnumField\n" +
                "          MemberAccess<TestServer.TestService.TestEnum> TestServer.TestService.TestStruct.EnumField\n" +
                "            Parameter<TestServer.TestService.TestStruct> param#0\n" +
                "        Assignment TestServer.TestService.TestStruct.ListField\n" +
                "          Convert<System.Collections.Generic.IList<System.Int32>>\n" +
                "            Call<System.Collections.Generic.List<System.Int32>> static KRPC.Service.KRPC.Expression.CopyList\n" +
                "              MemberAccess<System.Collections.Generic.IList<System.Int32>> TestServer.TestService.TestStruct.ListField\n" +
                "                Parameter<TestServer.TestService.TestStruct> param#0\n" +
                "              Constant<System.Func<System.Int32, System.Int32>> null\n",
                () => testService.StructEcho (
                    new TestStruct (1, "golden", TestEnum.ValueA, new List<int> { 2 })).StringField);
        }

        [Test]
        public void TestConditional ()
        {
            var obj = Connection.TestService ().CreateTestObject ("golden");
            Check (
                "Conditional<System.String>\n" +
                "  GreaterThan<System.Boolean>\n" +
                "    Block<System.Int32>\n" +
                "      Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene\n" +
                "        Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_get_IntProperty\n" +
                "      Call<System.Int32> TestServer.TestService.TestClass.get_IntProperty\n" +
                "        Constant<TestServer.TestService.TestClass> <TestServer.TestService.TestClass>\n" +
                "    Constant<System.Int32> 0\n" +
                "  Constant<System.String> \"positive\"\n" +
                "  Constant<System.String> \"non-positive\"\n",
                () => obj.IntProperty > 0 ? "positive" : "non-positive");
        }

        [Test]
        public void TestBooleanOperators ()
        {
            var obj = Connection.TestService ().CreateTestObject ("golden");
            // && short-circuits, & evaluates both operands
            Check (
                "AndAlso<System.Boolean>\n" +
                "  GreaterThan<System.Boolean>\n" +
                "    Block<System.Int32>\n" +
                "      Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene\n" +
                "        Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_get_IntProperty\n" +
                "      Call<System.Int32> TestServer.TestService.TestClass.get_IntProperty\n" +
                "        Constant<TestServer.TestService.TestClass> <TestServer.TestService.TestClass>\n" +
                "    Constant<System.Int32> 0\n" +
                "  Constant<System.Boolean> true\n",
                () => obj.IntProperty > 0 && true);
            Check (
                "And<System.Boolean>\n" +
                "  GreaterThan<System.Boolean>\n" +
                "    Block<System.Int32>\n" +
                "      Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene\n" +
                "        Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_get_IntProperty\n" +
                "      Call<System.Int32> TestServer.TestService.TestClass.get_IntProperty\n" +
                "        Constant<TestServer.TestService.TestClass> <TestServer.TestService.TestClass>\n" +
                "    Constant<System.Int32> 0\n" +
                "  Constant<System.Boolean> true\n",
                () => (obj.IntProperty > 0) & true);
        }
    }
}
