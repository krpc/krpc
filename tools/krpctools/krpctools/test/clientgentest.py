import json
from importlib.resources import files


class ClientGenTestCase:
    def run_test(self, service_name, name):
        macro_template = (
            files("krpctools.clientgen")
            .joinpath(self.language + ".tmpl")
            .read_text(encoding="utf-8")
        )
        defs = json.loads(
            files("krpctools.test").joinpath(name + ".json").read_text(encoding="utf-8")
        )
        g = self.generator(macro_template, service_name, defs[service_name])
        actual = g.generate()

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

    def test_empty(self):
        self.run_test("EmptyService", "Empty")

    def test_test_service(self):
        self.run_test("TestService", "TestService")
