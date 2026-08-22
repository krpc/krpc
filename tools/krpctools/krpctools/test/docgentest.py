import json
import tempfile
from importlib.resources import files
from ..definitions import Definitions
from ..docgen.nodes import Service
from ..docgen import process_file


class DocGenTestCase:
    @staticmethod
    def load(name):
        return json.loads(
            files("krpctools.test").joinpath(name + ".json").read_text(encoding="utf-8")
        )

    def generate(self, service_name, defs):
        def sort(member):
            return member.fullname

        def parse_service_info(info):
            del info["id"]
            keys = (
                "procedures",
                "classes",
                "enumerations",
                "exceptions",
                "documentation",
            )
            for key in keys:
                if key not in info:
                    if key == "documentation":
                        value = ""
                    else:
                        value = {}
                    info[key] = value
            return info

        definitions = Definitions(defs)

        services = {
            name: Service(
                name, sort=sort, types=definitions.types, **parse_service_info(info)
            )
            for name, info in defs.items()
        }

        rst_content = [
            ".. default-domain:: {{ domain.sphinxname }}",
            ".. highlight:: {{ domain.highlight }}",
            "",
            "{{ domain.currentmodule('%s') }}" % service_name,
            "{% import domain.macros as macros with context %}",
            "",
            "{{ macros.service(services['%s']) }}" % service_name,
        ]
        for cls in defs[service_name]["classes"].keys():
            rst_content.append(
                "{{ macros.class(services['%s'].classes['%s']) }}" % (service_name, cls)
            )
        for enm in defs[service_name]["enumerations"].keys():
            rst_content.append(
                "{{ macros.enumeration(services['%s'].enumerations['%s']) }}"
                % (service_name, enm)
            )
        for struct in defs[service_name].get("structs", {}).keys():
            rst_content.append(
                "{{ macros.struct(services['%s'].structs['%s']) }}"
                % (service_name, struct)
            )
        for exn in defs[service_name]["exceptions"].keys():
            rst_content.append(
                "{{ macros.exception(services['%s'].exceptions['%s']) }}"
                % (service_name, exn)
            )

        path = tempfile.mktemp()
        with open(path, "w") as fp:
            fp.write("\n".join(rst_content))

        macros = str(files("krpctools.docgen").joinpath("%s.tmpl" % self.language))
        domain = self.domain(macros)

        content, _ = process_file(domain, services, path)
        return content

    def run_test(self, service_name, name):
        actual = self.generate(service_name, self.load(name))

        # with open('/home/alex/workspaces/krpc/krpc/' +
        #           'tools/krpctools/krpctools/test/' +
        #           'docgen-'+name+'-'+self.language+'.rst', 'w') as f:
        #     f.write(actual)

        expected = (
            files("krpctools.test")
            .joinpath("docgen-" + name + "-" + self.language + ".rst")
            .read_text(encoding="utf-8")
        )
        self.assertEqual(expected, actual)

        # A service may be defined after one that uses its types, so the definitions in the
        # reverse order have to generate the same thing
        defs = self.load(name)
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
