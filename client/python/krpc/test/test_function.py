import unittest

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
        expr = expression.constant_object(obj._object_id)
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
            [expression.constant_object(x._object_id) for x in objs]
        )
        param = expression.parameter("x", types.class_type("TestService", "TestClass"))
        get_property = expression.call_with_arguments(
            self.conn.get_call(getattr, objs[0], "int_property"), {0: param}
        )
        selected = expression.select(objects, expression.lambda_([param], get_property))
        with self.conn.stream(expression.to_list(selected)) as stream:
            self.assertEqual([1, 2, 3], stream())

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
            [expression.constant_object(x._object_id) for x in objs]
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

    def test_function_streams_over_one_function(self) -> None:
        expression = self.conn.krpc.Expression
        counter = expression.call(
            self.conn.get_call(self.conn.test_service.counter, "TestFunction.shared")
        )
        first = self.conn.add_stream(counter)
        second = self.conn.add_stream(counter)
        # Each add creates its own stream, so removing one leaves the other
        # pylint: disable=protected-access
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

    def test_return_type_introspection(self) -> None:
        expression = self.conn.krpc.Expression
        type_code = self.conn.krpc.TypeCode
        expr = expression.constant_double(1.5)
        return_type = expr.return_type
        self.assertEqual(type_code.double, return_type.code)
        obj = self.conn.test_service.create_test_object("introspect")
        obj_type = expression.constant_object(obj._object_id).return_type
        self.assertEqual(type_code.class_, obj_type.code)
        self.assertEqual("TestService", obj_type.service)
        self.assertEqual("TestClass", obj_type.name)

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


if __name__ == "__main__":
    unittest.main()
