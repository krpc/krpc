import unittest

from krpctools.clientgen.csharp import CsharpGenerator
from krpctools.test.clientgentest import ClientGenTestCase, bytes_default


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

    def test_bytes_default(self):
        # A byte array is not a compile-time constant, so it defaults to null
        content = self.generate("ServiceA", bytes_default())
        self.assertIn("public void Frob (byte[] value = null)", content)
        self.assertIn("value ?? new byte[] { 0xca, 0xfe }", content)

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

    def test_nullable_stream(self):
        # A stream id is a ulong, a value type, so a nullable one takes the nullable form
        def procedure(pid, typ):
            return {
                "id": pid,
                "parameters": [],
                "return_type": typ,
                "documentation": "",
            }

        nullable_stream = {"code": "STREAM", "nullable": True}
        service = {
            "id": 1,
            "documentation": "",
            "procedures": {
                "GetStream": procedure(1, nullable_stream),
                "GetStreams": procedure(
                    2, {"code": "LIST", "types": [nullable_stream]}
                ),
            },
            "classes": {},
            "enumerations": {},
            "exceptions": {},
        }
        content = self.generate("ServiceA", {"ServiceA": service})
        self.assertIn("public ulong? GetStream ()", content)
        self.assertIn("IList<ulong?> GetStreams ()", content)


if __name__ == "__main__":
    unittest.main()
