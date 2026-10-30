import unittest
import warnings
from typing import Any, NamedTuple

import krpc.schema.KRPC_pb2 as KRPC
from krpc.client import _stub_definitions
from krpc.definitions import register_all
from krpc.service import create_service, service_definitions
from krpc.types import Types


class FakeClient:
    """As much of a client as creating a service needs, without a connection to a server"""

    # Set by create_services
    ServiceA: Any

    def __init__(self) -> None:
        self._types = Types()


def struct_service(name: str, field_type: KRPC.Type) -> KRPC.Service:
    """A service defining a structure with a single field of the given type"""
    service = KRPC.Service()
    service.name = name
    struct = service.structs.add()
    struct.name = "Thing"
    field = struct.fields.add()
    field.name = "Value"
    field.type.CopyFrom(field_type)
    return service


class TestServiceDefinitions(unittest.TestCase):
    @staticmethod
    def create_services(services: list[KRPC.Service]) -> FakeClient:
        client = FakeClient()
        definitions = [
            definition
            for service in services
            for definition in service_definitions(service)
        ]
        register_all(client._types, definitions)
        for service in services:
            setattr(client, service.name, create_service(client, service))  # pyright: ignore[reportArgumentType]
        return client

    def test_stub_with_more_fields_than_the_server(self) -> None:
        # An older server declares fewer fields than the stubs were generated against, so a
        # value of the structure cannot be built as the type the stubs provide
        class Thing(NamedTuple):
            value: int
            appended: str

        class StubService:
            _classes: dict[str, type] = {}
            _enumerations: dict[str, type] = {}
            _structs = {"Thing": Thing}

        client = FakeClient()
        service = struct_service("ServiceB", Types().sint32_type.protobuf_type)
        with warnings.catch_warnings(record=True) as caught:
            warnings.simplefilter("always")
            register_all(
                client._types,
                _stub_definitions(client, service, StubService()),  # pyright: ignore[reportArgumentType]
            )
        self.assertTrue(
            any("ServiceB.Thing" in str(warning.message) for warning in caught)
        )
        struct_type = client._types.struct_type("ServiceB", "Thing")
        self.assertEqual(["value"], struct_type.field_names)
        self.assertIsNot(Thing, struct_type.python_type)


if __name__ == "__main__":
    unittest.main()
