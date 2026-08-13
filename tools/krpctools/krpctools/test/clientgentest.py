import json
from importlib.resources import files
from ..definitions import Definitions


class ClientGenTestCase:
    def generate(self, service_name, defs):
        macro_template = (
            files("krpctools.clientgen")
            .joinpath(self.language + ".tmpl")
            .read_text(encoding="utf-8")
        )
        g = self.generator(macro_template, service_name, Definitions(defs))
        return g.generate()

    @staticmethod
    def load(name):
        return json.loads(
            files("krpctools.test").joinpath(name + ".json").read_text(encoding="utf-8")
        )

    def run_test(self, service_name, name):
        defs = self.load(name)
        actual = self.generate(service_name, defs)

        # with open('/home/alex/workspaces/krpc/krpc/' +
        #           'tools/krpctools/krpctools/test/' +
        #           'clientgen-'+name+'-'+self.language+'.txt', 'w') as f:
        #     f.write(actual)

        expected = (
            files("krpctools.test")
            .joinpath("clientgen-" + name + "-" + self.language + ".txt")
            .read_text(encoding="utf-8")
        )
        self.assertEqual(expected, actual)

        # A service may be defined after one that uses its types, so the definitions in the
        # reverse order have to generate the same thing
        reversed_defs = dict(reversed(list(defs.items())))
        self.assertEqual(actual, self.generate(service_name, reversed_defs))

    def test_empty(self):
        self.run_test("EmptyService", "Empty")

    def test_test_service(self):
        self.run_test("TestService", "TestService")

    def test_ordering(self):
        self.run_test("ServiceA", "Ordering")

    def test_service_that_is_not_defined(self):
        defs = self.load("Ordering")
        del defs["ServiceB"]
        with self.assertRaises(RuntimeError) as cm:
            self.generate("ServiceA", defs)
        self.assertIn("ServiceA.Frob parameter mode", str(cm.exception))
        self.assertIn("ServiceB", str(cm.exception))

    def test_value_the_enumeration_does_not_declare(self):
        with self.assertRaises(RuntimeError) as cm:
            self.generate("BadService", self.load("UndeclaredEnumValue"))
        self.assertIn("BadService.Frob parameter mode", str(cm.exception))
        self.assertIn("7 is not a valid Mode", str(cm.exception))
