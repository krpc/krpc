import unittest

from krpc.connection import Connection
from krpc.declarations import (
    CONNECTION_MEMBERS,
    DeclaredClass,
    is_class_type,
    service_attribute,
    type_name,
)
from krpc.types import _create_class_type


class Widget(metaclass=DeclaredClass):
    _service_name = "Shop"
    _class_name = "Widget"

    @staticmethod
    def make() -> None:
        raise TypeError


class Shop(metaclass=DeclaredClass):
    _service_name = "Shop"
    _class_name = None


class TestDeclarations(unittest.TestCase):
    def test_a_built_class_is_an_instance_of_its_declaration(self) -> None:
        built = _create_class_type("Shop", "Widget", None)
        other = _create_class_type("Shop", "Gadget", None)
        self.assertIsInstance(built(None, 1), Widget)
        self.assertTrue(issubclass(built, Widget))
        self.assertNotIsInstance(other(None, 1), Widget)
        self.assertFalse(issubclass(built, Shop))
        self.assertTrue(issubclass(Widget, Widget))
        self.assertNotIsInstance(1, Widget)

    def test_a_subclass_of_a_built_class_is_an_instance(self) -> None:
        built = _create_class_type("Shop", "Widget", None)
        subclass = type("Sub", (built,), {})
        self.assertTrue(issubclass(subclass, Widget))

    def test_a_subclass_of_a_declaration_compares_as_an_ordinary_class(self) -> None:
        class Special(Widget):
            pass

        class MoreSpecial(Special):
            pass

        built = _create_class_type("Shop", "Widget", None)
        self.assertNotIsInstance(built(None, 1), Special)
        self.assertNotIsInstance(1, Special)
        self.assertTrue(issubclass(MoreSpecial, Special))
        self.assertTrue(issubclass(Special, Widget))
        self.assertFalse(issubclass(str, Special))

    def test_calling_a_declaration_names_the_class_through_a_connection(self) -> None:
        with self.assertRaises(TypeError) as context:
            Widget()
        self.assertIn("conn.shop.Widget", str(context.exception))
        with self.assertRaises(TypeError) as context:
            Shop()
        self.assertIn("conn.shop", str(context.exception))

    def test_is_class_type(self) -> None:
        self.assertTrue(is_class_type(Widget))
        self.assertTrue(is_class_type(_create_class_type("Shop", "Widget", None)))
        self.assertFalse(is_class_type(Shop))
        self.assertFalse(is_class_type(int))
        self.assertFalse(is_class_type(Widget.make))

    def test_connection_members_are_the_public_members_of_a_connection(self) -> None:
        public = {x for x in dir(Connection) if not x.startswith("_")}
        self.assertEqual(public, CONNECTION_MEMBERS)

    def test_a_service_named_as_a_connection_member_is_escaped(self) -> None:
        self.assertEqual("close_", service_attribute("Close"))
        self.assertEqual("space_center", service_attribute("SpaceCenter"))

    def test_a_service_named_as_a_keyword_is_escaped(self) -> None:
        self.assertEqual("global_", service_attribute("Global"))

    def test_a_type_named_as_the_connection_type_is_escaped(self) -> None:
        self.assertEqual(
            "SpaceCenterConnection_", type_name("SpaceCenter", "SpaceCenterConnection")
        )
        self.assertEqual("Vessel", type_name("SpaceCenter", "Vessel"))

    def test_a_type_named_as_a_keyword_is_escaped(self) -> None:
        self.assertEqual("None_", type_name("SpaceCenter", "None"))


if __name__ == "__main__":
    unittest.main()
