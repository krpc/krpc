import unittest

from krpctools.clientgen.python import PythonGenerator
from krpctools.test.clientgentest import ClientGenTestCase


class TestClientGenPython(ClientGenTestCase, unittest.TestCase):
    language = "python"
    generator = PythonGenerator

    def test_service_imports_are_sorted(self):
        others = ["Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot"]
        empty = {
            "documentation": "",
            "procedures": {},
            "classes": {},
            "enumerations": {},
            "exceptions": {},
            "structs": {},
        }
        fields = [
            {
                "name": other,
                "type": {"code": "STRUCT", "service": other, "name": "Thing"},
                "documentation": "",
            }
            for other in others
        ]
        defs = {
            "Main": dict(
                empty,
                id=1,
                structs={"Thing": {"documentation": "", "fields": fields}},
            )
        }
        for i, other in enumerate(others):
            defs[other] = dict(
                empty,
                id=i + 2,
                structs={"Thing": {"documentation": "", "fields": []}},
            )
        lines = self.generate("Main", defs).splitlines()
        imports = [
            x
            for x in lines
            if x.startswith("from krpc.services import ") and "_service_" in x
        ]
        self.assertEqual(
            [
                "from krpc.services import %s as _service_%s" % (x.lower(), x.lower())
                for x in others
            ],
            imports,
        )


if __name__ == "__main__":
    unittest.main()
