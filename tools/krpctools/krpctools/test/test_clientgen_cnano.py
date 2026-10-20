import unittest

from krpctools.clientgen.cnano import CnanoGenerator
from krpctools.test.clientgentest import ClientGenTestCase


def _list_of_class(name, class_name):
    return {
        "name": name,
        "type": {
            "code": "LIST",
            "types": [{"code": "CLASS", "service": "ServiceA", "name": class_name}],
        },
    }


class TestClientGenCNano(ClientGenTestCase, unittest.TestCase):
    language = "cnano"
    generator = CnanoGenerator

    def test_collections_of_different_classes_share_one_struct(self):
        # Every class is a krpc_object_t in C, so a list of one class and a list of another
        # are the same struct, which can only be declared once
        defs = {
            "ServiceA": {
                "id": 1,
                "documentation": "",
                "procedures": {
                    "Frob": {
                        "id": 1,
                        "parameters": [
                            _list_of_class("things", "Thing"),
                            _list_of_class("widgets", "Widget"),
                        ],
                        "documentation": "",
                    }
                },
                "classes": {
                    "Thing": {"documentation": ""},
                    "Widget": {"documentation": ""},
                },
                "enumerations": {},
                "exceptions": {},
            }
        }
        self.assertEqual(
            1, self.generate("ServiceA", defs).count("struct krpc_list_object_s {")
        )

    def test_includes_the_services_it_uses(self):
        # ServiceA uses an enumeration of ServiceB only
        includes = {"ServiceB": "b/service_b.h", "ServiceC": "c/service_c.h"}
        actual = self.generate("ServiceA", self.load("Ordering"), includes)
        self.assertIn("#include <b/service_b.h>\n", actual)
        self.assertNotIn("service_c", actual)

    def test_service_whose_include_is_not_provided(self):
        with self.assertRaises(RuntimeError) as cm:
            self.generate("ServiceA", self.load("Ordering"), {})
        self.assertIn("ServiceB", str(cm.exception))

    def test_struct_of_a_service_that_is_not_defined(self):
        # The codec of a structure is generated from its fields, which are in the definitions
        # of the service that defines it
        thing = {"code": "STRUCT", "service": "ServiceB", "name": "Thing"}
        procedure = {
            "id": 1,
            "parameters": [{"name": "thing", "type": thing}],
            "documentation": "",
        }
        service = {
            "id": 1,
            "documentation": "",
            "procedures": {"Frob": procedure},
            "classes": {},
            "enumerations": {},
            "exceptions": {},
        }
        with self.assertRaises(RuntimeError) as cm:
            self.generate("ServiceA", {"ServiceA": service})
        self.assertIn("ServiceB.Thing", str(cm.exception))


if __name__ == "__main__":
    unittest.main()
