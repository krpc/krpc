import unittest
from typing import Any

import krpc.schema.KRPC_pb2 as KRPC
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


if __name__ == "__main__":
    unittest.main()
