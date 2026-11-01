import json
import unittest
from importlib.resources import files

from ..clientgen.dispatch import (
    CnanoDispatchGenerator,
    CppDispatchGenerator,
    CsharpDispatchGenerator,
    JavaDispatchGenerator,
)
from ..definitions import Definitions


def generate(language, generator, service):
    template = (
        files("krpctools.clientgen")
        .joinpath(language + "-dispatch.tmpl")
        .read_text(encoding="utf-8")
    )
    defs = {}
    for name in ("KRPC", "TestService"):
        defs.update(
            json.loads(
                files("krpctools.test")
                .joinpath(name + ".json")
                .read_text(encoding="utf-8")
            )
        )
    return generator(template, service, Definitions(defs)).generate()


class TestClientGenDispatch(unittest.TestCase):
    def test_csharp(self):
        content = generate("csharp", CsharpDispatchGenerator, "TestService")
        self.assertIn(
            "return Result.String (connection.TestService ().FloatToString (a0));",
            content,
        )
        self.assertIn("connection.TestService ().StringProperty = a0;", content)
        self.assertIn(
            "var a0 = Arg.Object<global::KRPC.Client.Services.TestService.TestClass>"
            " (args [0]);",
            content,
        )
        self.assertIn("return Result.String (a0.GetValue ());", content)
        self.assertIn(
            "global::KRPC.Client.Services.TestService.TestClass.StaticMethod"
            " (connection, a0, a1)",
            content,
        )
        self.assertIn(
            "return Result.Stream<int> (connection.AddStream (() =>"
            " connection.TestService ().Counter (a0, a1)), Result.Sint32);",
            content,
        )
        self.assertIn(
            "return Connection.GetCall (() => connection.TestService ().Counter (a0, a1));",
            content,
        )
        self.assertIn(
            "var a0 = Arg.List (Arg.Nullable (Arg.Sint32)) (args [0]);", content
        )
        self.assertIn(
            "return Result.Dictionary<string,global::System.Collections.Generic.IList<int>>"
            " (Result.String, Result.List<int> (Result.Sint32))"
            " (connection.TestService ().IncrementNestedCollection (a0));",
            content,
        )

    def test_java(self):
        content = generate("java", JavaDispatchGenerator, "TestService")
        self.assertIn(
            "return Values.<String>result(Values::resultString,"
            " TestService.newInstance(connection).floatToString(a0));",
            content,
        )
        self.assertIn(
            "TestService.newInstance(connection).setStringProperty(a0);", content
        )
        self.assertIn(
            "return Values.<String>result(Values::resultString, a0.getValue());",
            content,
        )
        self.assertIn(
            'new StreamHandle(connection.addStream(TestService.class, "counter", a0, a1),'
            " x -> Values.<Integer>result(Values::resultSint32, (Integer) x));",
            content,
        )
        self.assertIn(
            'return connection.getCall(TestService.class, "counter", a0, a1);', content
        )
        self.assertIn(
            "return Values.<java.util.List<Integer>>result("
            "Values.<Integer>resultList(Values.resultNullable(Values::resultSint32)),"
            " TestService.newInstance(connection).echoListOfNullableInts(a0));",
            content,
        )
        # The stub of a static method takes the connection first
        self.assertIn(
            "connection.addStream(krpc.client.services.TestService.TestClass.class,"
            ' "staticMethod", connection, a0, a1)',
            content,
        )

    def test_cpp(self):
        content = generate("cpp", CppDispatchGenerator, "TestService")
        self.assertIn(
            "return String::result(::krpc::services::TestService(&client)"
            ".float_to_string(a0));",
            content,
        )
        self.assertIn(
            "auto a0 = Optional<Object<::krpc::services::TestService::TestClass>>"
            "::arg(client, args.Get(0));",
            content,
        )
        self.assertIn(
            "return result_stream(::krpc::services::TestService(&client)"
            ".counter_stream(a0, a1),",
            content,
        )
        self.assertIn(
            "return ::krpc::services::TestService(&client).counter_call(a0, a1);",
            content,
        )
        self.assertIn("struct TestService_TestStruct {", content)
        self.assertIn(
            "dynamic_cast<const ::krpc::services::TestService::CustomException*>(&exn)",
            content,
        )

    def test_cnano(self):
        content = generate("cnano", CnanoDispatchGenerator, "TestService")
        self.assertIn(
            "check(krpc_TestService_FloatToString(connection, &result, a0));", content
        )
        self.assertIn(
            "check(krpc_TestService_set_StringProperty(connection, a0));", content
        )
        self.assertIn(
            "check(krpc_TestService_IncrementList(connection, &result, &a0));", content
        )
        # A nullable scalar is passed by pointer, and returned with a null flag
        self.assertIn(
            "check(krpc_TestService_EchoNullableInt(connection, &result,"
            " &result_is_null, a0 ? &*a0 : nullptr));",
            content,
        )
        self.assertIn("struct tuple_int32_int64 {", content)

    def test_a_call_for_each_number_of_arguments(self):
        # The stub fills in the defaults of the arguments left off the end
        content = generate("csharp", CsharpDispatchGenerator, "TestService")
        for count in (1, 2, 3, 4):
            self.assertIn(
                "if (args.Count == %d) {" % count,
                content.split('"OptionalArguments"')[1],
            )
        content = generate("java", JavaDispatchGenerator, "TestService")
        section = content.split('case "OptionalArguments"')[1].split("case ")[0]
        self.assertEqual(1, section.count("if (args.size() =="))

    def test_struct_conversion_generated_once(self):
        content = generate("csharp", CsharpDispatchGenerator, "TestService")
        self.assertEqual(
            1,
            content.count(
                "internal static global::KRPC.Client.Services.TestService.TestStruct"
                " Arg_TestService_TestStruct (Value value)"
            ),
        )

    def test_event(self):
        # An event is returned as a handle, and the procedure is not streamed
        content = generate("csharp", CsharpDispatchGenerator, "TestService")
        self.assertIn(
            "return Result.Event (connection.TestService ().OnTimer (a0, a1));", content
        )
        self.assertNotIn("AddStream (() => connection.TestService ().OnTimer", content)
        content = generate("java", JavaDispatchGenerator, "TestService")
        self.assertIn("Values::resultEvent", content)
        content = generate("cpp", CppDispatchGenerator, "TestService")
        self.assertIn("EventHandle::result(", content)
        self.assertNotIn("on_timer_stream(", content)
        content = generate("cnano", CnanoDispatchGenerator, "TestService")
        self.assertNotIn('"OnTimer"', content)

    def test_krpc(self):
        content = generate("csharp", CsharpDispatchGenerator, "KRPC")
        self.assertIn(
            "return Result.String (connection.KRPC ().GetClientName ());", content
        )
        # A protobuf message is returned serialized
        self.assertIn(
            "return Result.Message (connection.KRPC ().GetServices ());", content
        )
        self.assertIn(
            "return Result_KRPC_ServerStatus (connection.KRPC ().GetStatus ());",
            content,
        )
        content = generate("cnano", CnanoDispatchGenerator, "KRPC")
        self.assertIn("return KRPC_ServerStatus::result(result);", content)
        # A protobuf message is never an argument
        self.assertNotIn('"AddStream"', content)


if __name__ == "__main__":
    unittest.main()
