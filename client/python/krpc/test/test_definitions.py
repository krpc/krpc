import unittest
from collections.abc import Iterable

from krpc.definitions import (
    ENUMERATION,
    STRUCT,
    Definition,
    register_all,
    topological_order,
)
from krpc.schema.KRPC_pb2 import Type
from krpc.types import Types


def enumeration_type(service: str, name: str) -> Type:
    protobuf_type = Type()
    protobuf_type.code = Type.ENUMERATION
    protobuf_type.service = service
    protobuf_type.name = name
    return protobuf_type


def struct_type(service: str, name: str) -> Type:
    protobuf_type = Type()
    protobuf_type.code = Type.STRUCT
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
        self.registered: list[str] = []
        self.types = Types()

    def definition(
        self,
        name: str,
        references: Iterable[Type] = (),
        kind: str = ENUMERATION,
        service: str = "ServiceA",
    ) -> Definition:
        """A definition that records the order in which it is registered"""

        def register(types: Types) -> None:
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

    def test_struct_registered_after_the_types_of_its_fields(self) -> None:
        # The struct B has a field whose type is the struct A, so A has to be registered
        # first for the field to resolve
        definitions = {
            "A": self.definition("A", kind=STRUCT),
            "B": self.definition("B", [struct_type("ServiceA", "A")], kind=STRUCT),
        }
        for order in (["A", "B"], ["B", "A"]):
            self.registered = []
            register_all(self.types, [definitions[name] for name in order])
            self.assertEqual(["A", "B"], self.registered)

    def test_struct_cycle(self) -> None:
        # Neither struct can be registered first, as each has a field whose type is the
        # other. The server rejects this, but definitions can come from anywhere
        with self.assertRaises(RuntimeError) as cm:
            register_all(
                self.types,
                [
                    self.definition(
                        "A", [list_type(struct_type("ServiceA", "B"))], kind=STRUCT
                    ),
                    self.definition("B", [struct_type("ServiceA", "A")], kind=STRUCT),
                ],
            )
        self.assertEqual(
            "Cyclic dependency: struct ServiceA.A -> struct ServiceA.B "
            "-> struct ServiceA.A",
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
    def order(graph: dict[str, list[str]], nodes: Iterable[str]) -> list[str]:
        def key(node: str) -> str:
            return node

        def dependencies(node: str) -> list[str]:
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


if __name__ == "__main__":
    unittest.main()
