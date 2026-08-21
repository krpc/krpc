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
                Connection.CompileExpression (expression));
        }

        void Check<T> (string expected, Expression<Func<T>> expression)
        {
            Assert.AreEqual (expected, Dump (expression));
        }

        [Test]
        public void TestRemotePropertyComparison ()
        {
            var testService = Connection.TestService ();
            Check (
                "Equal<Boolean>\n" +
                "  Block<String> (returnValue#0)\n" +
                "    Call<Void> static Services.CheckExpressionGameScene\n" +
                "      Constant<ProcedureSignature> TestService.get_StringProperty\n" +
                "    Assign<String>\n" +
                "      Parameter<String> returnValue#0\n" +
                "      Call<String> static TestService.get_StringProperty\n" +
                "    Call<Void> static Services.CheckExpressionReturnValue\n" +
                "      Constant<ProcedureSignature> TestService.get_StringProperty\n" +
                "      Parameter<String> returnValue#0\n" +
                "    Parameter<String> returnValue#0\n" +
                "  Constant<String> \"foo\"\n",
                () => testService.StringProperty == "foo");
        }

        [Test]
        public void TestMathFunction ()
        {
            var obj = Connection.TestService ().CreateTestObject ("golden");
            Check (
                "Block<Double>\n" +
                "  Call<Void> static Services.CheckExpressionGameScene\n" +
                "    Constant<ProcedureSignature> StdLib.Sqrt\n" +
                "  Call<Double> static StdLib.Sqrt\n" +
                "    Convert<Double>\n" +
                "      Block<Int32>\n" +
                "        Call<Void> static Services.CheckExpressionGameScene\n" +
                "          Constant<ProcedureSignature> TestService.TestClass_get_IntProperty\n" +
                "        Call<Int32> TestClass.get_IntProperty\n" +
                "          Constant<TestClass> <TestClass>\n",
                () => Math.Sqrt (obj.IntProperty));
        }

        [Test]
        public void TestSelectWithRemoteCall ()
        {
            var obj = Connection.TestService ().CreateTestObject ("golden");
            var values = new List<float> { 1f, 2f };
            Check (
                "Call<List<String>> static Enumerable.ToList\n" +
                "  Call<IEnumerable<String>> static Enumerable.Select\n" +
                "    New<List<Single>>\n" +
                "      NewArrayInit<Single[]>\n" +
                "        Constant<Single> 1\n" +
                "        Constant<Single> 2\n" +
                "    Lambda<Func<Single, String>> (x#0)\n" +
                "      Label<String> label#0\n" +
                "        Block<String> (returnValue#1)\n" +
                "          Call<Void> static Services.CheckExpressionGameScene\n" +
                "            Constant<ProcedureSignature> TestService.TestClass_FloatToString\n" +
                "          Assign<String>\n" +
                "            Parameter<String> returnValue#1\n" +
                "            Call<String> TestClass.FloatToString\n" +
                "              Constant<TestClass> <TestClass>\n" +
                "              Parameter<Single> x#0\n" +
                "          Call<Void> static Services.CheckExpressionReturnValue\n" +
                "            Constant<ProcedureSignature> TestService.TestClass_FloatToString\n" +
                "            Parameter<String> returnValue#1\n" +
                "          Parameter<String> returnValue#1\n",
                () => values.Select (x => obj.FloatToString (x)).ToList ());
        }

        [Test]
        public void TestStringConcat ()
        {
            var testService = Connection.TestService ();
            Check (
                "Call<String> static String.Concat\n" +
                "  NewArrayInit<String[]>\n" +
                "    Block<String> (returnValue#0)\n" +
                "      Call<Void> static Services.CheckExpressionGameScene\n" +
                "        Constant<ProcedureSignature> TestService.get_StringProperty\n" +
                "      Assign<String>\n" +
                "        Parameter<String> returnValue#0\n" +
                "        Call<String> static TestService.get_StringProperty\n" +
                "      Call<Void> static Services.CheckExpressionReturnValue\n" +
                "        Constant<ProcedureSignature> TestService.get_StringProperty\n" +
                "        Parameter<String> returnValue#0\n" +
                "      Parameter<String> returnValue#0\n" +
                "    Constant<String> \"!\"\n",
                () => testService.StringProperty + "!");
        }

        [Test]
        public void TestStructConstructionAndField ()
        {
            var testService = Connection.TestService ();
            Check (
                "MemberAccess<String> TestStruct.StringField\n" +
                "  Block<TestStruct>\n" +
                "    Call<Void> static Services.CheckExpressionGameScene\n" +
                "      Constant<ProcedureSignature> TestService.StructEcho\n" +
                "    Call<TestStruct> static TestService.StructEcho\n" +
                "      MemberInit<TestStruct>\n" +
                "        New<TestStruct>\n" +
                "        Assignment TestStruct.IntField\n" +
                "          Constant<Int32> 1\n" +
                "        Assignment TestStruct.StringField\n" +
                "          Constant<String> \"golden\"\n" +
                "        Assignment TestStruct.EnumField\n" +
                "          Convert<TestEnum>\n" +
                "            Constant<Int32> 0\n" +
                "        Assignment TestStruct.ListField\n" +
                "          New<List<Int32>>\n" +
                "            NewArrayInit<Int32[]>\n" +
                "              Constant<Int32> 2\n",
                () => testService.StructEcho (
                    new TestStruct (1, "golden", TestEnum.ValueA, new List<int> { 2 })).StringField);
        }

        [Test]
        public void TestConditional ()
        {
            var obj = Connection.TestService ().CreateTestObject ("golden");
            Check (
                "Conditional<String>\n" +
                "  GreaterThan<Boolean>\n" +
                "    Block<Int32>\n" +
                "      Call<Void> static Services.CheckExpressionGameScene\n" +
                "        Constant<ProcedureSignature> TestService.TestClass_get_IntProperty\n" +
                "      Call<Int32> TestClass.get_IntProperty\n" +
                "        Constant<TestClass> <TestClass>\n" +
                "    Constant<Int32> 0\n" +
                "  Constant<String> \"positive\"\n" +
                "  Constant<String> \"non-positive\"\n",
                () => obj.IntProperty > 0 ? "positive" : "non-positive");
        }
    }
}
