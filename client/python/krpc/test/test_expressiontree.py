import math
import unittest
from typing import Any, Callable

from krpc.test.servertestcase import ServerTestCase

REMOTE_PROPERTY_COMPARISON = """\
Equal<System.Boolean> System.String.op_Equality
  Block<System.String> (returnValue#0: System.String)
    Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene
      Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.get_StringProperty
    Assign<System.String>
      Parameter<System.String> returnValue#0
      Call<System.String> static TestServer.TestService.get_StringProperty
    Call<System.Void> static KRPC.Service.Services.CheckExpressionReturnValue
      Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.get_StringProperty
      Parameter<System.String> returnValue#0
    Parameter<System.String> returnValue#0
  Constant<System.String> "foo"
"""

TRUE_DIVISION = """\
Divide<System.Double>
  Convert<System.Double>
    Block<System.Int32>
      Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene
        Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_get_IntProperty
      Call<System.Int32> TestServer.TestService.TestClass.get_IntProperty
        Constant<TestServer.TestService.TestClass> <TestServer.TestService.TestClass>
  Convert<System.Double>
    Constant<System.Int32> 2
"""

MATH_FUNCTION = """\
Block<System.Double>
  Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene
    Constant<KRPC.Service.Scanner.ProcedureSignature> StdLib.Sqrt
  Call<System.Double> static KRPC.Service.StdLib.StdLib.Sqrt
    Convert<System.Double>
      Block<System.Int32>
        Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene
          Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_get_IntProperty
        Call<System.Int32> TestServer.TestService.TestClass.get_IntProperty
          Constant<TestServer.TestService.TestClass> <TestServer.TestService.TestClass>
"""

PRINT = """\
Block<System.Void>
  Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene
    Constant<KRPC.Service.Scanner.ProcedureSignature> StdLib.Print
  Call<System.Void> static KRPC.Service.StdLib.StdLib.Print
    Call<System.String> static KRPC.Service.Services.CheckExpressionArgument
      Constant<KRPC.Service.Scanner.ProcedureSignature> StdLib.Print
      Constant<System.Int32> 0
      Call<System.String> static System.String.Concat
        NewArrayInit<System.String[]>
          Constant<System.String> "value="
          Call<System.String> static KRPC.Service.KRPC.Expression.ConvertToStringHelper
            Convert<System.Object>
              Block<System.Int32>
                Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene
                  Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_get_IntProperty
                Call<System.Int32> TestServer.TestService.TestClass.get_IntProperty
                  Constant<TestServer.TestService.TestClass> <TestServer.TestService.TestClass>
"""

RANGE = """\
Block<System.Collections.Generic.IList<System.Int32>> (range#0: System.Collections.Generic.IList<System.Int32>, counter#1: System.Int64, end#2: System.Int64)
  Assign<System.Collections.Generic.IList<System.Int32>>
    Parameter<System.Collections.Generic.IList<System.Int32>> range#0
    Convert<System.Collections.Generic.IList<System.Int32>>
      New<System.Collections.Generic.List<System.Int32>>
  Assign<System.Int64>
    Parameter<System.Int64> counter#1
    Constant<System.Int64> 0
  Assign<System.Int64>
    Parameter<System.Int64> end#2
    Convert<System.Int64>
      Block<System.Int32>
        Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene
          Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_get_IntProperty
        Call<System.Int32> TestServer.TestService.TestClass.get_IntProperty
          Constant<TestServer.TestService.TestClass> <TestServer.TestService.TestClass>
  Loop<System.Void> break:label#0 continue:label#1
    Conditional<System.Void>
      LessThan<System.Boolean>
        Parameter<System.Int64> counter#1
        Parameter<System.Int64> end#2
      Block<System.Void>
        Block<System.Int64>
          Call<System.Void> System.Collections.Generic.ICollection<System.Int32>.Add
            Parameter<System.Collections.Generic.IList<System.Int32>> range#0
            Convert<System.Int32>
              Parameter<System.Int64> counter#1
          Assign<System.Int64>
            Parameter<System.Int64> counter#1
            Add<System.Int64>
              Parameter<System.Int64> counter#1
              Constant<System.Int64> 1
      Goto<System.Void> Break label#0
  Parameter<System.Collections.Generic.IList<System.Int32>> range#0
"""

CONSTANT_RANGE = """\
Block<System.Collections.Generic.IList<System.Int32>> (range#0: System.Collections.Generic.IList<System.Int32>, counter#1: System.Int64, end#2: System.Int64)
  Assign<System.Collections.Generic.IList<System.Int32>>
    Parameter<System.Collections.Generic.IList<System.Int32>> range#0
    Convert<System.Collections.Generic.IList<System.Int32>>
      New<System.Collections.Generic.List<System.Int32>>
  Assign<System.Int64>
    Parameter<System.Int64> counter#1
    Constant<System.Int64> 0
  Assign<System.Int64>
    Parameter<System.Int64> end#2
    Constant<System.Int64> 1000
  Loop<System.Void> break:label#0 continue:label#1
    Conditional<System.Void>
      LessThan<System.Boolean>
        Parameter<System.Int64> counter#1
        Parameter<System.Int64> end#2
      Block<System.Void>
        Block<System.Int64>
          Call<System.Void> System.Collections.Generic.ICollection<System.Int32>.Add
            Parameter<System.Collections.Generic.IList<System.Int32>> range#0
            Convert<System.Int32>
              Parameter<System.Int64> counter#1
          Assign<System.Int64>
            Parameter<System.Int64> counter#1
            Add<System.Int64>
              Parameter<System.Int64> counter#1
              Constant<System.Int64> 1
      Goto<System.Void> Break label#0
  Parameter<System.Collections.Generic.IList<System.Int32>> range#0
"""

STATIC_METHOD = """\
Block<System.String> (returnValue#0: System.String)
  Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene
    Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_static_StaticMethod
  Assign<System.String>
    Parameter<System.String> returnValue#0
    Call<System.String> static TestServer.TestService.TestClass.StaticMethod
      Constant<System.String> "a"
      Constant<System.String> ""
  Call<System.Void> static KRPC.Service.Services.CheckExpressionReturnValue
    Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_static_StaticMethod
    Parameter<System.String> returnValue#0
  Parameter<System.String> returnValue#0
"""

CHAINED_COMPARISON = """\
Block<System.Boolean> ("chained comparison"#0: System.Int32)
  Assign<System.Int32>
    Parameter<System.Int32> "chained comparison"#0
    Block<System.Int32>
      Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene
        Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_get_IntProperty
      Call<System.Int32> TestServer.TestService.TestClass.get_IntProperty
        Constant<TestServer.TestService.TestClass> <TestServer.TestService.TestClass>
  AndAlso<System.Boolean>
    LessThan<System.Boolean>
      Constant<System.Int32> 0
      Parameter<System.Int32> "chained comparison"#0
    LessThan<System.Boolean>
      Parameter<System.Int32> "chained comparison"#0
      Constant<System.Int32> 10
"""

COMPREHENSION_WITH_REMOTE_CALL = """\
Convert<System.Collections.Generic.IList<System.String>>
  Call<System.Collections.Generic.List<System.String>> static System.Linq.Enumerable.ToList
    Call<System.Collections.Generic.IEnumerable<System.String>> static System.Linq.Enumerable.Select
      Convert<System.Collections.Generic.IList<System.Double>>
        New<System.Collections.Generic.List<System.Double>>
          NewArrayInit<System.Double[]>
            Constant<System.Double> 1
            Constant<System.Double> 2
      Lambda<System.Func<System.Double, System.String>> (x#0)
        Label<System.String> label#0
          Block<System.String> (returnValue#1: System.String)
            Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene
              Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_FloatToString
            Assign<System.String>
              Parameter<System.String> returnValue#1
              Call<System.String> TestServer.TestService.TestClass.FloatToString
                Constant<TestServer.TestService.TestClass> <TestServer.TestService.TestClass>
                Convert<System.Single>
                  Parameter<System.Double> x#0
            Call<System.Void> static KRPC.Service.Services.CheckExpressionReturnValue
              Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_FloatToString
              Parameter<System.String> returnValue#1
            Parameter<System.String> returnValue#1
"""

STRUCT_CONSTRUCTION_AND_FIELD = """\
MemberAccess<System.String> TestServer.TestService.TestStruct.StringField
  Block<TestServer.TestService.TestStruct>
    Call<System.Void> static KRPC.Service.Services.CheckExpressionGameScene
      Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.StructEcho
    Block<TestServer.TestService.TestStruct> (param#0: TestServer.TestService.TestStruct)
      Assign<TestServer.TestService.TestStruct>
        Parameter<TestServer.TestService.TestStruct> param#0
        Call<TestServer.TestService.TestStruct> static TestServer.TestService.StructEcho
          MemberInit<TestServer.TestService.TestStruct>
            New<TestServer.TestService.TestStruct>
            Assignment TestServer.TestService.TestStruct.IntField
              Constant<System.Int32> 1
            Assignment TestServer.TestService.TestStruct.StringField
              Constant<System.String> "golden"
            Assignment TestServer.TestService.TestStruct.EnumField
              Convert<TestServer.TestService.TestEnum>
                Constant<System.Int32> 0
            Assignment TestServer.TestService.TestStruct.ListField
              Convert<System.Collections.Generic.IList<System.Int32>>
                New<System.Collections.Generic.List<System.Int32>>
                  NewArrayInit<System.Int32[]>
                    Constant<System.Int32> 2
      MemberInit<TestServer.TestService.TestStruct>
        New<TestServer.TestService.TestStruct>
        Assignment TestServer.TestService.TestStruct.IntField
          MemberAccess<System.Int32> TestServer.TestService.TestStruct.IntField
            Parameter<TestServer.TestService.TestStruct> param#0
        Assignment TestServer.TestService.TestStruct.StringField
          MemberAccess<System.String> TestServer.TestService.TestStruct.StringField
            Parameter<TestServer.TestService.TestStruct> param#0
        Assignment TestServer.TestService.TestStruct.EnumField
          MemberAccess<TestServer.TestService.TestEnum> TestServer.TestService.TestStruct.EnumField
            Parameter<TestServer.TestService.TestStruct> param#0
        Assignment TestServer.TestService.TestStruct.ListField
          Convert<System.Collections.Generic.IList<System.Int32>>
            Call<System.Collections.Generic.List<System.Int32>> static KRPC.Service.KRPC.Expression.CopyList
              MemberAccess<System.Collections.Generic.IList<System.Int32>> TestServer.TestService.TestStruct.ListField
                Parameter<TestServer.TestService.TestStruct> param#0
              Constant<System.Func<System.Int32, System.Int32>> null
"""


class TestExpressionTree(ServerTestCase, unittest.TestCase):
    """Golden tests for the expression trees the python compiler generates,
    using the server's ExpressionTreePrinter dump. These verify the exact
    tree structure produced by compiling python source, without depending
    on the values the trees evaluate to."""

    maxDiff = None  # pylint: disable=invalid-name

    def dump(self, func: Callable[[], Any]) -> str:
        expression = self.conn.compile(func)
        return self.conn.test_service.dump_expression_tree(expression)

    def check(self, expected: str, func: Callable[[], Any]) -> None:
        self.assertEqual(expected, self.dump(func))

    def test_remote_property_comparison(self) -> None:
        self.check(
            REMOTE_PROPERTY_COMPARISON,
            lambda: self.conn.test_service.string_property == "foo",
        )

    def test_true_division(self) -> None:
        obj = self.conn.test_service.create_test_object("golden")
        self.check(
            TRUE_DIVISION,
            lambda: obj.int_property / 2,
        )

    def test_math_function(self) -> None:
        obj = self.conn.test_service.create_test_object("golden")
        self.check(
            MATH_FUNCTION,
            lambda: math.sqrt(obj.int_property),
        )

    def test_print(self) -> None:
        obj = self.conn.test_service.create_test_object("golden")
        self.check(
            PRINT,
            lambda: print("value", obj.int_property, sep="="),
        )

    def test_static_method_of_a_class_accessed_through_a_service(self) -> None:
        # A static method makes its call on each evaluation, rather than being
        # called once when the function is compiled
        cls = self.conn.test_service.TestClass
        self.check(STATIC_METHOD, lambda: cls.static_method("a"))

    def test_range(self) -> None:
        obj = self.conn.test_service.create_test_object("golden")
        self.check(RANGE, lambda: range(obj.int_property))

    def test_constant_range(self) -> None:
        # Built by the same loop as a computed range, with a constant bound
        self.check(CONSTANT_RANGE, lambda: range(1000))

    def test_struct_construction_and_field(self) -> None:
        test_struct = self.conn.test_service.TestStruct
        enum = self.conn.test_service.TestEnum
        echo = self.conn.test_service.struct_echo
        self.check(
            STRUCT_CONSTRUCTION_AND_FIELD,
            lambda: echo(test_struct(1, "golden", enum.value_a, [2])).string_field,
        )

    def test_chained_comparison(self) -> None:
        # The middle operand is read once, into a temporary, and the comparisons
        # short-circuit
        obj = self.conn.test_service.create_test_object("golden")
        self.check(
            CHAINED_COMPARISON,
            lambda: 0 < obj.int_property < 10,
        )

    def test_comprehension_with_remote_call(self) -> None:
        obj = self.conn.test_service.create_test_object("golden")
        self.check(
            COMPREHENSION_WITH_REMOTE_CALL,
            lambda: [obj.float_to_string(x) for x in [1.0, 2.0]],
        )


if __name__ == "__main__":
    unittest.main()
