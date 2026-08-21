import unittest

from krpc.test.servertestcase import ServerTestCase


class TestExpression(ServerTestCase, unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        super(TestExpression, cls).setUpClass()

    def test_expression_stream(self) -> None:
        expression = self.conn.krpc.Expression
        counter = expression.call(
            self.conn.get_call(self.conn.test_service.counter, "TestExpression.stream")
        )
        expr = expression.multiply(counter, expression.constant_int(2))
        with self.conn.expression_stream(expr) as stream:
            value = stream()
            self.assertEqual(0, value % 2)
            with stream.condition:
                stream.wait()
            self.assertGreater(stream(), value)
            self.assertEqual(0, stream() % 2)

    def test_expression_stream_mixed_type_arithmetic(self) -> None:
        expression = self.conn.krpc.Expression
        counter = expression.call(
            self.conn.get_call(self.conn.test_service.counter, "TestExpression.mixed")
        )
        # int RPC result * double constant promotes to double
        expr = expression.multiply(counter, expression.constant_double(0.5))
        with self.conn.expression_stream(expr) as stream:
            value = stream()
            self.assertIsInstance(value, float)

    def test_expression_stream_string(self) -> None:
        expression = self.conn.krpc.Expression
        self.conn.test_service.string_property = "foo"
        expr = expression.call(
            self.conn.get_call(getattr, self.conn.test_service, "string_property")
        )
        with self.conn.expression_stream(expr) as stream:
            self.assertEqual("foo", stream())

    def test_expression_stream_object(self) -> None:
        expression = self.conn.krpc.Expression
        obj = self.conn.test_service.create_test_object("streamed")
        expr = expression.constant_object(obj._object_id)
        with self.conn.expression_stream(expr) as stream:
            value = stream()
            self.assertEqual(obj, value)
            self.assertEqual("value=streamed", value.get_value())

    def test_expression_stream_collection(self) -> None:
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
        selected = expression.select(
            objects, expression.function([param], get_property)
        )
        with self.conn.expression_stream(expression.to_list(selected)) as stream:
            self.assertEqual([1, 2, 3], stream())

    def test_expression_stream_error(self) -> None:
        expression = self.conn.krpc.Expression
        expr = expression.call(
            self.conn.get_call(self.conn.test_service.throw_argument_exception)
        )
        with self.conn.expression_stream(expr) as stream:
            with self.assertRaises(Exception) as context:
                stream()
            self.assertIn("Invalid argument", str(context.exception))

    def test_expression_stream_lazy_collection_rejected(self) -> None:
        expression = self.conn.krpc.Expression
        types = self.conn.krpc.Type
        values = expression.create_list([expression.constant_int(x) for x in range(3)])
        param = expression.parameter("x", types.int())
        doubled = expression.select(
            values,
            expression.function(
                [param], expression.multiply(param, expression.constant_int(2))
            ),
        )
        with self.assertRaises(RuntimeError):
            self.conn.add_expression_stream(doubled)

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
        predicate = expression.function(
            [param], expression.equal(get_property, expression.constant_int(2))
        )
        event = self.conn.krpc.add_event(expression.any(objects, predicate))
        with event.condition:
            event.wait(2)
            self.assertTrue(event.stream())

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


if __name__ == "__main__":
    unittest.main()
