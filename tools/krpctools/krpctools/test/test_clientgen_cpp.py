import unittest

from krpctools.clientgen.cpp import CppGenerator
from krpctools.test.clientgentest import ClientGenTestCase, bytes_default


class TestClientGenCpp(ClientGenTestCase, unittest.TestCase):
    language = "cpp"
    generator = CppGenerator

    def test_bytes_default(self):
        content = self.generate("ServiceA", bytes_default())
        self.assertIn('std::string value = std::string("\\xca\\xfe", 2)', content)

    def test_invoke_returns(self):
        # A nullable stream or event is invoked with the same Returns kind as a non-nullable one
        def procedure(pid, code, nullable):
            typ = {"code": code}
            if nullable:
                typ["nullable"] = True
            return {
                "id": pid,
                "parameters": [],
                "return_type": typ,
                "documentation": "",
            }

        service = {
            "id": 1,
            "documentation": "",
            "procedures": {
                "GetEvent": procedure(1, "EVENT", False),
                "GetNullableEvent": procedure(2, "EVENT", True),
                "GetStream": procedure(3, "STREAM", False),
                "GetNullableStream": procedure(4, "STREAM", True),
                "GetValue": procedure(5, "SINT32", True),
            },
            "classes": {},
            "enumerations": {},
            "exceptions": {},
        }
        actual = self.generate("ServiceA", {"ServiceA": service})
        for name, returns in (
            ("GetEvent", "event"),
            ("GetNullableEvent", "event"),
            ("GetStream", "stream"),
            ("GetNullableStream", "stream"),
            ("GetValue", "value"),
        ):
            self.assertIn(
                'invoke("ServiceA", "%s", {}, ::krpc::Client::Returns::%s);'
                % (name, returns),
                actual,
            )
        self.assertIn("std::optional<::krpc::Event>", actual)
        self.assertIn("std::optional<krpc::schema::Stream>", actual)

    def test_includes_the_services_it_uses(self):
        # ServiceA uses an enumeration of ServiceB only
        includes = {"ServiceB": "b/service_b.hpp", "ServiceC": "c/service_c.hpp"}
        actual = self.generate("ServiceA", self.load("Ordering"), includes)
        self.assertIn("#include <b/service_b.hpp>\n", actual)
        self.assertNotIn("service_c", actual)

    def test_service_whose_include_is_not_provided(self):
        with self.assertRaises(RuntimeError) as cm:
            self.generate("ServiceA", self.load("Ordering"), {})
        self.assertIn("ServiceB", str(cm.exception))


if __name__ == "__main__":
    unittest.main()
