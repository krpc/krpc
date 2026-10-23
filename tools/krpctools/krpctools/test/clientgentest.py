import json
import unittest
import warnings
from importlib.resources import files
from typing import TYPE_CHECKING

from ..clientgen.docparser import DocumentationMarkupWarning
from ..clientgen.generator import Generator
from ..definitions import Definitions

# A mixin, so that the base class is not collected as a test case of its own
if TYPE_CHECKING:
    _Base = unittest.TestCase
else:
    _Base = object


def _services(value):
    """The names of the services that definitions define or refer to"""
    if isinstance(value, list):
        return set().union(*map(_services, value))
    if not isinstance(value, dict):
        return set()
    names = set().union(*map(_services, value.values()))
    if isinstance(value.get("service"), str):
        names.add(value["service"])
    return names


def documented_enumeration(summary):
    """A service holding one enumeration, whose value has the given summary"""
    doc = "<doc><summary>%s</summary></doc>" % summary
    return {
        "BadService": {
            "id": 1,
            "documentation": "",
            "procedures": {},
            "classes": {},
            "enumerations": {
                "Mode": {
                    "documentation": "",
                    "values": [{"name": "A", "value": 0, "documentation": doc}],
                }
            },
            "exceptions": {},
            "structs": {},
        }
    }


class ClientGenTestCase(_Base):
    language: str
    generator: type[Generator]

    def generate(self, service_name, defs, includes=None):
        macro_template = (
            files("krpctools.clientgen")
            .joinpath(self.language + ".tmpl")
            .read_text(encoding="utf-8")
        )
        if includes is None:
            includes = {name: name for name in _services(defs)}
        g = self.generator(macro_template, service_name, Definitions(defs), includes)
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

    def test_struct_of_the_same_name_in_another_service(self):
        # A field whose type is a structure of the same name in another service refers to
        # that one, not to the structure that holds it
        struct = {
            "documentation": "",
            "fields": [
                {
                    "name": "Other",
                    "type": {"code": "STRUCT", "service": "ServiceB", "name": "Thing"},
                    "documentation": "",
                }
            ],
        }
        service = {
            "id": 1,
            "documentation": "",
            "procedures": {},
            "classes": {},
            "enumerations": {},
            "exceptions": {},
            "structs": {"Thing": struct},
        }
        other = dict(
            service, id=2, structs={"Thing": {"documentation": "", "fields": []}}
        )
        self.generate("ServiceA", {"ServiceA": service, "ServiceB": other})

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

    def test_invalid_documentation_markup(self):
        # Invalid markup is rendered as text, with a warning for each problem
        with warnings.catch_warnings(record=True) as caught:
            warnings.simplefilter("always")
            actual = self.generate(
                "BadService",
                documented_enumeration(
                    "Rate in <unit>m/s^q</unit>, <math>\\foo{x}</math> and <bogus>y</bogus>."
                ),
            )
        self.assertIn("Rate in m/s^q, foo{x} and ", actual)
        messages = {str(w.message) for w in caught}
        self.assertTrue(all(w.category is DocumentationMarkupWarning for w in caught))
        problems = [
            "'q' has no Unicode superscript form",
            "unsupported command '\\foo'",
        ]
        # C# documentation keeps its XML, so an unknown tag reaches it as written
        if self.language != "csharp":
            self.assertIn("foo{x} and y.", actual)
            problems.append("unknown tag 'bogus'")
        for problem in problems:
            self.assertTrue(
                any(problem in message for message in messages), (problem, messages)
            )
