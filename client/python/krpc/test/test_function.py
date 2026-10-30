import unittest
from typing import Any
from unittest import mock

import krpc.functioncompiler
from krpc.error import StreamError
from krpc.test.servertestcase import ServerTestCase


class TestFunction(ServerTestCase, unittest.TestCase):
    def test_function_stream(self) -> None:
        expression = self.conn.krpc.Expression
        counter = expression.call(
            self.conn.get_call(self.conn.test_service.counter, "TestFunction.stream")
        )
        expr = expression.multiply(counter, expression.constant_int(2))
        with self.conn.stream(expr) as stream:
            value = stream()
            self.assertEqual(0, value % 2)
            with stream.condition:
                stream.wait()
            self.assertGreater(stream(), value)
            self.assertEqual(0, stream() % 2)

    def test_function_stream_mixed_type_arithmetic(self) -> None:
        expression = self.conn.krpc.Expression
        counter = expression.call(
            self.conn.get_call(self.conn.test_service.counter, "TestFunction.mixed")
        )
        # int RPC result * double constant promotes to double
        expr = expression.multiply(counter, expression.constant_double(0.5))
        with self.conn.stream(expr) as stream:
            value = stream()
            self.assertIsInstance(value, float)

    def test_function_stream_string(self) -> None:
        expression = self.conn.krpc.Expression
        self.conn.test_service.string_property = "foo"
        expr = expression.call(
            self.conn.get_call(getattr, self.conn.test_service, "string_property")
        )
        with self.conn.stream(expr) as stream:
            self.assertEqual("foo", stream())

    def test_function_stream_object(self) -> None:
        expression = self.conn.krpc.Expression
        obj = self.conn.test_service.create_test_object("streamed")
        expr = expression.constant_object(krpc.get_object_id(obj))
        with self.conn.stream(expr) as stream:
            value = stream()
            self.assertEqual(obj, value)
            self.assertEqual("value=streamed", value.get_value())

    def test_function_stream_collection(self) -> None:
        expression = self.conn.krpc.Expression
        types = self.conn.krpc.Type
        objs = [self.conn.test_service.create_test_object(str(i)) for i in range(1, 4)]
        for i, obj in enumerate(objs):
            obj.int_property = i + 1
        objects = expression.create_list(
            [expression.constant_object(krpc.get_object_id(x)) for x in objs]
        )
        param = expression.parameter("x", types.class_type("TestService", "TestClass"))
        get_property = expression.call_with_arguments(
            self.conn.get_call(getattr, objs[0], "int_property"), {0: param}
        )
        selected = expression.select(objects, expression.lambda_([param], get_property))
        with self.conn.stream(expression.to_list(selected)) as stream:
            self.assertEqual([1, 2, 3], stream())

    def test_function_stream_error(self) -> None:
        expression = self.conn.krpc.Expression
        expr = expression.call(
            self.conn.get_call(self.conn.test_service.throw_argument_exception)
        )
        # A stream's error is raised as the exception type the error maps to
        with self.conn.stream(expr) as stream:
            with self.assertRaises(ValueError) as context:
                stream()
            self.assertIn("Invalid argument", str(context.exception))
        obj = self.conn.test_service.create_test_object("streamerror")
        with self.conn.stream(lambda: {"a": obj.int_property}["b"]) as missing:
            with self.assertRaises(KeyError):
                missing()

    def test_function_of_nullable_elements(self) -> None:
        service = self.conn.test_service
        expr = self.conn.krpc.Expression.call(
            self.conn.get_call(service.echo_list_of_nullable_ints, [5, None])
        )
        self.assertEqual([5, None], self.conn.run(expr))
        with self.conn.stream(expr) as stream:
            self.assertEqual([5, None], stream())
        return_type = expr.return_type
        self.assertTrue(return_type.types[0].nullable)
        self.assertFalse(return_type.nullable)

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

    def test_function_stream_lazy_collection_rejected(self) -> None:
        expression = self.conn.krpc.Expression
        types = self.conn.krpc.Type
        values = expression.create_list([expression.constant_int(x) for x in range(3)])
        param = expression.parameter("x", types.int())
        doubled = expression.select(
            values,
            expression.lambda_(
                [param], expression.multiply(param, expression.constant_int(2))
            ),
        )
        with self.assertRaises(RuntimeError):
            self.conn.add_stream(doubled)

    def test_per_element_call_event(self) -> None:
        expression = self.conn.krpc.Expression
        types = self.conn.krpc.Type
        objs = [
            self.conn.test_service.create_test_object("event%d" % i) for i in range(3)
        ]
        for i, obj in enumerate(objs):
            obj.int_property = i
        objects = expression.create_list(
            [expression.constant_object(krpc.get_object_id(x)) for x in objs]
        )
        param = expression.parameter("x", types.class_type("TestService", "TestClass"))
        get_property = expression.call_with_arguments(
            self.conn.get_call(getattr, objs[0], "int_property"), {0: param}
        )
        predicate = expression.lambda_(
            [param], expression.equal(get_property, expression.constant_int(2))
        )
        event = self.conn.add_event(expression.any(objects, predicate))
        with event.condition:
            event.wait(2)
            self.assertTrue(event.stream())

    def test_events_over_one_function(self) -> None:
        obj = self.conn.test_service.create_test_object("sharedevent")
        obj.int_property = 0
        function = self.conn.compile(lambda: obj.int_property > 0)
        first = self.conn.add_event(function)
        second = self.conn.add_event(function)
        # Each add creates its own event, so removing one leaves the other
        self.assertNotEqual(
            first.stream._stream.stream_id, second.stream._stream.stream_id
        )
        first.remove()
        obj.int_property = 1
        with second.condition:
            self.assertTrue(second.wait(10))

    def test_function_streams_over_one_function(self) -> None:
        expression = self.conn.krpc.Expression
        counter = expression.call(
            self.conn.get_call(self.conn.test_service.counter, "TestFunction.shared")
        )
        first = self.conn.add_stream(counter)
        second = self.conn.add_stream(counter)
        # Each add creates its own stream, so removing one leaves the other
        self.assertIsNot(first._stream, second._stream)
        self.assertNotEqual(first._stream.stream_id, second._stream.stream_id)
        first.remove()
        with self.assertRaises(StreamError):
            first()
        value = second()
        with second.condition:
            self.assertTrue(second.wait(10))
        self.assertGreater(second(), value)
        second.remove()

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

    def test_function_stream_struct(self) -> None:
        expression = self.conn.krpc.Expression
        expr = expression.call(
            self.conn.get_call(
                self.conn.test_service.counter_struct, "TestFunction.struct"
            )
        )
        with self.conn.stream(expr) as stream:
            value = stream()
            self.assertIsInstance(value, self.conn.test_service.TestStruct)
            self.assertEqual("TestFunction.struct", value.string_field)
            with stream.condition:
                stream.wait()
            self.assertGreater(stream().int_field, value.int_field)

    def test_expression_struct_field(self) -> None:
        expression = self.conn.krpc.Expression
        struct = expression.call(
            self.conn.get_call(
                self.conn.test_service.counter_struct, "TestFunction.field"
            )
        )
        expr = expression.get_field(struct, "StringField")
        with self.conn.stream(expr) as stream:
            self.assertEqual("TestFunction.field", stream())

    def test_expression_create_struct(self) -> None:
        expression = self.conn.krpc.Expression
        types = self.conn.krpc.Type
        test_enum = self.conn.test_service.TestEnum
        struct = expression.create_struct(
            types.struct_type("TestService", "TestStruct"),
            [
                expression.constant_int(3),
                expression.constant_string("built"),
                expression.cast(
                    expression.constant_int(test_enum.value_c.value),
                    types.enumeration_type("TestService", "TestEnum"),
                ),
                expression.create_list([expression.constant_int(7)]),
            ],
        )
        expr = expression.call_with_arguments(
            self.conn.get_call(
                self.conn.test_service.struct_echo,
                self.conn.test_service.TestStruct(0, "", test_enum.value_a, []),
            ),
            {0: struct},
        )
        with self.conn.stream(expr) as stream:
            value = stream()
            self.assertEqual(3, value.int_field)
            self.assertEqual("built", value.string_field)
            self.assertEqual(test_enum.value_c, value.enum_field)
            self.assertEqual([7], value.list_field)

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

    def test_return_type_introspection(self) -> None:
        expression = self.conn.krpc.Expression
        type_code = self.conn.krpc.TypeCode
        expr = expression.constant_double(1.5)
        return_type = expr.return_type
        self.assertEqual(type_code.double, return_type.code)
        obj = self.conn.test_service.create_test_object("introspect")
        obj_type = expression.constant_object(krpc.get_object_id(obj)).return_type
        self.assertEqual(type_code.class_, obj_type.code)
        self.assertEqual("TestService", obj_type.service)
        self.assertEqual("TestClass", obj_type.name)

    def test_compiled_function_type_is_not_introspected(self) -> None:
        obj = self.conn.test_service.create_test_object("compiledtype")
        obj.int_property = 3
        function = self.conn.compile(lambda: obj.int_property * 2)
        with mock.patch(
            "krpc.client._type_message", side_effect=AssertionError("introspected")
        ):
            self.assertEqual(6, self.conn.run(function))

    def test_function_nullable_value(self) -> None:
        expression = self.conn.krpc.Expression
        expr = expression.call(
            self.conn.get_call(self.conn.test_service.echo_nullable_int, 3)
        )
        self.assertEqual(3, self.conn.run(expr))
        null = expression.call(
            self.conn.get_call(self.conn.test_service.echo_nullable_int, None)
        )
        self.assertIsNone(self.conn.run(null))

    def test_run_function_with_arguments(self) -> None:
        expression = self.conn.krpc.Expression
        types = self.conn.krpc.Type
        value = expression.parameter("x", types.double())
        factor = expression.parameter("n", types.int())
        function = expression.lambda_(
            [value, factor], expression.multiply(value, factor)
        )
        self.assertEqual(7.5, self.conn.run(function, 2.5, 3))
        self.assertEqual(-4.0, self.conn.run(function, 1.0, -4))
        with self.assertRaises(TypeError) as cm:
            self.conn.run(function, 1.0)
        self.assertIn("takes 2 arguments, got 1", str(cm.exception))
        with self.assertRaises(TypeError) as cm:
            self.conn.run(function)
        self.assertIn("takes 2 arguments, got 0", str(cm.exception))

    def test_run_function_with_object_and_collection_arguments(self) -> None:
        expression = self.conn.krpc.Expression
        types = self.conn.krpc.Type
        obj = self.conn.test_service.create_test_object("argument")
        obj.int_property = 4
        param = expression.parameter(
            "obj", types.class_type("TestService", "TestClass")
        )
        values = expression.parameter("values", types.list_type(types.int()))
        get_property = expression.call_with_arguments(
            self.conn.get_call(getattr, obj, "int_property"), {0: param}
        )
        function = expression.lambda_(
            [param, values], expression.add(get_property, expression.count(values))
        )
        self.assertEqual(7, self.conn.run(function, obj, [1, 2, 3]))
        self.assertEqual(4, self.conn.run(function, obj, []))

    def test_run_function_with_a_null_argument(self) -> None:
        expression = self.conn.krpc.Expression
        types = self.conn.krpc.Type
        nullable = expression.parameter("s", types.nullable_type(types.string()))
        function = expression.lambda_([nullable], expression.is_null(nullable))
        self.assertTrue(self.conn.run(function, None))
        self.assertFalse(self.conn.run(function, "foo"))
        string = expression.parameter("t", types.string())
        not_nullable = expression.lambda_([string], string)
        with self.assertRaises(ValueError) as cm:
            self.conn.run(not_nullable, None)
        self.assertIn("parameter t is null", str(cm.exception))

    def test_run_function_with_an_argument_its_parameter_cannot_take(self) -> None:
        expression = self.conn.krpc.Expression
        types = self.conn.krpc.Type
        number = expression.parameter("n", types.int())
        values = expression.parameter("values", types.list_type(types.int()))
        function = expression.lambda_(
            [number, values], expression.add(number, expression.count(values))
        )
        self.assertEqual(4, self.conn.run(function, 2.0, (1, 2)))
        for arguments, position in (
            ((2**31, []), 1),
            (("a", []), 1),
            ((1, [1, None]), 2),
            ((1, [1, 2**31]), 2),
            ((1, ["a"]), 2),
        ):
            with self.assertRaisesRegex(
                TypeError, "run\\(\\) argument %d must be a" % position
            ):
                self.conn.run(function, *arguments)

    def test_run_or_stream_a_value_that_is_not_a_function(self) -> None:
        obj = self.conn.test_service.create_test_object("notafunction")
        for value in (1.5, obj):
            for add in (self.conn.run, self.conn.add_stream, self.conn.add_event):
                with self.assertRaisesRegex(
                    TypeError, "expected .*a KRPC.Expression object"
                ):
                    add(value)

    def test_function_stream_of_a_function_with_parameters_rejected(self) -> None:
        expression = self.conn.krpc.Expression
        types = self.conn.krpc.Type
        flag = expression.parameter("b", types.bool())
        function = expression.lambda_([flag], flag)
        with self.assertRaises(ValueError) as cm:
            self.conn.add_stream(function)
        self.assertIn("cannot take parameters", str(cm.exception))
        with self.assertRaises(ValueError) as cm:
            self.conn.add_event(function)
        self.assertIn("cannot take parameters", str(cm.exception))

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

    def test_plain_stream_of_a_function_procedure_rejected(self) -> None:
        expression = self.conn.krpc.Expression
        expr = expression.constant_int(1)
        for procedure in (
            self.conn.krpc.run_function,
            self.conn.krpc.add_function_stream,
            self.conn.krpc.add_event,
        ):
            with self.assertRaises(RuntimeError) as cm:
                self.conn.add_stream(procedure, expr)
            self.assertIn("cannot be streamed", str(cm.exception))

    def test_stream_of_a_function_with_arguments_rejected(self) -> None:
        expr = self.conn.krpc.Expression.constant_int(1)
        for function in (expr, lambda: 1):
            with self.assertRaisesRegex(StreamError, "without arguments"):
                self.conn.add_stream(function, 1)
            with self.assertRaisesRegex(StreamError, "without arguments"):
                self.conn.add_stream(function, value=1)


if __name__ == "__main__":
    unittest.main()
