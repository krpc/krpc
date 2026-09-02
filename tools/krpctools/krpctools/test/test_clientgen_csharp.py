import unittest
from krpctools.test.clientgentest import ClientGenTestCase
from krpctools.clientgen.csharp import CsharpGenerator


def nullable_class(name):
    return {"code": "CLASS", "service": "ServiceA", "name": name, "nullable": True}


def service_with_a_parameter(typ):
    return {
        "ServiceA": {
            "id": 1,
            "documentation": "",
            "procedures": {
                "Frob": {
                    "id": 1,
                    "parameters": [{"name": "value", "type": typ}],
                    "documentation": "",
                }
            },
            "classes": {"Thing": {"documentation": ""}},
            "enumerations": {},
            "exceptions": {},
            "structs": {
                "Holder": {
                    "documentation": "",
                    "fields": [
                        {
                            "name": "Things",
                            "type": {
                                "code": "LIST",
                                "types": [nullable_class("Thing")],
                            },
                            "documentation": "",
                        }
                    ],
                }
            },
        }
    }


class TestClientGenCsharp(ClientGenTestCase, unittest.TestCase):
    language = "csharp"
    generator = CsharpGenerator

    # A nullable reference-typed position is invisible in the C# type, so the generated stub
    # names it with a spec. Every collection has to reach the positions inside it for that,
    # whether or not a position of its own can hold null.

    def test_nullable_position_inside_a_list(self):
        content = self.generate(
            "ServiceA",
            service_with_a_parameter(
                {"code": "LIST", "types": [nullable_class("Thing")]}
            ),
        )
        self.assertIn("TypeSpec.Null (typeof(global::KRPC.Client", content)

    def test_nullable_position_inside_a_set(self):
        # A set element cannot itself be null, but a value it holds may hold one
        content = self.generate(
            "ServiceA",
            service_with_a_parameter(
                {
                    "code": "SET",
                    "types": [
                        {"code": "STRUCT", "service": "ServiceA", "name": "Holder"}
                    ],
                }
            ),
        )
        self.assertIn("TypeSpec.Null (typeof(global::KRPC.Client", content)


if __name__ == "__main__":
    unittest.main()
