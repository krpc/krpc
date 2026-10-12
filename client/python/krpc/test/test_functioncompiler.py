# One test per construct the compiler accepts, so the module is long
# pylint: disable=too-many-lines

import functools
import linecache
import math
import time
import unittest
from typing import Any, Callable, List

from krpc.error import FunctionCompilationError
from krpc.test.servertestcase import ServerTestCase


class TestFunctionCompiler(ServerTestCase, unittest.TestCase):
    def evaluate(self, func: Callable[[], Any]) -> Any:
        with self.conn.stream(func) as stream:
            return stream()

    def test_constant_folding(self) -> None:
        threshold = 5
        self.assertEqual(11, self.conn.run(lambda: threshold * 2 + 1))
        self.assertEqual("foo", self.conn.run(lambda: "fo" + "o"))
        self.assertTrue(self.conn.run(lambda: threshold in [1, 5, 9]))
        zero = 0
        self.assertEqual(10, self.conn.run(lambda: zero or 10))
        self.assertEqual(0, self.conn.run(lambda: zero and 10))
        self.assertEqual("b", self.conn.run(lambda: "a" and "b"))

    def test_compiled_function_runs_repeatedly(self) -> None:
        obj = self.conn.test_service.create_test_object("compiled")
        obj.int_property = 3
        function = self.conn.compile(lambda: obj.int_property * 2)
        self.assertEqual(6, self.conn.run(function))
        obj.int_property = 4
        self.assertEqual(8, self.conn.run(function))

    def test_remote_property(self) -> None:
        self.conn.test_service.string_property = "foo"
        self.assertTrue(
            self.evaluate(lambda: self.conn.test_service.string_property == "foo")
        )
        self.assertFalse(
            self.evaluate(lambda: self.conn.test_service.string_property != "foo")
        )

    def test_mixed_type_arithmetic(self) -> None:
        counter = self.conn.test_service.counter
        value = self.evaluate(lambda: counter("Compiler.mixed") * 0.5 + 1)
        self.assertIsInstance(value, float)
        self.assertGreater(value, 1)

    def test_remote_method_with_arguments(self) -> None:
        obj = self.conn.test_service.create_test_object("compiled")
        expected = obj.float_to_string(0.5)
        self.assertEqual(expected, self.evaluate(lambda: obj.float_to_string(0.5)))

    def test_keyword_and_default_arguments(self) -> None:
        obj = self.conn.test_service.create_test_object("kw")
        expected = obj.optional_arguments("1", z="Z")
        self.assertEqual(
            expected, self.evaluate(lambda: obj.optional_arguments("1", z="Z"))
        )

    def test_static_method(self) -> None:
        obj = self.conn.test_service.create_test_object("static")
        cls = type(obj)
        expected = cls.static_method("bob")
        self.assertEqual(expected, self.evaluate(lambda: cls.static_method("bob")))

    def test_static_method_of_a_class_accessed_through_a_service(self) -> None:
        obj = self.conn.test_service.create_test_object("staticservice")
        cls = self.conn.test_service.TestClass
        self.assertEqual(
            cls.static_method("value=staticservice"),
            self.evaluate(lambda: cls.static_method(obj.get_value())),
        )
        service = self.conn.test_service
        self.assertEqual(
            cls.static_method("value=staticservice"),
            self.evaluate(lambda: service.TestClass.static_method(obj.get_value())),
        )

    def test_chained_calls(self) -> None:
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

    def test_object_equality(self) -> None:
        obj = self.conn.test_service.create_test_object("eq")
        other = self.conn.test_service.create_test_object("eq2")
        self.conn.test_service.object_property = obj
        self.assertTrue(
            self.evaluate(lambda: self.conn.test_service.object_property == obj)
        )
        self.assertFalse(
            self.evaluate(lambda: self.conn.test_service.object_property == other)
        )

    def test_enum(self) -> None:
        enum = self.conn.test_service.TestEnum
        self.assertTrue(
            self.evaluate(
                lambda: self.conn.test_service.enum_echo(enum.value_b) == enum.value_b
            )
        )

    def test_ternary(self) -> None:
        obj = self.conn.test_service.create_test_object("ternary")
        obj.int_property = 1
        self.assertEqual(
            "one", self.evaluate(lambda: "one" if obj.int_property == 1 else "other")
        )
        self.assertEqual(
            2.5, self.evaluate(lambda: 2.5 if obj.int_property == 1 else 2)
        )

    def test_lambda_taking_arguments(self) -> None:
        for func in (lambda x: x, lambda *args: args, lambda **kwargs: kwargs):
            with self.assertRaisesRegex(
                FunctionCompilationError, "a lambda with parameters cannot be compiled"
            ):
                self.conn.compile(func)

    def test_error_evaluating_a_constant(self) -> None:
        obj = self.conn.test_service.create_test_object("constanterror")
        values = [1, 2]
        for func in (
            lambda: obj.int_property + 1 // 0,
            lambda: obj.int_property + values[5],
            lambda: obj.int_property + int("x"),
            lambda: obj.int_property + round(1.5, "a"),
            lambda: obj.int_property + min(1, "a"),
            lambda: obj.int_property > 0 and values < 1,
            lambda: obj.int_property if obj.int_property > 0 else 1 // 0,
        ):
            with self.assertRaisesRegex(FunctionCompilationError, r"\(line \d+\)$"):
                self.conn.compile(func)

    def test_ternary_branches_must_agree(self) -> None:
        obj = self.conn.test_service.create_test_object("ternary2")
        other = obj.extension_method_returning_class_from_other_service()
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(lambda: obj if obj.int_property == 1 else other)
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(lambda: [obj] if obj.int_property == 1 else [other])

    def test_boolean_operators(self) -> None:
        obj = self.conn.test_service.create_test_object("bool")
        obj.int_property = 5
        self.assertTrue(
            self.evaluate(lambda: obj.int_property > 1 and obj.int_property < 10)
        )
        self.assertTrue(
            self.evaluate(lambda: obj.int_property > 10 or not obj.int_property == 4)
        )
        self.assertTrue(self.evaluate(lambda: 1 < obj.int_property < 10))

    def _make_objects(self, prefix: str) -> List[Any]:
        objs = [
            self.conn.test_service.create_test_object("%s%d" % (prefix, i))
            for i in range(3)
        ]
        for i, obj in enumerate(objs):
            obj.int_property = i + 1
        return objs

    def test_comprehension_select(self) -> None:
        objs = self._make_objects("select")
        self.assertEqual(
            [2, 4, 6], self.evaluate(lambda: [o.int_property * 2 for o in objs])
        )

    def test_comprehension_where(self) -> None:
        objs = self._make_objects("where")
        self.assertEqual(
            [2, 3],
            self.evaluate(lambda: [o.int_property for o in objs if o.int_property > 1]),
        )

    def test_aggregations(self) -> None:
        objs = self._make_objects("agg")
        self.assertEqual(6, self.evaluate(lambda: sum(o.int_property for o in objs)))
        self.assertEqual(1, self.evaluate(lambda: min(o.int_property for o in objs)))
        self.assertEqual(3, self.evaluate(lambda: max(o.int_property for o in objs)))
        self.assertEqual(3, self.evaluate(lambda: len([o.int_property for o in objs])))

    def test_length_of_a_collection_returned_by_a_call(self) -> None:
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

    def test_lambda_written_inside_a_with_statement(self) -> None:
        obj = self.conn.test_service.create_test_object("with")
        obj.int_property = 7
        with self.conn.stream(lambda: obj.int_property * 2) as stream:
            self.assertEqual(14, stream())

    def test_lambda_on_its_own_line_before_the_closing_bracket(self) -> None:
        obj = self.conn.test_service.create_test_object("ownline")
        obj.int_property = 7
        # fmt: off
        self.assertEqual(14, self.conn.run(
            lambda: obj.int_property * 2))
        self.assertEqual([7, 8], self.conn.run(
            lambda: [obj.int_property,
                     obj.int_property + 1]))
        # fmt: on

    def test_deeply_nested_expression(self) -> None:
        obj = self.conn.test_service.create_test_object("deep")
        # The source is registered with linecache, where the compiler reads it from
        source = "make = lambda obj: lambda: %s\n" % " + ".join(
            ["obj.int_property"] * 1000
        )
        linecache.cache["<deep>"] = (len(source), None, [source], "<deep>")
        namespace = {}
        exec(compile(source, "<deep>", "exec"), namespace)  # pylint: disable=exec-used
        with self.assertRaisesRegex(FunctionCompilationError, "too deeply nested"):
            self.conn.compile(namespace["make"](obj))

    def test_lambda_continued_on_the_next_line(self) -> None:
        obj = self.conn.test_service.create_test_object("continued")
        obj.int_property = 5
        other = self.conn.test_service.create_test_object("continued2")
        other.int_property = 2
        # fmt: off
        difference = (
            lambda: obj.int_property  # pylint: disable=unnecessary-lambda-assignment
            - other.int_property)
        self.assertEqual(3, self.conn.run(difference))
        self.assertEqual(6, self.conn.run(
            lambda: obj.int_property
            + 1))
        with self.conn.stream(
                lambda: obj.int_property
                - other.int_property) as stream:
            self.assertEqual(3, stream())
        both = (lambda: obj.int_property, lambda: other.int_property)
        # fmt: on
        self.assertEqual([5, 2], [self.conn.run(f) for f in both])

    def test_any_all(self) -> None:
        objs = self._make_objects("anyall")
        self.assertTrue(self.evaluate(lambda: any(o.int_property == 2 for o in objs)))
        self.assertFalse(self.evaluate(lambda: any(o.int_property == 4 for o in objs)))
        self.assertTrue(self.evaluate(lambda: all(o.int_property < 4 for o in objs)))
        self.assertFalse(self.evaluate(lambda: all(o.int_property > 1 for o in objs)))

    def test_any_and_all_over_several_for_clauses_stop_early(self) -> None:
        obj = self.conn.test_service.create_test_object("anyall")
        obj.int_property = 1
        # A value after the one deciding the result would divide by zero
        self.assertTrue(
            self.conn.run(
                lambda: any(
                    10 // x for x in [obj.int_property, 0] for _ in [obj.int_property]
                )
            )
        )
        self.assertFalse(
            self.conn.run(
                lambda: all(x - 1 for x in [obj.int_property, 0] for _ in [10 // x])
            )
        )
        self.assertTrue(
            self.conn.run(
                lambda: all(
                    x + y > 0 for x in [obj.int_property] if x > 0 for y in [1, 2]
                )
            )
        )

    def test_membership(self) -> None:
        objs = self._make_objects("in")
        self.assertTrue(self.evaluate(lambda: 2 in [o.int_property for o in objs]))
        self.assertTrue(self.evaluate(lambda: 4 not in [o.int_property for o in objs]))

    def test_membership_evaluates_the_value_first(self) -> None:
        counter = self.conn.test_service.counter
        # The counter gives 1 on its first call and 2 on its second, so the test
        # holds only when the value is computed before the container
        self.assertTrue(
            self.conn.run(
                lambda: counter("Compiler.inlist") in [1, counter("Compiler.inlist")]
            )
        )
        self.assertTrue(
            self.conn.run(
                lambda: counter("Compiler.indict")
                in {1: 0, counter("Compiler.indict"): 0}
            )
        )
        self.assertTrue(
            self.conn.run(
                lambda: str(counter("Compiler.instr"))
                in "1" + str(counter("Compiler.instr"))
            )
        )

    def test_set_display(self) -> None:
        obj = self.conn.test_service.create_test_object("set")
        obj.int_property = 3
        self.assertEqual({1, 3}, self.evaluate(lambda: {obj.int_property, 1}))
        self.assertTrue(self.evaluate(lambda: obj.int_property in {1, 3}))
        captured = {2, 3}
        self.assertTrue(self.evaluate(lambda: obj.int_property in captured))

    def test_sorted(self) -> None:
        objs = self._make_objects("sorted")
        self.assertEqual(
            [3, 2, 1],
            self.evaluate(
                lambda: sorted([o.int_property for o in objs], key=lambda x: -x)
            ),
        )
        with self.assertRaisesRegex(FunctionCompilationError, "'reverse'"):
            self.conn.compile(
                lambda: sorted([o.int_property for o in objs], reverse=True)
            )

    def test_subscript(self) -> None:
        objs = self._make_objects("subscript")
        self.assertEqual(1, self.evaluate(lambda: [o.int_property for o in objs][0]))

    def test_client_side_function_is_rejected(self) -> None:
        limit = 3
        with self.assertRaises(FunctionCompilationError):
            self.conn.add_event(lambda: limit > 1)
        with self.assertRaises(FunctionCompilationError):
            self.conn.add_stream(lambda: limit + 1)
        self.assertEqual(4, self.conn.run(lambda: limit + 1))
        service = self.conn.test_service
        service.string_property = "stream"
        with self.conn.stream(getattr, service, "string_property") as stream:
            with self.assertRaisesRegex(FunctionCompilationError, "stream"):
                self.conn.add_event(lambda: stream() == "stream")
            with self.assertRaisesRegex(FunctionCompilationError, "stream"):
                self.conn.run(lambda: stream() + service.string_property)

    def test_event(self) -> None:
        counter = self.conn.test_service.counter
        event = self.conn.add_event(lambda: counter("Compiler.event") > 5)
        with event.condition:
            event.wait(5)
            self.assertTrue(event.stream())

    def test_event_calling_a_procedure_that_pauses(self) -> None:
        blocking = self.conn.test_service.blocking_procedure
        event = self.conn.add_event(lambda: blocking(2) > 0)
        with event.condition:
            with self.assertRaisesRegex(
                RuntimeError, "A function is evaluated within a single tick"
            ):
                event.wait(5)

    def test_stream_calling_a_procedure_that_pauses(self) -> None:
        blocking = self.conn.test_service.blocking_procedure
        with self.conn.stream(lambda: blocking(2) + 1) as stream:
            with self.assertRaisesRegex(
                RuntimeError, "A function is evaluated within a single tick"
            ):
                stream()

    def test_tuple_argument_elements_widen(self) -> None:
        obj = self.conn.test_service.create_test_object("tuple")
        obj.int_property = 3
        self.assertEqual(
            (4, 6),
            self.evaluate(
                lambda: self.conn.test_service.increment_tuple((obj.int_property, 5))
            ),
        )

    def test_is_none(self) -> None:
        obj = self.conn.test_service.create_test_object("isnone")
        self.assertTrue(self.conn.run(lambda: obj.object_property is None))
        self.assertFalse(self.conn.run(lambda: obj.object_property is not None))
        obj.object_property = obj
        self.assertFalse(self.conn.run(lambda: obj.object_property is None))
        self.assertTrue(self.conn.run(lambda: None is not obj.object_property))
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(lambda: obj.object_property is obj)

    def test_value_type_is_never_none(self) -> None:
        obj = self.conn.test_service.create_test_object("nevernone")
        counter = self.conn.test_service.counter
        self.assertFalse(self.conn.run(lambda: obj.int_property is None))
        self.assertTrue(self.conn.run(lambda: obj.int_property is not None))
        self.assertTrue(
            self.conn.run(
                lambda: self.conn.test_service.echo_nullable_int(None) is None
            )
        )
        # The operand is still computed, once
        self.assertFalse(self.conn.run(lambda: counter("Compiler.nevernone") is None))
        self.assertEqual(2, counter("Compiler.nevernone"))

    def test_enumerate_evaluates_the_collection_first(self) -> None:
        counter = self.conn.test_service.counter
        # The counter gives 1 on its first call and 2 on its second
        self.assertEqual(
            [(2, 1)],
            self.conn.run(
                lambda: enumerate(
                    [counter("Compiler.enumerate")], counter("Compiler.enumerate")
                )
            ),
        )

    def test_tuple_length_and_membership(self) -> None:
        obj = self.conn.test_service.create_test_object("tuplein")
        obj.int_property = 5
        counter = self.conn.test_service.counter
        self.assertEqual(2, self.conn.run(lambda: len((obj.int_property, 1))))
        self.assertTrue(self.conn.run(lambda: obj.int_property in (1, 5)))
        self.assertTrue(self.conn.run(lambda: obj.int_property not in (1, 2)))
        self.assertTrue(self.conn.run(lambda: 5 in (obj.int_property, 2)))
        service = self.conn.test_service
        service.string_property = "c"
        self.assertFalse(self.conn.run(lambda: service.string_property in ("a", "b")))
        # The value is computed once, and the elements are compared until one is equal
        self.assertTrue(
            self.conn.run(lambda: counter("Compiler.tuplein") in (1, obj.int_property))
        )
        self.assertEqual(2, counter("Compiler.tuplein"))
        self.assertEqual(
            2, self.conn.run(lambda: len((counter("Compiler.tuplelen"), 1)))
        )
        self.assertEqual(2, counter("Compiler.tuplelen"))

    def test_tuple_slice(self) -> None:
        obj = self.conn.test_service.create_test_object("tupleslice")
        obj.int_property = 5
        self.assertEqual(
            (5, "a"), self.conn.run(lambda: (obj.int_property, "a", 2.0)[0:2])
        )
        self.assertEqual(
            ("a", 2.0), self.conn.run(lambda: (obj.int_property, "a", 2.0)[-2:])
        )
        self.assertEqual((5,), self.conn.run(lambda: (obj.int_property, "a")[:1]))
        with self.assertRaisesRegex(
            FunctionCompilationError, r"cannot be empty \(line \d+\)$"
        ):
            self.conn.compile(lambda: (obj.int_property, "a")[2:])

    def test_empty_collection_of_another_kind(self) -> None:
        obj = self.conn.test_service.create_test_object("emptykind")
        with self.assertRaisesRegex(
            FunctionCompilationError,
            r"an empty list cannot be given a set type; use set\(\) \(line \d+\)$",
        ):
            # pylint: disable-next=use-implicit-booleaness-not-comparison
            self.conn.compile(lambda: [] == {obj.int_property})

    def test_messages_name_the_construct(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("messages")
        for func, message in (
            (lambda: f"{obj.int_property=}", "the = specifier is not supported"),
            (lambda: f"{obj.int_property!r}", "the !r and !a conversions"),
            (
                lambda: service.echo_test_object(obj).get_value,
                "'get_value' is a remote method, which needs a call",
            ),
        ):
            with self.assertRaisesRegex(
                FunctionCompilationError, message + r".* \(line \d+\)$"
            ):
                self.conn.compile(func)

    def test_null_result(self) -> None:
        obj = self.conn.test_service.create_test_object("nullresult")
        # The property is nullable and unset, so the function's value is null
        self.assertIsNone(self.conn.run(lambda: obj.object_property))
        with self.conn.stream(lambda: obj.object_property) as stream:
            self.assertIsNone(stream())
        other = self.conn.test_service.create_test_object("nullresult2")
        obj.object_property = other
        self.assertEqual(other, self.conn.run(lambda: obj.object_property))

    def test_is_none_of_a_value_never_none(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("nevernone")
        # The server rejects a test for null of a value that cannot be null
        self.assertFalse(self.conn.run(lambda: obj.get_value() is None))
        self.assertTrue(self.conn.run(lambda: obj.get_value() is not None))
        self.assertEqual(
            "yes",
            self.conn.run(lambda: "yes" if service.not_nullable_object(obj) else "no"),
        )

    def test_truth_of_a_nullable_string(self) -> None:
        service = self.conn.test_service
        self.assertEqual(
            "no",
            self.conn.run(
                lambda: "yes" if service.echo_nullable_string(None) else "no"
            ),
        )
        self.assertEqual(
            "no",
            self.conn.run(lambda: "yes" if service.echo_nullable_string("") else "no"),
        )
        self.assertEqual(
            "yes",
            self.conn.run(lambda: "yes" if service.echo_nullable_string("a") else "no"),
        )

    def test_truth_of_a_nullable_bool(self) -> None:
        obj = self.conn.test_service.create_test_object("nullablebool")
        for value, expected in ((0, "no"), (1, "no"), (2, "yes")):
            obj.int_property = value
            self.assertEqual(
                expected,
                self.conn.run(
                    lambda: (
                        "yes"
                        if (obj.int_property > 1 if obj.int_property != 0 else None)
                        else "no"
                    )
                ),
            )

    def test_boolean_operators_on_a_nullable_bool(self) -> None:
        obj = self.conn.test_service.create_test_object("nullableboolop")
        for value, either, both in (
            (0, False, None),
            (1, False, False),
            (2, True, True),
        ):
            obj.int_property = value
            self.assertEqual(
                either,
                self.conn.run(
                    lambda: (obj.int_property > 1 if obj.int_property != 0 else None)
                    or False
                ),
            )
            self.assertEqual(
                both,
                self.conn.run(
                    lambda: (obj.int_property > 1 if obj.int_property != 0 else None)
                    and True
                ),
            )

    def test_bitwise_operators_on_a_nullable_bool(self) -> None:
        obj = self.conn.test_service.create_test_object("nullableboolbits")
        obj.int_property = 2
        self.assertEqual(
            (True, True, False),
            self.conn.run(
                lambda: (
                    (obj.int_property > 1 if obj.int_property != 0 else None) & True,
                    (obj.int_property > 1 if obj.int_property != 0 else None) | False,
                    (obj.int_property > 1 if obj.int_property != 0 else None) ^ True,
                )
            ),
        )
        obj.int_property = 0
        with self.assertRaises(TypeError):
            self.conn.run(
                lambda: (obj.int_property > 1 if obj.int_property != 0 else None) & True
            )

    def test_none_compared_with_a_typed_value(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("nonecompared")
        obj.int_property = 1
        # pylint: disable=singleton-comparison
        self.assertFalse(
            self.conn.run(lambda: service.int32_to_string(obj.int_property) == None)
        )
        self.assertTrue(
            self.conn.run(lambda: service.echo_nullable_string(None) == None)
        )
        # pylint: enable=singleton-comparison
        self.assertTrue(
            self.conn.run(lambda: None in [service.echo_nullable_string(None), "a"])
        )
        self.assertFalse(
            self.conn.run(
                lambda: None in (service.int32_to_string(obj.int_property), "b")
            )
        )
        self.assertTrue(self.conn.run(lambda: obj.int_property in (1, None)))
        self.assertFalse(
            self.conn.run(lambda: service.echo_nullable_int(None) in [1.5, 2.0])
        )

    def test_conversion_of_a_none_number(self) -> None:
        nullable = self.conn.test_service.echo_nullable_int
        for func in (
            lambda: int(nullable(None)),
            lambda: float(nullable(None)),
            lambda: round(nullable(None)),
            lambda: round(nullable(None), 1),
            lambda: round(nullable(None), -1),
            lambda: abs(nullable(None)),
        ):
            with self.assertRaises(TypeError):
                self.conn.run(func)
        self.assertEqual(3, self.conn.run(lambda: int(nullable(3))))
        self.assertEqual(-40, self.conn.run(lambda: round(nullable(-35), -1)))
        self.assertEqual(35, self.conn.run(lambda: round(nullable(35), 1)))

    def test_run_function_with_expression_object(self) -> None:
        expression = self.conn.krpc.Expression
        self.assertEqual(
            42,
            self.conn.run(
                expression.multiply(
                    expression.constant_int(6), expression.constant_int(7)
                )
            ),
        )

    def test_true_division(self) -> None:
        obj = self.conn.test_service.create_test_object("division")
        obj.int_property = 7
        self.assertEqual(3.5, self.conn.run(lambda: obj.int_property / 2))
        self.assertEqual(3, self.conn.run(lambda: obj.int_property // 2))
        obj.int_property = -7
        self.assertEqual(-4, self.conn.run(lambda: obj.int_property // 2))

    def test_modulo_takes_the_sign_of_the_divisor(self) -> None:
        obj = self.conn.test_service.create_test_object("modulo")
        obj.int_property = -30
        self.assertEqual(330, self.conn.run(lambda: obj.int_property % 360))
        self.assertEqual(329.5, self.conn.run(lambda: (obj.int_property - 0.5) % 360))
        self.assertEqual(-330, self.conn.run(lambda: -obj.int_property % -360))
        obj.int_property = -7
        self.assertEqual(-4, self.conn.run(lambda: 360 % obj.int_property))
        self.assertEqual(3, self.conn.run(lambda: 10 % -obj.int_property))

    def test_modulo_is_exact(self) -> None:
        obj = self.conn.test_service.create_test_object("modulo exact")
        obj.int_property = 1
        self.assertEqual(
            0.1 % 360, self.conn.run(lambda: (obj.int_property * 0.1) % 360)
        )
        self.assertEqual(
            1e-17 % 360,
            self.conn.run(lambda: (obj.int_property * 1e-17) % 360),
        )
        self.assertEqual(
            -0.1 % 360, self.conn.run(lambda: (obj.int_property * -0.1) % 360)
        )
        obj.int_property = 1_500_000_000
        self.assertEqual(
            1_500_000_000,
            self.conn.run(lambda: obj.int_property % 2_000_000_000),
        )
        obj.int_property = 720
        self.assertEqual(0, self.conn.run(lambda: obj.int_property % -360))

    def test_bitwise_operators(self) -> None:
        obj = self.conn.test_service.create_test_object("bitwise")
        obj.int_property = 12
        self.assertEqual(8, self.conn.run(lambda: obj.int_property & 10))
        self.assertEqual(14, self.conn.run(lambda: obj.int_property | 10))
        self.assertEqual(6, self.conn.run(lambda: obj.int_property ^ 10))
        self.assertEqual(-13, self.conn.run(lambda: ~obj.int_property))
        # A uint is inverted as a long, as it is negated
        uint = self.conn.test_service.uint32_special_defaults
        self.assertEqual(-6, self.conn.run(lambda: ~uint(5)[0]))
        ulong = self.conn.test_service.uint64_special_defaults
        with self.assertRaisesRegex(FunctionCompilationError, "cannot invert"):
            self.conn.compile(lambda: ~ulong(5)[0])
        big = 2**40
        self.assertEqual(big + 8, self.conn.run(lambda: obj.int_property & 8 | big))

    def test_shift_operators(self) -> None:
        obj = self.conn.test_service.create_test_object("shift")
        obj.int_property = 12
        self.assertEqual(96, self.conn.run(lambda: obj.int_property << 3))
        self.assertEqual(3, self.conn.run(lambda: obj.int_property >> 2))
        # A count of at least the width shifts every bit out of a fixed size integer
        self.assertEqual(0, self.conn.run(lambda: obj.int_property << 32))
        self.assertEqual(0, self.conn.run(lambda: obj.int_property >> 40))
        self.assertEqual(-1, self.conn.run(lambda: -obj.int_property >> 40))
        big = 2**40
        self.assertEqual(
            (12 | big) << 2,
            self.conn.run(lambda: (obj.int_property | big) << 2),
        )
        with self.assertRaises(ValueError):
            self.conn.run(lambda: obj.int_property << obj.int_property - 13)
        # A ulong shifts by a signed count, which has no common type with it
        ulong = self.conn.test_service.uint64_special_defaults
        self.assertEqual(
            (2**64 - 1) >> 12, self.conn.run(lambda: ulong()[0] >> obj.int_property)
        )
        self.assertEqual(
            10 << 12, self.conn.run(lambda: ulong(10)[0] << obj.int_property)
        )
        with self.assertRaises(ValueError):
            self.conn.run(lambda: ulong()[0] >> obj.int_property - 13)

    def test_conversions(self) -> None:
        obj = self.conn.test_service.create_test_object("convert")
        obj.int_property = 7
        self.assertEqual(3, self.conn.run(lambda: int(obj.int_property / 2)))
        self.assertEqual(7.0, self.conn.run(lambda: float(obj.int_property)))
        self.assertEqual("7", self.conn.run(lambda: str(obj.int_property)))

    def test_numeric_conversion_of_a_non_number(self) -> None:
        obj = self.conn.test_service.create_test_object("convert2")
        for function in (
            lambda: float(obj.get_value()),
            lambda: int(obj.get_value()),
            lambda: float(obj.int_property == 1),
            lambda: round(obj.int_property == 1),
            lambda: abs(obj.int_property == 1),
        ):
            with self.assertRaisesRegex(
                FunctionCompilationError, "only supported on a number"
            ):
                self.conn.compile(function)

    def test_abs_round_min_max(self) -> None:
        obj = self.conn.test_service.create_test_object("scalar")
        obj.int_property = -7
        self.assertEqual(7, self.conn.run(lambda: abs(obj.int_property)))
        self.assertEqual(-4, self.conn.run(lambda: round(obj.int_property / 2)))
        self.assertEqual(-7, self.conn.run(lambda: min(obj.int_property, 3)))
        self.assertEqual(10, self.conn.run(lambda: max(obj.int_property, 3, 10)))

    def test_min_max_of_several_arguments_with_a_key(self) -> None:
        obj = self.conn.test_service.create_test_object("scalar")
        obj.int_property = -7
        self.assertEqual(
            -7,
            self.conn.run(lambda: max(obj.int_property, 3, key=lambda v: v * v)),
        )
        self.assertEqual(
            3,
            self.conn.run(lambda: min(obj.int_property, 3, key=lambda v: v * v)),
        )
        self.assertEqual(-7, self.conn.run(lambda: max(-7, 3, key=lambda v: -v)))

    def test_math_functions(self) -> None:
        obj = self.conn.test_service.create_test_object("math")
        obj.int_property = 16
        self.assertEqual(4.0, self.conn.run(lambda: math.sqrt(obj.int_property)))
        self.assertAlmostEqual(
            math.pi / 4,
            self.conn.run(lambda: math.atan2(obj.int_property, 16)),
        )
        # Client side arguments are still evaluated on the client
        self.assertEqual(2.0, self.conn.run(lambda: math.sqrt(4) * 1.0))
        # math.pow gives a float, where ** keeps the type of its operands
        power = self.conn.run(lambda: math.pow(obj.int_property, 2))
        self.assertEqual(256.0, power)
        self.assertIsInstance(power, float)
        self.assertEqual(4.0, self.conn.run(lambda: math.pow(obj.int_property, 0.5)))
        self.assertEqual(256, self.conn.run(lambda: obj.int_property**2))
        # math.floor and math.ceil give an integer, as in python
        floor = self.conn.run(lambda: math.floor(obj.int_property / 3))
        self.assertEqual(5, floor)
        self.assertIsInstance(floor, int)
        self.assertEqual(6, self.conn.run(lambda: math.ceil(obj.int_property / 3)))
        self.assertEqual(16, self.conn.run(lambda: math.floor(obj.int_property)))

    def test_min_and_max_with_a_nan(self) -> None:
        obj = self.conn.test_service.create_test_object("minmaxnan")
        obj.int_property = 1
        nan = math.nan
        # The first value is kept unless a later one compares smaller or larger
        self.assertEqual(1.0, self.conn.run(lambda: max(1.0, obj.int_property * nan)))
        self.assertTrue(
            math.isnan(self.conn.run(lambda: max(obj.int_property * nan, 1.0)))
        )
        self.assertEqual(1.0, self.conn.run(lambda: max([1.0, obj.int_property * nan])))
        self.assertTrue(
            math.isnan(self.conn.run(lambda: min([obj.int_property * nan, 1.0])))
        )

    def test_min_and_max_of_an_empty_collection(self) -> None:
        # pylint: disable=consider-using-generator
        service = self.conn.test_service
        obj = service.create_test_object("minmaxempty")
        obj.int_property = 1
        nullable = service.echo_nullable_int
        for func in (
            lambda: min([x for x in [obj.int_property] if x > 5]),
            lambda: max([x * 0.5 for x in [obj.int_property] if x > 5]),
            lambda: max([nullable(x) for x in [obj.int_property] if x > 5]),
            lambda: min([x for x in [obj.int_property] if x > 5], key=lambda x: -x),
        ):
            with self.assertRaisesRegex(ValueError, "arg is an empty sequence"):
                self.conn.run(func)
        self.assertEqual(1, self.conn.run(lambda: max([nullable(obj.int_property)])))

    def test_rounding_and_logarithms(self) -> None:
        obj = self.conn.test_service.create_test_object("rounding")
        obj.int_property = 5
        self.assertAlmostEqual(
            0.62, self.conn.run(lambda: round(obj.int_property / 8, 2))
        )
        # Negative places round to tens and hundreds, and an integer stays one
        self.assertEqual(1200, self.conn.run(lambda: round(obj.int_property * 247, -2)))
        # A long rounded to places at or after the point keeps every digit
        large = 2**53 + 1
        self.assertEqual(
            large * 5,
            self.conn.run(lambda: round(obj.int_property * large, 2)),
        )
        self.assertEqual(
            large * 5,
            self.conn.run(
                lambda: round(obj.int_property * large, obj.int_property - 5)
            ),
        )
        # An integer rounded to negative places keeps every digit, and rounds
        # half to even
        longs = self.conn.test_service.int64_special_defaults
        uints = self.conn.test_service.uint32_special_defaults
        ulongs = self.conn.test_service.uint64_special_defaults
        cases = [
            (2**63 - 1, -2, lambda: round(longs()[0], -2)),
            (-(2**63), -2, lambda: round(longs()[1], -2)),
            (2**63 - 1, -2, lambda: round(longs()[0], obj.int_property - 7)),
            (0, -19, lambda: round(longs()[0] // 10, -19)),
            (2**32 - 1, -3, lambda: round(uints()[0], -3)),
            (2**64 - 1, -4, lambda: round(ulongs()[0], -4)),
            (2**64 - 1, -20, lambda: round(ulongs()[0], -20)),
        ]
        for value, places, func in cases:
            self.assertEqual(round(value, places), self.conn.run(func))
        for value in (-25, -15, -5, 5, 15, 25, 26):
            self.assertEqual(
                round(value, -1),
                self.conn.run(
                    # pylint: disable-next=cell-var-from-loop
                    lambda: round(obj.int_property * value // 5, -1)
                ),
            )
        self.assertAlmostEqual(
            3.0, self.conn.run(lambda: math.log(obj.int_property * 1.6, 2))
        )
        self.assertAlmostEqual(
            math.log(5), self.conn.run(lambda: math.log(obj.int_property))
        )

    def test_math_signs_and_distances(self) -> None:
        obj = self.conn.test_service.create_test_object("mathsigns")
        obj.int_property = 3
        self.assertEqual(
            -3.0, self.conn.run(lambda: math.copysign(obj.int_property, -0.0))
        )
        self.assertEqual(5.0, self.conn.run(lambda: math.hypot(obj.int_property, 4)))
        self.assertEqual(
            5.0,
            self.conn.run(lambda: math.dist((obj.int_property, 0, 1), (0, 4, 1))),
        )
        # Points of any number of coordinates
        self.assertEqual(
            5.0,
            self.conn.run(lambda: math.dist((obj.int_property, 0), (0, 4))),
        )
        self.assertEqual(
            3.0, self.conn.run(lambda: math.dist((obj.int_property,), (0,)))
        )
        with self.assertRaisesRegex(FunctionCompilationError, "same number"):
            self.conn.compile(lambda: math.dist((obj.int_property, 0), (0, 4, 1)))
        is_nan = self.conn.run(lambda: math.isnan(obj.int_property / 2))
        self.assertIs(False, is_nan)
        self.assertIs(
            False,
            self.conn.run(lambda: math.isfinite(obj.int_property * math.inf)),
        )
        # Client side arguments take any number the math module does
        self.assertEqual(3.0, self.conn.run(lambda: math.hypot(1, 2, 2) * 1.0))

    def test_math_call_with_too_many_arguments(self) -> None:
        obj = self.conn.test_service.create_test_object("matharity")
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(
                lambda: math.sqrt(obj.int_property, 2)  # type: ignore[call-arg]
            )
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(lambda: math.hypot(obj.int_property, 2, 2))

    def test_print(self) -> None:
        obj = self.conn.test_service.create_test_object("print")
        obj.int_property = 3
        self.conn.run(lambda: print("value", obj.int_property, sep="="))
        self.conn.run(lambda: print(obj.int_property, obj.get_value()))

    def test_print_errors(self) -> None:
        obj = self.conn.test_service.create_test_object("printerrors")
        with self.assertRaisesRegex(FunctionCompilationError, "keyword argument 'end'"):
            self.conn.compile(lambda: print(obj.int_property, end=""))
        with self.assertRaisesRegex(FunctionCompilationError, "must be a constant"):
            self.conn.compile(
                lambda: print(
                    obj.int_property, sep=self.conn.test_service.string_property
                )
            )
        with self.assertRaisesRegex(FunctionCompilationError, "converted to a string"):
            self.conn.compile(lambda: print(obj.object_property))

    def test_stdlib_service(self) -> None:
        self.assertEqual(3.0, self.conn.std_lib.sqrt(9))
        self.assertEqual(
            (0.0, 0.0, 1.0),
            self.conn.std_lib.vector_cross((1.0, 0.0, 0.0), (0.0, 1.0, 0.0)),
        )

    def test_vector_math_in_function(self) -> None:
        vector = (1.0, 2.0, 2.0)
        magnitude = self.conn.std_lib.vector_magnitude
        self.assertEqual(3.0, self.conn.run(lambda: magnitude(vector) * 1.0))

    def test_stream_of_a_stdlib_property_is_rejected(self) -> None:
        std_lib = self.conn.std_lib
        for add in (self.conn.add_stream, self.conn.add_event):
            with self.assertRaisesRegex(
                FunctionCompilationError, "makes no remote procedure call"
            ):
                add(lambda: std_lib.pi * 2 > 6)

    def test_slicing(self) -> None:
        objs = self._make_objects("slice")
        self.assertEqual(
            [2, 3],
            self.conn.run(lambda: [o.int_property for o in objs][1:3]),
        )
        self.assertEqual(
            [1, 2],
            self.conn.run(lambda: [o.int_property for o in objs][:2]),
        )
        self.assertEqual(
            [2, 3],
            self.conn.run(lambda: [o.int_property for o in objs][1:]),
        )
        # A None bound is no bound
        nullable = self.conn.test_service.echo_nullable_int
        for expected, func in (
            ([1, 2], lambda: [o.int_property for o in objs][None:2]),
            ([1, 2], lambda: [o.int_property for o in objs][nullable(None) : 2]),
            (
                [2, 3],
                lambda: [o.int_property for o in objs][nullable(1) : nullable(None)],
            ),
            ([2], lambda: [o.int_property for o in objs][nullable(1) : nullable(2)]),
            (
                "23",
                lambda: str(objs[0].int_property + 122)[nullable(1) : nullable(None)],
            ),
            ("12", lambda: str(objs[0].int_property + 122)[None : nullable(2)]),
        ):
            self.assertEqual(expected, self.conn.run(func))
        # A bound that is not an int is a compile error, as for a string
        ulong = self.conn.test_service.uint64_special_defaults
        # pylint: disable=invalid-slice-index
        for func in (
            lambda: [o.int_property for o in objs][ulong(10)[0] :],
            lambda: [o.int_property for o in objs][1.5:],
        ):
            # pylint: enable=invalid-slice-index
            with self.assertRaisesRegex(FunctionCompilationError, "bound of a slice"):
                self.conn.compile(func)

    def test_dict_comprehension(self) -> None:
        objs = self._make_objects("dictcomp")
        self.assertEqual(
            {"1": 2, "2": 4, "3": 6},
            self.conn.run(
                lambda: {str(o.int_property): o.int_property * 2 for o in objs}
            ),
        )

    def test_dict_comprehension_with_a_nullable_key(self) -> None:
        service = self.conn.test_service
        objs = self._make_objects("dictnullable")
        self.assertEqual(
            {1: 1, 2: 2, 3: 3},
            self.conn.run(
                lambda: {
                    service.echo_nullable_int(o.int_property): o.int_property
                    for o in objs
                }
            ),
        )

    def test_set_and_dictionary_of_nullable_numbers(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("setnullable")
        obj.int_property = 3
        nullable = service.echo_nullable_int
        self.assertEqual({3}, self.conn.run(lambda: {nullable(3)}))
        self.assertEqual({3}, self.conn.run(lambda: {nullable(obj.int_property)}))
        self.assertEqual({3: 1}, self.conn.run(lambda: {nullable(obj.int_property): 1}))
        self.assertEqual(
            {3},
            self.conn.run(lambda: {nullable(x) for x in [obj.int_property]}),
        )

    def test_dict_comprehension_with_a_repeated_key(self) -> None:
        objs = self._make_objects("dictrepeat")
        self.assertEqual(
            {1: 3, 0: 2},
            self.conn.run(lambda: {o.int_property % 2: o.int_property for o in objs}),
        )
        self.assertEqual(
            {1: 3},
            self.conn.run(
                lambda: {
                    o.int_property % 2: o.int_property
                    for o in objs
                    if o.int_property != 2
                }
            ),
        )
        factors = [1, 2]
        self.assertEqual(
            {"a": 6},
            self.conn.run(
                lambda: {"a": x * o.int_property for x in factors for o in objs}
            ),
        )

    def test_nested_comprehension(self) -> None:
        objs = self._make_objects("nested")
        factors = [1, 2]
        self.assertEqual(
            [1, 2, 3, 2, 4, 6],
            self.conn.run(lambda: [x * o.int_property for x in factors for o in objs]),
        )

    def test_fstrings(self) -> None:
        obj = self.conn.test_service.create_test_object("fstring")
        obj.int_property = 42
        self.assertEqual(
            "value is 42!",
            self.conn.run(lambda: f"value is {obj.int_property}!"),
        )

    def test_str_of_none(self) -> None:
        service = self.conn.test_service
        run = self.conn.run
        self.assertEqual("None", run(lambda: str(service.echo_nullable_int(None))))
        self.assertEqual("1", run(lambda: str(service.echo_nullable_int(1))))
        self.assertEqual(
            "<None>", run(lambda: f"<{service.echo_nullable_string(None)}>")
        )
        self.assertEqual("<a>", run(lambda: f"<{service.echo_nullable_string('a')}>"))

    def test_str_of_a_value_with_no_server_text(self) -> None:
        objs = self._make_objects("str")
        service = self.conn.test_service
        enum = service.TestEnum
        rejected = [
            lambda: str([o.int_property for o in objs]),
            lambda: f"{[o.int_property for o in objs]}",
            lambda: str({o.int_property: 1 for o in objs}),
            lambda: str(objs[0].object_property),
            lambda: f"{service.enum_echo(enum.value_b)}",
        ]
        for function in rejected:
            with self.assertRaises(FunctionCompilationError):
                self.conn.compile(function)
        self.assertEqual("1", self.conn.run(lambda: str(objs[0].int_property)))

    def test_tuple_elements_widen_in_any_order(self) -> None:
        test_service = self.conn.test_service
        obj = test_service.create_test_object("tuplewiden")
        obj.int_property = 1
        ulong = test_service.uint64_special_defaults
        self.assertEqual(
            [(1.0,), (10.0,), (0.5,)],
            self.conn.run(
                lambda: [
                    (obj.int_property,),
                    (ulong(10)[0],),
                    (obj.int_property * 0.5,),
                ]
            ),
        )
        self.assertEqual(
            [(10.0,), (0.5,), (1.0,)],
            self.conn.run(
                lambda: [
                    (ulong(10)[0],),
                    (obj.int_property * 0.5,),
                    (obj.int_property,),
                ]
            ),
        )
        self.assertEqual(
            [(0.5,), (1.0,), (10.0,)],
            self.conn.run(
                lambda: [
                    (obj.int_property * 0.5,),
                    (obj.int_property,),
                    (ulong(10)[0],),
                ]
            ),
        )

    def test_none_takes_the_type_of_other_values(self) -> None:
        obj = self.conn.test_service.create_test_object("nonecontext")
        obj.int_property = 0
        self.assertIsNone(
            self.conn.run(lambda: obj.get_value() if obj.int_property else None)
        )
        self.assertEqual(
            [None, 1, 2],
            self.conn.run(lambda: [None, obj.int_property + 1, 2]),
        )
        self.assertEqual([None, 1], self.conn.run(lambda: [None, 1]))
        self.assertEqual(
            {"a": None, "b": 0},
            self.conn.run(lambda: {"a": None, "b": obj.int_property}),
        )
        self.assertIsNone(self.conn.run(lambda: obj.int_property or None))
        for function in (
            lambda: {None, obj.int_property},
            lambda: {None: obj.int_property},
            lambda: [None],
        ):
            with self.assertRaises(FunctionCompilationError):
                self.conn.compile(function)

    def test_captured_collection_with_none(self) -> None:
        test_service = self.conn.test_service
        obj = test_service.create_test_object("capturednone")
        obj.int_property = 0
        values = [5, None]
        objs = [obj, None]
        named = {"a": None, "b": 1}
        nested = [[1], None]
        inner = [[None], [1]]
        self.assertEqual(
            [5, None],
            self.conn.run(lambda: test_service.echo_list_of_nullable_ints(values)),
        )
        self.assertEqual(
            [obj, None],
            self.conn.run(lambda: test_service.echo_list_of_nullable_objects(objs)),
        )
        self.assertEqual(
            1,
            self.conn.run(
                lambda: len([x for x in values if x is None]) + obj.int_property
            ),
        )
        self.assertEqual(
            {"a": None, "b": 1},
            self.conn.run(
                lambda: {k: v for k, v in named.items() if obj.int_property == 0}
            ),
        )
        self.assertEqual(2, self.conn.run(lambda: len(nested) + obj.int_property))
        self.assertEqual(
            [[None], [1]],
            self.conn.run(lambda: [x for x in inner if obj.int_property == 0]),
        )
        nones = [None]
        with_none = {None, 1}
        for function in (
            lambda: nones if obj.int_property else nones,
            lambda: obj.int_property in with_none,
        ):
            with self.assertRaises(FunctionCompilationError):
                self.conn.compile(function)

    def test_equality_of_values_of_different_types(self) -> None:
        obj = self.conn.test_service.create_test_object("mixedequal")
        obj.int_property = 5
        service = self.conn.test_service
        for func in (
            lambda: (obj.int_property > 0) == 1,
            lambda: service.string_property != obj.int_property,
            lambda: obj == obj.int_property,
            lambda: obj.int_property in ["5"],
            lambda: service.string_property in {5: "a"},
            lambda: obj.int_property in (5, "a"),
        ):
            with self.assertRaisesRegex(
                FunctionCompilationError, "cannot compare a value of type"
            ):
                self.conn.compile(func)
        with self.assertRaisesRegex(
            FunctionCompilationError, "no implicit conversion from double to int"
        ):
            self.conn.compile(lambda: {5: "a"}[obj.int_property * 1.0])
        # Numbers of different types widen to a common type
        self.assertTrue(self.conn.run(lambda: obj.int_property == 5.0))
        self.assertTrue(self.conn.run(lambda: obj.int_property * 1.0 in [5]))
        self.assertFalse(self.conn.run(lambda: obj.int_property * 1.0 in [6]))
        self.assertTrue(self.conn.run(lambda: obj.int_property in [2.5, 5.0]))
        self.assertTrue(self.conn.run(lambda: 5 in [obj.int_property * 1.0]))
        self.assertEqual(
            "a", self.conn.run(lambda: {5: "a", 2**40: "b"}[obj.int_property])
        )

    def test_collections_compare_by_contents(self) -> None:
        obj = self.conn.test_service.create_test_object("contents")
        obj.int_property = 1
        service = self.conn.test_service
        self.assertTrue(
            self.conn.run(
                lambda: service.increment_list([obj.int_property, 2]) == [2, 3]
            )
        )
        self.assertTrue(
            self.conn.run(
                lambda: service.increment_dictionary({"a": obj.int_property})
                == {"a": 2}
            )
        )
        self.assertTrue(self.conn.run(lambda: [obj.int_property] in [[0], [1]]))

    def test_list_concatenation(self) -> None:
        objs = self._make_objects("concat")
        self.assertEqual(
            [1, 2, 3, 9],
            self.conn.run(lambda: [o.int_property for o in objs] + [9]),
        )

    def test_operators_on_values_that_are_not_numbers(self) -> None:
        obj = self.conn.test_service.create_test_object("notnumbers")
        obj.int_property = 3
        service = self.conn.test_service
        flag = obj.int_property > 1
        rejected = [
            lambda: service.string_property * 2,
            lambda: "%d" % obj.int_property,
            lambda: (obj.int_property > 1) + 1,
            lambda: sum([obj.int_property > 1, flag]),
            lambda: (obj.int_property, 1) < (2, 1),
            lambda: service.string_property < "b",
            lambda: max(service.string_property, "b"),
            lambda: max([service.string_property, "b"]),
        ]
        for function in rejected:
            with self.assertRaises(FunctionCompilationError):
                self.conn.compile(function)
        self.assertTrue(
            self.conn.run(lambda: (obj.int_property > 1) & (obj.int_property < 5))
        )

    def test_argument_unpacking_and_unary_plus(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("unpacking")
        keywords = {"value": 1}
        arguments = [1]
        with self.assertRaisesRegex(FunctionCompilationError, r"\*\*kwargs"):
            self.conn.compile(lambda: service.int32_to_string(**keywords))
        with self.assertRaisesRegex(FunctionCompilationError, r"\*args"):
            self.conn.compile(lambda: service.int32_to_string(*arguments))
        with self.assertRaisesRegex(FunctionCompilationError, "needs numbers"):
            self.conn.compile(lambda: +(obj.int_property > 0))
        self.assertEqual(1, self.conn.run(lambda: +True + obj.int_property))
        nullable = service.echo_nullable_int
        self.assertEqual(2, self.conn.run(lambda: +nullable(2)))
        with self.assertRaises(TypeError):
            self.conn.run(lambda: +nullable(None))

    def test_remote_method_without_a_call(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("nocall")
        for func in (lambda: obj.get_value, lambda: service.float_to_string):
            with self.assertRaisesRegex(FunctionCompilationError, "needs a call"):
                self.conn.compile(func)

    def test_raw_python_errors_are_compile_errors(self) -> None:
        obj = self.conn.test_service.create_test_object("rawerrors")
        mixed = [1, "a"]
        table = {"x": 1, "y": "two"}
        missing = None
        unhashable = [1, 2]
        # The errors these test are ones the linter finds too
        # pylint: disable=unhashable-member,unsupported-membership-test
        # pylint: disable=invalid-sequence-index
        for func in (
            lambda: mixed[obj.int_property],
            lambda: table[obj.string_property],
            lambda: {unhashable},
            lambda: {unhashable: 2},
            lambda: "a" in missing,
            lambda: [obj.int_property][1:2, 0],
        ):
            # pylint: enable=unhashable-member,unsupported-membership-test
            # pylint: enable=invalid-sequence-index
            with self.assertRaises(FunctionCompilationError):
                self.conn.compile(func)
        with self.assertRaisesRegex(FunctionCompilationError, "only a lambda or a def"):
            self.conn.compile(functools.partial(lambda x: x, 1))

    def test_remote_call_missing_an_argument(self) -> None:
        service = self.conn.test_service
        with self.assertRaisesRegex(
            FunctionCompilationError, r"missing the argument 'value' \(line \d+\)"
        ):
            # pylint: disable-next=no-value-for-parameter,unnecessary-lambda
            self.conn.compile(lambda: service.float_to_string())

    def test_empty_collection_error_names_the_line(self) -> None:
        obj = self.conn.test_service.create_test_object("emptyerror")
        empty: list = []
        with self.assertRaisesRegex(
            FunctionCompilationError,
            r"^Cannot compile expression: cannot determine the type of an empty "
            r"collection.*\(line \d+\)$",
        ):
            self.conn.compile(lambda: (obj.int_property, empty))

    def test_empty_collection_of_a_known_type(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("emptyknown")
        obj.int_property = 0
        empty: list = []
        self.assertEqual([], self.conn.run(lambda: service.increment_list([])))
        self.assertEqual({}, self.conn.run(lambda: service.increment_dictionary({})))
        self.assertEqual(set(), self.conn.run(lambda: service.increment_set(set())))
        self.assertEqual([], self.conn.run(lambda: service.increment_list(empty)))
        self.assertEqual(
            [],
            self.conn.run(lambda: [obj.int_property] if obj.int_property else []),
        )
        self.assertEqual([[1], []], self.conn.run(lambda: [[obj.int_property + 1], []]))
        self.assertEqual([0], self.conn.run(lambda: [obj.int_property] + empty))
        self.assertFalse(self.conn.run(lambda: obj.int_property in []))
        self.assertFalse(self.conn.run(lambda: obj.int_property in {}))

    def test_enumerate(self) -> None:
        values = self.conn.test_service.increment_list([10, 20])
        self.assertEqual(
            [(0, 11), (1, 21)],
            self.conn.run(lambda: enumerate(values)),
        )
        self.assertEqual(
            [11, 42],
            self.conn.run(lambda: [i * x for i, x in enumerate(values, start=1)]),
        )
        self.assertEqual([(0, "a"), (1, "b")], self.conn.run(lambda: enumerate("ab")))

    def test_zip(self) -> None:
        values = self.conn.test_service.increment_list([10, 20])
        names = ["a", "b", "c"]
        self.assertEqual(
            [("a", 11), ("b", 21)], self.conn.run(lambda: zip(names, values))
        )
        self.assertEqual(
            [22, 42],
            self.conn.run(lambda: [x + y for x, y in zip(values, values)]),
        )
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(lambda: zip(values, values, values))

    def test_unknown_member(self) -> None:
        obj = self.conn.test_service.create_test_object("unknown")
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(lambda: obj.no_such_member)

    def test_struct_field(self) -> None:
        counter_struct = self.conn.test_service.counter_struct
        self.assertEqual(
            "Compiler.field",
            self.evaluate(lambda: counter_struct("Compiler.field").string_field),
        )
        self.assertTrue(
            self.evaluate(lambda: counter_struct("Compiler.field2").int_field >= 0)
        )

    def test_nested_struct_field(self) -> None:
        enum = self.conn.test_service.TestEnum
        obj = self.conn.test_service.create_test_object("nested")
        struct = self.conn.test_service.TestStruct(1, "inner", enum.value_a, [2])
        nested = self.conn.test_service.TestNestedStruct(struct, obj, "outer")
        echo = self.conn.test_service.nested_struct_echo
        self.assertEqual(
            "inner", self.evaluate(lambda: echo(nested).struct_field.string_field)
        )
        self.assertEqual("outer", self.evaluate(lambda: echo(nested).string_field))

    def test_construct_struct(self) -> None:
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

    def test_construct_struct_with_field_names(self) -> None:
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

    def test_captured_struct_value(self) -> None:
        enum = self.conn.test_service.TestEnum
        struct = self.conn.test_service.TestStruct(9, "captured", enum.value_b, [4])
        echo = self.conn.test_service.struct_echo
        self.assertEqual("captured", self.evaluate(lambda: echo(struct).string_field))

    def test_struct_field_errors(self) -> None:
        test_struct = self.conn.test_service.TestStruct
        enum = self.conn.test_service.TestEnum
        echo = self.conn.test_service.struct_echo
        struct = test_struct(1, "x", enum.value_a, [2])
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(lambda: echo(struct).no_such_field)
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(lambda: echo(test_struct(1, "x")))
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(
                lambda: echo(test_struct(1, "x", enum.value_a, [2], no_such_field=1))
            )

    def _text(self) -> Any:
        """The test service, with its string property set to a text."""
        self.conn.test_service.string_property = "Hello, World"
        return self.conn.test_service

    def test_string_length_and_indexing(self) -> None:
        text = self._text()
        self.assertEqual(12, self.conn.run(lambda: len(text.string_property)))
        self.assertEqual("H", self.conn.run(lambda: text.string_property[0]))
        self.assertEqual("d", self.conn.run(lambda: text.string_property[11]))

    def test_string_slicing(self) -> None:
        text = self._text()
        self.assertEqual("World", self.conn.run(lambda: text.string_property[7:12]))
        self.assertEqual("Hello", self.conn.run(lambda: text.string_property[:5]))
        self.assertEqual("World", self.conn.run(lambda: text.string_property[7:]))

    def test_string_methods(self) -> None:
        # pylint: disable=unnecessary-lambda
        text = self._text()
        self.assertEqual(
            "HELLO, WORLD", self.conn.run(lambda: text.string_property.upper())
        )
        self.assertEqual(
            "hello, world", self.conn.run(lambda: text.string_property.lower())
        )
        self.assertEqual(
            "Hello, Mars",
            self.conn.run(lambda: text.string_property.replace("World", "Mars")),
        )
        self.assertEqual(
            ["Hello", "World"], self.conn.run(lambda: text.string_property.split(", "))
        )
        self.assertEqual(7, self.conn.run(lambda: text.string_property.find("World")))
        self.assertEqual(-1, self.conn.run(lambda: text.string_property.find("Mars")))
        self.assertTrue(self.conn.run(lambda: text.string_property.startswith("Hello")))
        self.assertTrue(self.conn.run(lambda: text.string_property.endswith("World")))
        self.assertEqual(
            "Hello|World",
            self.conn.run(lambda: "|".join(text.string_property.split(", "))),
        )

    def test_string_containment(self) -> None:
        text = self._text()
        self.assertTrue(self.conn.run(lambda: "World" in text.string_property))
        self.assertFalse(self.conn.run(lambda: "Mars" in text.string_property))
        self.assertTrue(self.conn.run(lambda: "Mars" not in text.string_property))

    def test_string_trimming(self) -> None:
        # pylint: disable=unnecessary-lambda
        text = self.conn.test_service
        text.string_property = "  pad  "
        self.assertEqual("pad", self.conn.run(lambda: text.string_property.strip()))
        self.assertEqual("pad  ", self.conn.run(lambda: text.string_property.lstrip()))
        self.assertEqual("  pad", self.conn.run(lambda: text.string_property.rstrip()))

    def test_min_max_by_key(self) -> None:
        objs = self._make_objects("minmaxby")
        self.assertEqual(
            1,
            self.conn.run(lambda: min(objs, key=lambda o: o.int_property).int_property),
        )
        self.assertEqual(
            3,
            self.conn.run(lambda: max(objs, key=lambda o: o.int_property).int_property),
        )

    def test_reversed(self) -> None:
        objs = self._make_objects("reversed")
        self.assertEqual(
            [3, 2, 1],
            self.conn.run(lambda: [o.int_property for o in reversed(objs)]),
        )

    def test_collections_that_cannot_be_used_in_order(self) -> None:
        # pylint: disable=unsubscriptable-object,bad-reversed-sequence
        service = self.conn.test_service
        obj = service.create_test_object("unordered")
        for func, message in (
            (lambda: {obj.int_property}[0:1], "cannot slice a value of type set"),
            (lambda: {obj.int_property}[-1], "cannot index a value of type set"),
            (
                lambda: reversed({obj.int_property}),
                r"cannot call reversed\(\) on a value of type set",
            ),
            (
                lambda: sorted(service.string_property),
                r"cannot call sorted\(\) on a value of type str",
            ),
            (
                lambda: reversed(service.string_property),
                r"cannot call reversed\(\) on a value of type str",
            ),
            (
                lambda: (1, 2, 3)[obj.int_property :],
                "a tuple can only be sliced by constant bounds",
            ),
        ):
            with self.assertRaisesRegex(
                FunctionCompilationError, message + r".* \(line \d+\)$"
            ):
                self.conn.compile(func)

    def test_errors_name_the_construct_and_the_line(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("errorsnamed")
        longs = service.int64_special_defaults
        # pylint: disable=unsubscriptable-object,undefined-variable
        for func, message in (
            (
                lambda: [obj.int_property] + [obj.get_value()],
                "cannot add a value of type list.str. to a value of type list.int.",
            ),
            (
                lambda: service.int32_to_string(obj.get_value()),
                "cannot use a value of type str for parameter 'value'",
            ),
            (lambda: math.sqrt(obj.get_value()), "cannot use a value of type str"),
            (lambda: obj.get_value().find(5), "str.find takes a str here"),
            (lambda: obj.get_value().replace(1, 2), "str.replace takes a str here"),
            (
                lambda: obj.get_value().startswith(("a", "b")),
                "str.startswith takes a str here, not a value of type tuple",
            ),
            (
                lambda: ",".join([obj.int_property]),
                "str.join takes a list or set of str here",
            ),
            (
                lambda: (obj.int_property, 2)[obj.int_property],
                "a tuple can only be indexed by a constant integer",
            ),
            (lambda: {obj.int_property}[0], "cannot index a value of type set"),
            (
                lambda: [obj.int_property][longs()[0]],
                "an index must be an int, not a value of type long",
            ),
            (
                lambda: [obj.int_property][obj.int_property * 1.0],
                "an index must be an int, not a value of type double",
            ),
            (
                lambda: obj.get_value()[obj.int_property > 0],
                "an index must be an int, not a value of type bool",
            ),
            (
                lambda: (obj.int_property, 1, 2, 3, 4, 5, 6, 7),
                "a tuple can hold at most 7 values",
            ),
            (
                lambda: len(obj.int_property),
                r"cannot call len\(\) on a value of type int",
            ),
            (
                lambda: sorted([obj, service.create_test_object("other")]),
                r"sorted\(\) cannot order values of type TestService.TestClass",
            ),
            (
                lambda: min([obj], key=lambda o: o),
                r"the key of min\(\) cannot order values of type TestService.TestClass",
            ),
            (
                lambda: {obj.get_value(): 1}.get("a"),
                "'get' is not supported on a value of type dict",
            ),
            (
                lambda: [obj.int_property].index(1),
                "'index' is not supported on a value of type list",
            ),
            (
                lambda: obj.get_value().count("a"),
                "'count' is not supported on a value of type str",
            ),
            (lambda: obj.int_property + unknown, "cannot resolve the name 'unknown'"),
        ):
            with self.assertRaisesRegex(
                FunctionCompilationError, message + r".* \(line \d+\)$"
            ):
                self.conn.compile(func)
        captured = tuple(range(8))
        with self.assertRaisesRegex(
            FunctionCompilationError, "a tuple can hold at most 7 values"
        ):
            self.conn.compile(lambda: (obj.int_property, captured))

    def test_index_from_the_end_out_of_range(self) -> None:
        obj = self.conn.test_service.create_test_object("indexfromend")
        with self.assertRaisesRegex(IndexError, "index -4 is out of range"):
            self.conn.run(lambda: [obj.int_property, 1, 2][-4])
        with self.assertRaisesRegex(IndexError, "index -2 is out of range"):
            self.conn.run(lambda: str(obj.int_property)[-2])

    def test_collection_elements_widen(self) -> None:
        test_service = self.conn.test_service
        obj = test_service.create_test_object("widen")
        obj.int_property = 3
        captured = [1, 2.5]
        self.assertEqual([1.5, 0.0], self.conn.run(lambda: [obj.int_property * 0.5, 0]))
        self.assertEqual(
            {"a": 1.5, "b": 0.0},
            self.conn.run(lambda: {"a": obj.int_property * 0.5, "b": 0}),
        )
        self.assertEqual(
            3.5, self.conn.run(lambda: sum(captured) + obj.int_property * 0)
        )
        self.assertEqual(
            [10, 1],
            self.conn.run(lambda: [test_service.uint64_special_defaults(10)[0], 1]),
        )

    def test_integer_types_are_kept(self) -> None:
        obj = self.conn.test_service.create_test_object("wide")
        obj.int_property = 3
        self.assertEqual(
            9000000000, self.evaluate(lambda: obj.int_property * 3000000000)
        )
        self.assertEqual(
            4500000000, self.evaluate(lambda: obj.int_property * 3000000000 // 2)
        )
        self.assertEqual(
            9000000000, self.evaluate(lambda: abs(obj.int_property * -3000000000))
        )
        self.assertEqual(
            9000000000,
            self.evaluate(lambda: max(obj.int_property * 3000000000, obj.int_property)),
        )
        self.assertEqual(
            3,
            self.evaluate(lambda: min(obj.int_property * 3000000000, obj.int_property)),
        )
        self.assertEqual(
            9000000000, self.evaluate(lambda: int(obj.int_property * 3000000000))
        )
        self.assertEqual(
            9000000000, self.evaluate(lambda: round(obj.int_property * 3000000000))
        )
        self.assertEqual(
            3000000001.5, self.evaluate(lambda: obj.int_property * 0.5 + 3000000000)
        )
        self.assertEqual(
            "5000000000",
            self.evaluate(
                lambda: self.conn.test_service.int64_to_string(5000000000)
                + str(obj.int_property)[:0]
            ),
        )
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(
                lambda: self.conn.test_service.throw_argument_out_of_range_exception(
                    5000000000
                )
            )
        self.assertEqual(
            -4294967295,
            self.evaluate(lambda: -self.conn.test_service.uint32_special_defaults()[0]),
        )
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(
                lambda: -self.conn.test_service.uint64_special_defaults()[0]
            )

    def test_integer_constant_beside_a_ulong(self) -> None:
        test_service = self.conn.test_service
        obj = test_service.create_test_object("ulongconstant")
        self.assertEqual(
            11, self.evaluate(lambda: test_service.uint64_special_defaults(10)[0] + 1)
        )
        self.assertTrue(
            self.evaluate(lambda: test_service.uint64_special_defaults(10)[0] == 10)
        )
        self.assertEqual(
            3, self.evaluate(lambda: test_service.uint64_special_defaults(10)[0] // 3)
        )
        self.assertEqual(
            2, self.evaluate(lambda: test_service.uint64_special_defaults(10)[0] % 4)
        )
        self.assertEqual(
            4,
            self.evaluate(lambda: min(test_service.uint64_special_defaults(10)[0], 4)),
        )
        with self.assertRaisesRegex(FunctionCompilationError, "out of range"):
            self.conn.compile(lambda: test_service.uint64_special_defaults(10)[0] + -1)
        with self.assertRaisesRegex(FunctionCompilationError, "no common type"):
            self.conn.compile(
                lambda: test_service.uint64_special_defaults(10)[0] // obj.int_property
            )

    def test_ulong_beside_a_signed_integer(self) -> None:
        test_service = self.conn.test_service
        obj = test_service.create_test_object("ulongsigned")
        ulong = test_service.uint64_special_defaults
        for func in (
            lambda: ulong(10)[0] + obj.int_property,
            lambda: obj.int_property + 2**63,
            lambda: ulong(10)[0] == obj.int_property,
            lambda: ulong(10)[0] != obj.int_property,
            lambda: ulong(10)[0] < obj.int_property,
            lambda: ulong(10)[0] in (obj.int_property, 2),
            lambda: ulong(10)[0] in [obj.int_property],
        ):
            with self.assertRaisesRegex(
                FunctionCompilationError,
                r"(no common type|cannot compare).* \(line \d+\)$",
            ):
                self.conn.compile(func)

    def test_single_precision_arithmetic_is_in_double_precision(self) -> None:
        floats = self.conn.test_service.float_special_defaults
        # The single precision value nearest 0.1
        value = floats()[5]
        for expected, func in (
            (value + 1, lambda: floats()[5] + 1),
            (1 - value, lambda: 1 - floats()[5]),
            (value * value, lambda: floats()[5] * floats()[5]),
            (value / 3, lambda: floats()[5] / 3),
            (value**2, lambda: floats()[5] ** 2),
            (value**-1, lambda: floats()[5] ** -1),
            (math.pow(value, 2), lambda: math.pow(floats()[5], 2)),
            (
                value + value + value,
                lambda: sum([floats()[5], floats()[5], floats()[5]]),
            ),
            (
                math.dist((value, 0), (0, value)),
                lambda: math.dist((floats()[5], 0), (0, floats()[5])),
            ),
        ):
            self.assertEqual(expected, self.conn.run(func))

    def test_negative_power_of_an_integer(self) -> None:
        obj = self.conn.test_service.create_test_object("power")
        obj.int_property = 4
        self.assertEqual(0.25, self.evaluate(lambda: obj.int_property**-1))

    def test_invert_a_bool(self) -> None:
        obj = self.conn.test_service.create_test_object("invert")
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(lambda: ~(obj.int_property > 0))

    def test_index_from_the_end(self) -> None:
        values = self.conn.test_service.increment_list([1, 2, 3])
        obj = self.conn.test_service.create_test_object("end")
        obj.int_property = 1
        self.assertEqual(4, self.conn.run(lambda: values[-1]))
        self.assertEqual(2, self.conn.run(lambda: values[-3]))
        self.assertEqual("d", self.evaluate(lambda: obj.get_value()[-1]))
        self.assertEqual("a", self.evaluate(lambda: (obj.int_property, "a")[-1]))
        with self.assertRaises(FunctionCompilationError):
            # pylint: disable-next=potential-index-error
            self.conn.compile(lambda: (obj.int_property, "a")[2])

    def test_string_slice_is_clamped(self) -> None:
        obj = self.conn.test_service.create_test_object("clamp")
        self.assertEqual("value=clamp", self.evaluate(lambda: obj.get_value()[:100]))
        self.assertEqual("", self.evaluate(lambda: obj.get_value()[50:]))
        self.assertEqual("", self.evaluate(lambda: obj.get_value()[3:1]))
        self.assertEqual("ue", self.evaluate(lambda: obj.get_value()[3:5]))


if __name__ == "__main__":
    unittest.main()
