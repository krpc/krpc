import unittest
from krpc.types import Types
from ..definitions import Definitions


def enumeration(*values):
    return {
        "documentation": "",
        "values": [
            {"name": name, "value": value, "documentation": ""}
            for name, value in values
        ],
    }


class TestDefinitions(unittest.TestCase):
    def setUp(self):
        self.definitions = Definitions(
            {
                "ServiceA": {
                    "id": 1,
                    "documentation": "",
                    "classes": {"Thing": {"documentation": ""}},
                    "enumerations": {"Mode": enumeration(("Slow", 0), ("Fast", 1))},
                    "exceptions": {"Failure": {"documentation": ""}},
                }
            }
        )

    def test_enumeration_values_are_registered(self):
        typ = self.definitions.types.enumeration_type("ServiceA", "Mode")
        self.assertEqual(1, typ.python_type.Fast.value)

    def test_class_is_registered(self):
        self.assertEqual(
            "Thing",
            self.definitions.types.class_type("ServiceA", "Thing").python_type.__name__,
        )

    def test_as_type(self):
        typ = self.definitions.as_type(
            {"code": "ENUMERATION", "service": "ServiceA", "name": "Mode"}
        )
        self.assertEqual(
            self.definitions.types.enumeration_type("ServiceA", "Mode"), typ
        )

    def test_a_run_has_a_registry_of_its_own(self):
        # Registering the same enumeration twice in one registry raises, so generating from
        # two sets of definitions in one process must not share one
        other = Definitions(
            {
                "ServiceA": {
                    "id": 1,
                    "documentation": "",
                    "enumerations": {"Mode": enumeration(("Slow", 0))},
                }
            }
        )
        self.assertNotEqual(self.definitions.types, other.types)
        self.assertFalse(
            hasattr(
                other.types.enumeration_type("ServiceA", "Mode").python_type, "Fast"
            )
        )

    def test_collection_types_are_innermost_first(self):
        types = Types()
        pair = types.tuple_type(types.sint32_type, types.string_type)
        listing = types.list_type(pair)
        mapping = types.dictionary_type(types.string_type, listing)
        self.assertEqual(
            [pair, listing, mapping],
            Definitions.collection_types([mapping, types.sint32_type, listing]),
        )

    def test_collection_types_are_returned_once(self):
        types = Types()
        listing = types.list_type(types.sint32_type)
        self.assertEqual([listing], Definitions.collection_types([listing, listing]))


if __name__ == "__main__":
    unittest.main()
