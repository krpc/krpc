import unittest
from typing import Any
from unittest import mock

import krpc.functioncompiler
from krpc.error import StreamError
from krpc.test.servertestcase import ServerTestCase


class TestFunction(ServerTestCase, unittest.TestCase):
    def test_function_stream_error(self) -> None:
        obj = self.conn.test_service.create_test_object("streamerror")
        with self.conn.stream(lambda: {"a": obj.int_property}["b"]) as missing:
            with self.assertRaises(KeyError):
                missing()

    def test_nullable_reference_call_in_a_collection(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("nullableelement")
        # A nullable string or object element is sent with a presence flag, and the
        # values of a set and the keys of a dictionary are sent without one
        self.assertEqual(
            ["hello"],
            self.conn.run(lambda: [service.echo_nullable_string("hello")]),
        )
        self.assertEqual(
            {"hello"},
            self.conn.run(lambda: {service.echo_nullable_string("hello")}),
        )
        self.assertEqual(
            [obj, obj],
            self.conn.run(lambda: [obj, service.echo_test_object(obj)]),
        )
        self.assertEqual(
            {1: obj, 2: obj},
            self.conn.run(lambda: {1: obj, 2: service.echo_test_object(obj)}),
        )
        with self.conn.stream(
            lambda: {service.echo_nullable_string("key"): service.echo_test_object(obj)}
        ) as stream:
            self.assertEqual({"key": obj}, stream())

    def test_nullable_reference_elements(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("nullableelements")
        null = service.echo_test_object
        self.assertEqual(
            [obj, None],
            self.conn.run(
                lambda: service.echo_list_of_nullable_objects([obj, null(None)])
            ),
        )
        self.assertEqual(
            {"a": obj, "b": None},
            self.conn.run(
                lambda: service.echo_dictionary_of_nullable_objects(
                    {"a": obj, "b": null(None)}
                )
            ),
        )
        self.assertEqual(
            (1, None),
            self.conn.run(
                lambda: service.echo_tuple_with_a_nullable_object((1, null(None)))
            ),
        )
        self.assertEqual(
            ["a", None],
            self.conn.run(lambda: ["a", service.echo_nullable_string(None)]),
        )
        with self.conn.stream(
            lambda: [
                (x, x is None)
                for x in service.echo_list_of_nullable_objects([null(None), obj])
            ]
        ) as stream:
            self.assertEqual([(None, True), (obj, False)], stream())

    def test_null_in_a_set(self) -> None:
        service = self.conn.test_service
        # The values of a set cannot be null, which the server checks
        with self.assertRaises(TypeError):
            self.conn.run(lambda: {"a", service.echo_nullable_string(None)})

    def test_type_the_compiler_does_not_know(self) -> None:
        obj = self.conn.test_service.create_test_object("untracked")
        obj.int_property = 4
        compile_with_type = krpc.functioncompiler.compile_function_with_type

        def without_type(*args: Any) -> Any:
            function, _, parameters = compile_with_type(*args)
            return function, None, parameters

        # The helpers ask the server for the type of a value the compiler did not
        # track, and treat the function as valueless only when it has no value
        with mock.patch("krpc.client.compile_function_with_type", without_type):
            self.assertEqual([4], self.conn.run(lambda: [obj.int_property]))
            with self.conn.stream(lambda: [obj.int_property]) as stream:
                self.assertEqual([4], stream())

    def test_valueless_function(self) -> None:
        expression = self.conn.krpc.Expression
        # A void procedure. The tests that use the count it resets reset it first
        call = expression.call(
            self.conn.get_call(self.conn.test_service.reset_custom_exception_later)
        )
        self.assertIsNone(self.conn.run(call))
        with self.assertRaises(StreamError):
            self.conn.add_stream(call)

    def test_compiled_valueless_function_caches_no_type(self) -> None:
        service = self.conn.test_service

        def reset() -> None:
            service.reset_custom_exception_later()

        cached = len(self.conn._expression_return_types)
        for _ in range(3):
            self.assertIsNone(self.conn.run(reset))
        self.assertEqual(cached, len(self.conn._expression_return_types))

    def test_boolean_operators_short_circuit(self) -> None:
        service = self.conn.test_service
        counter = service.counter
        obj = service.create_test_object("shortcircuit")
        obj.int_property = 0
        # The left operand is computed on the server, which skips the right operand,
        # so the client's own call is the first one
        self.assertFalse(
            self.conn.run(
                lambda: obj.int_property > 0 and counter("TestFunction.and") > 0
            )
        )
        self.assertEqual(1, counter("TestFunction.and"))
        self.assertTrue(
            self.conn.run(
                lambda: obj.int_property == 0 or counter("TestFunction.or") > 0
            )
        )
        self.assertEqual(1, counter("TestFunction.or"))

    def test_folded_boolean_operator_skips_the_right_operand(self) -> None:
        counter = self.conn.test_service.counter
        # The constant left operand decides the result when compiling
        self.assertFalse(
            self.conn.run(lambda: False and counter("TestFunction.folded") > 0)
        )
        self.assertEqual(1, counter("TestFunction.folded"))

    def test_chained_comparison_reads_the_middle_operand_once(self) -> None:
        counter = self.conn.test_service.counter
        # The counter gives 1 on its first call and 2 on its second, so reading it
        # a second time would make the right hand comparison fail
        self.assertTrue(self.conn.run(lambda: 0 < counter("TestFunction.chained") < 2))

    def test_compiled_function_type_is_not_introspected(self) -> None:
        obj = self.conn.test_service.create_test_object("compiledtype")
        obj.int_property = 3
        function = self.conn.compile(lambda: obj.int_property * 2)
        with mock.patch(
            "krpc.client._type_message", side_effect=AssertionError("introspected")
        ):
            self.assertEqual(6, self.conn.run(function))

    def test_run_function_coerces_its_arguments(self) -> None:
        expression = self.conn.krpc.Expression
        types = self.conn.krpc.Type
        number = expression.parameter("n", types.int())
        values = expression.parameter("values", types.list_type(types.int()))
        function = expression.lambda_(
            [number, values], expression.add(number, expression.count(values))
        )
        # A float with an integer value to an int, and a tuple to a list
        self.assertEqual(4, self.conn.run(function, 2.0, (1, 2)))

    def test_run_or_stream_a_value_that_is_not_a_function(self) -> None:
        obj = self.conn.test_service.create_test_object("notafunction")
        for value in (1.5, obj):
            for add in (self.conn.run, self.conn.add_stream, self.conn.add_event):
                with self.assertRaisesRegex(
                    TypeError, "expected .*a KRPC.Expression object"
                ):
                    add(value)

    def test_stream_of_a_compiled_function_with_parameters_rejected(self) -> None:
        obj = self.conn.test_service.create_test_object("compiledparameters")

        def above(x: int) -> bool:
            return obj.int_property > x

        function = self.conn.compile(above)
        # Rejected before the return type is introspected
        self.conn._expression_return_types.pop(function._object_id, None)
        with mock.patch(
            "krpc.client._type_message", side_effect=AssertionError("introspected")
        ):
            for add in (self.conn.add_stream, self.conn.add_event):
                with self.assertRaisesRegex(StreamError, "takes parameters"):
                    add(function)

    def test_stream_of_a_function_with_arguments_rejected(self) -> None:
        expr = self.conn.krpc.Expression.constant_int(1)
        for function in (expr, lambda: 1):
            with self.assertRaisesRegex(StreamError, "without arguments"):
                self.conn.add_stream(function, 1)
            with self.assertRaisesRegex(StreamError, "without arguments"):
                self.conn.add_stream(function, value=1)


if __name__ == "__main__":
    unittest.main()
