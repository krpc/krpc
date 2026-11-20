"""Tests of the HTTP server, through krpc_http, against a TestServer."""

import http.client
import json
import os
import socket
import unittest

from krpc_http import Client, Error
from openapi_spec_validator import validate

PORT = int(os.environ.get("KRPC_TEST_HTTP_PORT", "50003"))
SERVICE = "/test-service"


class TestCase(unittest.TestCase):
    def setUp(self) -> None:
        self.client = Client(port=PORT)

    def tearDown(self) -> None:
        self.client.close()

    def assert_error(self, status: int, method: str, path: str, body=None) -> Error:
        with self.assertRaises(Error) as context:
            self.client.call(method, path, body)
        self.assertEqual(status, context.exception.status)
        return context.exception

    def post_text(self, path: str, text: str) -> tuple[int, dict]:
        """POST a body as written, for a number Python's json does not write."""
        connection = http.client.HTTPConnection("127.0.0.1", PORT, timeout=30)
        try:
            connection.request(
                "POST", path, text.encode("utf-8"), {"Content-Type": "application/json"}
            )
            response = connection.getresponse()
            return response.status, json.loads(response.read())
        finally:
            connection.close()


class TestHealthCheck(unittest.TestCase):
    def test_root(self) -> None:
        connection = http.client.HTTPConnection("127.0.0.1", PORT, timeout=30)
        try:
            connection.request("GET", "/")
            response = connection.getresponse()
            self.assertEqual(200, response.status)
            self.assertEqual("0", response.getheader("Content-Length"))
            self.assertEqual(b"", response.read())
            # The connection stays open for a further request
            connection.request("GET", "/")
            self.assertEqual(200, connection.getresponse().status)
        finally:
            connection.close()


class TestOpenApi(TestCase):
    def document(self) -> dict:
        status, headers, document = self.client.request("GET", "/openapi.json")
        self.assertEqual(200, status)
        self.assertEqual("application/json; charset=utf-8", headers["Content-Type"])
        return document

    def test_valid(self) -> None:
        validate(self.document())

    def test_streams_and_expressions_have_no_path(self) -> None:
        operations = [
            operation["operationId"]
            for methods in self.document()["paths"].values()
            for operation in methods.values()
        ]
        self.assertIn("KRPC_GetStatus", operations)
        self.assertNotIn("KRPC_AddStream", operations)
        self.assertNotIn("KRPC_GetServices", operations)
        # Nothing runs a server side function over HTTP, so expressions have no paths
        self.assertEqual(
            [],
            [x for x in operations if x.startswith(("KRPC_Expression_", "KRPC_Type_"))],
        )

    def test_class_property(self) -> None:
        path = self.document()["paths"]["/test-service/test-class/{id}/int-property"]
        get = path["get"]
        self.assertEqual("TestService_TestClass_get_IntProperty", get["operationId"])
        self.assertEqual(["TestService"], get["tags"])
        self.assertEqual("id", get["parameters"][0]["name"])
        self.assertEqual(
            {"$ref": "#/components/schemas/TestService.TestClass"},
            get["parameters"][0]["schema"],
        )
        self.assertEqual(
            {"type": "integer", "format": "int32"},
            get["responses"]["200"]["content"]["application/json"]["schema"][
                "properties"
            ]["value"],
        )
        body = path["put"]["requestBody"]["content"]["application/json"]["schema"]
        self.assertEqual(["value"], body["required"])

    def test_optional_parameters(self) -> None:
        post = self.document()["paths"]["/test-service/optional-arguments"]["post"]
        body = post["requestBody"]["content"]["application/json"]["schema"]
        self.assertEqual(["x", "y", "z", "obj"], list(body["properties"]))
        self.assertEqual(["x"], body["required"])
        self.assertEqual(
            {
                "allOf": [{"$ref": "#/components/schemas/TestService.TestClass"}],
                "nullable": True,
            },
            body["properties"]["obj"],
        )

    def test_types(self) -> None:
        schemas = self.document()["components"]["schemas"]
        self.assertEqual(
            ["value_a", "value_b", "value_c"], schemas["TestService.TestEnum"]["enum"]
        )
        self.assertEqual("array", schemas["TestService.TestFlags"]["type"])
        struct = schemas["TestService.TestStruct"]
        self.assertEqual(
            ["int_field", "string_field", "enum_field", "list_field"],
            struct["required"],
        )
        self.assertNotIn("KRPC.Expression", schemas)


class TestCalls(TestCase):
    def test_service_property(self) -> None:
        self.client.put(SERVICE + "/string-property", "foo")
        self.assertEqual("foo", self.client.get(SERVICE + "/string-property"))

    def test_procedure(self) -> None:
        self.assertEqual(
            "3.14159",
            self.client.post(SERVICE + "/float-to-string", value=3.14159),
        )

    def test_named_arguments_and_defaults(self) -> None:
        path = SERVICE + "/optional-arguments"
        self.assertEqual("xfoobarnull", self.client.post(path, x="x"))
        self.assertEqual("xfoobaznull", self.client.post(path, x="x", z="baz"))

    def test_missing_argument(self) -> None:
        error = self.assert_error(400, "POST", SERVICE + "/optional-arguments", {})
        self.assertIn("x", error.description)

    def test_unknown_argument(self) -> None:
        self.assert_error(
            400, "POST", SERVICE + "/optional-arguments", {"x": "x", "w": "w"}
        )

    def test_names_in_snake_case(self) -> None:
        self.assertEqual(
            "foo@bar",
            self.client.post(
                SERVICE + "/join-with-at-sign", first_value="foo", **{"from": "bar"}
            ),
        )

    def test_no_return_value(self) -> None:
        status, _, content = self.client.request(
            "PUT", SERVICE + "/string-property", {"value": "foo"}
        )
        self.assertEqual(200, status)
        self.assertEqual({}, content)


class TestValues(TestCase):
    def test_integers(self) -> None:
        self.assertEqual(
            "-42", self.client.post(SERVICE + "/int32-to-string", value=-42)
        )
        self.assertEqual(
            "18446744073709551615",
            self.client.post(SERVICE + "/uint64-to-string", value=18446744073709551615),
        )
        self.assertEqual(
            18446744073709551615,
            self.client.post(
                SERVICE + "/string-to-uint64", value="18446744073709551615"
            ),
        )

    def test_large_integer(self) -> None:
        self.assertEqual(
            "9007199254740993",
            self.client.post(SERVICE + "/int64-to-string", value=9007199254740993),
        )

    def test_integer_as_string(self) -> None:
        self.assert_error(
            400, "POST", SERVICE + "/int64-to-string", {"value": "9007199254740993"}
        )

    def test_integer_out_of_range(self) -> None:
        self.assert_error(400, "POST", SERVICE + "/int32-to-string", {"value": 2**31})
        self.assert_error(400, "POST", SERVICE + "/uint32-to-string", {"value": -1})

    def test_integer_with_fraction(self) -> None:
        self.assert_error(400, "POST", SERVICE + "/int32-to-string", {"value": 1.5})
        error = self.assert_error(
            400, "POST", SERVICE + "/int32-to-string", {"value": 1e-30}
        )
        self.assertIn("must be an integer", error.description)
        self.assertEqual(
            (200, {"value": "1"}),
            self.post_text(SERVICE + "/int32-to-string", '{"value": 100e-2}'),
        )

    def test_integer_exponent_out_of_range(self) -> None:
        status, content = self.post_text(
            SERVICE + "/int64-to-string", '{"value": 1e30}'
        )
        self.assertEqual(400, status)
        self.assertIn("out of range", content["error"]["description"])

    def test_double(self) -> None:
        self.assertEqual(
            0.1, self.client.post(SERVICE + "/string-to-double", value="0.1")
        )
        self.assertEqual(
            "NaN", self.client.post(SERVICE + "/string-to-double", value="NaN")
        )

    def test_float(self) -> None:
        self.assertEqual(
            0.1, self.client.post(SERVICE + "/string-to-float", value="0.1")
        )
        error = self.assert_error(
            400, "POST", SERVICE + "/float-to-string", {"value": 1e39}
        )
        self.assertIn("out of range", error.description)

    def test_bool(self) -> None:
        self.assertEqual(
            "True", self.client.post(SERVICE + "/bool-to-string", value=True)
        )
        self.assert_error(400, "POST", SERVICE + "/bool-to-string", {"value": 1})

    def test_bytes(self) -> None:
        self.assertEqual(
            "deadbeef",
            self.client.post(SERVICE + "/bytes-to-hex-string", value="3q2+7w=="),
        )
        self.assertEqual(
            "3q2+7w==",
            self.client.post(SERVICE + "/hex-string-to-bytes", value="deadbeef"),
        )

    def test_string(self) -> None:
        self.assertEqual(
            "héllo ☃",
            self.client.post(SERVICE + "/echo-nullable-string", value="héllo ☃"),
        )

    def test_null(self) -> None:
        self.assertIsNone(self.client.post(SERVICE + "/echo-nullable-int", value=None))
        self.assertIsNone(
            self.client.post(SERVICE + "/echo-nullable-string", value=None)
        )
        self.assert_error(400, "POST", SERVICE + "/int32-to-string", {"value": None})

    def test_nullable_items(self) -> None:
        self.assertEqual(
            [1, None, 3],
            self.client.post(SERVICE + "/echo-list-of-nullable-ints", l=[1, None, 3]),
        )

    def test_enumeration(self) -> None:
        self.assertEqual(
            "value_b", self.client.post(SERVICE + "/enum-echo", x="value_b")
        )
        self.assertEqual("value_c", self.client.post(SERVICE + "/enum-default"))
        error = self.assert_error(400, "POST", SERVICE + "/enum-echo", {"x": 1})
        self.assertIn("must be a string", error.description)
        error = self.assert_error(400, "POST", SERVICE + "/enum-echo", {"x": "ValueB"})
        self.assertIn("value_a, value_b, value_c", error.description)

    def test_flags(self) -> None:
        self.assertEqual(
            ["value_a", "value_c"], self.client.post(SERVICE + "/flags-default")
        )
        self.assertEqual(
            ["value_b"], self.client.post(SERVICE + "/flags-echo", x=["value_b"])
        )
        self.assertEqual([], self.client.post(SERVICE + "/flags-echo", x=[]))

    def test_list(self) -> None:
        self.assertEqual(
            [1, 2, 3], self.client.post(SERVICE + "/increment-list", l=[0, 1, 2])
        )
        self.assertEqual(
            [1, None],
            self.client.post(SERVICE + "/echo-list-of-nullable-ints", l=[1, None]),
        )

    def test_list_after_an_invalid_element(self) -> None:
        path = SERVICE + "/echo-list-of-nullable-ints"
        self.assert_error(400, "POST", path, {"l": [1, "x"]})
        self.assertEqual([2, None], self.client.post(path, l=[2, None]))

    def test_set(self) -> None:
        self.assertEqual(
            [1, 2, 3], sorted(self.client.post(SERVICE + "/increment-set", h=[0, 1, 2]))
        )

    def test_tuple(self) -> None:
        self.assertEqual(
            [2, 3], self.client.post(SERVICE + "/increment-tuple", t=[1, 2])
        )
        self.assert_error(400, "POST", SERVICE + "/increment-tuple", {"t": [1]})

    def test_dictionary(self) -> None:
        self.assertEqual(
            {"a": 1, "b": 2},
            self.client.post(SERVICE + "/increment-dictionary", d={"a": 0, "b": 1}),
        )
        self.assertEqual(
            {"a": [1, 2]},
            self.client.post(SERVICE + "/increment-nested-collection", d={"a": [0, 1]}),
        )

    def test_dictionary_with_other_keys(self) -> None:
        self.assertEqual(
            [[1, False], [2, True]],
            self.client.post(SERVICE + "/dictionary-default"),
        )
        self.assertEqual(
            [[3, True]],
            self.client.post(SERVICE + "/dictionary-default", x=[[3, True]]),
        )

    def test_structure(self) -> None:
        value = {
            "int_field": 42,
            "string_field": "foo",
            "enum_field": "value_b",
            "list_field": [1, 2],
        }
        self.assertEqual(value, self.client.post(SERVICE + "/struct-echo", x=value))
        error = self.assert_error(
            400, "POST", SERVICE + "/struct-echo", {"x": {"int_field": 42}}
        )
        self.assertIn("string_field", error.description)


class TestObjects(TestCase):
    def create(self, value: str) -> int:
        return self.client.post(SERVICE + "/create-test-object", value=value)

    def test_method(self) -> None:
        obj = self.create("jeb")
        self.assertIsInstance(obj, int)
        self.assertEqual(
            "value=jeb", self.client.post("%s/test-class/%d/get-value" % (SERVICE, obj))
        )

    def test_property(self) -> None:
        path = "%s/test-class/%d/int-property" % (SERVICE, self.create("bill"))
        self.client.put(path, 42)
        self.assertEqual(42, self.client.get(path))

    def test_object_argument(self) -> None:
        obj = self.create("bob")
        self.assertEqual(
            obj, self.client.post(SERVICE + "/echo-test-object", value=obj)
        )
        self.assertIsNone(self.client.post(SERVICE + "/echo-test-object", value=None))

    def test_object_persists_between_requests(self) -> None:
        obj = self.create("val")
        self.client.close()
        self.client = Client(port=PORT)
        self.assertEqual(
            "value=val", self.client.post("%s/test-class/%d/get-value" % (SERVICE, obj))
        )

    def test_static_method(self) -> None:
        self.assertEqual(
            "staticab",
            self.client.post(SERVICE + "/test-class/static-method", a="a", b="b"),
        )

    def test_invalid_object_id(self) -> None:
        self.assert_error(404, "POST", SERVICE + "/test-class/abc/get-value")


class TestSession(TestCase):
    def test_one_client(self) -> None:
        clients = self.client.get("/krpc/clients")
        self.assertEqual(1, len(clients))
        self.assertEqual("HTTP", clients[0]["name"])
        other = Client(port=PORT)
        try:
            self.assertEqual(clients, other.get("/krpc/clients"))
        finally:
            other.close()


class TestErrors(TestCase):
    def test_argument_exception(self) -> None:
        error = self.assert_error(400, "POST", SERVICE + "/throw-argument-exception")
        self.assertEqual("KRPC", error.service)
        self.assertEqual("ArgumentException", error.name)

    def test_invalid_operation(self) -> None:
        error = self.assert_error(
            500, "POST", SERVICE + "/throw-invalid-operation-exception"
        )
        self.assertEqual("InvalidOperationException", error.name)

    def test_unknown_path(self) -> None:
        self.assert_error(404, "GET", SERVICE + "/no-such-property")
        self.assert_error(404, "GET", "/no-such-service/foo")

    def test_wrong_method(self) -> None:
        status, headers, _ = self.client.request("POST", SERVICE + "/string-property")
        self.assertEqual(405, status)
        self.assertEqual("GET, PUT", headers["Allow"])
        status, _, _ = self.client.request("GET", SERVICE + "/float-to-string")
        self.assertEqual(405, status)

    def test_content_type(self) -> None:
        status, _, _ = self.client.request(
            "POST",
            SERVICE + "/float-to-string",
            {"value": 1},
            {"Content-Type": "text/plain"},
        )
        self.assertEqual(415, status)

    def test_malformed_json(self) -> None:
        self.client.connection.request(
            "POST",
            SERVICE + "/float-to-string",
            b"{value: 1}",
            {"Content-Type": "application/json"},
        )
        response = self.client.connection.getresponse()
        response.read()
        self.assertEqual(400, response.status)

    def test_body_not_an_object(self) -> None:
        self.assert_error(400, "POST", SERVICE + "/float-to-string", [1])

    def test_host(self) -> None:
        status, _, _ = self.client.request(
            "GET", SERVICE + "/string-property", headers={"Host": "example.com"}
        )
        self.assertEqual(403, status)
        status, _, _ = self.client.request(
            "GET", SERVICE + "/string-property", headers={"Host": "localhost:%d" % PORT}
        )
        self.assertEqual(200, status)

    def test_origin(self) -> None:
        status, _, _ = self.client.request(
            "GET",
            SERVICE + "/string-property",
            headers={"Origin": "http://example.com"},
        )
        self.assertEqual(403, status)


# The origins the server is started with, in client/http/BUILD.bazel
ALLOWED = "http://allowed.example"


class TestOrigins(unittest.TestCase):
    """The checks on a request from a web page, which names its origin"""

    def exchange(
        self, method: str, path: str, headers: dict[str, str], body: bytes | None = None
    ) -> tuple[int, dict[str, str], bytes]:
        connection = http.client.HTTPConnection("127.0.0.1", PORT, timeout=30)
        try:
            connection.request(method, path, body, headers)
            response = connection.getresponse()
            return response.status, dict(response.getheaders()), response.read()
        finally:
            connection.close()

    def test_same_origin(self) -> None:
        host = "127.0.0.1:%d" % PORT
        status, headers, _ = self.exchange(
            "GET", "/krpc/clients", {"Host": host, "Origin": "http://" + host}
        )
        self.assertEqual(200, status)
        self.assertNotIn("Access-Control-Allow-Origin", headers)

    def test_refused_origin(self) -> None:
        status, headers, _ = self.exchange(
            "GET", "/krpc/clients", {"Origin": "http://other.example"}
        )
        self.assertEqual(403, status)
        self.assertNotIn("Access-Control-Allow-Origin", headers)

    def test_allowed_origin(self) -> None:
        status, headers, _ = self.exchange("GET", "/krpc/clients", {"Origin": ALLOWED})
        self.assertEqual(200, status)
        self.assertEqual(ALLOWED, headers["Access-Control-Allow-Origin"])
        self.assertEqual("Origin", headers["Vary"])

    def test_allowed_origin_with_a_port(self) -> None:
        origin = "http://localhost:8080"
        status, headers, _ = self.exchange("GET", "/krpc/clients", {"Origin": origin})
        self.assertEqual(200, status)
        self.assertEqual(origin, headers["Access-Control-Allow-Origin"])
        status, _, _ = self.exchange(
            "GET", "/krpc/clients", {"Origin": "http://localhost:8081"}
        )
        self.assertEqual(403, status)

    def test_error_names_the_origin(self) -> None:
        status, headers, _ = self.exchange("GET", "/no-such-path", {"Origin": ALLOWED})
        self.assertEqual(404, status)
        self.assertEqual(ALLOWED, headers["Access-Control-Allow-Origin"])

    def test_null_origin(self) -> None:
        status, _, _ = self.exchange("GET", "/krpc/clients", {"Origin": "null"})
        self.assertEqual(403, status)

    def test_preflight(self) -> None:
        status, headers, body = self.exchange(
            "OPTIONS",
            SERVICE + "/string-property",
            {
                "Origin": ALLOWED,
                "Access-Control-Request-Method": "PUT",
                "Access-Control-Request-Headers": "content-type",
            },
        )
        self.assertEqual(204, status)
        self.assertEqual(b"", body)
        self.assertNotIn("Content-Length", headers)
        self.assertEqual(ALLOWED, headers["Access-Control-Allow-Origin"])
        self.assertEqual("GET, PUT", headers["Access-Control-Allow-Methods"])
        self.assertEqual("Content-Type", headers["Access-Control-Allow-Headers"])
        self.assertEqual("600", headers["Access-Control-Max-Age"])

    def test_preflight_from_a_refused_origin(self) -> None:
        status, headers, _ = self.exchange(
            "OPTIONS",
            SERVICE + "/string-property",
            {"Origin": "http://other.example", "Access-Control-Request-Method": "PUT"},
        )
        self.assertEqual(403, status)
        self.assertNotIn("Access-Control-Allow-Origin", headers)

    def test_preflight_of_documents_and_objects(self) -> None:
        for path, methods in (
            ("/openapi.json", "GET"),
            ("/", "GET"),
            (SERVICE + "/test-class/1/get-value", "POST"),
        ):
            status, headers, _ = self.exchange("OPTIONS", path, {"Origin": ALLOWED})
            self.assertEqual(204, status, path)
            self.assertEqual(methods, headers["Access-Control-Allow-Methods"], path)

    def test_preflight_of_an_unknown_path(self) -> None:
        status, _, _ = self.exchange("OPTIONS", "/no-such-path", {"Origin": ALLOWED})
        self.assertEqual(404, status)

    def test_options_without_an_origin(self) -> None:
        status, headers, _ = self.exchange("OPTIONS", SERVICE + "/string-property", {})
        self.assertEqual(204, status)
        self.assertEqual("GET, PUT", headers["Allow"])

    def test_cross_origin_call(self) -> None:
        status, headers, body = self.exchange(
            "POST",
            SERVICE + "/int32-to-string",
            {"Origin": ALLOWED, "Content-Type": "application/json"},
            b'{"value": 42}',
        )
        self.assertEqual(200, status)
        self.assertEqual(ALLOWED, headers["Access-Control-Allow-Origin"])
        self.assertEqual({"value": "42"}, json.loads(body))

    def test_machine_name(self) -> None:
        for name in (socket.gethostname(), socket.gethostname() + ".local"):
            status, _, _ = self.exchange(
                "GET", "/krpc/clients", {"Host": "%s:%d" % (name, PORT)}
            )
            self.assertEqual(200, status, name)

    def test_openapi_server_url(self) -> None:
        for host in ("127.0.0.1:%d" % PORT, "localhost:%d" % PORT):
            _, _, body = self.exchange("GET", "/openapi.json", {"Host": host})
            self.assertEqual([{"url": "http://" + host}], json.loads(body)["servers"])


class TestConnection(unittest.TestCase):
    def exchange(self, data: bytes) -> bytes:
        with socket.create_connection(("127.0.0.1", PORT), timeout=30) as sock:
            sock.sendall(data)
            received = b""
            while True:
                chunk = sock.recv(65536)
                if not chunk:
                    return received
                received += chunk

    def test_pipelined_requests_answer_in_order(self) -> None:
        request = (
            "POST %s/int32-to-string HTTP/1.1\r\nHost: 127.0.0.1\r\n"
            "Content-Type: application/json\r\nContent-Length: %d\r\n%s\r\n%s"
        )
        data = b""
        for value, last in ((1, False), (2, False), (3, True)):
            body = '{"value": %d}' % value
            close = "Connection: close\r\n" if last else ""
            data += (request % (SERVICE, len(body), close, body)).encode("ascii")
        received = self.exchange(data).decode("utf-8")
        self.assertEqual(3, received.count("HTTP/1.1 200 OK"))
        self.assertLess(received.index('"1"'), received.index('"2"'))
        self.assertLess(received.index('"2"'), received.index('"3"'))

    def post(self, path: str, body: str) -> str:
        request = (
            "POST %s HTTP/1.1\r\nHost: 127.0.0.1\r\nConnection: close\r\n"
            "Content-Type: application/json\r\nContent-Length: %d\r\n\r\n%s"
        )
        data = (request % (path, len(body), body)).encode("ascii")
        return self.exchange(data).decode("utf-8")

    def test_double_out_of_range(self) -> None:
        received = self.post(SERVICE + "/double-to-string", '{"value": 1e400}')
        self.assertTrue(received.startswith("HTTP/1.1 400 Bad Request"), received)
        self.assertIn("out of range", received)

    def test_lone_surrogate(self) -> None:
        received = self.post(SERVICE + "/echo-nullable-string", '{"value": "\\ud800"}')
        self.assertTrue(received.startswith("HTTP/1.1 400 Bad Request"), received)

    def test_header_too_large(self) -> None:
        header = "X-Padding: %s\r\n" % ("a" * (17 * 1024))
        data = (
            "GET /krpc/clients HTTP/1.1\r\nHost: 127.0.0.1\r\n%s\r\n" % header
        ).encode("ascii")
        received = self.exchange(data).decode("utf-8")
        self.assertTrue(received.startswith("HTTP/1.1 431"), received)

    def test_body_too_large(self) -> None:
        # The server reads and discards a body it refuses, so the client can send it all
        size = 17 * 1024 * 1024
        head = (
            "POST %s/int32-to-string HTTP/1.1\r\nHost: 127.0.0.1\r\n"
            "Content-Type: application/json\r\nContent-Length: %d\r\n\r\n"
            % (SERVICE, size)
        )
        received = self.exchange(head.encode("ascii") + b" " * size).decode("utf-8")
        self.assertTrue(received.startswith("HTTP/1.1 413"), received)

    def test_transfer_encoding(self) -> None:
        received = self.exchange(
            b"POST /krpc/get-status HTTP/1.1\r\nHost: 127.0.0.1\r\n"
            b"Transfer-Encoding: chunked\r\n\r\n0\r\n\r\n"
        ).decode("utf-8")
        self.assertTrue(received.startswith("HTTP/1.1 501"), received)

    def test_http_version(self) -> None:
        received = self.exchange(
            b"GET /krpc/clients HTTP/2.0\r\nHost: 127.0.0.1\r\n\r\n"
        ).decode("utf-8")
        self.assertTrue(received.startswith("HTTP/1.1 505"), received)

    def test_bare_line_feed_in_a_header(self) -> None:
        received = self.exchange(
            b"GET /krpc/clients HTTP/1.1\r\nHost: 127.0.0.1\r\n"
            b"Origin: http://a\nX-Injected: 1\r\n\r\n"
        ).decode("utf-8")
        self.assertTrue(received.startswith("HTTP/1.1 400"), received)

    def test_connection_close(self) -> None:
        request = "GET /krpc/clients HTTP/1.1\r\nHost: 127.0.0.1\r\n%s\r\n"
        received = self.exchange(
            (request % "Connection: close\r\n" + request % "").encode("ascii")
        ).decode("utf-8")
        self.assertEqual(1, received.count("HTTP/1.1 200 OK"))
        self.assertIn("Connection: close", received)

    def test_http_1_0_keep_alive(self) -> None:
        request = "GET /krpc/clients HTTP/1.0\r\nHost: 127.0.0.1\r\n%s\r\n"
        keep_alive = request % "Connection: keep-alive\r\n"
        received = self.exchange(
            (keep_alive + request % "" + request % "").encode("ascii")
        ).decode("utf-8")
        self.assertEqual(2, received.count("HTTP/1.1 200 OK"))

    def test_expect_continue(self) -> None:
        body = b'{"value": 7}'
        head = (
            "POST %s/int32-to-string HTTP/1.1\r\nHost: 127.0.0.1\r\n"
            "Content-Type: application/json\r\nContent-Length: %d\r\n"
            "Expect: 100-continue\r\nConnection: close\r\n\r\n" % (SERVICE, len(body))
        )
        with socket.create_connection(("127.0.0.1", PORT), timeout=5) as sock:
            sock.sendall(head.encode("ascii"))
            interim = b""
            while not interim.endswith(b"\r\n\r\n"):
                interim += sock.recv(1)
            self.assertEqual(b"HTTP/1.1 100 Continue\r\n\r\n", interim)
            sock.sendall(body)
            received = b""
            while chunk := sock.recv(65536):
                received += chunk
        self.assertTrue(received.startswith(b"HTTP/1.1 200 OK"), received)
        self.assertIn(b'"7"', received)

    def test_many_pipelined_requests(self) -> None:
        request = (
            "POST %s/int32-to-string HTTP/1.1\r\nHost: 127.0.0.1\r\n"
            "Content-Type: application/json\r\nContent-Length: %d\r\n%s\r\n%s"
        )
        data = b""
        count = 200
        for value in range(count):
            body = '{"value": %d}' % value
            close = "Connection: close\r\n" if value == count - 1 else ""
            data += (request % (SERVICE, len(body), close, body)).encode("ascii")
        received = self.exchange(data).decode("utf-8")
        values = [
            json.loads(part.split("\r\n\r\n", 1)[1])["value"]
            for part in received.split("HTTP/1.1 ")[1:]
        ]
        self.assertEqual([str(x) for x in range(count)], values)

    def test_malformed_request_closes_the_connection(self) -> None:
        received = self.exchange(b"NOT HTTP\r\n\r\n").decode("utf-8")
        self.assertTrue(received.startswith("HTTP/1.1 400 Bad Request"))
        self.assertIn("Connection: close", received)


if __name__ == "__main__":
    unittest.main()
