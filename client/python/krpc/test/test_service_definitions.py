import unittest
import warnings
from inspect import signature
from typing import Dict, List, NamedTuple
from krpc.client import _stub_definitions
from krpc.definitions import register_all
from krpc.encoder import Encoder
from krpc.service import create_service, service_definitions
from krpc.types import Types
import krpc.schema.KRPC_pb2 as KRPC


class FakeClient:
    """As much of a client as creating a service needs, without a connection to a server"""

    def __init__(self) -> None:
        self._types = Types()

    def _invoke(self, *args: object, **kwargs: object) -> object:
        raise NotImplementedError

    def _build_call(self, *args: object, **kwargs: object) -> object:
        raise NotImplementedError


def enum_service(name: str) -> KRPC.Service:
    """A service defining an enumeration and nothing else"""
    service = KRPC.Service()
    service.name = name
    enumeration = service.enumerations.add()
    enumeration.name = "Mode"
    for value_name, value in (("Slow", 1), ("Fast", 2)):
        enumeration_value = enumeration.values.add()
        enumeration_value.name = value_name
        enumeration_value.value = value
    return service


def default_service(
    name: str, enum_service_name: str, in_a_list: bool = False
) -> KRPC.Service:
    """A service with a procedure whose parameter defaults to a member of another service's
    enumeration, either directly or through a list"""
    types = Types()
    enumeration_type = types.enumeration_type(enum_service_name, "Mode")
    encoded = Encoder.encode(2, types.sint32_type)
    if in_a_list:
        items = KRPC.List()
        items.items.extend([encoded])
        encoded = items.SerializeToString()

    service = KRPC.Service()
    service.name = name
    procedure = service.procedures.add()
    procedure.name = "Frob"
    parameter = procedure.parameters.add()
    parameter.name = "mode"
    if in_a_list:
        parameter.type.CopyFrom(types.list_type(enumeration_type).protobuf_type)
    else:
        parameter.type.CopyFrom(enumeration_type.protobuf_type)
    parameter.has_default_value = True
    parameter.default_value = encoded
    return service


# A type code from a later version of the protocol than this client knows about
UNKNOWN_TYPE_CODE = 9999


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


def struct_using_service(
    name: str, struct_service_name: str, with_default: bool = False
) -> KRPC.Service:
    """A service with a procedure taking a parameter of another service's structure,
    optionally one with a default value"""
    types = Types()
    struct_type = types.struct_type(struct_service_name, "Thing")

    service = KRPC.Service()
    service.name = name
    procedure = service.procedures.add()
    procedure.name = "Frob"
    parameter = procedure.parameters.add()
    parameter.name = "thing"
    parameter.type.CopyFrom(struct_type.protobuf_type)
    if with_default:
        parameter.has_default_value = True
        parameter.default_value = KRPC.Tuple().SerializeToString()
    return service


def unknown_type() -> KRPC.Type:
    typ = KRPC.Type()
    typ.code = UNKNOWN_TYPE_CODE
    return typ


class TestServiceDefinitions(unittest.TestCase):
    @staticmethod
    def create_services(services: List[KRPC.Service]) -> FakeClient:
        client = FakeClient()
        definitions = [
            definition
            for service in services
            for definition in service_definitions(service)
        ]
        register_all(client._types, definitions)
        for service in services:
            setattr(client, service.name, create_service(client, service))  # type: ignore[arg-type]
        return client

    def check_default(self, client: FakeClient, expected: str) -> None:
        service = getattr(client, "ServiceA")
        default = signature(service.frob).parameters["mode"].default
        self.assertEqual(expected, str(default))

    def test_defined_before_it_is_used(self) -> None:
        client = self.create_services(
            [enum_service("ServiceB"), default_service("ServiceA", "ServiceB")]
        )
        self.check_default(client, "Mode.fast")

    def test_defined_after_it_is_used(self) -> None:
        client = self.create_services(
            [default_service("ServiceA", "ServiceB"), enum_service("ServiceB")]
        )
        self.check_default(client, "Mode.fast")

    def test_used_inside_a_collection(self) -> None:
        client = self.create_services(
            [
                default_service("ServiceA", "ServiceB", in_a_list=True),
                enum_service("ServiceB"),
            ]
        )
        self.check_default(client, "[<Mode.fast: 2>]")

    def test_defined_by_the_service_that_uses_it(self) -> None:
        service = default_service("ServiceA", "ServiceA")
        service.enumerations.extend(enum_service("ServiceA").enumerations)
        self.check_default(self.create_services([service]), "Mode.fast")

    def check_struct_is_skipped(self, services: List[KRPC.Service]) -> None:
        with warnings.catch_warnings(record=True) as caught:
            warnings.simplefilter("always")
            client = self.create_services(services)
        messages = [str(warning.message) for warning in caught]
        self.assertTrue(any("Thing" in message for message in messages), messages)
        self.assertTrue(
            any("ServiceA.Frob" in message for message in messages), messages
        )
        self.assertFalse(hasattr(getattr(client, "ServiceA"), "frob"))

    def test_procedure_naming_a_skipped_struct_is_skipped(self) -> None:
        self.check_struct_is_skipped(
            [
                struct_service("ServiceB", unknown_type()),
                struct_using_service("ServiceA", "ServiceB"),
            ]
        )

    def test_defaulted_parameter_of_a_skipped_struct_is_skipped(self) -> None:
        self.check_struct_is_skipped(
            [
                struct_service("ServiceB", unknown_type()),
                struct_using_service("ServiceA", "ServiceB", with_default=True),
            ]
        )

    def test_struct_holding_a_skipped_struct_is_skipped(self) -> None:
        types = Types()
        skipped = struct_service("ServiceC", unknown_type())
        holder = struct_service(
            "ServiceB", types.struct_type("ServiceC", "Thing").protobuf_type
        )
        self.check_struct_is_skipped(
            [skipped, holder, struct_using_service("ServiceA", "ServiceB")]
        )

    def test_stub_with_more_fields_than_the_server(self) -> None:
        # An older server declares fewer fields than the stubs were generated against, so a
        # value of the structure cannot be built as the type the stubs provide
        class Thing(NamedTuple):
            value: int
            appended: str

        class StubService:
            _classes: Dict[str, type] = {}
            _enumerations: Dict[str, type] = {}
            _structs = {"Thing": Thing}

        client = FakeClient()
        service = struct_service("ServiceB", Types().sint32_type.protobuf_type)
        with warnings.catch_warnings(record=True) as caught:
            warnings.simplefilter("always")
            register_all(
                client._types, _stub_definitions(client, service, StubService())
            )
        self.assertTrue(
            any("ServiceB.Thing" in str(warning.message) for warning in caught)
        )
        struct_type = client._types.struct_type("ServiceB", "Thing")
        self.assertEqual(["value"], struct_type.field_names)
        self.assertIsNot(Thing, struct_type.python_type)

    def test_defined_by_no_service(self) -> None:
        with self.assertRaises(RuntimeError) as cm:
            self.create_services([default_service("ServiceA", "ServiceB")])
        self.assertIn("ServiceA.Frob parameter mode", str(cm.exception))
        self.assertIn("ServiceB.Mode", str(cm.exception))


if __name__ == "__main__":
    unittest.main()
