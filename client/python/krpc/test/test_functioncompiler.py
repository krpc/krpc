import math
import unittest

from krpc.error import FunctionCompilationError
from krpc.test.servertestcase import ServerTestCase


class TestFunctionCompiler(ServerTestCase, unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        super(TestFunctionCompiler, cls).setUpClass()

    def evaluate(self, func):
        with self.conn.function_stream(func) as stream:
            return stream()

    def test_constant_folding(self):
        threshold = 5
        self.assertEqual(11, self.evaluate(lambda: threshold * 2 + 1))
        self.assertEqual("foo", self.evaluate(lambda: "fo" + "o"))
        self.assertTrue(self.evaluate(lambda: threshold in [1, 5, 9]))

    def test_remote_property(self):
        self.conn.test_service.string_property = "foo"
        self.assertTrue(
            self.evaluate(lambda: self.conn.test_service.string_property == "foo")
        )
        self.assertFalse(
            self.evaluate(lambda: self.conn.test_service.string_property != "foo")
        )

    def test_mixed_type_arithmetic(self):
        counter = self.conn.test_service.counter
        value = self.evaluate(lambda: counter("Compiler.mixed") * 0.5 + 1)
        self.assertIsInstance(value, float)
        self.assertGreater(value, 1)

    def test_remote_method_with_arguments(self):
        obj = self.conn.test_service.create_test_object("compiled")
        expected = obj.float_to_string(0.5)
        self.assertEqual(expected, self.evaluate(lambda: obj.float_to_string(0.5)))

    def test_keyword_and_default_arguments(self):
        obj = self.conn.test_service.create_test_object("kw")
        expected = obj.optional_arguments("1", z="Z")
        self.assertEqual(
            expected, self.evaluate(lambda: obj.optional_arguments("1", z="Z"))
        )

    def test_static_method(self):
        obj = self.conn.test_service.create_test_object("static")
        cls = type(obj)
        expected = cls.static_method("bob")
        self.assertEqual(expected, self.evaluate(lambda: cls.static_method("bob")))

    def test_chained_calls(self):
        obj = self.conn.test_service.create_test_object("outer")
        inner = self.conn.test_service.create_test_object("inner")
        obj.object_property = inner
        inner.int_property = 7
        self.assertTrue(self.evaluate(lambda: obj.object_property.int_property == 7))
        self.assertEqual(
            "value=inner",
            # The lambda is compiled from source, not called
            # pylint: disable-next=unnecessary-lambda
            self.evaluate(lambda: obj.object_property.get_value()),
        )

    def test_object_equality(self):
        obj = self.conn.test_service.create_test_object("eq")
        other = self.conn.test_service.create_test_object("eq2")
        self.conn.test_service.object_property = obj
        self.assertTrue(
            self.evaluate(lambda: self.conn.test_service.object_property == obj)
        )
        self.assertFalse(
            self.evaluate(lambda: self.conn.test_service.object_property == other)
        )

    def test_enum(self):
        enum = self.conn.test_service.TestEnum
        self.assertTrue(
            self.evaluate(
                lambda: self.conn.test_service.enum_echo(enum.value_b) == enum.value_b
            )
        )

    def test_ternary(self):
        obj = self.conn.test_service.create_test_object("ternary")
        obj.int_property = 1
        self.assertEqual(
            "one", self.evaluate(lambda: "one" if obj.int_property == 1 else "other")
        )
        self.assertEqual(
            2.5, self.evaluate(lambda: 2.5 if obj.int_property == 1 else 2)
        )

    def test_ternary_branches_must_agree(self):
        obj = self.conn.test_service.create_test_object("ternary2")
        other = obj.extension_method_returning_class_from_other_service()
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile_function(lambda: obj if obj.int_property == 1 else other)
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile_function(
                lambda: [obj] if obj.int_property == 1 else [other]
            )

    def test_boolean_operators(self):
        obj = self.conn.test_service.create_test_object("bool")
        obj.int_property = 5
        self.assertTrue(
            self.evaluate(lambda: obj.int_property > 1 and obj.int_property < 10)
        )
        self.assertTrue(
            self.evaluate(lambda: obj.int_property > 10 or not obj.int_property == 4)
        )
        self.assertTrue(self.evaluate(lambda: 1 < obj.int_property < 10))

    def _make_objects(self, prefix):
        objs = [
            self.conn.test_service.create_test_object("%s%d" % (prefix, i))
            for i in range(3)
        ]
        for i, obj in enumerate(objs):
            obj.int_property = i + 1
        return objs

    def test_comprehension_select(self):
        objs = self._make_objects("select")
        self.assertEqual(
            [2, 4, 6], self.evaluate(lambda: [o.int_property * 2 for o in objs])
        )

    def test_comprehension_where(self):
        objs = self._make_objects("where")
        self.assertEqual(
            [2, 3],
            self.evaluate(lambda: [o.int_property for o in objs if o.int_property > 1]),
        )

    def test_aggregations(self):
        objs = self._make_objects("agg")
        self.assertEqual(6, self.evaluate(lambda: sum(o.int_property for o in objs)))
        self.assertEqual(1, self.evaluate(lambda: min(o.int_property for o in objs)))
        self.assertEqual(3, self.evaluate(lambda: max(o.int_property for o in objs)))
        self.assertEqual(3, self.evaluate(lambda: len([o.int_property for o in objs])))

    def test_length_of_a_collection_returned_by_a_call(self):
        self.assertEqual(
            3,
            self.evaluate(
                lambda: len(self.conn.test_service.increment_list([0, 1, 2]))
            ),
        )
        self.assertEqual(
            2,
            self.evaluate(
                lambda: len(
                    self.conn.test_service.increment_dictionary({"a": 0, "b": 1})
                )
            ),
        )

    def test_lambda_written_inside_a_with_statement(self):
        obj = self.conn.test_service.create_test_object("with")
        obj.int_property = 7
        with self.conn.function_stream(lambda: obj.int_property * 2) as stream:
            self.assertEqual(14, stream())

    def test_any_all(self):
        objs = self._make_objects("anyall")
        self.assertTrue(self.evaluate(lambda: any(o.int_property == 2 for o in objs)))
        self.assertFalse(self.evaluate(lambda: any(o.int_property == 4 for o in objs)))
        self.assertTrue(self.evaluate(lambda: all(o.int_property < 4 for o in objs)))
        self.assertFalse(self.evaluate(lambda: all(o.int_property > 1 for o in objs)))

    def test_membership(self):
        objs = self._make_objects("in")
        self.assertTrue(self.evaluate(lambda: 2 in [o.int_property for o in objs]))
        self.assertTrue(self.evaluate(lambda: 4 not in [o.int_property for o in objs]))

    def test_sorted(self):
        objs = self._make_objects("sorted")
        self.assertEqual(
            [3, 2, 1],
            self.evaluate(
                lambda: sorted([o.int_property for o in objs], key=lambda x: -x)
            ),
        )

    def test_subscript(self):
        objs = self._make_objects("subscript")
        self.assertEqual(1, self.evaluate(lambda: [o.int_property for o in objs][0]))

    def test_function_with_assignments(self):
        obj = self.conn.test_service.create_test_object("func")
        obj.int_property = 21

        def expression():
            doubled = obj.int_property * 2
            return doubled + 1

        self.assertEqual(43, self.evaluate(expression))

    def test_event(self):
        counter = self.conn.test_service.counter
        event = self.conn.add_event(lambda: counter("Compiler.event") > 5)
        with event.condition:
            event.wait(5)
            self.assertTrue(event.stream())

    def test_statement_loops(self):
        objs = self._make_objects("stmt")

        def total_of_objects():
            total = 0
            for obj in objs:
                if obj.int_property == 2:
                    continue
                total += obj.int_property
            return total

        self.assertEqual(4, self.conn.run_function(total_of_objects))

    def test_while_loop_with_break(self):
        def count_up():
            i = 0
            while True:
                i += 1
                if i >= 5:
                    break
            return i

        self.assertEqual(5, self.conn.run_function(count_up))

    def test_side_effects(self):
        obj = self.conn.test_service.create_test_object("effects")
        obj.int_property = 1

        def set_property():
            obj.int_property = 42

        self.assertIsNone(self.conn.run_function(set_property))
        self.assertEqual(42, obj.int_property)

    def test_service_property_side_effect(self):
        def set_string():
            self.conn.test_service.string_property = "written by function"

        self.conn.run_function(set_string)
        self.assertEqual("written by function", self.conn.test_service.string_property)

    def test_build_list_in_loop(self):
        objs = self._make_objects("buildlist")

        def doubled():
            result: list[int] = []
            for obj in objs:
                result.append(obj.int_property * 2)
            return result

        self.assertEqual([2, 4, 6], self.conn.run_function(doubled))

    def test_build_dictionary(self):
        def counts():
            values: dict[str, int] = {}
            values["a"] = 1
            values["a"] += 2
            values["b"] = 5
            return values

        self.assertEqual({"a": 3, "b": 5}, self.conn.run_function(counts))

    def test_collection_element_is_converted(self):
        def numbers():
            values: list[float] = []
            values.append(3)
            values.append(4.5)
            return values

        self.assertEqual([3.0, 4.5], self.conn.run_function(numbers))

        def truncated():
            values: list[int] = []
            values.append(3.5)
            return values

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile_function(truncated)

    def test_empty_collection_is_not_a_missing_result(self):
        def empty_list():
            result: list[int] = []
            return result

        def empty_dictionary():
            values: dict[str, int] = {}
            return values

        self.assertEqual([], self.conn.run_function(empty_list))
        self.assertEqual({}, self.conn.run_function(empty_dictionary))

    def test_null_result(self):
        obj = self.conn.test_service.create_test_object("nullresult")
        # The property is nullable and unset, so the function's value is null
        self.assertIsNone(self.conn.run_function(lambda: obj.object_property))
        with self.conn.function_stream(lambda: obj.object_property) as stream:
            self.assertIsNone(stream())
        other = self.conn.test_service.create_test_object("nullresult2")
        obj.object_property = other
        self.assertEqual(other, self.conn.run_function(lambda: obj.object_property))

    def test_early_return(self):
        obj = self.conn.test_service.create_test_object("early")
        obj.int_property = 3

        def classify():
            if obj.int_property > 10:
                return "big"
            return "small"

        self.assertEqual("small", self.conn.run_function(classify))
        obj.int_property = 20
        self.assertEqual("big", self.conn.run_function(classify))

    def test_run_function_with_expression_object(self):
        expression = self.conn.krpc.Expression
        self.assertEqual(
            42,
            self.conn.run_function(
                expression.multiply(
                    expression.constant_int(6), expression.constant_int(7)
                )
            ),
        )

    def test_statement_function_as_stream(self):
        objs = self._make_objects("stmtstream")

        def total():
            value = 0
            for obj in objs:
                value += obj.int_property
            return value

        with self.conn.function_stream(total) as stream:
            self.assertEqual(6, stream())

    def test_true_division(self):
        obj = self.conn.test_service.create_test_object("division")
        obj.int_property = 7
        self.assertEqual(3.5, self.conn.run_function(lambda: obj.int_property / 2))
        self.assertEqual(3, self.conn.run_function(lambda: obj.int_property // 2))
        obj.int_property = -7
        self.assertEqual(-4, self.conn.run_function(lambda: obj.int_property // 2))

    def test_bitwise_operators(self):
        obj = self.conn.test_service.create_test_object("bitwise")
        obj.int_property = 12
        self.assertEqual(8, self.conn.run_function(lambda: obj.int_property & 10))
        self.assertEqual(14, self.conn.run_function(lambda: obj.int_property | 10))
        self.assertEqual(6, self.conn.run_function(lambda: obj.int_property ^ 10))
        self.assertEqual(-13, self.conn.run_function(lambda: ~obj.int_property))

    def test_walrus_operator(self):
        obj = self.conn.test_service.create_test_object("walrus")
        obj.int_property = 4

        def doubled_if_large():
            if (x := obj.int_property * 2) > 5:
                return x
            return 0

        self.assertEqual(8, self.conn.run_function(doubled_if_large))

    def test_multiple_assignment(self):
        obj = self.conn.test_service.create_test_object("multi")
        obj.int_property = 5

        def double_read():
            first = second = obj.int_property
            return first + second

        self.assertEqual(10, self.conn.run_function(double_read))

    def test_conversions(self):
        obj = self.conn.test_service.create_test_object("convert")
        obj.int_property = 7
        self.assertEqual(3, self.conn.run_function(lambda: int(obj.int_property / 2)))
        self.assertEqual(7.0, self.conn.run_function(lambda: float(obj.int_property)))
        self.assertEqual("7", self.conn.run_function(lambda: str(obj.int_property)))

    def test_argument_type_is_not_narrowed(self):
        obj = self.conn.test_service.create_test_object("argument")

        def widened():
            return obj.float_to_string(2)

        self.assertEqual(obj.float_to_string(2), self.conn.run_function(widened))

        def truncated():
            obj.int_property = 2.5

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile_function(truncated)

    def test_abs_round_min_max(self):
        obj = self.conn.test_service.create_test_object("scalar")
        obj.int_property = -7
        self.assertEqual(7, self.conn.run_function(lambda: abs(obj.int_property)))
        self.assertEqual(
            -4, self.conn.run_function(lambda: round(obj.int_property / 2))
        )
        self.assertEqual(-7, self.conn.run_function(lambda: min(obj.int_property, 3)))
        self.assertEqual(
            10, self.conn.run_function(lambda: max(obj.int_property, 3, 10))
        )

    def test_math_functions(self):
        obj = self.conn.test_service.create_test_object("math")
        obj.int_property = 16
        self.assertEqual(
            4.0, self.conn.run_function(lambda: math.sqrt(obj.int_property))
        )
        self.assertAlmostEqual(
            math.pi / 4,
            self.conn.run_function(lambda: math.atan2(obj.int_property, 16)),
        )
        # Client side arguments are still evaluated on the client
        self.assertEqual(2.0, self.conn.run_function(lambda: math.sqrt(4) * 1.0))
        # math.pow gives a float, where ** keeps the type of its operands
        power = self.conn.run_function(lambda: math.pow(obj.int_property, 2))
        self.assertEqual(256.0, power)
        self.assertIsInstance(power, float)
        self.assertEqual(
            4.0, self.conn.run_function(lambda: math.pow(obj.int_property, 0.5))
        )
        self.assertEqual(256, self.conn.run_function(lambda: obj.int_property**2))

    def test_rounding_and_logarithms(self):
        obj = self.conn.test_service.create_test_object("rounding")
        obj.int_property = 5
        self.assertAlmostEqual(
            0.62, self.conn.run_function(lambda: round(obj.int_property / 8, 2))
        )
        # Negative places round to tens and hundreds, and an integer stays one
        self.assertEqual(
            1200, self.conn.run_function(lambda: round(obj.int_property * 247, -2))
        )
        self.assertAlmostEqual(
            3.0, self.conn.run_function(lambda: math.log(obj.int_property * 1.6, 2))
        )
        self.assertAlmostEqual(
            math.log(5), self.conn.run_function(lambda: math.log(obj.int_property))
        )

    def test_math_call_with_too_many_arguments(self):
        obj = self.conn.test_service.create_test_object("matharity")
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile_function(
                lambda: math.sqrt(obj.int_property, 2)  # type: ignore[call-arg]
            )

    def test_stdlib_service(self):
        self.assertEqual(3.0, self.conn.std_lib.sqrt(9))
        self.assertEqual(
            (0.0, 0.0, 1.0),
            self.conn.std_lib.vector_cross((1.0, 0.0, 0.0), (0.0, 1.0, 0.0)),
        )

    def test_vector_math_in_function(self):
        vector = (1.0, 2.0, 2.0)
        magnitude = self.conn.std_lib.vector_magnitude
        self.assertEqual(3.0, self.conn.run_function(lambda: magnitude(vector) * 1.0))

    def test_slicing(self):
        objs = self._make_objects("slice")
        self.assertEqual(
            [2, 3],
            self.conn.run_function(lambda: [o.int_property for o in objs][1:3]),
        )
        self.assertEqual(
            [1, 2],
            self.conn.run_function(lambda: [o.int_property for o in objs][:2]),
        )
        self.assertEqual(
            [2, 3],
            self.conn.run_function(lambda: [o.int_property for o in objs][1:]),
        )

    def test_dict_comprehension(self):
        objs = self._make_objects("dictcomp")
        self.assertEqual(
            {"1": 2, "2": 4, "3": 6},
            self.conn.run_function(
                lambda: {str(o.int_property): o.int_property * 2 for o in objs}
            ),
        )

    def test_nested_comprehension(self):
        objs = self._make_objects("nested")
        factors = [1, 2]
        self.assertEqual(
            [1, 2, 3, 2, 4, 6],
            self.conn.run_function(
                lambda: [x * o.int_property for x in factors for o in objs]
            ),
        )

    def test_fstrings(self):
        obj = self.conn.test_service.create_test_object("fstring")
        obj.int_property = 42
        self.assertEqual(
            "value is 42!",
            self.conn.run_function(lambda: f"value is {obj.int_property}!"),
        )

    def test_local_functions(self):
        objs = self._make_objects("localfn")

        def program():
            def double(x: int):
                return x * 2

            total = 0
            for obj in objs:
                total += double(obj.int_property)
            return total

        self.assertEqual(12, self.conn.run_function(program))

    def test_local_lambda(self):
        obj = self.conn.test_service.create_test_object("locallambda")
        obj.int_property = 20

        def program():
            base = obj.int_property
            # pylint: disable-next=unnecessary-lambda-assignment
            offset = lambda: base + 1  # noqa: E731
            return offset() + offset()

        self.assertEqual(42, self.conn.run_function(program))

    def test_local_function_argument_types(self):
        obj = self.conn.test_service.create_test_object("localargs")
        obj.int_property = 3

        def widened():
            def half(x: float):
                return x / 2

            return half(1) + half(obj.int_property)

        self.assertEqual(2, self.conn.run_function(widened))

        def narrowed():
            def double(x: int):
                return x * 2

            return double(obj.int_property / 2)

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile_function(narrowed)

        def mistyped():
            def double(x: int):
                return x * 2

            return double("two")

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile_function(mistyped)

    def test_unsupported_constructs(self):
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile_function(lambda x: x + 1)

        def exceptions():
            try:
                return 1
            except RuntimeError:
                return 2

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile_function(exceptions)

        def unannotated():
            result = []
            result.append(1)
            return result

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile_function(unannotated)

        obj = self.conn.test_service.create_test_object("unsupported")
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile_function(lambda: f"{obj.int_property:.2f}")

    def test_unknown_member(self):
        obj = self.conn.test_service.create_test_object("unknown")
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile_function(lambda: obj.no_such_member)

    def test_struct_field(self):
        counter_struct = self.conn.test_service.counter_struct
        self.assertEqual(
            "Compiler.field",
            self.evaluate(lambda: counter_struct("Compiler.field").string_field),
        )
        self.assertTrue(
            self.evaluate(lambda: counter_struct("Compiler.field2").int_field >= 0)
        )

    def test_nested_struct_field(self):
        enum = self.conn.test_service.TestEnum
        obj = self.conn.test_service.create_test_object("nested")
        struct = self.conn.test_service.TestStruct(1, "inner", enum.value_a, [2])
        nested = self.conn.test_service.TestNestedStruct(struct, obj, "outer")
        echo = self.conn.test_service.nested_struct_echo
        self.assertEqual(
            "inner", self.evaluate(lambda: echo(nested).struct_field.string_field)
        )
        self.assertEqual("outer", self.evaluate(lambda: echo(nested).string_field))

    def test_construct_struct(self):
        test_struct = self.conn.test_service.TestStruct
        enum = self.conn.test_service.TestEnum
        echo = self.conn.test_service.struct_echo
        value = self.evaluate(
            lambda: echo(test_struct(3, "built", enum.value_c, [1, 2]))
        )
        self.assertEqual(3, value.int_field)
        self.assertEqual("built", value.string_field)
        self.assertEqual(enum.value_c, value.enum_field)
        self.assertEqual([1, 2], value.list_field)

    def test_construct_struct_with_field_names(self):
        enum = self.conn.test_service.TestEnum
        echo = self.conn.test_service.struct_echo
        value = self.evaluate(
            lambda: echo(
                self.conn.test_service.TestStruct(
                    3, list_field=[1], string_field="named", enum_field=enum.value_a
                )
            )
        )
        self.assertEqual(3, value.int_field)
        self.assertEqual("named", value.string_field)
        self.assertEqual([1], value.list_field)

    def test_captured_struct_value(self):
        enum = self.conn.test_service.TestEnum
        struct = self.conn.test_service.TestStruct(9, "captured", enum.value_b, [4])
        echo = self.conn.test_service.struct_echo
        self.assertEqual("captured", self.evaluate(lambda: echo(struct).string_field))

    def test_struct_field_errors(self):
        test_struct = self.conn.test_service.TestStruct
        enum = self.conn.test_service.TestEnum
        echo = self.conn.test_service.struct_echo
        struct = test_struct(1, "x", enum.value_a, [2])
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile_function(lambda: echo(struct).no_such_field)
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile_function(lambda: echo(test_struct(1, "x")))
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile_function(
                lambda: echo(test_struct(1, "x", enum.value_a, [2], no_such_field=1))
            )

    def _text(self):
        self.conn.test_service.string_property = "Hello, World"
        return lambda: self.conn.test_service.string_property

    def test_string_length_and_indexing(self):
        text = self._text()
        self.assertEqual(12, self.evaluate(lambda: len(text())))
        self.assertEqual("H", self.evaluate(lambda: text()[0]))
        self.assertEqual("d", self.evaluate(lambda: text()[11]))

    def test_string_slicing(self):
        text = self._text()
        self.assertEqual("World", self.evaluate(lambda: text()[7:12]))
        self.assertEqual("Hello", self.evaluate(lambda: text()[:5]))
        self.assertEqual("World", self.evaluate(lambda: text()[7:]))

    def test_string_methods(self):
        text = self._text()
        self.assertEqual("HELLO, WORLD", self.evaluate(lambda: text().upper()))
        self.assertEqual("hello, world", self.evaluate(lambda: text().lower()))
        self.assertEqual(
            "Hello, Mars", self.evaluate(lambda: text().replace("World", "Mars"))
        )
        self.assertEqual(["Hello", "World"], self.evaluate(lambda: text().split(", ")))
        self.assertEqual(7, self.evaluate(lambda: text().find("World")))
        self.assertEqual(-1, self.evaluate(lambda: text().find("Mars")))
        self.assertTrue(self.evaluate(lambda: text().startswith("Hello")))
        self.assertTrue(self.evaluate(lambda: text().endswith("World")))
        self.assertEqual(
            "Hello|World", self.evaluate(lambda: "|".join(text().split(", ")))
        )

    def test_string_containment(self):
        text = self._text()
        self.assertTrue(self.evaluate(lambda: "World" in text()))
        self.assertFalse(self.evaluate(lambda: "Mars" in text()))
        self.assertTrue(self.evaluate(lambda: "Mars" not in text()))

    def test_string_trimming(self):
        self.conn.test_service.string_property = "  pad  "

        def text():
            return self.conn.test_service.string_property

        self.assertEqual("pad", self.evaluate(lambda: text().strip()))
        self.assertEqual("pad  ", self.evaluate(lambda: text().lstrip()))
        self.assertEqual("  pad", self.evaluate(lambda: text().rstrip()))

    def test_raise(self):
        def failing():
            raise RuntimeError("no good")

        with self.assertRaises(RuntimeError) as caught:
            self.conn.run_function(failing)
        self.assertIn("no good", str(caught.exception))

    def test_raise_a_computed_message(self):
        text = self._text()

        def failing():
            raise ValueError("bad: " + text())

        with self.assertRaises(ValueError) as caught:
            self.conn.run_function(failing)
        self.assertIn("bad: Hello, World", str(caught.exception))

    def test_try_except(self):
        def guarded() -> str:
            try:
                raise RuntimeError("inner")
            except RuntimeError as exn:
                return "caught " + exn
            return "not reached"

        self.assertEqual("caught inner", self.conn.run_function(guarded))

    def test_try_except_all(self):
        def guarded() -> str:
            try:
                raise ValueError("inner")
            except Exception as exn:  # pylint: disable=broad-exception-caught
                return "caught " + exn
            return "not reached"

        self.assertEqual("caught inner", self.conn.run_function(guarded))

    def test_try_except_does_not_catch_another_exception(self):
        destroyed = self.conn.krpc.ObjectDestroyedException

        def guarded() -> str:
            try:
                raise ValueError("inner")
            except destroyed:
                return "wrong"
            return "not reached"

        with self.assertRaises(ValueError):
            self.conn.run_function(guarded)

    def test_try_finally(self):
        def guarded() -> str:
            result = "start"
            try:
                try:
                    raise RuntimeError("inner")
                finally:
                    result = "cleaned"
            except Exception:  # pylint: disable=broad-exception-caught
                pass
            return result

        self.assertEqual("cleaned", self.conn.run_function(guarded))

    def test_unsupported_raise(self):
        def bare():
            raise  # pylint: disable=misplaced-bare-raise

        def not_an_exception():
            raise len("x")  # pylint: disable=raising-bad-type

        for func in (bare, not_an_exception):
            with self.assertRaises(FunctionCompilationError):
                self.conn.compile_function(func)

    def test_min_max_by_key(self):
        objs = self._make_objects("minmaxby")
        self.assertEqual(
            1,
            self.conn.run_function(
                lambda: min(objs, key=lambda o: o.int_property).int_property
            ),
        )
        self.assertEqual(
            3,
            self.conn.run_function(
                lambda: max(objs, key=lambda o: o.int_property).int_property
            ),
        )

    def test_reversed(self):
        objs = self._make_objects("reversed")
        self.assertEqual(
            [3, 2, 1],
            self.conn.run_function(lambda: [o.int_property for o in reversed(objs)]),
        )

    def test_list_removal(self):
        objs = self._make_objects("listremoval")

        def trimmed() -> list:
            values = [o.int_property for o in objs]
            values.remove(2)
            return values

        def cleared() -> list:
            values = [o.int_property for o in objs]
            values.clear()
            return values

        def deleted() -> list:
            values = [o.int_property for o in objs]
            del values[0]
            return values

        self.assertEqual([1, 3], self.conn.run_function(trimmed))
        self.assertEqual([], self.conn.run_function(cleared))
        self.assertEqual([2, 3], self.conn.run_function(deleted))

    def test_set_removal(self):
        objs = self._make_objects("setremoval")

        def trimmed() -> int:
            values = {o.int_property for o in objs}
            values.discard(2)
            return len(values)

        self.assertEqual(2, self.conn.run_function(trimmed))

    def test_dictionary_keys_values_and_removal(self):
        objs = self._make_objects("dictops")

        def keys() -> list:
            values = {o.int_property: o.int_property * 2 for o in objs}
            return sorted(values.keys())

        def values_of() -> list:
            values = {o.int_property: o.int_property * 2 for o in objs}
            return sorted(values.values())

        def without_a_key() -> list:
            values = {o.int_property: o.int_property * 2 for o in objs}
            del values[1]
            return sorted(values.keys())

        def cleared() -> list:
            values = {o.int_property: o.int_property * 2 for o in objs}
            values.clear()
            return sorted(values.keys())

        self.assertEqual([1, 2, 3], self.conn.run_function(keys))
        self.assertEqual([2, 4, 6], self.conn.run_function(values_of))
        self.assertEqual([2, 3], self.conn.run_function(without_a_key))
        self.assertEqual([], self.conn.run_function(cleared))

    def test_iterate_over_dictionary_keys(self):
        objs = self._make_objects("dictloop")

        def total() -> int:
            values = {o.int_property: o.int_property * 2 for o in objs}
            result = 0
            for key in values.keys():
                result += values[key]
            return result

        self.assertEqual(12, self.conn.run_function(total))

    def test_unsupported_delete(self):
        objs = self._make_objects("baddelete")

        def whole_name():
            values = [o.int_property for o in objs]
            del values

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile_function(whole_name)

    def test_variable_type_is_not_narrowed(self):
        def widened():
            value: float = 0
            value = 1
            return value + 0.5

        self.assertEqual(1.5, self.conn.run_function(widened))

        def annotated():
            count: float = 1
            count = count / 2
            return count

        self.assertEqual(0.5, self.conn.run_function(annotated))

        def truncated():
            count = 1
            count = 1.5
            return count

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile_function(truncated)

        def divided():
            count = 1
            count = count / 2
            return count

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile_function(divided)

    def test_variable_type_cannot_be_reannotated(self):
        def reannotated():
            count: int = 1
            count: float = 2
            return count

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile_function(reannotated)

    def test_variable_keeps_its_element_type(self):
        def mismatched():
            values: list[float] = []
            values = [1, 2]
            return values

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile_function(mismatched)


if __name__ == "__main__":
    unittest.main()
