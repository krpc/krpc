import unittest

from krpctools.clientgen.cpp import CppGenerator
from krpctools.test.clientgentest import ClientGenTestCase


class TestClientGenCpp(ClientGenTestCase, unittest.TestCase):
    language = "cpp"
    generator = CppGenerator

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


if __name__ == "__main__":
    unittest.main()
