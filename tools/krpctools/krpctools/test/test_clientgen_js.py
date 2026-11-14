import unittest
from importlib.resources import files

from krpctools.clientgen.js import JsGenerator
from krpctools.definitions import Definitions
from krpctools.test.clientgentest import ClientGenTestCase


class TestClientGenJs(ClientGenTestCase, unittest.TestCase):
    language = "js"
    generator = JsGenerator

    def generate(self, service_name, defs, includes=None):
        # With no include paths, a module imports the other services from the client's package
        return super().generate(service_name, defs, includes or {})

    def test_include(self):
        defs = self.load_with_deps("TestService", ["KRPC"])
        actual = self.generate("TestService", defs, {"KRPC": "./KRPC.js"})
        self.assertIn('import type * as KRPC from "./KRPC.js";', actual)
        package = next(x for x in actual.split("\n") if x.startswith("import type {"))
        self.assertNotIn("KRPC, ", package)

    def test_client_module(self):
        template = files("krpctools.clientgen").joinpath("js.tmpl").read_text()
        generator = JsGenerator(template, "KRPC", Definitions(self.load("KRPC")))
        generator.client_module = "../declarations.js"
        actual = generator.generate()
        self.assertIn(
            "import { declareEnumeration, declareException, declareStruct } from "
            '"../declarations.js";',
            actual,
        )


if __name__ == "__main__":
    unittest.main()
