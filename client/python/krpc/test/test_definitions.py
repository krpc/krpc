import unittest
from typing import Dict, Iterable, List
from krpc.definitions import (
    ENUMERATION,
    Definition,
    decode_default_value,
    register_all,
    topological_order,
)
from krpc.encoder import Encoder
from krpc.types import EnumerationType, Types
from krpc.schema.KRPC_pb2 import Type


def enumeration_type(service: str, name: str) -> Type:
    protobuf_type = Type()
    protobuf_type.code = Type.ENUMERATION
    protobuf_type.service = service
    protobuf_type.name = name
    return protobuf_type


def class_type(service: str, name: str) -> Type:
    protobuf_type = Type()
    protobuf_type.code = Type.CLASS
    protobuf_type.service = service
    protobuf_type.name = name
    return protobuf_type


def list_type(value_type: Type) -> Type:
    protobuf_type = Type()
    protobuf_type.code = Type.LIST
    protobuf_type.types.extend([value_type])
    return protobuf_type


class TestRegisterAll(unittest.TestCase):
    def setUp(self) -> None:
        self.registered: List[str] = []
        self.types = Types()

    def definition(
        self,
        name: str,
        references: Iterable[Type] = (),
        kind: str = ENUMERATION,
        service: str = "ServiceA",
    ) -> Definition:
        """A definition that records the order in which it is registered"""

        def register(types: Types) -> None:  # pylint: disable=unused-argument
            self.registered.append(name)

        return Definition(kind, service, name, references, register)

    def test_registers_every_definition(self) -> None:
        register_all(
            self.types,
            [self.definition("A"), self.definition("B"), self.definition("C")],
        )
        self.assertEqual(["A", "B", "C"], self.registered)

    def test_registered_after_what_it_refers_to(self) -> None:
        # C refers to B, which refers to A, so the only valid order is A, B, C
        definitions = {
            "A": self.definition("A"),
            "B": self.definition("B", [enumeration_type("ServiceA", "A")]),
            "C": self.definition("C", [enumeration_type("ServiceA", "B")]),
        }
        for order in (["A", "B", "C"], ["C", "B", "A"], ["B", "C", "A"]):
            self.registered = []
            register_all(self.types, [definitions[name] for name in order])
            self.assertEqual(["A", "B", "C"], self.registered)

    def test_refers_to_a_type_inside_a_collection(self) -> None:
        register_all(
            self.types,
            [
                self.definition("B", [list_type(enumeration_type("ServiceA", "A"))]),
                self.definition("A"),
            ],
        )
        self.assertEqual(["A", "B"], self.registered)

    def test_refers_to_another_service(self) -> None:
        register_all(
            self.types,
            [
                self.definition("B", [enumeration_type("ServiceB", "A")]),
                self.definition("A", service="ServiceB"),
            ],
        )
        self.assertEqual(["A", "B"], self.registered)

    def test_registered_once_when_referred_to_repeatedly(self) -> None:
        reference = enumeration_type("ServiceA", "A")
        register_all(
            self.types,
            [
                self.definition("B", [reference]),
                self.definition("C", [reference, enumeration_type("ServiceA", "B")]),
                self.definition("A"),
            ],
        )
        self.assertEqual(["A", "B", "C"], self.registered)

    def test_a_reference_names_a_kind_as_well_as_a_name(self) -> None:
        # The reference is to a class, so the enumeration of the same name does not satisfy it
        with self.assertRaises(RuntimeError) as cm:
            register_all(
                self.types,
                [
                    self.definition("B", [class_type("ServiceA", "A")]),
                    self.definition("A", kind=ENUMERATION),
                ],
            )
        self.assertIn("class ServiceA.A", str(cm.exception))

    def test_reference_that_nothing_defines(self) -> None:
        with self.assertRaises(RuntimeError) as cm:
            register_all(
                self.types,
                [self.definition("B", [enumeration_type("ServiceB", "Mode")])],
            )
        self.assertIn("enumeration ServiceA.B", str(cm.exception))
        self.assertIn("enumeration ServiceB.Mode", str(cm.exception))
        self.assertEqual([], self.registered)

    def test_cycle(self) -> None:
        with self.assertRaises(RuntimeError) as cm:
            register_all(
                self.types,
                [
                    self.definition("A", [enumeration_type("ServiceA", "B")]),
                    self.definition("B", [enumeration_type("ServiceA", "A")]),
                ],
            )
        self.assertEqual(
            "Cyclic dependency: enumeration ServiceA.A -> enumeration ServiceA.B "
            "-> enumeration ServiceA.A",
            str(cm.exception),
        )

    def test_defined_twice(self) -> None:
        with self.assertRaises(RuntimeError) as cm:
            register_all(self.types, [self.definition("A"), self.definition("A")])
        self.assertIn(
            "enumeration ServiceA.A is defined more than once", str(cm.exception)
        )


class TestTopologicalOrder(unittest.TestCase):
    @staticmethod
    def order(graph: Dict[str, List[str]], nodes: Iterable[str]) -> List[str]:
        def key(node: str) -> str:
            return node

        def dependencies(node: str) -> List[str]:
            return graph[node]

        return topological_order(nodes, key, dependencies)

    def test_each_node_is_returned_once(self) -> None:
        graph = {"a": ["b", "c"], "b": ["c"], "c": []}
        self.assertEqual(["c", "b", "a"], self.order(graph, ["a", "b", "c"]))

    def test_dependencies_that_were_not_given_are_included(self) -> None:
        self.assertEqual(["b", "a"], self.order({"a": ["b"], "b": []}, ["a"]))

    def test_cycle(self) -> None:
        graph = {"a": ["b"], "b": ["c"], "c": ["b"]}
        with self.assertRaises(RuntimeError) as cm:
            self.order(graph, ["a"])
        self.assertEqual("Cyclic dependency: b -> c -> b", str(cm.exception))


class TestDecodeDefaultValue(unittest.TestCase):
    location = "ServiceA.Frob parameter mode"

    def setUp(self) -> None:
        self.types = Types()

    def encode(self, value: int) -> bytes:
        return Encoder.encode(value, self.types.sint32_type)

    def enumeration(self, registered: bool = True) -> EnumerationType:
        typ = self.types.enumeration_type("ServiceA", "Mode")
        if registered:
            typ.set_values(
                {"slow": {"value": 1, "doc": ""}, "fast": {"value": 2, "doc": ""}}
            )
        return typ

    def test_decodes_an_enumeration_value(self) -> None:
        typ = self.enumeration()
        value = decode_default_value(None, self.encode(2), typ, self.location)
        self.assertEqual(getattr(typ.python_type, "fast"), value)

    def test_enumeration_that_was_never_registered(self) -> None:
        with self.assertRaises(RuntimeError) as cm:
            decode_default_value(
                None, self.encode(2), self.enumeration(registered=False), self.location
            )
        self.assertIn(self.location, str(cm.exception))
        self.assertIn("ServiceA.Mode", str(cm.exception))

    def test_enumeration_inside_a_collection(self) -> None:
        typ = self.types.list_type(self.enumeration(registered=False))
        # The value is never reached, as the type it would be decoded through is incomplete
        with self.assertRaises(RuntimeError) as cm:
            decode_default_value(None, b"", typ, self.location)
        self.assertIn(self.location, str(cm.exception))
        self.assertIn("ServiceA.Mode", str(cm.exception))

    def test_value_the_enumeration_does_not_declare(self) -> None:
        with self.assertRaises(RuntimeError) as cm:
            decode_default_value(
                None, self.encode(7), self.enumeration(), self.location
            )
        self.assertIn(self.location, str(cm.exception))
        self.assertIn("7 is not a valid Mode", str(cm.exception))


if __name__ == "__main__":
    unittest.main()
