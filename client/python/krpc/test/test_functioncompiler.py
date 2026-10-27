# One test per construct the compiler accepts, so the module is long

import functools
import linecache
import math
import time
import unittest
from collections.abc import Callable
from typing import Any, cast

from krpc.error import FunctionCompilationError
from krpc.test.servertestcase import ServerTestCase

# A global that a function also assigns as a local
SHADOWED = 100

# A class accessed through a service, set by the test that annotates with it
SERVICE_CLASS: Any = None


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

    def test_local_read_before_assignment(self) -> None:
        obj = self.conn.test_service.create_test_object("local")
        obj.int_property = 3

        def shadows_a_global():
            for i in range(obj.int_property):
                if i == 2:
                    return SHADOWED  # noqa: F823  # pyright: ignore[reportPossiblyUnboundVariable]
                SHADOWED = i  # noqa: F841
            return -1

        with self.assertRaises(FunctionCompilationError) as cm:
            self.conn.compile(shadows_a_global)
        self.assertIn("'SHADOWED' is read before it is assigned", str(cm.exception))
        with self.assertRaisesRegex(
            FunctionCompilationError, r"'y' is read before it is assigned \(line \d+\)$"
        ):
            self.conn.compile(lambda: obj.int_property + y + (y := 1))  # noqa: F821  # pyright: ignore[reportUnboundVariable]

        def reads_the_global():
            value = SHADOWED
            return value + [x * 2 for x in [obj.int_property]][0]

        self.assertEqual(106, self.conn.run(reads_the_global))

    def test_local_assigned_on_some_paths(self) -> None:
        obj = self.conn.test_service.create_test_object("somepaths")

        def branch():
            if obj.int_property > 0:
                value = 1
            return value  # pyright: ignore[reportPossiblyUnboundVariable]

        def loop():
            for element in [obj.int_property]:
                value = element
            return value

        def guarded():
            try:
                value = obj.int_property
            except ValueError:
                pass
            return value  # pyright: ignore[reportPossiblyUnboundVariable]

        for func in (branch, loop, guarded):
            with self.assertRaisesRegex(
                FunctionCompilationError,
                "'value' is read before it is assigned on every path",
            ):
                self.conn.compile(func)

        def every_path():
            if obj.int_property > 0:
                value = 1
            else:
                value = 2
            try:
                other = obj.int_property
            except ValueError:
                other = 0
            while True:
                last = other
                break
            return value + other + last

        obj.int_property = 3
        self.assertEqual(7, self.conn.run(every_path))

    def test_local_function_reads_a_variable_assigned_after_it(self) -> None:
        obj = self.conn.test_service.create_test_object("readslater")
        obj.int_property = 5

        def defined_first():
            def add(k: int) -> None:
                values.append(k + offset)

            double = lambda: offset * 2  # noqa: E731
            offset = obj.int_property
            values = [0]
            add(1)
            add(2)
            values.append(double())
            return values

        def calls_a_later_function() -> int:
            def outer() -> int:
                return inner() + 1

            def inner() -> int:
                return offset

            offset = obj.int_property
            return outer()

        def called_too_early() -> int:
            double = lambda: offset * 2  # noqa: E731
            result = double()
            offset = obj.int_property
            return result

        def called_too_early_through_another() -> int:
            def outer() -> int:
                return inner() + 1

            def inner() -> int:
                return offset

            result = outer()
            offset = obj.int_property
            return result

        self.assertEqual([0, 6, 7, 10], self.conn.run(defined_first))
        self.assertEqual(6, self.conn.run(calls_a_later_function))
        for func, name in (
            (called_too_early, "double"),
            (called_too_early_through_another, "outer"),
        ):
            with self.assertRaisesRegex(
                FunctionCompilationError,
                "local function '%s' reads the local variable 'offset', which is "
                "not assigned on every path to this call" % name,
            ):
                self.conn.compile(func)

    def test_captured_variable_not_yet_assigned(self) -> None:
        obj = self.conn.test_service.create_test_object("unassigned")

        def reads_later():
            return obj.int_property + later

        with self.assertRaisesRegex(
            FunctionCompilationError,
            r"captured variable 'later' is not assigned .*\(line \d+\)$",
        ):
            self.conn.compile(reads_later)
        later = 1
        self.assertEqual(1, later)

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
        expected = cls.static_method("beta")
        self.assertEqual(expected, self.evaluate(lambda: cls.static_method("beta")))

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
        self.assertTrue(self.evaluate(lambda: obj.object_property.int_property == 7))  # pyright: ignore[reportOptionalMemberAccess]
        self.assertEqual(
            "value=inner",
            # The lambda is compiled from source, not called
            self.evaluate(lambda: obj.object_property.get_value()),  # noqa: PLW0108  # pyright: ignore[reportOptionalMemberAccess]
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
            lambda: obj.int_property + round(1.5, "a"),  # pyright: ignore[reportArgumentType, reportCallIssue]
            lambda: obj.int_property + min(1, "a"),  # pyright: ignore[reportOperatorIssue]
            lambda: obj.int_property > 0 and values < 1,  # pyright: ignore[reportOperatorIssue]
            lambda: obj.int_property if obj.int_property > 0 else 1 // 0,
        ):
            with self.assertRaisesRegex(FunctionCompilationError, r"\(line \d+\)$"):
                self.conn.compile(func)

    def test_ternary_branches_must_agree(self) -> None:
        obj = self.conn.test_service.create_test_object("ternary2")
        other = obj.extension_method_returning_class_from_other_service()  # pyright: ignore[reportAttributeAccessIssue]
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

    def test_boolean_operators_stop_at_a_deciding_constant(self) -> None:
        obj = self.conn.test_service.create_test_object("booldecide")
        obj.int_property = 5
        divisor = 0
        xs = []
        lim = None
        self.assertFalse(
            self.conn.run(lambda: divisor != 0 and obj.int_property / divisor > 1)
        )
        self.assertFalse(
            self.conn.run(lambda: len(xs) > 0 and xs[0] > obj.int_property)
        )
        self.assertTrue(self.conn.run(lambda: lim is None or obj.int_property > lim))
        self.assertEqual(5, self.evaluate(lambda: 1 and obj.int_property))
        self.assertEqual(0, self.evaluate(lambda: obj.int_property and 0))

        def condition():
            if divisor != 0 and obj.int_property / divisor > 1:
                return 1
            if lim is not None and obj.int_property > lim:
                return 2
            return 3

        self.assertEqual(3, self.conn.run(condition))

    def _make_objects(self, prefix: str) -> list[Any]:
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

    def test_generator_held_in_a_variable(self) -> None:
        objs = self._make_objects("generator")

        def total() -> int:
            values = (o.int_property for o in objs)
            return sum(values)

        def passed() -> int:
            def add(values: list[int]) -> int:
                return sum(values)

            return add(o.int_property for o in objs)  # pyright: ignore[reportArgumentType]

        self.assertEqual(6, self.conn.run(total))
        self.assertEqual(6, self.conn.run(passed))

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
        exec(compile(source, "<deep>", "exec"), namespace)
        with self.assertRaisesRegex(FunctionCompilationError, "too deeply nested"):
            self.conn.compile(namespace["make"](obj))

    def test_lambda_continued_on_the_next_line(self) -> None:
        obj = self.conn.test_service.create_test_object("continued")
        obj.int_property = 5
        other = self.conn.test_service.create_test_object("continued2")
        other.int_property = 2
        # fmt: off
        difference = (  # noqa: E731
            lambda: obj.int_property
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

    def test_membership_of_nullable_and_wider_numbers(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("nullablein")
        obj.int_property = 5
        nullable = service.echo_nullable_int
        long = service.int64_special_defaults
        with_none = service.echo_list_of_nullable_ints
        for expected, func in (
            (False, lambda: long()[0] in with_none([None, 5])),
            (True, lambda: obj.int_property * 1.0 in with_none([None, 5])),
            (True, lambda: nullable(None) in with_none([None, 5])),
            (False, lambda: nullable(None) in [obj.int_property, 2**40]),
            (False, lambda: nullable(None) in {2**40: 2}),
            (True, lambda: nullable(5) in {2**40: 2, 5: 1}),
            (False, lambda: nullable(None) in {obj.int_property: 1}),
            (True, lambda: obj.int_property / 1 in {5: 2}),
            (False, lambda: 5.5 in {obj.int_property: 2}),
            (False, lambda: 2**40 in {obj.int_property: 2}),
            (False, lambda: long()[0] in {obj.int_property: 2}),
        ):
            self.assertEqual(expected, self.conn.run(func))

        def none_in_a_dictionary():
            values = {obj.int_property: 2}
            return None in values

        self.assertFalse(self.conn.run(none_in_a_dictionary))

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
                lambda: (
                    counter("Compiler.indict") in {1: 0, counter("Compiler.indict"): 0}
                )
            )
        )
        self.assertTrue(
            self.conn.run(
                lambda: (
                    str(counter("Compiler.instr"))
                    in "1" + str(counter("Compiler.instr"))
                )
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

    def test_function_with_assignments(self) -> None:
        obj = self.conn.test_service.create_test_object("func")
        obj.int_property = 21

        def expression():
            doubled = obj.int_property * 2
            return doubled + 1

        self.assertEqual(43, self.evaluate(expression))

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

    def test_client_side_function_making_a_remote_call_is_rejected(self) -> None:
        service = self.conn.test_service

        def helper() -> int:
            return service.counter("Compiler.helper")

        class Holder:
            @property
            def value(self) -> int:
                return service.counter("Compiler.holder")

        holder = Holder()
        for func in (
            lambda: helper() + service.counter("Compiler.other"),
            lambda: holder.value + service.counter("Compiler.other"),
        ):
            with self.assertRaisesRegex(
                FunctionCompilationError,
                r"makes a remote procedure call.* \(line \d+\)$",
            ):
                self.conn.compile(func)

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

    def test_run_function_calling_a_procedure_that_pauses(self) -> None:
        def go() -> None:
            self.conn.test_service.blocking_procedure(3)
            self.conn.test_service.string_property = "carried on"

        # The server reports the pause as KRPC.InvalidOperationException,
        # which the client raises as a RuntimeError
        with self.assertRaisesRegex(RuntimeError, "paused execution"):
            self.conn.run(go)

    def test_statement_loops(self) -> None:
        objs = self._make_objects("stmt")

        def total_of_objects():
            total = 0
            for obj in objs:
                if obj.int_property == 2:
                    continue
                total += obj.int_property
            return total

        self.assertEqual(4, self.conn.run(total_of_objects))

    def test_while_loop_with_break(self) -> None:
        def count_up():
            i = 0
            while True:
                i += 1
                if i >= 5:
                    break
            return i

        self.assertEqual(5, self.conn.run(count_up))

    def test_side_effects(self) -> None:
        obj = self.conn.test_service.create_test_object("effects")
        obj.int_property = 1

        def set_property() -> None:
            obj.int_property = 42

        self.assertIsNone(self.conn.run(set_property))
        self.assertEqual(42, obj.int_property)

    def test_service_property_side_effect(self) -> None:
        def set_string() -> None:
            self.conn.test_service.string_property = "written by function"

        self.conn.run(set_string)
        self.assertEqual("written by function", self.conn.test_service.string_property)

    def test_function_ending_in_a_loop_that_returns(self) -> None:
        obj = self.conn.test_service.create_test_object("loopreturns")
        obj.int_property = 3

        def first_square_over_ten() -> int:
            value = obj.int_property
            while True:
                for _ in range(2):
                    if value > 100:
                        break
                if value * value > 10:
                    return value * value
                value += 1

        def breaks() -> int:  # pyright: ignore[reportReturnType]
            while True:
                if obj.int_property > 0:
                    break
                return 1

        self.assertEqual(16, self.conn.run(first_square_over_ten))
        with self.assertRaisesRegex(
            FunctionCompilationError, "must end with a return.*while True loop"
        ):
            self.conn.compile(breaks)

    def test_narrowing_assignment_hint(self) -> None:
        obj = self.conn.test_service.create_test_object("narrowinghint")

        def inferred():
            x = obj.int_property
            x = obj.int_property / 2
            return x

        def annotated():
            x: int = obj.int_property
            x = obj.int_property / 2  # pyright: ignore[reportAssignmentType]
            return x

        def parameter(x: int):
            x = obj.int_property / 2  # pyright: ignore[reportAssignmentType]
            return x

        with self.assertRaisesRegex(
            FunctionCompilationError, "annotate it, for example x: float = 0"
        ):
            self.conn.compile(inferred)
        for function in (annotated, parameter):
            with self.assertRaises(FunctionCompilationError) as cm:
                self.conn.compile(function)
            self.assertIn("no implicit conversion", str(cm.exception))
            self.assertNotIn("annotate it", str(cm.exception))

    def test_chained_assignment_gives_each_target_the_value(self) -> None:
        obj = self.conn.test_service.create_test_object("chained")
        obj.int_property = 3

        def chained():
            total = 0.5
            total = count = obj.int_property
            count += 1
            return total, str(count)

        def constant():
            first = second = None
            first = obj.int_property
            second = "text"
            return first, second

        self.assertEqual((3.0, "4"), self.conn.run(chained))
        self.assertEqual((3, "text"), self.conn.run(constant))

    def test_assignment_expression_in_a_lambda(self) -> None:
        obj = self.conn.test_service.create_test_object("walruslambda")
        obj.int_property = 3

        def local_lambda():
            value = 1
            doubled = lambda: (value := obj.int_property * 2) + value  # noqa: E731
            return doubled(), value

        def key_lambda():
            value = 1
            values = [obj.int_property, 1, 2]
            ordered = sorted(values, key=lambda x: (value := -x))
            return ordered, value

        self.assertEqual((12, 1), self.conn.run(local_lambda))
        self.assertEqual(([3, 2, 1], 1), self.conn.run(key_lambda))

    def test_build_list_in_loop(self) -> None:
        objs = self._make_objects("buildlist")

        def doubled():
            result: list[int] = []
            for obj in objs:
                result.append(obj.int_property * 2)
            return result

        self.assertEqual([2, 4, 6], self.conn.run(doubled))

    def test_build_dictionary(self) -> None:
        def counts():
            values: dict[str, int] = {}
            values["a"] = 1
            values["a"] += 2
            values["b"] = 5
            return values

        self.assertEqual({"a": 3, "b": 5}, self.conn.run(counts))

    def test_nested_collections_in_variables(self) -> None:
        obj = self.conn.test_service.create_test_object("nested")
        obj.int_property = 3

        def nested_list():
            xs = [[obj.int_property]]
            xs.append([1, 2])
            return xs

        self.assertEqual([[3], [1, 2]], self.conn.run(nested_list))

        def dictionary_of_lists():
            lists = {"a": [obj.int_property]}
            lists["b"] = [1]
            return lists

        self.assertEqual({"a": [3], "b": [1]}, self.conn.run(dictionary_of_lists))

        def comprehensions():
            pairs = [[x, x] for x in [obj.int_property, 1]]
            lists = {k: [k] for k in [obj.int_property]}
            return len(pairs) + len(lists)

        self.assertEqual(3, self.conn.run(comprehensions))

        def branches():
            empty: list[int] = []
            xs = [obj.int_property] if obj.int_property > 0 else empty
            if obj.int_property > 1:
                return xs
            return [obj.int_property]

        self.assertEqual([3], self.conn.run(branches))

    def test_tuple_argument_elements_widen(self) -> None:
        obj = self.conn.test_service.create_test_object("tuple")
        obj.int_property = 3
        self.assertEqual(
            (4, 6),
            self.evaluate(
                lambda: self.conn.test_service.increment_tuple((obj.int_property, 5))
            ),
        )

    def test_collection_element_is_converted(self) -> None:
        def numbers():
            values: list[float] = []
            values.append(3)
            values.append(4.5)
            return values

        self.assertEqual([3.0, 4.5], self.conn.run(numbers))

        def truncated():
            values: list[int] = []
            values.append(3.5)  # pyright: ignore[reportArgumentType]
            return values

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(truncated)

    def test_empty_collection_is_not_a_missing_result(self) -> None:
        def empty_list():
            result: list[int] = []
            return result

        def empty_dictionary():
            values: dict[str, int] = {}
            return values

        self.assertEqual([], self.conn.run(empty_list))
        self.assertEqual({}, self.conn.run(empty_dictionary))

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

    def test_is_none_and_tuple_length_of_a_lookup_that_raises(self) -> None:
        obj = self.conn.test_service.create_test_object("lookupraises")

        def missing_key():
            values: dict[str, int] = {}
            if obj.int_property > 0:
                values["k"] = 1
            return values["k"] is not None

        def missing_element():
            values: list[int] = []
            if obj.int_property > 0:
                values.append(1)
            return values[0] is None

        def missing_tuple_element():
            values: list[int] = []
            if obj.int_property > 0:
                values.append(1)
            return len((values[0], 1))

        for func, error in (
            (missing_key, KeyError),
            (missing_element, IndexError),
            (missing_tuple_element, IndexError),
        ):
            with self.assertRaises(error):
                self.conn.run(func)

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
            self.conn.compile(lambda: [] == {obj.int_property})

    def test_operations_the_server_does_not_have_name_the_line(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("noserverop")
        data = b"abc"
        float_keys = {1.5: 2}

        class AlwaysEqual:  # noqa: PLW1641
            def __eq__(self, value: object) -> bool:
                return True

            def __call__(self) -> int:
                return 3

        always_equal = AlwaysEqual()
        for func, message in (
            (lambda: obj.int_property * 1.0 & 1, "&, | and ^ need integers or bools"),  # pyright: ignore[reportOperatorIssue]
            (lambda: obj.int_property[0], "cannot index a value of type int"),  # pyright: ignore[reportIndexIssue]
            (
                lambda: (data if obj.int_property else data)[0],
                "cannot index a value of type bytes",
            ),
            (
                lambda: len(data if obj.int_property else data),
                r"cannot call len\(\) on a value of type bytes",
            ),
            (
                lambda: obj.int_property in data,
                "cannot test membership in a value of type bytes",
            ),
            (
                lambda: math.dist((obj.get_value(), "a"), (1, 2)),  # pyright: ignore[reportArgumentType]
                "math.dist takes two tuples of numbers",
            ),
            (
                lambda: float_keys[obj.int_property * 1.5],
                "the keys of a dictionary must be integers, bools or strings",
            ),
            (
                lambda: {obj.int_property * 1.5: 2},
                "the keys of a dictionary must be integers, bools or strings",
            ),
        ):
            with self.assertRaisesRegex(
                FunctionCompilationError, message + r".* \(line \d+\)$"
            ):
                self.conn.compile(func)
        self.assertEqual(3, self.conn.run(lambda: always_equal() + obj.int_property))

    def test_messages_name_the_construct(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("messages")
        for func, message in (
            (lambda: f"{obj.int_property=}", "the = specifier is not supported"),
            (lambda: f"{obj.int_property!r}", "the !r and !a conversions"),
            (
                lambda: service.echo_test_object(obj).get_value,  # pyright: ignore[reportOptionalMemberAccess]
                "'get_value' is a remote method, which needs a call",
            ),
        ):
            with self.assertRaisesRegex(
                FunctionCompilationError, message + r".* \(line \d+\)$"
            ):
                self.conn.compile(func)

    def test_float_remainder_and_floor_division_of_zero_are_signed(self) -> None:
        obj = self.conn.test_service.create_test_object("signedzero")
        obj.int_property = 1

        def remainder(dividend: float, divisor: float) -> Callable[[], float]:
            return lambda: (dividend * obj.int_property) % divisor

        def quotient(dividend: float, divisor: float) -> Callable[[], float]:
            return lambda: (dividend * obj.int_property) // divisor

        for dividend, divisor in ((-0.0, 5.0), (0.0, -5.0), (-0.0, -5.0), (6.0, -3.0)):
            for expected, func in (
                (dividend % divisor, remainder(dividend, divisor)),
                (dividend // divisor, quotient(dividend, divisor)),
            ):
                result = self.conn.run(func)
                self.assertEqual(expected, result)
                self.assertEqual(math.copysign(1, expected), math.copysign(1, result))

    def test_local_value_is_never_none(self) -> None:
        def check():
            value = 1
            return value is None

        self.assertFalse(self.conn.run(check))

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

    def test_truth_of_a_nullable_number(self) -> None:
        service = self.conn.test_service

        def truth(value: Any) -> Any:
            return self.conn.run(
                lambda: "yes" if service.echo_nullable_int(value) else "no"
            )

        self.assertEqual("no", truth(None))
        self.assertEqual("no", truth(0))
        self.assertEqual("yes", truth(2))
        self.assertEqual(
            ["no", "no", "yes"],
            self.conn.run(
                lambda: [
                    "yes" if x else "no"
                    for x in service.echo_list_of_nullable_ints([None, 0, 3])
                ]
            ),
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
                    lambda: (
                        (obj.int_property > 1 if obj.int_property != 0 else None)
                        or False
                    )
                ),
            )
            self.assertEqual(
                both,
                self.conn.run(
                    lambda: (
                        (obj.int_property > 1 if obj.int_property != 0 else None)
                        and True
                    )
                ),
            )

    def test_bitwise_operators_on_a_nullable_bool(self) -> None:
        obj = self.conn.test_service.create_test_object("nullableboolbits")
        obj.int_property = 2
        self.assertEqual(
            (True, True, False),
            self.conn.run(
                lambda: (
                    (obj.int_property > 1 if obj.int_property != 0 else None) & True,  # pyright: ignore[reportOptionalOperand]
                    (obj.int_property > 1 if obj.int_property != 0 else None) | False,  # pyright: ignore[reportOptionalOperand]
                    (obj.int_property > 1 if obj.int_property != 0 else None) ^ True,  # pyright: ignore[reportOptionalOperand]
                )
            ),
        )
        obj.int_property = 0
        with self.assertRaises(TypeError):
            self.conn.run(
                lambda: (obj.int_property > 1 if obj.int_property != 0 else None) & True  # pyright: ignore[reportOptionalOperand]
            )

    def test_none_compared_with_a_typed_value(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("nonecompared")
        obj.int_property = 1
        self.assertFalse(
            self.conn.run(lambda: service.int32_to_string(obj.int_property) == None)  # noqa: E711
        )
        self.assertTrue(
            self.conn.run(lambda: service.echo_nullable_string(None) == None)  # noqa: E711
        )
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

    def test_nullable_conditional(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("nullableconditional")
        obj.int_property = 0

        def either():
            return ["a" if obj.int_property > 0 else service.echo_nullable_string(None)]

        self.assertEqual([None], self.conn.run(either))
        obj.int_property = 1
        self.assertEqual(["a"], self.conn.run(either))

    def test_nullable_returns(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("nullablereturns")
        obj.int_property = 0

        def returns():
            if obj.int_property > 0:
                return ["a"]
            return [service.echo_nullable_string(None)]

        self.assertEqual([None], self.conn.run(returns))
        obj.int_property = 1
        self.assertEqual(["a"], self.conn.run(returns))

    def test_variable_assigned_a_nullable_value(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("nullablevariable")
        obj.int_property = 1

        def reassigned():
            name = obj.get_value()
            if obj.int_property > 0:
                name = service.echo_nullable_string(None)
            return name

        def looped():
            value = obj
            count = 0
            objects = [obj, service.echo_test_object(None)]
            for value in service.echo_list_of_nullable_objects(objects):  # noqa: B007
                count += 1
            return value

        def optional():
            name: str | None = obj.get_value()
            name = service.echo_nullable_string(None)
            number: int | None = obj.int_property
            number = service.echo_nullable_int(None)
            return name is None and number is None

        self.assertIsNone(self.conn.run(reassigned))
        obj.int_property = 0
        self.assertEqual("value=nullablevariable", self.conn.run(reassigned))
        self.assertIsNone(self.conn.run(looped))
        self.assertTrue(self.conn.run(optional))

    def test_number_and_bool_variables_assigned_none(self) -> None:
        obj = self.conn.test_service.create_test_object("nonenumber")
        obj.int_property = 1

        def number(clear: bool):
            x = obj.int_property
            if clear:
                x = None
            return x

        def flag():
            x = obj.int_property > 0
            x = None
            return x

        self.assertIsNone(self.conn.run(number, True))
        self.assertEqual(1, self.conn.run(number, False))
        self.assertIsNone(self.conn.run(flag))

    def test_none_assigned_to_a_parameter(self) -> None:
        def number(x: int):
            x = None  # pyright: ignore[reportAssignmentType]
            return x

        def string(s: str):
            s = None  # pyright: ignore[reportAssignmentType]
            return s

        def optional(x: int | None):
            x = None
            return x

        for function in (number, string):
            with self.assertRaisesRegex(
                FunctionCompilationError, "annotated with a type that cannot be None"
            ):
                self.conn.compile(function)
        self.assertIsNone(self.conn.run(optional, 1))

    def test_none_where_none_is_not_allowed(self) -> None:
        service = self.conn.test_service

        def annotated():
            name: str = service.echo_nullable_string(None)  # pyright: ignore[reportAssignmentType]
            return name

        with self.assertRaisesRegex(TypeError, "null"):
            self.conn.run(annotated)
        with self.assertRaisesRegex(TypeError, "null"):
            self.conn.run(
                lambda: service.string_to_int32(service.echo_nullable_string(None))  # pyright: ignore[reportArgumentType]
            )
        with self.assertRaisesRegex(TypeError, "null"):
            self.conn.run(
                lambda: service.not_nullable_object(service.echo_test_object(None))  # pyright: ignore[reportArgumentType]
            )
        self.assertEqual(
            3,
            self.conn.run(
                lambda: service.string_to_int32(service.echo_nullable_string("3"))  # pyright: ignore[reportArgumentType]
            ),
        )

    def test_early_return(self) -> None:
        obj = self.conn.test_service.create_test_object("early")
        obj.int_property = 3

        def classify():
            if obj.int_property > 10:
                return "big"
            return "small"

        self.assertEqual("small", self.conn.run(classify))
        obj.int_property = 20
        self.assertEqual("big", self.conn.run(classify))

    def test_nullable_number_in_a_local(self) -> None:
        svc = self.conn.test_service

        def or_zero(value):
            def expression():
                x = svc.echo_nullable_int(value)
                if x is None:
                    x = 0
                return x + 1

            return expression

        self.assertEqual(1, self.conn.run(or_zero(None)))
        self.assertEqual(5, self.conn.run(or_zero(4)))

    def test_conversion_of_a_none_number(self) -> None:
        nullable = self.conn.test_service.echo_nullable_int
        for func in (
            lambda: int(nullable(None)),  # pyright: ignore[reportArgumentType]
            lambda: float(nullable(None)),  # pyright: ignore[reportArgumentType]
            lambda: round(nullable(None)),  # pyright: ignore[reportArgumentType]
            lambda: round(nullable(None), 1),  # pyright: ignore[reportArgumentType, reportCallIssue]
            lambda: round(nullable(None), -1),  # pyright: ignore[reportArgumentType, reportCallIssue]
            lambda: abs(nullable(None)),  # pyright: ignore[reportArgumentType]
        ):
            with self.assertRaises(TypeError):
                self.conn.run(func)
        self.assertEqual(3, self.conn.run(lambda: int(nullable(3))))  # pyright: ignore[reportArgumentType]
        self.assertEqual(-40, self.conn.run(lambda: round(nullable(-35), -1)))  # pyright: ignore[reportArgumentType, reportCallIssue]
        self.assertEqual(35, self.conn.run(lambda: round(nullable(35), 1)))  # pyright: ignore[reportArgumentType, reportCallIssue]

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

    def test_statement_function_as_stream(self) -> None:
        objs = self._make_objects("stmtstream")

        def total():
            value = 0
            for obj in objs:
                value += obj.int_property
            return value

        with self.conn.stream(total) as stream:
            self.assertEqual(6, stream())

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

    def test_walrus_operator(self) -> None:
        obj = self.conn.test_service.create_test_object("walrus")
        obj.int_property = 4

        def doubled_if_large():
            if (x := obj.int_property * 2) > 5:
                return x
            return 0

        self.assertEqual(8, self.conn.run(doubled_if_large))

        # A test that folds to a constant still makes the assignment
        def assigned_in_a_test(n: int) -> int:
            if (y := n * 2) is not None:
                return y
            return -1

        def assigned_in_a_tuple(n: int) -> int:
            size = len((y := n + 1, 2))
            return y + size

        self.assertEqual(6, self.conn.run(assigned_in_a_test, 3))
        self.assertEqual(6, self.conn.run(assigned_in_a_tuple, 3))

    def test_dictionary_display_and_keywords_in_source_order(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("sourceorder")
        obj.int_property = 0

        def pairs():
            count = obj.int_property
            return {
                (count := count + 1): (count := count + 1),
                (count := count + 1): (count := count + 1),
            }

        def keywords():
            count = obj.int_property
            return service.optional_arguments(y=str(count := count + 1), x=str(count))

        self.assertEqual({1: 2, 3: 4}, self.conn.run(pairs))
        self.assertEqual("11barnull", self.conn.run(keywords))

    def test_multiple_assignment(self) -> None:
        obj = self.conn.test_service.create_test_object("multi")
        obj.int_property = 5

        def double_read():
            first = second = obj.int_property
            return first + second

        self.assertEqual(10, self.conn.run(double_read))

    def test_conversions(self) -> None:
        obj = self.conn.test_service.create_test_object("convert")
        obj.int_property = 7
        self.assertEqual(3, self.conn.run(lambda: int(obj.int_property / 2)))
        self.assertEqual(7.0, self.conn.run(lambda: float(obj.int_property)))
        self.assertEqual("7", self.conn.run(lambda: str(obj.int_property)))

    def test_condition_with_operands_of_mixed_kinds(self) -> None:
        objs = self._make_objects("mixedcondition")
        test_service = self.conn.test_service
        test_service.string_property = "Apollo"
        objs[0].object_property = None

        def first_is_positive() -> bool:
            values = [o.int_property for o in objs]
            if values and values[0] > 0:
                return True
            return False

        def starts_with_a() -> bool:
            name = test_service.string_property
            if name and name.startswith("A"):
                return True
            return False

        def has_object() -> int:
            inner = objs[0].object_property
            if not (inner and inner.int_property > 0):
                return 0
            return 1

        self.assertTrue(self.conn.run(first_is_positive))
        self.assertTrue(self.conn.run(starts_with_a))
        self.assertEqual(0, self.conn.run(has_object))
        objs[0].object_property = objs[1]
        self.assertEqual(1, self.conn.run(has_object))

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

    def test_argument_type_is_not_narrowed(self) -> None:
        obj = self.conn.test_service.create_test_object("argument")

        def widened():
            return obj.float_to_string(2)

        self.assertEqual(obj.float_to_string(2), self.conn.run(widened))

        def truncated() -> None:
            obj.int_property = 2.5  # pyright: ignore[reportAttributeAccessIssue]

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(truncated)

        test_service = self.conn.test_service

        def computed_argument():
            return test_service.int32_to_string(obj.int_property / 2)  # pyright: ignore[reportArgumentType]

        def computed_property() -> None:
            obj.int_property = obj.int_property / 2  # pyright: ignore[reportAttributeAccessIssue]

        for func in (computed_argument, computed_property):
            with self.assertRaisesRegex(
                FunctionCompilationError, "no implicit conversion from double to int"
            ):
                self.conn.compile(func)

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
        service = self.conn.test_service
        obj = service.create_test_object("minmaxempty")
        obj.int_property = 1
        nullable = service.echo_nullable_int
        for func in (
            lambda: min([x for x in [obj.int_property] if x > 5]),
            lambda: max([x * 0.5 for x in [obj.int_property] if x > 5]),
            lambda: max([nullable(x) for x in [obj.int_property] if x > 5]),  # pyright: ignore[reportArgumentType]
            lambda: min([x for x in [obj.int_property] if x > 5], key=lambda x: -x),
        ):
            with self.assertRaisesRegex(ValueError, "arg is an empty sequence"):
                self.conn.run(func)
        self.assertEqual(1, self.conn.run(lambda: max([nullable(obj.int_property)])))  # pyright: ignore[reportArgumentType]

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
                self.conn.run(lambda: round(obj.int_property * value // 5, -1)),  # noqa: B023
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
            self.conn.compile(lambda: math.sqrt(obj.int_property, 2))  # pyright: ignore[reportCallIssue]
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(lambda: math.hypot(obj.int_property, 2, 2))

    def test_print(self) -> None:
        obj = self.conn.test_service.create_test_object("print")
        obj.int_property = 3
        self.conn.run(lambda: print("value", obj.int_property, sep="="))
        self.conn.run(lambda: print(obj.int_property, obj.get_value()))

    def test_print_as_a_statement(self) -> None:
        obj = self.conn.test_service.create_test_object("printstatement")
        obj.int_property = 3

        def func():
            print("value", obj.int_property, sep="=")
            print()
            return obj.int_property

        self.assertEqual(3, self.conn.run(func))

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
        for func in (
            lambda: [o.int_property for o in objs][ulong(10)[0] :],
            lambda: [o.int_property for o in objs][1.5:],  # pyright: ignore[reportArgumentType, reportCallIssue]
        ):
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
                lambda: {"a": x * o.int_property for x in factors for o in objs}  # noqa: B035
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

    def test_local_functions(self) -> None:
        objs = self._make_objects("localfn")

        def program():
            def double(x: int):
                return x * 2

            total = 0
            for obj in objs:
                total += double(obj.int_property)
            return total

        self.assertEqual(12, self.conn.run(program))

    def test_local_functions_defined_in_branches(self) -> None:
        obj = self.conn.test_service.create_test_object("localbranch")
        obj.int_property = 5

        def within():
            if obj.int_property > 0:

                def pick():  # pyright: ignore[reportRedeclaration]
                    return 1

                return pick()

            def pick():
                return 2

            return pick()

        self.assertEqual(1, self.conn.run(within))

        def after():
            if obj.int_property > 0:

                def pick():  # pyright: ignore[reportRedeclaration]
                    return 1

                obj.int_property = 6
            else:

                def pick():
                    return 2

                obj.int_property = 7
            return pick()

        with self.assertRaisesRegex(
            FunctionCompilationError, "local function 'pick' is defined in a nested"
        ):
            self.conn.compile(after)

        def redefined() -> int:
            def pick() -> int:
                return 1

            if obj.int_property > 0:

                def pick() -> int:
                    return 2

            return pick()

        with self.assertRaisesRegex(
            FunctionCompilationError, "local function 'pick' is defined in a nested"
        ):
            self.conn.compile(redefined)

        def defined_again() -> int:
            if obj.int_property > 0:

                def pick() -> int:
                    return 1

                obj.int_property = 8

            def pick() -> int:
                return 2

            return pick()

        self.assertEqual(2, self.conn.run(defined_again))

    def test_local_lambda(self) -> None:
        obj = self.conn.test_service.create_test_object("locallambda")
        obj.int_property = 20

        def program():
            base = obj.int_property
            offset = lambda: base + 1  # noqa: E731
            return offset() + offset()

        self.assertEqual(42, self.conn.run(program))

    def test_local_function_argument_types(self) -> None:
        obj = self.conn.test_service.create_test_object("localargs")
        obj.int_property = 3

        def widened():
            def half(x: float):
                return x / 2

            return half(1) + half(obj.int_property)

        self.assertEqual(2, self.conn.run(widened))

        def narrowed():
            def double(x: int):
                return x * 2

            return double(obj.int_property / 2)  # pyright: ignore[reportArgumentType]

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(narrowed)

        def mistyped():
            def double(x: int):
                return x * 2

            return double("two")  # pyright: ignore[reportArgumentType]

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(mistyped)

    def test_function_with_parameters(self) -> None:
        obj = self.conn.test_service.create_test_object("parameters")
        obj.int_property = 3
        TestClass = self.conn.test_service.TestClass

        def scaled(target: TestClass, factor: float, offset: int = 1) -> float:  # pyright: ignore[reportInvalidTypeForm]
            return target.int_property * factor + offset

        function = self.conn.compile(scaled)
        self.assertEqual(7.0, self.conn.run(function, obj, 2.0))
        self.assertEqual(4.5, self.conn.run(function, obj, 0.5, offset=3))
        self.assertEqual(0.0, self.conn.run(function, offset=0, factor=0.0, target=obj))
        # An uncompiled function is compiled on each call
        self.assertEqual(10.0, self.conn.run(scaled, obj, 3.0))

    def test_function_with_collection_and_nullable_parameters(self) -> None:
        def summary(values: list[int], label: str | None = None) -> str:
            if label is None:
                return str(len(values))
            return label + str(sum(values))

        function = self.conn.compile(summary)
        self.assertEqual("3", self.conn.run(function, [1, 2, 3]))
        self.assertEqual("total6", self.conn.run(function, [1, 2, 3], "total"))
        self.assertEqual("0", self.conn.run(function, [], None))

    def test_function_with_keyword_and_positional_only_parameters(self) -> None:
        def combine(first: int, /, second: int, *, third: int = 0) -> int:
            return first * 100 + second * 10 + third

        function = self.conn.compile(combine)
        self.assertEqual(123, self.conn.run(function, 1, second=2, third=3))
        self.assertEqual(120, self.conn.run(function, 1, 2))
        with self.assertRaises(TypeError):
            self.conn.run(function, first=1, second=2)
        with self.assertRaises(TypeError):
            self.conn.run(function, 1, 2, 3)

    def test_function_with_parameters_given_the_wrong_arguments(self) -> None:
        def double(x: int) -> int:
            return x * 2

        function = self.conn.compile(double)
        with self.assertRaises(TypeError):
            self.conn.run(function)
        with self.assertRaises(TypeError):
            self.conn.run(function, 1, 2)
        with self.assertRaises(TypeError):
            self.conn.run(function, y=1)

    def test_collection_of_another_kind_for_a_parameter_or_variable(self) -> None:
        obj = self.conn.test_service.create_test_object("anotherkind")

        def list_from_a_dict(x: list[int] = {}) -> int:  # noqa: B006  # pyright: ignore[reportArgumentType]
            return len(x)

        def set_from_a_list(x: set[int] = []) -> int:  # noqa: B006  # pyright: ignore[reportArgumentType]
            return len(x)

        def list_from_a_set(x: list[int] = {1}) -> int:  # noqa: B006  # pyright: ignore[reportArgumentType]
            return len(x)

        def reassigned():
            values = {obj.int_property}
            values = []
            return values

        for func, message in (
            (list_from_a_dict, r"an empty dict cannot be given a list type; use \[\]"),
            (set_from_a_list, r"an empty list cannot be given a set type; use set\(\)"),
            (list_from_a_set, "default value of parameter 'x'"),
            (reassigned, r"an empty list cannot be given a set type; use set\(\)"),
        ):
            with self.assertRaisesRegex(FunctionCompilationError, message):
                self.conn.compile(func)

    def test_function_with_parameters_rejected(self) -> None:
        obj = self.conn.test_service.create_test_object("parametersrejected")

        def unannotated(x) -> int:
            return x

        def variadic(*values: int) -> int:
            return len(values)

        def keywords(**values: int) -> int:
            return len(values)

        def mistyped_default(x: int = "one") -> int:  # pyright: ignore[reportArgumentType]
            return x

        def none_default(x: int = None) -> int:  # pyright: ignore[reportArgumentType]
            return x

        def too_large_default(x: int = 2**40) -> int:
            return x

        def short_tuple_default(x: tuple[int, int] = (1, 2, 3)) -> int:  # pyright: ignore[reportArgumentType]
            return x[0]

        for func, message in (
            (unannotated, "annotate the type of parameter 'x'"),
            (variadic, r"\*args and \*\*kwargs"),
            (keywords, r"\*args and \*\*kwargs"),
            (
                mistyped_default,
                "default value of parameter 'x' is not a value of type int",
            ),
            (none_default, "default value of parameter 'x' is not a value of type int"),
            (too_large_default, "default value of parameter 'x'"),
            (short_tuple_default, "default value of parameter 'x'"),
        ):
            with self.assertRaisesRegex(FunctionCompilationError, message):
                self.conn.compile(func)

        def streamed(x: int) -> bool:
            return obj.int_property > x

        for add in (self.conn.add_stream, self.conn.add_event):
            with self.assertRaisesRegex(
                FunctionCompilationError, "cannot take parameters"
            ):
                add(streamed)

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

    def test_none_argument(self) -> None:
        obj = self.conn.test_service.create_test_object("none")
        obj.int_property = 3
        service = self.conn.test_service
        self.assertTrue(
            self.conn.run(
                lambda: obj.int_property > 1 and service.echo_test_object(None) is None
            )
        )
        with self.assertRaisesRegex(FunctionCompilationError, "does not take None"):
            self.conn.compile(
                lambda: obj.int_property > 1 and service.not_nullable_object(None)  # pyright: ignore[reportArgumentType]
            )

        def assigns_none():
            best = None
            if obj.int_property > 1:
                best = obj
            return best

        self.assertEqual(obj, self.conn.run(assigns_none))
        obj.int_property = 0
        self.assertIsNone(self.conn.run(assigns_none))

    def test_returned_values_widen_to_their_common_type(self) -> None:
        obj = self.conn.test_service.create_test_object("commonreturn")

        def int_first():
            if obj.int_property > 0:
                return obj.int_property
            return obj.int_property - 0.5

        def double_first():
            if obj.int_property <= 0:
                return obj.int_property - 0.5
            return obj.int_property

        obj.int_property = 1
        for function in (int_first, double_first):
            result = self.conn.run(function)
            self.assertEqual(1.0, result)
            self.assertIsInstance(result, float)
        obj.int_property = 0
        for function in (int_first, double_first):
            self.assertEqual(-0.5, self.conn.run(function))

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

    def test_return_none_from_a_function_that_returns_values(self) -> None:
        obj = self.conn.test_service.create_test_object("returnnone")

        def none_first():
            if obj.int_property > 0:
                return None
            return obj.get_value()

        def none_last():
            if obj.int_property > 0:
                return obj.get_value()
            return None

        def annotated() -> int | None:
            if obj.int_property > 0:
                return None
            return 1

        obj.int_property = 1
        self.assertIsNone(self.conn.run(none_first))
        self.assertEqual("value=returnnone", self.conn.run(none_last))
        self.assertIsNone(self.conn.run(annotated))
        obj.int_property = 0
        self.assertEqual("value=returnnone", self.conn.run(none_first))
        self.assertIsNone(self.conn.run(none_last))
        self.assertEqual(1, self.conn.run(annotated))

        def bare_return():
            if obj.int_property > 0:
                return
            return 1

        def not_optional() -> int:
            return None  # pyright: ignore[reportReturnType]

        for function in (bare_return, not_optional):
            with self.assertRaises(FunctionCompilationError):
                self.conn.compile(function)

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

    def test_misused_local_function_is_named(self) -> None:
        obj = self.conn.test_service.create_test_object("misusedlocal")

        def assigned() -> int:
            def g(a: int) -> int:
                return a

            alias = g
            return alias(obj.int_property)

        def keyword() -> int:
            def g(a: int, b: int) -> int:
                return a + b

            return g(obj.int_property, b=1)

        for func, message in (
            (assigned, "local function 'g' can only be called"),
            (keyword, "'g' takes positional arguments only"),
        ):
            with self.assertRaisesRegex(
                FunctionCompilationError, message + r" \(line \d+\)$"
            ):
                self.conn.compile(func)

    def test_cast_and_comparison_of_unrelated_types_name_the_line(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("unrelatedcast")
        Deprecated = service.DeprecatedClass
        for func, message in (
            (lambda: cast(str, obj.int_property), "cannot cast a value of type int"),
            (lambda: cast(list[str], [obj.int_property]), "cannot cast a value"),
            (
                lambda: obj == cast(Deprecated | None, None),  # pyright: ignore[reportInvalidTypeForm]
                "cannot compare a value of type TestService",
            ),
            (
                lambda: cast(Deprecated | None, None) in [obj],  # pyright: ignore[reportInvalidTypeForm]
                "cannot compare a value of type TestService",
            ),
        ):
            with self.assertRaisesRegex(
                FunctionCompilationError, message + r".* \(line \d+\)$"
            ):
                self.conn.compile(func)
        # A cast converts the numbers in a collection
        self.assertEqual(
            [2.0], self.conn.run(lambda: cast(list[float], [obj.int_property + 2]))
        )

    def test_cast_none(self) -> None:
        obj = self.conn.test_service.create_test_object("castnone")

        def cast_none():
            if obj.int_property > 0:
                return obj.get_value()
            return cast(str | None, None)

        self.assertIsNone(self.conn.run(lambda: cast(str | None, None)))
        self.assertEqual([None], self.conn.run(lambda: [cast(int | None, None)]))
        self.assertIsNone(self.conn.run(cast_none))
        self.assertEqual(2.0, self.conn.run(lambda: cast(float, obj.int_property + 2)))
        # A cast narrows a float to an integer, truncating it toward zero
        obj.int_property = 7
        self.assertEqual(3, self.conn.run(lambda: cast(int, obj.int_property / 2)))
        self.assertEqual(-3, self.conn.run(lambda: cast(int, -obj.int_property / 2)))

    def test_variable_assigned_none_first(self) -> None:
        service = self.conn.test_service
        objs = [service.create_test_object(name) for name in ("x", "yy", "zzz")]

        def longest():
            best = None
            for item in objs:
                if best is None or len(item.get_value()) > len(best.get_value()):
                    best = item
            return best

        def annotated():
            best: str | None = None
            for item in objs:
                best = item.get_value()
            return best

        def later_none():
            value = objs[0].get_value()
            value = None
            return value

        def never_typed():
            value = None
            return value is None

        self.assertEqual(objs[2], self.conn.run(longest))
        self.assertEqual("value=zzz", self.conn.run(annotated))
        self.assertIsNone(self.conn.run(later_none))
        with self.assertRaisesRegex(FunctionCompilationError, "variable 'value'"):
            self.conn.compile(never_typed)

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
            self.conn.compile(lambda: {5: "a"}[obj.int_property * 1.0])  # pyright: ignore[reportArgumentType]
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
                lambda: (
                    service.increment_dictionary({"a": obj.int_property}) == {"a": 2}
                )
            )
        )
        self.assertTrue(self.conn.run(lambda: [obj.int_property] in [[0], [1]]))

    def test_list_concatenation(self) -> None:
        objs = self._make_objects("concat")
        self.assertEqual(
            [1, 2, 3, 9],
            self.conn.run(lambda: [o.int_property for o in objs] + [9]),
        )

    def test_list_augmented_addition_extends_in_place(self) -> None:
        obj = self.conn.test_service.create_test_object("extend")
        obj.int_property = 5

        def alias():
            values = [obj.int_property]
            same = values
            same += [1]
            return values

        def element():
            outer = [[obj.int_property]]
            inner = outer[0]
            inner += [2, 3]
            outer[0] += [4]
            return outer

        def itself():
            values = [obj.int_property]
            values += values
            return values

        self.assertEqual([5, 1], self.conn.run(alias))
        self.assertEqual([[5, 2, 3, 4]], self.conn.run(element))
        self.assertEqual([5, 5], self.conn.run(itself))

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

    def test_unsupported_constructs(self) -> None:
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(lambda x: x + 1)

        def unannotated():
            result = []
            result.append(1)
            return result

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(unannotated)

        obj = self.conn.test_service.create_test_object("unsupported")
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(lambda: f"{obj.int_property:.2f}")

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
        self.assertEqual(2, self.conn.run(lambda: +nullable(2)))  # pyright: ignore[reportOptionalOperand]
        with self.assertRaises(TypeError):
            self.conn.run(lambda: +nullable(None))  # pyright: ignore[reportOptionalOperand]

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
        for func in (
            lambda: mixed[obj.int_property],
            lambda: table[obj.string_property],  # pyright: ignore[reportAttributeAccessIssue]
            lambda: {unhashable},  # pyright: ignore[reportUnhashable]
            lambda: {unhashable: 2},  # pyright: ignore[reportUnhashable]
            lambda: "a" in missing,  # pyright: ignore[reportOperatorIssue]
            lambda: [obj.int_property][1:2, 0],  # pyright: ignore[reportArgumentType, reportCallIssue]
        ):
            with self.assertRaises(FunctionCompilationError):
                self.conn.compile(func)
        with self.assertRaisesRegex(FunctionCompilationError, "only a lambda or a def"):
            self.conn.compile(functools.partial(lambda x: x, 1))

    def test_remote_call_missing_an_argument(self) -> None:
        service = self.conn.test_service
        with self.assertRaisesRegex(
            FunctionCompilationError, r"missing the argument 'value' \(line \d+\)"
        ):
            self.conn.compile(lambda: service.float_to_string())  # noqa: PLW0108  # pyright: ignore[reportCallIssue]

    def test_recursive_local_function(self) -> None:
        obj = self.conn.test_service.create_test_object("recursive")

        def factorial():
            def fact(n: int) -> int:
                return 1 if n <= 1 else n * fact(n - 1)

            return fact(obj.int_property)

        with self.assertRaisesRegex(FunctionCompilationError, "recursion"):
            self.conn.compile(factorial)

    def test_empty_collection_error_names_the_line(self) -> None:
        obj = self.conn.test_service.create_test_object("emptyerror")
        empty: list = []
        with self.assertRaisesRegex(
            FunctionCompilationError,
            r"^Cannot compile expression: cannot determine the type of an empty "
            r"collection.*cast\(List\[int\], \[\]\) \(line \d+\)$",
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

    def test_empty_collection_of_a_declared_type(self) -> None:
        obj = self.conn.test_service.create_test_object("emptydeclared")

        def returned() -> list[int]:
            if obj.int_property > 100:
                return [obj.int_property]
            return []

        def reassigned():
            values: list[int] = [obj.int_property]
            values = []
            return values

        def passed():
            def count(values: dict[str, int]) -> int:
                return len(values)

            return count({}) + obj.int_property * 0

        self.assertEqual([], self.conn.run(lambda: cast(list[int], [])))
        self.assertEqual([], self.conn.run(returned))
        self.assertEqual([], self.conn.run(reassigned))
        self.assertEqual(0, self.conn.run(passed))

    def test_empty_collection_of_another_kind_than_its_annotation(self) -> None:
        obj = self.conn.test_service.create_test_object("emptykind")
        history: list[int] = []

        def set_from_a_dict():
            values: set[int] = {}  # pyright: ignore[reportAssignmentType]
            values.add(obj.int_property)
            return values

        def dict_from_a_list():
            values: dict[str, int] = []  # pyright: ignore[reportAssignmentType]
            return values

        def state_set_from_a_list():
            nonlocal history
            history = cast(set[int], history)  # pyright: ignore[reportAssignmentType]
            return obj.int_property in history

        for func, message in (
            (set_from_a_dict, "an empty dict cannot be given a set type; use set()"),
            (dict_from_a_list, "an empty list cannot be given a dict type; use {}"),
            (
                state_set_from_a_list,
                "an empty list cannot be given a set type; use set()",
            ),
        ):
            with self.assertRaises(FunctionCompilationError) as cm:
                self.conn.compile(func)
            self.assertIn(message, str(cm.exception))

    def test_unannotated_empty_variable_error_names_the_line(self) -> None:
        obj = self.conn.test_service.create_test_object("emptyvariable")

        def unannotated():
            result = []
            result.append(obj.int_property)
            return result

        with self.assertRaisesRegex(
            FunctionCompilationError,
            r"^Cannot compile expression: .*annotate.*\(line \d+\)$",
        ):
            self.conn.compile(unannotated)

    def test_client_call_statement_is_an_error(self) -> None:
        calls = []

        def logged():
            calls.append(1)
            return 1

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(logged)
        self.assertEqual([], calls)

    def test_range(self) -> None:
        obj = self.conn.test_service.create_test_object("range")
        obj.int_property = 4
        self.assertEqual([0, 1, 2, 3], self.evaluate(lambda: range(obj.int_property)))
        self.assertEqual([2, 3], self.evaluate(lambda: range(2, obj.int_property)))
        self.assertEqual([4, 2], self.evaluate(lambda: range(obj.int_property, 0, -2)))
        self.assertEqual([], self.evaluate(lambda: range(obj.int_property, 0)))
        # The counter does not wrap around past the end of an int
        self.assertEqual(
            [2147483600],
            self.evaluate(
                lambda: range(2147483600 + obj.int_property * 0, 2147483647, 100)
            ),
        )
        self.assertEqual(
            [-2147483600],
            self.evaluate(
                lambda: range(-2147483600 + obj.int_property * 0, -2147483648, -100)
            ),
        )
        self.assertEqual(
            [0, 2, 4, 6],
            self.evaluate(lambda: [2 * i for i in range(obj.int_property)]),
        )

        def total():
            result = 0
            for i in range(obj.int_property):
                if i == 1:
                    continue
                result += i
            return result

        self.assertEqual(5, self.conn.run(total))

        def empty():
            result = obj.int_property
            for i in range(0):
                result += i
            return result

        self.assertEqual(4, self.conn.run(empty))

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(lambda: range(0, 4, obj.int_property))
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(lambda: range(0, obj.int_property, 0))

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

    def test_dictionary_iteration(self) -> None:
        counts = self.conn.test_service.increment_dictionary({"a": 1, "b": 2})
        self.assertEqual(["a", "b"], self.conn.run(lambda: sorted(counts)))
        self.assertEqual(["a", "b"], self.conn.run(lambda: sorted(k for k in counts)))
        self.assertTrue(self.conn.run(lambda: "a" in counts))
        self.assertTrue(self.conn.run(lambda: "c" not in counts))
        self.assertEqual(
            {"a2", "b3"},
            self.conn.run(lambda: {k + str(v) for k, v in counts.items()}),
        )

        def keys():
            result: list[str] = []
            for key in counts:
                result.append(key)
            return sorted(result)

        self.assertEqual(["a", "b"], self.conn.run(keys))

        def pairs():
            total = 0
            for key, value in counts.items():
                if key == "b":
                    total += value * 10
            return total

        self.assertEqual(30, self.conn.run(pairs))

    def test_tuple_unpacking(self) -> None:
        obj = self.conn.test_service.create_test_object("unpack")
        obj.int_property = 3

        def swap():
            first, second = obj.int_property, 5
            first, second = second, first
            return first * 10 + second

        self.assertEqual(53, self.conn.run(swap))

        def constant():
            one, (two, three) = 1, (2, obj.int_property)
            return one + two + three

        self.assertEqual(6, self.conn.run(constant))

        values = self.conn.test_service.increment_list([1, 2])

        def nested():
            total = 0
            for i, (x, y) in enumerate(zip(values, values)):
                total += i * (x + y)
            return total

        self.assertEqual(6, self.conn.run(nested))

        def mismatched():
            one, two, three = obj.int_property, 1  # pyright: ignore[reportAssignmentType]
            return one + two + three

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(mismatched)

    def test_truthiness(self) -> None:
        obj = self.conn.test_service.create_test_object("truth")
        obj.int_property = 3
        obj.object_property = None
        values = self.conn.test_service.increment_list([1])
        self.assertEqual("yes", self.conn.run(lambda: "yes" if values else "no"))
        self.assertTrue(self.evaluate(lambda: not obj.object_property))
        self.assertFalse(self.evaluate(lambda: not obj.int_property))
        self.assertTrue(self.evaluate(lambda: not obj.get_value()[:0]))
        self.assertEqual(3, self.evaluate(lambda: obj.int_property or 7))
        self.assertEqual(7, self.evaluate(lambda: obj.int_property and 7))
        self.assertEqual(
            [3], self.evaluate(lambda: [x for x in [0, obj.int_property] if x])
        )

        def countdown():
            remaining = obj.int_property
            steps = 0
            while remaining:
                remaining -= 1
                steps += 1
            return steps

        self.assertEqual(3, self.conn.run(countdown))

        # A tuple, an enumeration value and a structure are true unless None, and
        # bytes are true unless empty
        service = self.conn.test_service

        def tuple_truth():
            pair = (obj.int_property, 1)
            return 1 if pair else 2

        def bytes_truth():
            data = b"" if obj.int_property > 0 else b"x"
            return 1 if data else 2

        self.assertEqual(1, self.conn.run(tuple_truth))
        self.assertEqual(2, self.conn.run(bytes_truth))
        self.assertEqual(
            1,
            self.conn.run(
                lambda: 1 if service.enum_echo(service.TestEnum.value_a) else 2
            ),
        )
        self.assertEqual(
            1,
            self.conn.run(lambda: 1 if service.counter_struct("truth") else 2),
        )
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(lambda: obj.int_property or "none")

    def test_unknown_member(self) -> None:
        obj = self.conn.test_service.create_test_object("unknown")
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(lambda: obj.no_such_member)  # pyright: ignore[reportAttributeAccessIssue]

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

    def test_construct_struct_in_source_order(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("structorder")
        obj.int_property = 0

        def built():
            count = obj.int_property
            return service.struct_echo(
                service.TestStruct(
                    string_field=str(count := count + 1),
                    int_field=count,
                    enum_field=service.TestEnum.value_a,
                    list_field=[1],
                )
            )

        value = self.conn.run(built)
        self.assertEqual("1", value.string_field)
        self.assertEqual(1, value.int_field)

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
            self.conn.compile(lambda: echo(struct).no_such_field)  # pyright: ignore[reportAttributeAccessIssue]
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(lambda: echo(test_struct(1, "x")))  # pyright: ignore[reportCallIssue]
        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(
                lambda: echo(test_struct(1, "x", enum.value_a, [2], no_such_field=1))  # pyright: ignore[reportCallIssue]
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
        text = self._text()
        self.assertEqual(
            "HELLO, WORLD",
            self.conn.run(lambda: text.string_property.upper()),  # noqa: PLW0108
        )
        self.assertEqual(
            "hello, world",
            self.conn.run(lambda: text.string_property.lower()),  # noqa: PLW0108
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
        text = self.conn.test_service
        text.string_property = "  pad  "
        self.assertEqual("pad", self.conn.run(lambda: text.string_property.strip()))  # noqa: PLW0108
        self.assertEqual("pad  ", self.conn.run(lambda: text.string_property.lstrip()))  # noqa: PLW0108
        self.assertEqual("  pad", self.conn.run(lambda: text.string_property.rstrip()))  # noqa: PLW0108

    def test_raise(self) -> None:
        def failing() -> None:
            raise RuntimeError("no good")

        with self.assertRaises(RuntimeError) as caught:
            self.conn.run(failing)
        self.assertIn("no good", str(caught.exception))

    def test_raise_a_computed_message(self) -> None:
        text = self._text()

        def failing() -> None:
            raise ValueError("bad: " + text.string_property)

        with self.assertRaises(ValueError) as caught:
            self.conn.run(failing)
        self.assertIn("bad: Hello, World", str(caught.exception))

    def test_raise_a_message_that_is_not_a_string(self) -> None:
        obj = self.conn.test_service.create_test_object("raisenumber")
        obj.int_property = 3

        def constant() -> None:
            if obj.int_property > 0:
                raise ValueError(5)

        def computed() -> None:
            raise ValueError(obj.int_property)

        for func, message in ((constant, "5"), (computed, "3")):
            with self.assertRaises(ValueError) as caught:
                self.conn.run(func)
            self.assertIn(message, str(caught.exception))

    def test_try_except(self) -> None:
        def guarded() -> str:
            try:
                raise RuntimeError("inner")
            except RuntimeError as exn:
                return "caught " + exn  # pyright: ignore[reportOperatorIssue]
            return "not reached"

        self.assertEqual("caught inner", self.conn.run(guarded))

    def test_try_that_returns_on_every_path_ends_a_function(self) -> None:
        obj = self.conn.test_service.create_test_object("tryreturn")
        obj.int_property = 3

        def at(index) -> str:
            def lookup() -> str:
                try:
                    values = [obj.int_property]
                    return str(values[obj.int_property - index])
                except Exception as error:
                    return "missing: " + error  # pyright: ignore[reportOperatorIssue]

            return lookup  # pyright: ignore[reportReturnType]

        self.assertEqual("3", self.conn.run(at(3)))
        self.assertTrue(self.conn.run(at(0)).startswith("missing: "))

        def no_handler_value() -> int:  # pyright: ignore[reportReturnType]
            try:
                return obj.int_property
            except Exception:
                obj.int_property = 0

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(no_handler_value)

    def test_function_ending_in_raise(self) -> None:
        obj = self.conn.test_service.create_test_object("endraise")

        def positive() -> int:
            if obj.int_property > 0:
                return obj.int_property
            raise ValueError("not positive")

        def handled() -> str:
            try:
                raise KeyError("key")
            except KeyError as error:
                return error  # pyright: ignore[reportReturnType]

        obj.int_property = 2
        self.assertEqual(2, self.conn.run(positive))
        obj.int_property = 0
        with self.assertRaisesRegex(ValueError, "not positive"):
            self.conn.run(positive)
        self.assertEqual("key", self.conn.run(handled))

    def test_annotated_function_that_only_raises(self) -> None:
        obj = self.conn.test_service.create_test_object("onlyraises")
        obj.int_property = 1

        def fails() -> int:
            raise ValueError("always")

        def fails_on_both_branches() -> list[str]:
            if obj.int_property > 0:
                raise ValueError("positive")
            else:
                raise KeyError("not positive")

        def calls_a_failing_function() -> int:
            def fail() -> int:
                raise ValueError("local")

            return fail() + obj.int_property

        for func, error, message in (
            (fails, ValueError, "always"),
            (fails_on_both_branches, ValueError, "positive"),
            (calls_a_failing_function, ValueError, "local"),
        ):
            with self.assertRaisesRegex(error, message):
                self.conn.run(func)

    def test_final_if_else_with_a_branch_that_raises_or_tries(self) -> None:
        obj = self.conn.test_service.create_test_object("finalbranches")

        def raises() -> int:
            if obj.int_property > 0:
                return obj.int_property
            else:
                message = "not positive: " + str(obj.int_property)
                raise ValueError(message)

        def tries() -> int:
            values = {1: 10}
            if obj.int_property > 0:
                try:
                    return values[obj.int_property]
                except KeyError:
                    return 0
            else:
                return -1

        obj.int_property = 2
        self.assertEqual(2, self.conn.run(raises))
        self.assertEqual(0, self.conn.run(tries))
        obj.int_property = 1
        self.assertEqual(10, self.conn.run(tries))
        obj.int_property = 0
        with self.assertRaisesRegex(ValueError, "not positive: 0"):
            self.conn.run(raises)
        self.assertEqual(-1, self.conn.run(tries))

    def test_raise_each_builtin(self) -> None:
        cases = (
            (RuntimeError, RuntimeError),
            (ValueError, ValueError),
            (KeyError, KeyError),
            (IndexError, IndexError),
            (ZeroDivisionError, ZeroDivisionError),
            (NotImplementedError, NotImplementedError),
            (TimeoutError, TimeoutError),
            (OverflowError, OverflowError),
            (Exception, self.conn.krpc.Error),
        )

        def raising(raised):
            def failing() -> None:
                raise raised("boom")

            return failing

        for raised, received in cases:
            with self.subTest(raised.__name__):
                with self.assertRaisesRegex(received, "boom"):
                    self.conn.run(raising(raised))

    def test_except_the_server_errors(self) -> None:
        obj = self.conn.test_service.create_test_object("servererrors")
        obj.int_property = 0

        def guarded() -> str:
            result = ""
            values = [1]
            try:
                values[obj.int_property + 1] = 2
            except IndexError:
                result += "index "
            try:
                result += str(1 // obj.int_property)
            except ZeroDivisionError:
                result += "zero "
            try:
                result += str(values[obj.int_property - 2])
            except LookupError:
                result += "lookup "
            try:
                result += str(1 % obj.int_property)
            except ArithmeticError:
                result += "arithmetic "
            try:
                raise NotImplementedError("unsupported")
            except RuntimeError:
                result += "runtime "
            try:
                raise TimeoutError("late")
            except OSError:
                result += "os "
            try:
                raise Exception("general")
            except Exception:
                result += "error"
            return result

        self.assertEqual(
            "index zero lookup arithmetic runtime os error",
            self.conn.run(guarded),
        )

    def test_try_except_all(self) -> None:
        def guarded() -> str:
            try:
                raise ValueError("inner")
            except Exception as exn:
                return "caught " + exn  # pyright: ignore[reportOperatorIssue]
            return "not reached"

        self.assertEqual("caught inner", self.conn.run(guarded))

    def test_except_as_name_is_bound_only_in_the_handler(self) -> None:
        obj = self.conn.test_service.create_test_object("exceptas")
        obj.int_property = 1

        def shadowed(fail: bool) -> int:
            x = obj.int_property
            try:
                if fail:
                    raise ValueError("inner")
            except ValueError as x:  # noqa: F811
                if len(x) > 10:  # pyright: ignore[reportArgumentType]
                    return -1
            x = 2
            return x

        def several(fail: bool) -> int:
            try:
                if fail:
                    raise RuntimeError("inner")
            except ValueError:
                pass
            except RuntimeError:
                pass
            x = obj.int_property
            return x

        self.assertEqual(2, self.conn.run(shadowed, True))
        self.assertEqual(2, self.conn.run(shadowed, False))
        self.assertEqual(1, self.conn.run(several, True))

    def test_try_except_does_not_catch_another_exception(self) -> None:
        destroyed = self.conn.krpc.ObjectDestroyedException

        def guarded() -> str:
            try:
                raise ValueError("inner")
            except destroyed:
                return "wrong"
            return "not reached"

        with self.assertRaises(ValueError):
            self.conn.run(guarded)

    def test_try_except_tries_handlers_in_order(self) -> None:
        def guarded() -> str:
            result = ""
            for message in ["first", "second", "none"]:
                try:
                    if message == "first":
                        raise ValueError(message)
                    if message == "second":
                        raise RuntimeError(message)
                    result += "none,"
                except ValueError as exn:
                    result += "value " + exn + ","  # pyright: ignore[reportOperatorIssue]
                except Exception as exn:
                    result += "other " + exn + ","  # pyright: ignore[reportOperatorIssue]
            return result

        self.assertEqual("value first,other second,none,", self.conn.run(guarded))

    def test_try_except_does_not_catch_from_an_earlier_handler(self) -> None:
        def guarded() -> str:
            try:
                raise ValueError("inner")
            except ValueError:
                raise RuntimeError("from the handler")  # noqa: B904
            except RuntimeError:
                return "wrong"
            return "not reached"

        with self.assertRaises(RuntimeError) as caught:
            self.conn.run(guarded)
        self.assertIn("from the handler", str(caught.exception))

    def test_try_finally(self) -> None:
        def guarded() -> str:
            result = "start"
            try:
                try:
                    raise RuntimeError("inner")
                finally:
                    result = "cleaned"
            except Exception:
                pass
            return result

        self.assertEqual("cleaned", self.conn.run(guarded))

    def test_jump_out_of_a_finally_clause(self) -> None:
        obj = self.conn.test_service.create_test_object("finallyjump")

        def returns() -> int:
            try:
                obj.int_property = 1
            finally:
                return 2  # noqa: B012

        def breaks() -> None:
            while obj.int_property < 10:
                try:
                    obj.int_property += 1
                finally:
                    if obj.int_property > 5:
                        break  # noqa: B012

        def loops() -> int:
            total = 0
            try:
                obj.int_property = 1
            finally:
                for value in [1, 2, 3]:
                    if value == 2:
                        continue
                    total += value
            return total

        for func, message in ((returns, "return"), (breaks, "break")):
            with self.assertRaisesRegex(
                FunctionCompilationError,
                message + r" cannot leave a finally clause \(line \d+\)$",
            ):
                self.conn.compile(func)
        self.assertEqual(4, self.conn.run(loops))

    def test_assign_at_an_index_that_is_not_an_int(self) -> None:
        obj = self.conn.test_service.create_test_object("assignindex")

        def assigned():
            values = [obj.int_property]
            values[obj.int_property * 1.0] = 1  # pyright: ignore[reportArgumentType, reportCallIssue]
            return values

        with self.assertRaisesRegex(
            FunctionCompilationError,
            r"an index must be an int, not a value of type double \(line \d+\)$",
        ):
            self.conn.compile(assigned)

    def test_unsupported_raise(self) -> None:
        def bare() -> None:
            raise  # noqa: PLE0704

        def not_an_exception() -> None:
            raise len("x")  # pyright: ignore[reportGeneralTypeIssues]

        for func in (bare, not_an_exception):
            with self.assertRaises(FunctionCompilationError):
                self.conn.compile(func)

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
        service = self.conn.test_service
        obj = service.create_test_object("unordered")
        for func, message in (
            (lambda: {obj.int_property}[0:1], "cannot slice a value of type set"),  # pyright: ignore[reportIndexIssue]
            (lambda: {obj.int_property}[-1], "cannot index a value of type set"),  # pyright: ignore[reportIndexIssue]
            (
                lambda: reversed({obj.int_property}),  # pyright: ignore[reportArgumentType, reportCallIssue]
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
        for func, message in (
            (
                lambda: [obj.int_property] + [obj.get_value()],
                "cannot add a value of type list.str. to a value of type list.int.",
            ),
            (
                lambda: service.int32_to_string(obj.get_value()),  # pyright: ignore[reportArgumentType]
                "cannot use a value of type str for parameter 'value'",
            ),
            (lambda: math.sqrt(obj.get_value()), "cannot use a value of type str"),  # pyright: ignore[reportArgumentType]
            (lambda: obj.get_value().find(5), "str.find takes a str here"),  # pyright: ignore[reportArgumentType]
            (lambda: obj.get_value().replace(1, 2), "str.replace takes a str here"),  # pyright: ignore[reportArgumentType]
            (
                lambda: obj.get_value().startswith(("a", "b")),
                "str.startswith takes a str here, not a value of type tuple",
            ),
            (
                lambda: ",".join([obj.int_property]),  # pyright: ignore[reportArgumentType, reportCallIssue]
                "str.join takes a list or set of str here",
            ),
            (
                lambda: (obj.int_property, 2)[obj.int_property],
                "a tuple can only be indexed by a constant integer",
            ),
            (lambda: {obj.int_property}[0], "cannot index a value of type set"),  # pyright: ignore[reportIndexIssue]
            (
                lambda: [obj.int_property][longs()[0]],
                "an index must be an int, not a value of type long",
            ),
            (
                lambda: [obj.int_property][obj.int_property * 1.0],  # pyright: ignore[reportArgumentType, reportCallIssue]
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
                lambda: len(obj.int_property),  # pyright: ignore[reportArgumentType]
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
            (lambda: obj.int_property + unknown, "cannot resolve the name 'unknown'"),  # noqa: F821  # pyright: ignore[reportUndefinedVariable]
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
            self.conn.run(lambda: [obj.int_property, 1, 2][-4])  # noqa: PLE0643
        with self.assertRaisesRegex(IndexError, "index -2 is out of range"):
            self.conn.run(lambda: str(obj.int_property)[-2])

    def test_list_removal(self) -> None:
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

        self.assertEqual([1, 3], self.conn.run(trimmed))
        self.assertEqual([], self.conn.run(cleared))
        self.assertEqual([2, 3], self.conn.run(deleted))

    def test_dictionary_comprehension_with_several_for_clauses(self) -> None:
        objs = self._make_objects("nesteddict")

        def products() -> dict:
            return {
                a.int_property * 10 + b.int_property: a.int_property * b.int_property
                for a in objs
                for b in objs
                if a.int_property < b.int_property
            }

        self.assertEqual({12: 2, 13: 3, 23: 6}, self.conn.run(products))

    def test_evaluation_order(self) -> None:
        counter = self.conn.test_service.counter
        objs = self._make_objects("order")

        def chained() -> bool:
            return (
                counter("order.chain") < counter("order.chain") < counter("order.chain")
            )

        def short_circuit() -> bool:
            return counter("order.a") > counter("order.a") < counter("order.b")

        def sliced() -> list:
            values = [o.int_property for o in objs]
            return values[counter("order.slice") : 3]

        def incremented() -> int:
            values = {1: 10, 2: 20}
            values[counter("order.key")] += 1
            return values[1] * 100 + values[2] + counter("order.key") * 10000

        self.assertTrue(self.conn.run(chained))
        self.assertFalse(self.conn.run(short_circuit))
        self.assertEqual(1, counter("order.b"))
        self.assertEqual([2, 3], self.conn.run(sliced))
        self.assertEqual(2, counter("order.slice"))
        self.assertEqual(1100 + 20 + 20000, self.conn.run(incremented))

        def assigned() -> int:
            values = {1: 0, 2: 0}
            values[counter("order.assign")] = counter("order.assign")
            return values[1] * 10 + values[2]

        def property_assigned() -> None:
            objs[counter("order.target") - 1].int_property = counter("order.target")

        def property_incremented() -> int:
            objs[counter("order.object") - 1].int_property += 5
            return counter("order.object")

        # The value is evaluated before the target, and a target's object once
        self.assertEqual(1, self.conn.run(assigned))
        self.conn.run(property_assigned)
        self.assertEqual(1, objs[1].int_property)
        self.assertEqual(2, self.conn.run(property_incremented))
        self.assertEqual(6, objs[0].int_property)

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

    def test_removing_a_missing_value(self) -> None:
        objs = self._make_objects("missingremoval")

        def from_a_list() -> None:
            values = [o.int_property for o in objs]
            values.remove(4)

        def from_a_set() -> None:
            values = {o.int_property for o in objs}
            values.remove(4)

        def discarded() -> int:
            values = {o.int_property for o in objs}
            values.discard(4)
            return len(values)

        def deleted() -> None:
            values = {o.int_property: o.int_property for o in objs}
            del values[4]

        def caught() -> int:
            values = {o.int_property for o in objs}
            try:
                values.remove(4)
            except KeyError:
                return -1
            return len(values)

        with self.assertRaisesRegex(ValueError, "value not in list"):
            self.conn.run(from_a_list)
        with self.assertRaisesRegex(KeyError, "value not in set"):
            self.conn.run(from_a_set)
        self.assertEqual(3, self.conn.run(discarded))
        with self.assertRaisesRegex(KeyError, "key not in dictionary"):
            self.conn.run(deleted)
        self.assertEqual(-1, self.conn.run(caught))

        def discard_value():
            values = {o.int_property for o in objs}
            return values.discard(1)

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(discard_value)

    def test_negative_index_on_write(self) -> None:
        objs = self._make_objects("negativewrite")

        def assigned() -> list:
            values = [o.int_property for o in objs]
            values[-1] = 7
            return values

        def incremented() -> list:
            values = [o.int_property for o in objs]
            values[-2] += 10
            return values

        def deleted() -> list:
            values = [o.int_property for o in objs]
            del values[-1]
            return values

        self.assertEqual([1, 2, 7], self.conn.run(assigned))
        self.assertEqual([1, 12, 3], self.conn.run(incremented))
        self.assertEqual([1, 2], self.conn.run(deleted))

    def test_set_removal(self) -> None:
        objs = self._make_objects("setremoval")

        def trimmed() -> int:
            values = {o.int_property for o in objs}
            values.discard(2)
            return len(values)

        self.assertEqual(2, self.conn.run(trimmed))

    def test_dictionary_keys_values_and_removal(self) -> None:
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

        self.assertEqual([1, 2, 3], self.conn.run(keys))
        self.assertEqual([2, 4, 6], self.conn.run(values_of))
        self.assertEqual([2, 3], self.conn.run(without_a_key))
        self.assertEqual([], self.conn.run(cleared))

    def test_iterate_over_dictionary_keys(self) -> None:
        objs = self._make_objects("dictloop")

        def total() -> int:
            values = {o.int_property: o.int_property * 2 for o in objs}
            result = 0
            for key in values.keys():
                result += values[key]
            return result

        self.assertEqual(12, self.conn.run(total))

    def test_unsupported_delete(self) -> None:
        objs = self._make_objects("baddelete")

        def whole_name() -> None:
            values = [o.int_property for o in objs]
            del values

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(whole_name)

    def test_write_to_a_captured_collection_rejected(self) -> None:
        obj = self.conn.test_service.create_test_object("captured")
        data = [1, 2, 3]
        mapping = {"a": 1}

        def assign() -> None:
            data[0] = obj.int_property

        def assign_key() -> None:
            mapping["a"] = obj.int_property

        def augment() -> None:
            data[0] += obj.int_property

        def delete():
            del data[0]
            return obj.int_property

        for func, message in (
            (assign, "cannot assign to an element of a client side value"),
            (assign_key, "cannot assign to an element of a client side value"),
            (augment, "cannot assign to an element of a client side value"),
            (delete, "cannot delete an element of a client side value"),
        ):
            with self.assertRaises(FunctionCompilationError) as cm:
                self.conn.compile(func)
            self.assertIn(message, str(cm.exception))

    def test_state_variable_keeps_its_value(self) -> None:
        obj = self.conn.test_service.create_test_object("state")
        obj.int_property = 2
        count = 0

        def step():
            nonlocal count
            count += obj.int_property
            return count

        function = self.conn.compile(step)
        self.assertEqual(2, self.conn.run(function))
        self.assertEqual(4, self.conn.run(function))
        self.assertEqual(6, self.conn.run(function))
        # An uncompiled function starts again from the captured value
        self.assertEqual(2, self.conn.run(step))
        self.assertEqual(2, self.conn.run(step))
        self.assertEqual(0, count)

    def test_state_variable_shared_by_two_streams(self) -> None:
        ticks = 0

        def step():
            nonlocal ticks
            ticks += 1
            return ticks

        function = self.conn.compile(step)
        first: list = []
        second: list = []
        with self.conn.stream(function) as stream1:
            with self.conn.stream(function) as stream2:
                stream1.add_callback(first.append)
                stream2.add_callback(second.append)
                deadline = time.monotonic() + 10
                while (len(first) < 3 or len(second) < 3) and (
                    time.monotonic() < deadline
                ):
                    time.sleep(0.01)
        # Each evaluation advances the one state, so no value repeats
        self.assertGreaterEqual(len(first), 3)
        self.assertGreaterEqual(len(second), 3)
        self.assertFalse(set(first) & set(second))

    def test_compile_as_a_decorator(self) -> None:
        obj = self.conn.test_service.create_test_object("decorated")
        obj.int_property = 3

        def counter():
            count = 0

            @self.conn.compile
            def step():
                nonlocal count
                count += obj.int_property
                return count

            return step

        function = counter()
        self.assertEqual(3, self.conn.run(function))
        self.assertEqual(6, self.conn.run(function))

    def test_state_variable_in_a_stream(self) -> None:
        obj = self.conn.test_service.create_test_object("statestream")
        obj.int_property = 1
        ticks = 0

        def step():
            nonlocal ticks
            ticks += obj.int_property
            return ticks

        with self.conn.stream(step) as stream:
            first = stream()
            deadline = time.monotonic() + 10
            while stream() <= first + 1 and time.monotonic() < deadline:
                time.sleep(0.01)
            self.assertGreater(stream(), first + 1)

    def test_stream_of_a_state_variable_alone(self) -> None:
        ticks = 0

        def step():
            nonlocal ticks
            ticks += 1
            return ticks

        with self.conn.stream(step) as stream:
            first = stream()
            deadline = time.monotonic() + 10
            while stream() <= first + 1 and time.monotonic() < deadline:
                time.sleep(0.01)
            self.assertGreater(stream(), first + 1)
        # A StdLib call gives the same value on every update
        with self.assertRaises(FunctionCompilationError) as cm:
            self.conn.add_stream(lambda: math.sqrt(len(range(4))))
        self.assertIn("same on every update", str(cm.exception))

    def test_annotated_return_type_with_no_return(self) -> None:
        obj = self.conn.test_service.create_test_object("noreturn")

        def assigns() -> int:  # pyright: ignore[reportReturnType]
            obj.int_property = 5

        with self.assertRaisesRegex(FunctionCompilationError, "returns no value"):
            self.conn.compile(assigns)

    def test_loop_over_a_variable_assigned_none(self) -> None:
        objs = self._make_objects("loopnone")

        def last():
            value = None
            for value in [o.int_property for o in objs]:  # noqa: B007
                pass
            return value

        self.assertEqual(3, self.conn.run(last))

    def test_state_variable_as_a_loop_variable(self) -> None:
        objs = self._make_objects("stateloop")
        last = 0

        def step():
            nonlocal last
            for last in [o.int_property for o in objs]:  # noqa: B007
                pass
            return last

        self.assertEqual(3, self.conn.run(step))

    def test_state_variable_collection(self) -> None:
        obj = self.conn.test_service.create_test_object("statelist")
        obj.int_property = 7
        values = [1]

        def step():
            nonlocal values
            values.append(obj.int_property)
            values[0] += 1
            return values

        function = self.conn.compile(step)
        self.assertEqual([2, 7], self.conn.run(function))
        self.assertEqual([3, 7, 7], self.conn.run(function))
        self.assertEqual([1], values)

    def test_state_variable_errors(self) -> None:
        obj = self.conn.test_service.create_test_object("stateerrors")
        count = 0
        last = None
        total = 0.0

        def narrowed():
            nonlocal count
            count = obj.int_property * 0.5
            return count

        def untyped():
            nonlocal last
            last = obj.int_property
            return last

        def assigned_none():
            nonlocal total
            total = None
            return obj.int_property

        def cast_none_to_a_number():
            nonlocal last
            last = cast(float, last)
            return last + obj.int_property

        def in_a_local_function():
            sum_ = 0

            def add(x: int):
                nonlocal sum_
                sum_ += x

            add(obj.int_property)
            return sum_

        for func, message in (
            (narrowed, "when assigning to variable 'count'"),
            (untyped, "state variable 'last' is None when the function is compiled"),
            (assigned_none, "state variable 'total' has a type that cannot be None"),
            (
                cast_none_to_a_number,
                "state variable 'last' is None when the function is compiled, and is "
                "cast to a type that cannot be None",
            ),
            (in_a_local_function, "nonlocal is supported only in the function"),
        ):
            with self.assertRaises(FunctionCompilationError) as cm:
                self.conn.compile(func)
            self.assertIn(message, str(cm.exception))

    def test_state_variable_typed_by_a_cast(self) -> None:
        obj = self.conn.test_service.create_test_object("statecast")
        obj.int_property = 3
        history = []
        last = None
        total = 0

        def step():
            nonlocal history, last, total
            history = cast(list[int], history)
            last = cast(int | None, last)
            total = cast(float, total)
            history.append(obj.int_property)
            total += obj.int_property / 2
            last = obj.int_property
            return (len(history), total)

        function = self.conn.compile(step)
        self.assertEqual((1, 1.5), self.conn.run(function))
        self.assertEqual((2, 3.0), self.conn.run(function))

        def untyped():
            nonlocal history
            history.append(obj.int_property)
            return history

        with self.assertRaisesRegex(
            FunctionCompilationError, r"empty when the function is compiled.*cast"
        ):
            self.conn.compile(untyped)

        def cast_after_a_use():
            nonlocal total
            value = total + obj.int_property
            total = cast(float, total)
            return value

        def cast_in_a_block():
            nonlocal total
            if obj.int_property > 0:
                total = cast(float, total)
            return total

        for func, message in (
            (cast_after_a_use, "must come before any other use of it"),
            (cast_in_a_block, "must be a statement of the function's body"),
        ):
            with self.assertRaisesRegex(
                FunctionCompilationError,
                r"cast of state variable 'total' %s.* \(line \d+\)$" % message,
            ):
                self.conn.compile(func)

    def test_variable_type_is_not_narrowed(self) -> None:
        def widened():
            value: float = 0
            value = 1
            return value + 0.5

        self.assertEqual(1.5, self.conn.run(widened))

        def annotated():
            count: float = 1
            count = count / 2
            return count

        self.assertEqual(0.5, self.conn.run(annotated))

        def truncated():
            count = 1
            count = 1.5
            return count

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(truncated)

        def divided():
            count = 1
            count = count / 2
            return count

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(divided)

    def test_variable_type_cannot_be_reannotated(self) -> None:
        def reannotated():
            count: int = 1  # pyright: ignore[reportRedeclaration]
            count: float = 2
            return count

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(reannotated)

    def test_client_side_call_runs_once(self) -> None:
        obj = self.conn.test_service.create_test_object("clientonce")
        obj.int_property = 2
        calls = []

        def offset() -> int:
            calls.append(1)
            return 5

        # The None returned first compiles the function again once the type of
        # the later return is known
        def func():
            if obj.int_property > 100:
                return None
            return obj.int_property + offset()

        self.assertEqual(7, self.conn.run(func))
        self.assertEqual(1, len(calls))

    def test_annotations_of_tuples_and_service_types(self) -> None:
        objs = self._make_objects("annotations")
        test_service = self.conn.test_service
        TestEnum = test_service.TestEnum
        TestStruct = test_service.TestStruct
        global SERVICE_CLASS  # noqa: PLW0603
        SERVICE_CLASS = test_service.TestClass

        def pairs() -> list[Any]:
            result: list[tuple[str, int]] = []
            for item in objs:
                result.append((item.get_value(), item.int_property))
            return result

        def typing_names() -> int:
            counts: dict[str, list[int]] = {}
            counts["a"] = [item.int_property for item in objs]
            return len(counts["a"])

        def enums() -> list[Any]:
            values: list[TestEnum] = []  # pyright: ignore[reportInvalidTypeForm]
            values.append(test_service.enum_echo(TestEnum.value_b))
            return values

        def structs() -> int:
            values: list[TestStruct] = []  # pyright: ignore[reportInvalidTypeForm]
            value = TestStruct(4, "s", TestEnum.value_a, [1])
            values.append(test_service.struct_echo(value))
            return values[0].int_field

        def classes() -> str:
            values: list[SERVICE_CLASS] = [objs[0]]  # pyright: ignore[reportInvalidTypeForm]
            return values[0].get_value()

        self.assertEqual(
            [
                ("value=annotations0", 1),
                ("value=annotations1", 2),
                ("value=annotations2", 3),
            ],
            [tuple(pair) for pair in self.conn.run(pairs)],
        )
        self.assertEqual(3, self.conn.run(typing_names))
        self.assertEqual([TestEnum.value_b], self.conn.run(enums))
        self.assertEqual(4, self.conn.run(structs))
        self.assertEqual("value=annotations0", self.conn.run(classes))

    def test_variable_keeps_its_element_type(self) -> None:
        def mismatched():
            values: list[float] = []
            values = [1, 2]
            return values

        with self.assertRaises(FunctionCompilationError):
            self.conn.compile(mismatched)

    def test_except_value_error_catches_every_argument_exception(self) -> None:
        service = self.conn.test_service

        def guarded():
            caught = 0
            try:
                service.throw_argument_exception()
            except ValueError:
                caught += 1
            try:
                service.throw_argument_null_exception("")
            except ValueError:
                caught += 1
            try:
                service.throw_argument_out_of_range_exception(0)
            except ValueError:
                caught += 1
            return caught

        self.assertEqual(3, self.conn.run(guarded))

    def test_except_runtime_error_catches_service_exceptions(self) -> None:
        service = self.conn.test_service

        def guarded():
            caught = 0
            try:
                service.throw_invalid_operation_exception()
            except RuntimeError:
                caught += 1
            try:
                service.throw_custom_exception()
            except RuntimeError:
                caught += 1
            try:
                try:
                    service.throw_argument_exception()
                except RuntimeError:
                    caught += 10
            except ValueError:
                caught += 1
            return caught

        self.assertEqual(3, self.conn.run(guarded))

    def test_except_object_not_loaded(self) -> None:
        not_loaded = self.conn.krpc.ObjectNotLoadedException

        def guarded():
            try:
                raise not_loaded("unloaded")
            except not_loaded:
                return False
            return True

        self.assertFalse(self.conn.run(guarded))

    def test_except_a_tuple_of_exceptions(self) -> None:
        service = self.conn.test_service

        def guarded():
            try:
                service.throw_invalid_operation_exception()
            except (ValueError, RuntimeError) as exn:
                return "caught " + exn  # pyright: ignore[reportOperatorIssue]
            return "not reached"

        self.assertEqual("caught Invalid operation", self.conn.run(guarded))

    def test_except_a_tuple_with_exception_catches_everything(self) -> None:
        service = self.conn.test_service

        def guarded():
            caught = 0
            for _ in range(2):
                try:
                    if caught == 0:
                        service.throw_custom_exception()
                    else:
                        raise Exception("plain")
                except (ValueError, Exception):
                    caught += 1
            try:
                raise KeyError("key")
            except Exception:
                caught += 1
            return caught

        self.assertEqual(3, self.conn.run(guarded))

    def test_except_runtime_error_does_not_catch_exception(self) -> None:
        def guarded():
            try:
                raise Exception("plain")
            except RuntimeError:
                return "caught"
            return "not reached"

        with self.assertRaisesRegex(Exception, "plain"):
            self.conn.run(guarded)

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
                lambda: (
                    self.conn.test_service.int64_to_string(5000000000)
                    + str(obj.int_property)[:0]
                )
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

    def test_floor_division_rounds_down(self) -> None:
        obj = self.conn.test_service.create_test_object("floor")

        def floor_divided(divisor):
            return self.evaluate(lambda: obj.int_property // divisor)

        for dividend, divisor in ((-7, 2), (7, -2), (-8, 2), (7, 2), (-7, -2)):
            obj.int_property = dividend
            self.assertEqual(dividend // divisor, floor_divided(divisor))

    def test_float_floor_division(self) -> None:
        obj = self.conn.test_service.create_test_object("floatfloor")

        def floor_divided(scale, divisor):
            return self.evaluate(lambda: (obj.int_property * scale) // divisor)

        obj.int_property = 1
        for scale, divisor in (
            (1.0, 0.1),
            (-1.0, 0.1),
            (7.5, -2.0),
            (-7.5, -2.0),
            (0.5, 2.0),
        ):
            self.assertEqual(scale // divisor, floor_divided(scale, divisor))

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

    def test_variable_of_a_single_precision_value_is_a_double(self) -> None:
        floats = self.conn.test_service.float_special_defaults
        value = floats()[5]

        def added() -> float:
            x = floats()[5]
            x = x + 0.5
            x += 1
            return x

        def loop() -> float:
            total = 0.0
            for x in floats():
                if 0 < x < 1:
                    x += 1
                    total += x
            return total

        def unpacked() -> float:
            first, second = floats()[5], 1
            first += second
            return first

        self.assertEqual(value + 0.5 + 1, self.conn.run(added))
        self.assertEqual(value + 1, self.conn.run(loop))
        self.assertEqual(value + 1, self.conn.run(unpacked))

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
            self.conn.compile(lambda: (obj.int_property, "a")[2])  # noqa: PLE0643  # pyright: ignore[reportGeneralTypeIssues]

    def test_string_slice_is_clamped(self) -> None:
        obj = self.conn.test_service.create_test_object("clamp")
        self.assertEqual("value=clamp", self.evaluate(lambda: obj.get_value()[:100]))
        self.assertEqual("", self.evaluate(lambda: obj.get_value()[50:]))
        self.assertEqual("", self.evaluate(lambda: obj.get_value()[3:1]))
        self.assertEqual("ue", self.evaluate(lambda: obj.get_value()[3:5]))

    def test_final_if_else_returns(self) -> None:
        obj = self.conn.test_service.create_test_object("sign")

        def sign():
            if obj.int_property < 0:
                return -1
            elif obj.int_property == 0:
                return 0
            else:
                return 1

        for value, expected in ((-5, -1), (0, 0), (5, 1)):
            obj.int_property = value
            self.assertEqual(expected, self.conn.run(sign))

    def test_return_none(self) -> None:
        obj = self.conn.test_service.create_test_object("none")

        def set_property():
            obj.int_property = 5

        self.assertIsNone(self.conn.run(set_property))
        self.assertEqual(5, obj.int_property)

    def test_string_method_argument_is_compiled_once(self) -> None:
        obj = self.conn.test_service.create_test_object("a,b")
        calls = []

        def separator():
            calls.append(1)
            return ","

        self.assertEqual(
            ["value=a", "b"], self.evaluate(lambda: obj.get_value().split(separator()))
        )
        self.assertEqual(["x", "y"], self.conn.run(lambda: "x,y".split(separator())))
        self.assertEqual([1, 1], calls)

    def test_missing_key_is_a_key_error(self) -> None:
        obj = self.conn.test_service.create_test_object("missing")

        def lookup():
            counts: dict[str, int] = {}
            counts["a"] = obj.int_property
            result = 0
            try:
                result = counts["z"]
            except ValueError:
                result = -1
            except KeyError:
                result = -2
            return result

        self.assertEqual(-2, self.conn.run(lookup))
        with self.assertRaisesRegex(KeyError, "not in the dictionary"):
            self.evaluate(lambda: {"a": obj.int_property}["z"])


if __name__ == "__main__":
    unittest.main()
