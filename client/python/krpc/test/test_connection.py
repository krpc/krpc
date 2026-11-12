import inspect
import unittest
from typing import Any

import krpc
import krpc.schema.KRPC_pb2 as KRPC
from krpc.declarations import register
from krpc.decoder import Decoder
from krpc.encoder import Encoder
from krpc.event import Event
from krpc.services import testservice
from krpc.test.servertestcase import ServerTestCase
from krpc.types import RemoteEnum


class TestConnection(ServerTestCase, unittest.TestCase):
    def test_extension_members_are_per_connection(self) -> None:
        # Each connection builds its own classes, so a member a mod adds goes through the
        # connection the object was reached through
        other = self.connect()
        try:
            obj = other.test_service.create_test_object("alpha")
            self.assertEqual("value=alpha", obj.extension_property)  # pyright: ignore[reportAttributeAccessIssue]
            obj2 = obj.extension_method_returning_class_from_other_service()  # pyright: ignore[reportAttributeAccessIssue]
            self.assertIs(other, obj2._connection)
            self.assertIn("extension_method", dir(other.test_service.TestClass))
        finally:
            other.close()

    def test_nullable_class_type_shares_one_python_type(self) -> None:
        # A nullable class-typed value is the class of the non-nullable one, and not a second
        # class minted for the nullable declaration
        obj = self.conn.test_service.create_test_object("alpha")
        self.assertIs(type(obj.echo_nullable_object(obj)), type(obj))

    def test_matching_definitions_use_the_generated_classes(self) -> None:
        # The server's definitions match the generated module, so the client uses its classes
        service = self.conn.test_service
        self.assertIs(testservice.TestEnum, service.TestEnum)
        self.assertIs(testservice.TestStruct, service.TestStruct)
        self.assertIs(testservice.CustomException, service.CustomException)
        self.assertIs(
            testservice.TestEnum.value_b,
            service.enum_echo(testservice.TestEnum.value_b),
        )
        obj = service.create_test_object("alpha")
        self.assertIsInstance(obj, testservice.TestClass)
        self.assertIsInstance(service, testservice.TestService)
        with self.assertRaises(testservice.CustomException):
            service.throw_custom_exception()

    def test_connect_with_the_connection_type_of_a_service(self) -> None:
        conn = krpc.connect[testservice.TestServiceConnection](
            name="python_client_test", address="localhost", port=self.tcpip_port()
        )
        try:
            self.assertIsInstance(conn, testservice.TestServiceConnection)
            self.assertEqual("42", conn.test_service.int32_to_string(42))
        finally:
            conn.close()

    def test_connect_functions_have_their_signatures(self) -> None:
        for connect in (krpc.connect, krpc.connect[krpc.Connection]):
            parameters = list(inspect.signature(connect).parameters)
            self.assertEqual(["name", "address", "port"], parameters[:3])
            self.assertEqual("connect", connect.__name__)
        self.assertEqual(
            ["name", "path"], list(inspect.signature(krpc.connect_local).parameters)[:2]
        )

    def test_a_number_the_server_lacks_names_the_enumeration(self) -> None:
        class TestEnum(RemoteEnum):
            value_a = 0
            value_z = 25

        # A module whose enumeration differs from the server's, imported last
        register("TestService", TestEnum)
        try:
            conn = krpc.connect(
                name="python_client_test", address="localhost", port=self.tcpip_port()
            )
        finally:
            register("TestService", testservice.TestEnum)
        try:
            # The module's enumeration differs from the one the service declares
            service: Any = conn.test_service
            self.assertEqual(
                service.TestEnum.value_a, service.enum_echo(TestEnum.value_a)
            )
            with self.assertRaises(TypeError) as cm:
                service.enum_echo(TestEnum.value_z)
            self.assertIn(
                "TestService.EnumEcho() argument 0: TestEnum.value_z is numbered 25, which "
                "the server's definition of TestService.TestEnum does not have",
                str(cm.exception),
            )
        finally:
            conn.close()

    def test_class_static_methods_via_instance(self) -> None:
        obj = self.conn.test_service.create_test_object("alpha")
        self.assertEqual("static", obj.static_method())
        self.assertEqual("staticbetagamma", obj.static_method("beta", "gamma"))

    def test_events_inside_a_value(self) -> None:
        # An event at a position inside a value is an Event, as a returned one is
        types = self.conn._types
        uint64 = types.uint64_type
        items = [Encoder.encode(stream_id, uint64) for stream_id in (17, 18)]
        data = KRPC.List(items=items).SerializeToString()
        value = Decoder.decode(self.conn, data, types.list_type(types.event_type))
        assert isinstance(value, list)
        self.assertEqual(2, len(value))
        for event, stream_id in zip(value, (17, 18)):
            self.assertIsInstance(event, Event)
            self.assertEqual(stream_id, event.stream._stream._stream_id)


if __name__ == "__main__":
    unittest.main()
