import unittest
from krpctools.test.clientgentest import ClientGenTestCase
from krpctools.clientgen.cnano import CnanoGenerator


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


if __name__ == "__main__":
    unittest.main()
