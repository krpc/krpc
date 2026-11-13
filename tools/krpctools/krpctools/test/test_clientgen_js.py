import unittest

from krpctools.clientgen.js import JsGenerator
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
        self.assertNotIn("KRPC, ", actual.split("\n")[2])


if __name__ == "__main__":
    unittest.main()
