import importlib
import pkgutil
import unittest

import krpc.ksp
import krpc.services
from krpctools.clientgen.python import SHIPPED_MODULES, PythonGenerator
from krpctools.test.clientgentest import ClientGenTestCase


class TestClientGenPython(ClientGenTestCase, unittest.TestCase):
    language = "python"
    generator = PythonGenerator
    include_services = False

    def test_shipped_modules_are_the_modules_the_client_ships(self):
        shipped = {}
        for package in (krpc.services, krpc.ksp):
            for info in pkgutil.iter_modules(package.__path__):
                module = importlib.import_module(package.__name__ + "." + info.name)
                for value in vars(module).values():
                    if isinstance(value, type) and value.__module__ == module.__name__:
                        service = vars(value).get("_declares_service")
                        if service is not None:
                            shipped[service] = module.__name__
        self.assertEqual(SHIPPED_MODULES, shipped)

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
        imports = [x for x in lines if x.startswith("import ") and "_service_" in x]
        self.assertEqual(
            [
                "import krpc.services.%s as _service_%s" % (x.lower(), x.lower())
                for x in others
            ],
            imports,
        )

    def test_include_names_a_module(self):
        defs = {
            "Main": {
                "id": 1,
                "documentation": "",
                "procedures": {},
                "classes": {},
                "enumerations": {},
                "exceptions": {},
                "structs": {
                    "Thing": {
                        "documentation": "",
                        "fields": [
                            {
                                "name": name,
                                "type": {
                                    "code": "STRUCT",
                                    "service": name,
                                    "name": "Thing",
                                },
                                "documentation": "",
                            }
                            for name in ("Other", "Path", "SpaceCenter")
                        ],
                    }
                },
            }
        }
        for i, name in enumerate(("Other", "Path", "SpaceCenter")):
            defs[name] = {
                "id": i + 2,
                "documentation": "",
                "procedures": {},
                "classes": {},
                "enumerations": {},
                "exceptions": {},
                "structs": {"Thing": {"documentation": "", "fields": []}},
            }
        includes = {"Other": "other_krpc.other", "Path": "path_krpc/path.py"}
        lines = self.generate("Main", defs, includes).splitlines()
        self.assertIn("import other_krpc.other as _service_other", lines)
        self.assertIn("import path_krpc.path as _service_path", lines)
        # A shipped service with no include takes the module it ships in
        self.assertIn("import krpc.ksp.spacecenter as _service_spacecenter", lines)


if __name__ == "__main__":
    unittest.main()
