import unittest

import krpc.schema.KRPC_pb2 as KRPC
from krpc.decoder import Decoder
from krpc.encoder import Encoder
from krpc.event import Event
from krpc.test.servertestcase import ServerTestCase


class TestClient(ServerTestCase, unittest.TestCase):
    def test_extension_members_are_per_connection(self) -> None:
        # A pre-generated class is shared by every client in the process, so the members
        # merged onto it go through the connection the object was reached through
        other = self.connect()
        try:
            obj = other.test_service.create_test_object("alpha")
            self.assertEqual("value=alpha", obj.extension_property)  # pyright: ignore[reportAttributeAccessIssue]
            obj2 = obj.extension_method_returning_class_from_other_service()  # pyright: ignore[reportAttributeAccessIssue]
            self.assertIs(other, obj2._client)
            self.assertIn("extension_method", dir(other.test_service.TestClass))
        finally:
            other.close()

    def test_nullable_class_type_shares_one_python_type(self) -> None:
        # A nullable class-typed value is the generated class of the non-nullable one, and
        # not a second class minted for the nullable declaration
        obj = self.conn.test_service.create_test_object("alpha")
        self.assertIs(type(obj.echo_nullable_object(obj)), type(obj))

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
