import math
import unittest

from krpc import defer
from krpc.error import FunctionCompilationError
from krpc.test.servertestcase import ServerTestCase


class TestDeferredCall(ServerTestCase, unittest.TestCase):
    """Compilation of krpc.defer, which starts a call to a procedure that
    pauses execution without waiting for it."""

    @classmethod
    def setUpClass(cls) -> None:
        super(TestDeferredCall, cls).setUpClass()

    def test_deferred_call(self):
        def go():
            defer(self.conn.test_service.blocking_procedure(3))
            self.conn.test_service.string_property = "carried on"

        self.conn.run_function(go)
        self.assertEqual("carried on", self.conn.test_service.string_property)

    def test_a_procedure_that_pauses_needs_defer(self):
        def go():
            self.conn.test_service.blocking_procedure(3)
            self.conn.test_service.string_property = "carried on"

        # The server reports the pause as KRPC.InvalidOperationException,
        # which the client raises as a RuntimeError
        with self.assertRaises(RuntimeError) as caught:
            self.conn.run_function(go)
        self.assertIn("paused execution", str(caught.exception))

    def test_deferred_call_with_computed_arguments(self):
        obj = self.conn.test_service.create_test_object("deferred")
        obj.int_property = 3

        def go():
            defer(self.conn.test_service.blocking_procedure(obj.int_property))

        self.assertIsNone(self.conn.run_function(go))

    def test_deferred_call_on_a_remote_object(self):
        obj = self.conn.test_service.create_test_object("deferredobject")

        def go():
            defer(obj.object_to_string(obj))

        self.assertIsNone(self.conn.run_function(go))

    def test_deferred_call_within_a_loop_and_a_conditional(self):
        def go():
            for i in [1, 2]:
                if i > 1:
                    defer(self.conn.test_service.blocking_procedure(i))

        self.assertIsNone(self.conn.run_function(go))

    def test_deferred_call_as_a_lambda_body(self):
        self.assertIsNone(
            self.conn.run_function(
                lambda: defer(self.conn.test_service.blocking_procedure(2))
            )
        )

    def test_defer_in_value_position(self):
        def assigned():
            value = defer(self.conn.test_service.blocking_procedure(1))
            return value

        def nested():
            return len(defer(self.conn.test_service.blocking_procedure(1)))

        for func in (assigned, nested):
            with self.assertRaises(FunctionCompilationError) as caught:
                self.conn.compile_function(func)
            self.assertIn("only be used as a statement", str(caught.exception))

    def test_defer_takes_a_single_call(self):
        def none():
            defer()  # pylint: disable=no-value-for-parameter

        def two():
            defer(  # pylint: disable=too-many-function-args
                self.conn.test_service.blocking_procedure(1),
                self.conn.test_service.blocking_procedure(2),
            )

        def keyword():
            defer(call=self.conn.test_service.blocking_procedure(1))

        for func in (none, two, keyword):
            with self.assertRaises(FunctionCompilationError) as caught:
                self.conn.compile_function(func)
            self.assertIn("a single remote call", str(caught.exception))

    def test_defer_of_something_other_than_a_remote_call(self):
        obj = self.conn.test_service.create_test_object("deferredother")

        def arithmetic():
            defer(1 + 2)

        def a_property():
            defer(obj.int_property)

        def a_builtin():
            defer(len([1, 2]))

        def a_math_function():
            defer(math.sqrt(obj.int_property))

        for func in (arithmetic, a_property, a_builtin, a_math_function):
            with self.assertRaises(FunctionCompilationError) as caught:
                self.conn.compile_function(func)
            self.assertIn("a call to a remote procedure", str(caught.exception))

    def test_defer_called_directly(self):
        with self.assertRaises(FunctionCompilationError):
            defer(None)


if __name__ == "__main__":
    unittest.main()
