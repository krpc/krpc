import unittest
from typing import Any, NamedTuple

import krpc.schema.KRPC_pb2 as KRPC
from krpc.declarations import Lookup, client_lookup, register, registered
from krpc.definitions import register_all
from krpc.service import create_service, service_definitions
from krpc.types import DocEnum, Types


class FakeClient:
    """As much of a client as creating a service needs, without a connection to a server"""

    # Set by create_services
    Generated: Any

    def __init__(self) -> None:
        self._types = Types()


# The classes of a generated module for the service Generated
class Mode(DocEnum):
    slow = 0
    fast = 1


class Thing(NamedTuple):
    value: int


class Failure(RuntimeError):
    pass


register("Generated", Mode, Thing, Failure)


# The client type of the generated module, which is this module
class GeneratedClient:
    _declares_service = "Generated"


def generated_service(
    mode_values: dict[str, int], thing_fields: list[str], failure_mapped_type: str = ""
) -> KRPC.Service:
    """The definition of the service Generated, with the given values of its enumeration and
    the given fields of its structure"""
    service = KRPC.Service()
    service.name = "Generated"
    enumeration = service.enumerations.add()
    enumeration.name = "Mode"
    for name, value in mode_values.items():
        enum_value = enumeration.values.add()
        enum_value.name = name
        enum_value.value = value
    struct = service.structs.add()
    struct.name = "Thing"
    for name in thing_fields:
        field = struct.fields.add()
        field.name = name
        field.type.code = KRPC.Type.SINT32
    exception = service.exceptions.add()
    exception.name = "Failure"
    exception.mapped_type = failure_mapped_type
    return service


def create_services(
    services: list[KRPC.Service], lookup: Lookup = registered
) -> FakeClient:
    client = FakeClient()
    definitions = [
        definition
        for service in services
        for definition in service_definitions(service, lookup)
    ]
    register_all(client._types, definitions)
    for service in services:
        setattr(client, service.name, create_service(client, service))  # pyright: ignore[reportArgumentType]
    return client


class TestServiceDefinitions(unittest.TestCase):
    def test_matching_definitions_use_the_generated_classes(self) -> None:
        client = create_services([generated_service({"Slow": 0, "Fast": 1}, ["Value"])])
        self.assertIs(Mode, client.Generated.Mode)
        self.assertIs(Thing, client.Generated.Thing)
        self.assertIs(Failure, client.Generated.Failure)

    def test_differing_definitions_build_their_own_classes(self) -> None:
        client = create_services(
            [generated_service({"Slow": 0, "Medium": 1, "Fast": 2}, ["Value", "Other"])]
        )
        built_mode = client.Generated.Mode
        self.assertIsNot(Mode, built_mode)
        self.assertIsNot(Thing, client.Generated.Thing)
        self.assertEqual(("value", "other"), client.Generated.Thing._fields)
        # An exception matches on its name and the builtin it derives from
        self.assertIs(Failure, client.Generated.Failure)

        # A generated value is a value of its own enumeration, and coerces to the built one by
        # its number
        self.assertNotEqual(Mode.fast, built_mode.fast)
        mode_type = client._types.enumeration_type("Generated", "Mode")
        self.assertIs(built_mode.medium, client._types.coerce_to(Mode.fast, mode_type))

    def test_an_exception_mapped_to_another_builtin_builds_its_own_class(self) -> None:
        client = create_services(
            [
                generated_service(
                    {"Slow": 0, "Fast": 1}, ["Value"], "System.ArgumentException"
                )
            ]
        )
        self.assertIsNot(Failure, client.Generated.Failure)
        self.assertTrue(issubclass(client.Generated.Failure, ValueError))

    def test_an_exception_with_a_narrower_builtin_builds_its_own_class(self) -> None:
        class Narrower(NotImplementedError):
            pass

        Narrower.__name__ = "Failure"
        register("Generated", Narrower)
        try:
            client = create_services(
                [generated_service({"Slow": 0, "Fast": 1}, ["Value"])]
            )
            self.assertIsNot(Narrower, client.Generated.Failure)
            self.assertIs(RuntimeError, client.Generated.Failure.__base__)
        finally:
            register("Generated", Failure)

    def test_an_enumeration_with_an_alias_uses_the_generated_class(self) -> None:
        class Aliased(DocEnum):
            slow = 0
            fast = 1
            quick = 1

        Aliased.__name__ = "Mode"
        register("Generated", Aliased)
        try:
            client = create_services(
                [generated_service({"Slow": 0, "Fast": 1, "Quick": 1}, ["Value"])]
            )
            self.assertIs(Aliased, client.Generated.Mode)
        finally:
            register("Generated", Mode)

    def test_a_number_the_server_lacks_does_not_coerce(self) -> None:
        client = create_services([generated_service({"Slow": 0}, ["Value"])])
        mode_type = client._types.enumeration_type("Generated", "Mode")
        self.assertIs(
            client.Generated.Mode.slow, client._types.coerce_to(Mode.slow, mode_type)
        )
        self.assertRaises(ValueError, client._types.coerce_to, Mode.fast, mode_type)

    def test_a_later_registration_replaces_an_earlier_one(self) -> None:
        class Newer(DocEnum):
            slow = 0
            fast = 1

        Newer.__name__ = "Mode"
        register("Generated", Newer)
        try:
            client = create_services(
                [generated_service({"Slow": 0, "Fast": 1}, ["Value"])]
            )
            self.assertIs(Newer, client.Generated.Mode)
            # A client type with the service takes the classes of its own module
            client = create_services(
                [generated_service({"Slow": 0, "Fast": 1}, ["Value"])],
                client_lookup(GeneratedClient),
            )
            self.assertIs(Mode, client.Generated.Mode)
        finally:
            register("Generated", Mode)

    def test_a_client_type_module_lacking_a_class_builds_it(self) -> None:
        class Other(DocEnum):
            first = 0

        register("Generated", Other)
        service = generated_service({"Slow": 0, "Fast": 1}, ["Value"])
        enumeration = service.enumerations.add()
        enumeration.name = "Other"
        enumeration.values.add(name="First", value=0)
        # The registry has Other, but the client type's module does not
        client = create_services([service], client_lookup(GeneratedClient))
        self.assertIsNot(Other, client.Generated.Other)
        self.assertIs(Mode, client.Generated.Mode)

    def test_a_client_type_module_outside_sys_modules_uses_the_registry(self) -> None:
        class Unlisted:
            _declares_service = "Generated"

        Unlisted.__module__ = "not_in_sys_modules"
        client = create_services(
            [generated_service({"Slow": 0, "Fast": 1}, ["Value"])],
            client_lookup(Unlisted),
        )
        self.assertIs(Mode, client.Generated.Mode)


if __name__ == "__main__":
    unittest.main()
