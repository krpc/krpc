import unittest

from krpc.declarations import DeclaredClass, is_class_type
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


if __name__ == "__main__":
    unittest.main()
