import math
import unittest

from krpc.test.servertestcase import ServerTestCase

REMOTE_PROPERTY_COMPARISON = """\
Equal<Boolean>
  Block<String> (returnValue#0)
    Call<Void> static Services.CheckExpressionGameScene
      Constant<ProcedureSignature> TestService.get_StringProperty
    Assign<String>
      Parameter<String> returnValue#0
      Call<String> static TestService.get_StringProperty
    Call<Void> static Services.CheckExpressionReturnValue
      Constant<ProcedureSignature> TestService.get_StringProperty
      Parameter<String> returnValue#0
    Parameter<String> returnValue#0
  Constant<String> "foo"
"""

TRUE_DIVISION = """\
Divide<Double>
  Convert<Double>
    Block<Int32>
      Call<Void> static Services.CheckExpressionGameScene
        Constant<ProcedureSignature> TestService.TestClass_get_IntProperty
      Call<Int32> TestClass.get_IntProperty
        Constant<TestClass> <TestClass>
  Convert<Double>
    Constant<Int32> 2
"""

MATH_FUNCTION = """\
Block<Double>
  Call<Void> static Services.CheckExpressionGameScene
    Constant<ProcedureSignature> StdLib.Sqrt
  Call<Double> static StdLib.Sqrt
    Convert<Double>
      Block<Int32>
        Call<Void> static Services.CheckExpressionGameScene
          Constant<ProcedureSignature> TestService.TestClass_get_IntProperty
        Call<Int32> TestClass.get_IntProperty
          Constant<TestClass> <TestClass>
"""

FUNCTION_WITH_EARLY_RETURN = """\
Invoke<Int32>
  Lambda<Func<Int32>> ()
    Label<Int32> label#0
      Block<Int32>
        Conditional<Void>
          LessThan<Boolean>
            Block<Int32>
              Call<Void> static Services.CheckExpressionGameScene
                Constant<ProcedureSignature> TestService.TestClass_get_IntProperty
              Call<Int32> TestClass.get_IntProperty
                Constant<TestClass> <TestClass>
            Constant<Int32> 0
          Block<Void>
            Goto<Void> Return label#0
              Constant<Int32> 0
          Default<Void>
        Block<Int32>
          Call<Void> static Services.CheckExpressionGameScene
            Constant<ProcedureSignature> TestService.TestClass_get_IntProperty
          Call<Int32> TestClass.get_IntProperty
            Constant<TestClass> <TestClass>
"""

LOOP_BUILDING_VALUE = """\
Invoke<Int32>
  Lambda<Func<Int32>> ()
    Label<Int32> label#0
      Block<Int32> (total#0, x#1)
        Assign<Int32>
          Parameter<Int32> total#0
          Constant<Int32> 0
        Block<Void> (enumerator#2)
          Assign<IEnumerator<Int32>>
            Parameter<IEnumerator<Int32>> enumerator#2
            Call<IEnumerator<Int32>> IEnumerable<Int32>.GetEnumerator
              New<List<Int32>>
                NewArrayInit<Int32[]>
                  Constant<Int32> 1
                  Constant<Int32> 2
                  Constant<Int32> 3
          Try<Void>
            Loop<Void> break:label#1 continue:label#2
              Conditional<Void>
                Call<Boolean> IEnumerator.MoveNext
                  Parameter<IEnumerator<Int32>> enumerator#2
                Block<Void>
                  Assign<Int32>
                    Parameter<Int32> x#1
                    MemberAccess<Int32> IEnumerator<Int32>.Current
                      Parameter<IEnumerator<Int32>> enumerator#2
                  Block<Void>
                    Assign<Int32>
                      Parameter<Int32> total#0
                      Add<Int32>
                        Parameter<Int32> total#0
                        Parameter<Int32> x#1
                Goto<Void> Break label#1
            Finally
              Call<Void> IDisposable.Dispose
                Parameter<IEnumerator<Int32>> enumerator#2
        Parameter<Int32> total#0
"""

PROPERTY_SETTER = """\
Invoke<Void>
  Lambda<Action> ()
    Block<Void>
      Block<Void>
        Block<Void>
          Call<Void> static Services.CheckExpressionGameScene
            Constant<ProcedureSignature> TestService.set_StringProperty
          Call<Void> static TestService.set_StringProperty
            Constant<String> "set-by-golden"
        Goto<Void> Return label#0
      Label<Void> label#0
"""

COMPREHENSION_WITH_REMOTE_CALL = """\
Call<List<String>> static Enumerable.ToList
  Call<IEnumerable<String>> static Enumerable.Select
    New<List<Double>>
      NewArrayInit<Double[]>
        Constant<Double> 1
        Constant<Double> 2
    Lambda<Func<Double, String>> (x#0)
      Label<String> label#0
        Block<String> (returnValue#1)
          Call<Void> static Services.CheckExpressionGameScene
            Constant<ProcedureSignature> TestService.TestClass_FloatToString
          Assign<String>
            Parameter<String> returnValue#1
            Call<String> TestClass.FloatToString
              Constant<TestClass> <TestClass>
              Convert<Single>
                Parameter<Double> x#0
          Call<Void> static Services.CheckExpressionReturnValue
            Constant<ProcedureSignature> TestService.TestClass_FloatToString
            Parameter<String> returnValue#1
          Parameter<String> returnValue#1
"""

STRUCT_CONSTRUCTION_AND_FIELD = """\
MemberAccess<String> TestStruct.StringField
  Block<TestStruct>
    Call<Void> static Services.CheckExpressionGameScene
      Constant<ProcedureSignature> TestService.StructEcho
    Call<TestStruct> static TestService.StructEcho
      MemberInit<TestStruct>
        New<TestStruct>
        Assignment TestStruct.IntField
          Constant<Int32> 1
        Assignment TestStruct.StringField
          Constant<String> "golden"
        Assignment TestStruct.EnumField
          Convert<TestEnum>
            Constant<Int32> 0
        Assignment TestStruct.ListField
          New<List<Int32>>
            NewArrayInit<Int32[]>
              Constant<Int32> 2
"""


class TestExpressionTree(ServerTestCase, unittest.TestCase):
    """Golden tests for the expression trees the python compiler generates,
    using the server's ExpressionTreePrinter dump. These verify the exact
    tree structure produced by compiling python source, without depending
    on the values the trees evaluate to."""

    maxDiff = None  # pylint: disable=invalid-name

    @classmethod
    def setUpClass(cls) -> None:
        super(TestExpressionTree, cls).setUpClass()

    def dump(self, func):
        expression = self.conn.compile_expression(func)
        return self.conn.test_service.dump_expression_tree(expression)

    def check(self, expected, func):
        self.assertEqual(expected, self.dump(func))

    def test_remote_property_comparison(self):
        self.check(
            REMOTE_PROPERTY_COMPARISON,
            lambda: self.conn.test_service.string_property == "foo",
        )

    def test_true_division(self):
        obj = self.conn.test_service.create_test_object("golden")
        self.check(
            TRUE_DIVISION,
            lambda: obj.int_property / 2,
        )

    def test_math_function(self):
        obj = self.conn.test_service.create_test_object("golden")
        self.check(
            MATH_FUNCTION,
            lambda: math.sqrt(obj.int_property),
        )

    def test_function_with_early_return(self):
        obj = self.conn.test_service.create_test_object("golden")

        def func():
            if obj.int_property < 0:
                return 0
            return obj.int_property

        self.check(FUNCTION_WITH_EARLY_RETURN, func)

    def test_loop_building_value(self):
        def func():
            total: int = 0
            for x in [1, 2, 3]:
                total = total + x
            return total

        self.check(LOOP_BUILDING_VALUE, func)

    def test_property_setter(self):
        def func():
            self.conn.test_service.string_property = "set-by-golden"

        self.check(PROPERTY_SETTER, func)

    def test_struct_construction_and_field(self):
        test_struct = self.conn.test_service.TestStruct
        enum = self.conn.test_service.TestEnum
        echo = self.conn.test_service.struct_echo
        self.check(
            STRUCT_CONSTRUCTION_AND_FIELD,
            lambda: echo(test_struct(1, "golden", enum.value_a, [2])).string_field,
        )

    def test_comprehension_with_remote_call(self):
        obj = self.conn.test_service.create_test_object("golden")
        self.check(
            COMPREHENSION_WITH_REMOTE_CALL,
            lambda: [obj.float_to_string(x) for x in [1.0, 2.0]],
        )


if __name__ == "__main__":
    unittest.main()
