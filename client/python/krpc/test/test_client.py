import math
import os
import unittest
import socket
import threading
import warnings
from typing import Callable
import krpc
import krpc.limits
import krpc.schema.KRPC_pb2 as KRPC
from krpc.error import RPCError
from krpc.test.servertestcase import ServerTestCase


class TestClient(ServerTestCase, unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        super(TestClient, cls).setUpClass()

    def test_get_status(self) -> None:
        status = self.conn.krpc.get_status()
        self.assertRegex(status.version, r"^[0-9]+\.[0-9]+\.[0-9]+$")
        self.assertGreater(status.bytes_read, 0)

    @unittest.skipIf(
        os.getenv("RPC_PATH") is not None,
        "the server is listening on socket paths rather than on ports",
    )
    def test_wrong_rpc_port(self) -> None:
        with self.assertRaises(socket.error):
            krpc.connect(
                name="python_client_test_wrong_rpc_port",
                address="localhost",
                rpc_port=ServerTestCase.unused_port(),
                stream_port=ServerTestCase.stream_port(),
                timeout=ServerTestCase.CONNECT_TIMEOUT,
            )

    @unittest.skipIf(
        os.getenv("RPC_PATH") is not None,
        "the server is listening on socket paths rather than on ports",
    )
    def test_wrong_stream_port(self) -> None:
        with self.assertRaises(socket.error):
            krpc.connect(
                name="python_client_test_wrong_stream_port",
                address="localhost",
                rpc_port=ServerTestCase.rpc_port(),
                stream_port=ServerTestCase.unused_port(),
                timeout=ServerTestCase.CONNECT_TIMEOUT,
            )

    def test_wrong_rpc_server(self) -> None:
        with self.assertRaises(krpc.error.ConnectionError) as cm:
            ServerTestCase.connect(
                name="python_client_test_wrong_rpc_server",
                rpc="stream",
                stream="stream",
            )
        self.assertEqual(
            "Connection request was for the rpc server, "
            + "but this is the stream server. "
            + "Did you connect to the wrong port number or socket path?",
            str(cm.exception),
        )

    def test_wrong_stream_server(self) -> None:
        with self.assertRaises(krpc.error.ConnectionError) as cm:
            ServerTestCase.connect(
                name="python_client_test_wrong_stream_server",
                rpc="rpc",
                stream="rpc",
            )
        self.assertEqual(
            "Connection request was for the stream server, "
            + "but this is the rpc server. "
            + "Did you connect to the wrong port number or socket path?",
            str(cm.exception),
        )

    def test_value_parameters(self) -> None:
        self.assertEqual("3.14159", self.conn.test_service.float_to_string(3.14159))
        self.assertEqual("3.14159", self.conn.test_service.double_to_string(3.14159))
        self.assertEqual("42", self.conn.test_service.int32_to_string(42))
        self.assertEqual(
            "123456789000", self.conn.test_service.int64_to_string(123456789000)
        )
        self.assertEqual("True", self.conn.test_service.bool_to_string(True))
        self.assertEqual("False", self.conn.test_service.bool_to_string(False))
        self.assertEqual(12345, self.conn.test_service.string_to_int32("12345"))
        self.assertEqual(
            "deadbeef", self.conn.test_service.bytes_to_hex_string(b"\xde\xad\xbe\xef")
        )

    def test_multiple_value_parameters(self) -> None:
        self.assertEqual(
            "3.14159", self.conn.test_service.add_multiple_values(0.14159, 1, 2)
        )

    def test_auto_value_type_conversion(self) -> None:
        self.assertEqual("42", self.conn.test_service.float_to_string(42))
        self.assertEqual("42", self.conn.test_service.float_to_string(42))
        self.assertEqual("6", self.conn.test_service.add_multiple_values(1, 2, 3))
        self.assertRaises(TypeError, self.conn.test_service.float_to_string, "42")

    def test_incorrect_parameter_type(self) -> None:
        self.assertRaises(TypeError, self.conn.test_service.float_to_string, "foo")
        self.assertRaises(
            TypeError, self.conn.test_service.add_multiple_values, 0.14159, "foo", 2
        )

    def test_properties(self) -> None:
        self.conn.test_service.string_property = "foo"
        self.assertEqual("foo", self.conn.test_service.string_property)
        self.assertEqual("foo", self.conn.test_service.string_property_private_set)
        self.conn.test_service.string_property_private_get = "foo"
        obj = self.conn.test_service.create_test_object("bar")
        self.conn.test_service.object_property = obj
        self.assertEqual(obj, self.conn.test_service.object_property)

    def assert_warns_deprecation(self, action: Callable[[], None]) -> None:
        with warnings.catch_warnings(record=True) as caught:
            warnings.simplefilter("always")
            action()
        self.assertTrue(
            any(issubclass(w.category, DeprecationWarning) for w in caught),
            "expected a DeprecationWarning to be raised",
        )

    # The runtime DeprecationWarning is a feature of the dynamically-created
    # services, so these tests use a connection without the pre-generated stubs.
    def test_deprecated_procedure_warns(self) -> None:
        conn = ServerTestCase.connect(
            name="python_client_test_dynamic",
            use_pregenerated_stubs=False,
        )
        try:
            self.assert_warns_deprecation(
                lambda: conn.test_service.deprecated_procedure(3.14159)
            )
            self.assert_warns_deprecation(
                lambda: conn.test_service.deprecated_procedure_no_message(3.14159)
            )
            self.assert_warns_deprecation(
                lambda: setattr(conn.test_service, "deprecated_property", "foo")
            )
            self.assert_warns_deprecation(
                lambda: getattr(conn.test_service, "deprecated_property")
            )
        finally:
            conn.close()

    def test_extension_members(self) -> None:
        obj = self.conn.test_service.create_test_object("jeb")
        self.assertEqual("value=jeb42", obj.extension_method(42))
        self.assertEqual("value=jeb", obj.extension_property)
        obj.extension_read_write_property = 42
        self.assertEqual(42, obj.extension_read_write_property)
        # The extension property writes through to the class's own int_property
        self.assertEqual(42, obj.int_property)

    def test_extension_member_returning_class_from_other_service(self) -> None:
        obj = self.conn.test_service.create_test_object("jeb")
        obj2 = obj.extension_method_returning_class_from_other_service()
        self.assertEqual("TestClass2", type(obj2).__name__)
        self.assertEqual("value=jeb", obj2.value)

    def test_extension_members_are_per_connection(self) -> None:
        # A pre-generated class is shared by every client in the process, so the members
        # merged onto it go through the connection the object was reached through
        other = self.connect()
        try:
            obj = other.test_service.create_test_object("jeb")
            self.assertEqual("value=jeb", obj.extension_property)
            obj2 = obj.extension_method_returning_class_from_other_service()
            self.assertIs(other, obj2._client)
            self.assertIn("extension_method", dir(other.test_service.TestClass))
        finally:
            other.close()

    def test_extension_member_stream(self) -> None:
        obj = self.conn.test_service.create_test_object("jeb")
        with self.conn.stream(getattr, obj, "extension_property") as stream:
            self.assertEqual("value=jeb", stream())

    def test_class_as_return_value(self) -> None:
        obj = self.conn.test_service.create_test_object("jeb")
        self.assertEqual("TestClass", type(obj).__name__)

    def test_class_none_value(self) -> None:
        self.assertIsNone(self.conn.test_service.echo_test_object(None))
        obj = self.conn.test_service.create_test_object("bob")
        self.assertEqual("bobnull", obj.object_to_string(None))
        self.conn.test_service.object_property = None
        self.assertIsNone(self.conn.test_service.object_property)

    def test_class_none_value_when_not_allowed(self) -> None:
        with self.assertRaises(krpc.error.RPCError) as cm:
            self.conn.test_service.return_null_when_not_allowed()
        self.assertTrue(
            str(cm.exception).startswith(
                "Incorrect value returned by "
                "TestService.ReturnNullWhenNotAllowed. "
                "Expected a non-null value of type "
                "TestServer.TestService+TestClass, "
                "got null, but the procedure is not marked as nullable."
            )
        )

    def test_nullable_procedure_non_class(self) -> None:
        # Nullable value-type, string and collection parameters and return values
        self.assertEqual(42, self.conn.test_service.echo_nullable_int(42))
        self.assertIsNone(self.conn.test_service.echo_nullable_int(None))
        self.assertEqual("foo", self.conn.test_service.echo_nullable_string("foo"))
        self.assertIsNone(self.conn.test_service.echo_nullable_string(None))
        self.assertEqual(
            [1, 2, 3], self.conn.test_service.echo_nullable_list([1, 2, 3])
        )
        self.assertIsNone(self.conn.test_service.echo_nullable_list(None))

    def test_nullable_procedure_class(self) -> None:
        obj = self.conn.test_service.create_test_object("jeb")
        self.assertEqual(obj, self.conn.test_service.echo_test_object(obj))
        self.assertIsNone(self.conn.test_service.echo_test_object(None))

    def test_non_nullable_parameter_rejects_null(self) -> None:
        with self.assertRaises(krpc.error.RPCError):
            self.conn.test_service.not_nullable_object(None)

    def test_nullable_class_method(self) -> None:
        obj = self.conn.test_service.create_test_object("jeb")
        obj2 = self.conn.test_service.create_test_object("bob")
        self.assertEqual(obj2, obj.echo_nullable_object(obj2))
        self.assertIsNone(obj.echo_nullable_object(None))

    def test_nullable_class_type_shares_one_python_type(self) -> None:
        # A nullable class-typed value is the generated class of the non-nullable one, and
        # not a second class minted for the nullable declaration
        obj = self.conn.test_service.create_test_object("jeb")
        self.assertIs(type(obj.echo_nullable_object(obj)), type(obj))

    def test_nullable_class_static_method(self) -> None:
        obj = self.conn.test_service.create_test_object("jeb")
        self.assertEqual(
            obj, self.conn.test_service.TestClass.static_nullable_object(obj)
        )
        self.assertIsNone(self.conn.test_service.TestClass.static_nullable_object(None))

    def test_nullable_property(self) -> None:
        obj = self.conn.test_service.create_test_object("jeb")
        # ObjectProperty is nullable and its setter accepts null
        self.conn.test_service.object_property = None
        self.assertIsNone(self.conn.test_service.object_property)
        # NullableObject is nullable for reads, but its setter guards against null, so writing
        # null raises the server's ArgumentNullException (mapped to ValueError on the client)
        self.conn.test_service.nullable_object = obj
        self.assertEqual(obj, self.conn.test_service.nullable_object)
        with self.assertRaises(ValueError):
            self.conn.test_service.nullable_object = None

    def test_non_nullable_property_rejects_null(self) -> None:
        with self.assertRaises(krpc.error.RPCError):
            self.conn.test_service.string_property = None

    def test_nullable_class_property(self) -> None:
        obj = self.conn.test_service.create_test_object("jeb")
        obj2 = self.conn.test_service.create_test_object("bob")
        obj.object_property = obj2
        self.assertEqual(obj2, obj.object_property)
        obj.object_property = None
        self.assertIsNone(obj.object_property)

    def test_empty_collection_default(self) -> None:
        # An empty-collection default is distinguishable from no default: the argument
        # can be omitted and the empty list is used.
        self.assertEqual([], self.conn.test_service.empty_list_default())
        self.assertEqual(
            ["foo", "bar"], self.conn.test_service.empty_list_default(["foo", "bar"])
        )

    def test_nullable_stream(self) -> None:
        with self.conn.stream(self.conn.test_service.echo_nullable_int, None) as stream:
            self.assertIsNone(stream())

    def test_stream_of_nullable_list_elements(self) -> None:
        # A stream carries the value itself, so a null inside a collection reaches the
        # decoder as a presence bool where a null result does not
        service = self.conn.test_service
        with self.conn.stream(
            service.echo_list_of_nullable_ints, [1, None, 3]
        ) as stream:
            self.assertEqual([1, None, 3], stream())

    def test_class_methods(self) -> None:
        obj = self.conn.test_service.create_test_object("bob")
        self.assertEqual("value=bob", obj.get_value())
        self.assertEqual("bob3.14159", obj.float_to_string(3.14159))
        obj2 = self.conn.test_service.create_test_object("bill")
        self.assertEqual("bobbill", obj.object_to_string(obj2))

    def test_class_static_methods(self) -> None:
        self.assertEqual("jeb", self.conn.test_service.TestClass.static_method())
        self.assertEqual(
            "jebbobbill", self.conn.test_service.TestClass.static_method("bob", "bill")
        )

    def test_class_static_methods_via_instance(self) -> None:
        obj = self.conn.test_service.create_test_object("jeb")
        self.assertEqual("jeb", obj.static_method())
        self.assertEqual("jebbobbill", obj.static_method("bob", "bill"))

    def test_class_properties(self) -> None:
        obj = self.conn.test_service.create_test_object("jeb")
        obj.int_property = 0
        self.assertEqual(0, obj.int_property)
        obj.int_property = 42
        self.assertEqual(42, obj.int_property)
        obj2 = self.conn.test_service.create_test_object("kermin")
        obj.object_property = obj2
        self.assertEqual(obj2._object_id, obj.object_property._object_id)
        obj.string_property_private_get = "bob"
        self.assertEqual("bob", obj.string_property_private_set)

    def test_optional_arguments(self) -> None:
        self.assertEqual(
            "jebfoobarnull", self.conn.test_service.optional_arguments("jeb")
        )
        self.assertEqual(
            "jebbobbillnull",
            self.conn.test_service.optional_arguments("jeb", "bob", "bill"),
        )
        obj = self.conn.test_service.create_test_object("kermin")
        self.assertEqual(
            "jebbobbillkermin",
            self.conn.test_service.optional_arguments("jeb", "bob", "bill", obj),
        )

    def test_named_parameters(self) -> None:
        obj3 = self.conn.test_service.create_test_object("3")
        obj4 = self.conn.test_service.create_test_object("4")
        obj5 = self.conn.test_service.create_test_object("5")
        self.assertEqual(
            "1234",
            self.conn.test_service.optional_arguments(x="1", y="2", z="3", obj=obj4),
        )
        self.assertEqual(
            "2413",
            self.conn.test_service.optional_arguments(z="1", x="2", obj=obj3, y="4"),
        )
        self.assertEqual(
            "1243", self.conn.test_service.optional_arguments("1", "2", obj=obj3, z="4")
        )
        self.assertEqual(
            "123null", self.conn.test_service.optional_arguments("1", "2", z="3")
        )
        self.assertEqual(
            "12bar3", self.conn.test_service.optional_arguments("1", "2", obj=obj3)
        )
        self.assertRaises(
            TypeError,
            self.conn.test_service.optional_arguments,
            "1",
            "2",
            "3",
            "4",
            obj=obj5,
        )
        self.assertRaises(
            TypeError, self.conn.test_service.optional_arguments, "1", "2", "3", y="4"
        )
        self.assertRaises(
            TypeError, self.conn.test_service.optional_arguments, "1", foo="4"
        )

        obj = self.conn.test_service.create_test_object("jeb")
        self.assertEqual("1234", obj.optional_arguments(x="1", y="2", z="3", obj=obj4))
        self.assertEqual("2413", obj.optional_arguments(z="1", x="2", obj=obj3, y="4"))
        self.assertEqual("1243", obj.optional_arguments("1", "2", obj=obj3, z="4"))
        self.assertEqual("123null", obj.optional_arguments("1", "2", z="3"))
        self.assertEqual("12bar3", obj.optional_arguments("1", "2", obj=obj3))
        self.assertRaises(
            TypeError, obj.optional_arguments, "1", "2", "3", "4", obj=obj5
        )
        self.assertRaises(TypeError, obj.optional_arguments, "1", "2", "3", y="4")
        self.assertRaises(TypeError, obj.optional_arguments, "1", foo="4")

    def test_blocking_procedure(self) -> None:
        self.assertEqual(0, self.conn.test_service.blocking_procedure(0, 0))
        self.assertEqual(1, self.conn.test_service.blocking_procedure(1, 0))
        self.assertEqual(1 + 2, self.conn.test_service.blocking_procedure(2))
        self.assertEqual(
            sum(x for x in range(1, 43)), self.conn.test_service.blocking_procedure(42)
        )

    def test_too_many_arguments(self) -> None:
        self.assertRaises(
            TypeError,
            self.conn.test_service.optional_arguments,
            "1",
            "2",
            "3",
            "4",
            "5",
        )
        obj = self.conn.test_service.create_test_object("jeb")
        self.assertRaises(TypeError, obj.optional_arguments, "1", "2", "3", "4", "5")

    def test_too_few_arguments(self) -> None:
        self.assertRaises(TypeError, self.conn.test_service.optional_arguments)
        obj = self.conn.test_service.create_test_object("jeb")
        self.assertRaises(TypeError, obj.optional_arguments)

    def test_enums(self) -> None:
        enum = self.conn.test_service.TestEnum
        self.assertEqual(enum.value_b, self.conn.test_service.enum_return())
        self.assertEqual(enum.value_a, self.conn.test_service.enum_echo(enum.value_a))
        self.assertEqual(enum.value_b, self.conn.test_service.enum_echo(enum.value_b))
        self.assertEqual(enum.value_c, self.conn.test_service.enum_echo(enum.value_c))

        self.assertEqual(
            enum.value_a, self.conn.test_service.enum_default_arg(enum.value_a)
        )
        self.assertEqual(enum.value_c, self.conn.test_service.enum_default_arg())
        self.assertEqual(
            enum.value_b, self.conn.test_service.enum_default_arg(enum.value_b)
        )

        self.assertEqual(
            [enum.value_b, enum.value_c], self.conn.test_service.enum_list_default()
        )
        self.assertEqual(
            [enum.value_a, enum.value_b],
            self.conn.test_service.enum_list_default([enum.value_a, enum.value_b]),
        )

    def test_invalid_enum(self) -> None:
        self.assertRaises(ValueError, self.conn.test_service.TestEnum, 9999)

    def test_collections(self) -> None:
        self.assertEqual([], self.conn.test_service.increment_list([]))
        self.assertEqual([1, 2, 3], self.conn.test_service.increment_list([0, 1, 2]))
        self.assertEqual({}, self.conn.test_service.increment_dictionary({}))
        self.assertEqual(
            {"a": 1, "b": 2, "c": 3},
            self.conn.test_service.increment_dictionary({"a": 0, "b": 1, "c": 2}),
        )
        self.assertEqual(set(), self.conn.test_service.increment_set(set()))
        self.assertEqual(
            set([1, 2, 3]), self.conn.test_service.increment_set(set([0, 1, 2]))
        )
        self.assertEqual((2, 3), self.conn.test_service.increment_tuple((1, 2)))
        # These collection parameters are not nullable, so null is rejected by the server
        self.assertRaises(RPCError, self.conn.test_service.increment_list, None)
        self.assertRaises(RPCError, self.conn.test_service.increment_set, None)
        self.assertRaises(RPCError, self.conn.test_service.increment_dictionary, None)

    def test_nested_collections(self) -> None:
        self.assertEqual({}, self.conn.test_service.increment_nested_collection({}))
        self.assertEqual(
            {"a": [1, 2], "b": [], "c": [3]},
            self.conn.test_service.increment_nested_collection(
                {"a": [0, 1], "b": [], "c": [2]}
            ),
        )

    def test_collections_of_objects(self) -> None:
        objs = self.conn.test_service.add_to_object_list([], "jeb")
        self.assertEqual(1, len(objs))
        self.assertEqual("value=jeb", objs[0].get_value())
        objs = self.conn.test_service.add_to_object_list(objs, "bob")
        self.assertEqual(2, len(objs))
        self.assertEqual("value=jeb", objs[0].get_value())
        self.assertEqual("value=bob", objs[1].get_value())

    def test_structs(self) -> None:
        service = self.conn.test_service
        value = service.TestStruct(
            int_field=42,
            string_field="jeb",
            enum_field=service.TestEnum.value_b,
            list_field=[1, 2, 3],
        )
        result = service.struct_echo(value)
        self.assertEqual(value, result)
        # A structure has named fields, and is a tuple of their values
        self.assertEqual(42, result.int_field)
        self.assertEqual(service.TestEnum.value_b, result.enum_field)
        self.assertEqual(
            (42, "jeb", service.TestEnum.value_b, [1, 2, 3]), tuple(result)
        )
        # A tuple of the field values coerces to the structure
        self.assertEqual(
            value, service.struct_echo((42, "jeb", service.TestEnum.value_b, [1, 2, 3]))
        )

    def test_nested_structs(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("bob")
        value = service.TestNestedStruct(
            struct_field=service.TestStruct(
                int_field=1,
                string_field="jeb",
                enum_field=service.TestEnum.value_a,
                list_field=[],
            ),
            object_field=obj,
            string_field="bill",
        )
        result = service.nested_struct_echo(value)
        self.assertEqual(value, result)
        self.assertEqual(1, result.struct_field.int_field)
        self.assertEqual("value=bob", result.object_field.get_value())

    def test_collections_of_structs(self) -> None:
        service = self.conn.test_service
        values = [
            service.TestStruct(
                int_field=i,
                string_field="jeb",
                enum_field=service.TestEnum.value_c,
                list_field=[i],
            )
            for i in range(3)
        ]
        result = service.increment_list_of_structs(values)
        self.assertEqual([1, 2, 3], [x.int_field for x in result])

    def test_nullable_structs(self) -> None:
        service = self.conn.test_service
        self.assertIsNone(service.struct_echo_nullable(None))
        value = service.TestStruct(
            int_field=1,
            string_field="jeb",
            enum_field=service.TestEnum.value_a,
            list_field=[],
        )
        self.assertEqual(value, service.struct_echo_nullable(value))

    def test_nullable_list_elements(self) -> None:
        service = self.conn.test_service
        self.assertEqual([1, None, 3], service.echo_list_of_nullable_ints([1, None, 3]))
        # A zero is a value like any other, and survives a position that could hold a null
        self.assertEqual([0, None], service.echo_list_of_nullable_ints([0, None]))
        obj = service.create_test_object("jeb")
        self.assertEqual(
            [obj, None], service.echo_list_of_nullable_objects([obj, None])
        )

    def test_nullable_dictionary_values(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("jeb")
        value = {"a": obj, "b": None}
        self.assertEqual(value, service.echo_dictionary_of_nullable_objects(value))

    def test_nullable_tuple_item(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("jeb")
        self.assertEqual((1, obj), service.echo_tuple_with_a_nullable_object((1, obj)))
        self.assertEqual(
            (1, None), service.echo_tuple_with_a_nullable_object((1, None))
        )

    def test_nullable_nested_list_elements(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("jeb")
        value = [[obj, None], []]
        self.assertEqual(value, service.echo_nested_list_of_nullable_objects(value))

    def test_nullable_struct_fields(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("jeb")
        value = service.TestNullableStruct(
            int_field=1,
            nullable_int_field=2,
            nullable_enum_field=service.TestEnum.value_b,
            nullable_string_field="jeb",
            nullable_object_field=obj,
        )
        self.assertEqual(value, service.nullable_struct_echo(value))

    def test_null_struct_fields(self) -> None:
        service = self.conn.test_service
        value = service.TestNullableStruct(
            int_field=1,
            nullable_int_field=None,
            nullable_enum_field=None,
            nullable_string_field=None,
            nullable_object_field=None,
        )
        self.assertEqual(value, service.nullable_struct_echo(value))

    def test_nullable_elements_of_a_struct_field(self) -> None:
        service = self.conn.test_service
        obj = service.create_test_object("jeb")
        value = service.TestNestedNullableStruct(
            list_field=[obj, None], string_field="jeb"
        )
        self.assertEqual(value, service.echo_nested_nullable_struct(value))

    def test_struct_default_value(self) -> None:
        service = self.conn.test_service
        self.assertEqual(
            service.TestStruct(
                int_field=42,
                string_field="jeb",
                enum_field=service.TestEnum.value_b,
                list_field=[1, 2, 3],
            ),
            service.struct_default(),
        )

    def test_struct_comparison(self) -> None:
        service = self.conn.test_service
        value = service.TestStruct(
            int_field=42,
            string_field="jeb",
            enum_field=service.TestEnum.value_b,
            list_field=[1, 2, 3],
        )
        result = service.struct_echo(value)
        other = service.struct_echo(value._replace(int_field=43))
        self.assertEqual(value, result)
        self.assertLess(result, other)
        self.assertEqual([result, other], sorted([other, result]))

    def test_colllections_default_values(self) -> None:
        self.assertEqual((1, False), self.conn.test_service.tuple_default())
        self.assertEqual([1, 2, 3], self.conn.test_service.list_default())
        self.assertEqual(set([1, 2, 3]), self.conn.test_service.set_default())
        self.assertEqual(
            {1: False, 2: True}, self.conn.test_service.dictionary_default()
        )

    def test_special_default_values(self) -> None:
        # The special values of the numeric types survive the round trip from the
        # declaration in the service, through the generated client, to the server, and the
        # defaults apply the client's own constants for them
        values = self.conn.test_service.double_special_defaults()
        self.assertTrue(math.isnan(values[0]))
        self.assertEqual(
            [
                float("inf"),
                -float("inf"),
                krpc.limits.DOUBLE_MAX,
                krpc.limits.DOUBLE_LOWEST,
            ],
            values[1:],
        )
        values = self.conn.test_service.float_special_defaults()
        self.assertTrue(math.isnan(values[0]))
        self.assertEqual(
            [
                float("inf"),
                -float("inf"),
                krpc.limits.FLOAT_MAX,
                krpc.limits.FLOAT_LOWEST,
            ],
            values[1:5],
        )
        self.assertAlmostEqual(0.1, values[5], places=6)
        self.assertEqual(
            [krpc.limits.SINT32_MAX, krpc.limits.SINT32_MIN],
            self.conn.test_service.int32_special_defaults(),
        )
        self.assertEqual(
            [krpc.limits.SINT64_MAX, krpc.limits.SINT64_MIN],
            self.conn.test_service.int64_special_defaults(),
        )
        self.assertEqual(
            [krpc.limits.UINT32_MAX],
            self.conn.test_service.uint32_special_defaults(),
        )
        self.assertEqual(
            [krpc.limits.UINT64_MAX],
            self.conn.test_service.uint64_special_defaults(),
        )

    def test_unknown_exception_type(self) -> None:
        # An error naming a service or type this client does not know about still reports
        # what went wrong, and does not fail while building the exception for it
        for service, name in (
            ("NotAService", "NotAnException"),
            ("KRPC", "NotAnException"),
        ):
            error = KRPC.Error(
                service=service, name=name, description="something went wrong"
            )
            exn = self.conn._build_error(error)
            self.assertIsInstance(exn, RPCError)
            self.assertEqual("%s.%s: something went wrong" % (service, name), str(exn))

    def test_invalid_operation_exception(self) -> None:
        with self.assertRaises(RuntimeError) as cm:
            self.conn.test_service.throw_invalid_operation_exception()
        self.assertTrue(str(cm.exception).startswith("Invalid operation"))

    def test_argument_exception(self) -> None:
        with self.assertRaises(ValueError) as cm:
            self.conn.test_service.throw_argument_exception()
        self.assertTrue(str(cm.exception).startswith("Invalid argument"))

    def test_argument_null_exception(self) -> None:
        with self.assertRaises(ValueError) as cm:
            self.conn.test_service.throw_argument_null_exception("")
        # The parameter name formatting differs between .NET Framework/mono
        # ("Parameter name: foo") and modern .NET ("(Parameter 'foo')")
        self.assertTrue(str(cm.exception).startswith("Value cannot be null."))
        self.assertIn("foo", str(cm.exception))

    def test_argument_out_of_range_exception(self) -> None:
        with self.assertRaises(ValueError) as cm:
            self.conn.test_service.throw_argument_out_of_range_exception(0)
        self.assertTrue(
            str(cm.exception).startswith(
                "Specified argument was out of the range of valid values."
            )
        )
        self.assertIn("foo", str(cm.exception))

    def test_custom_exception(self) -> None:
        with self.assertRaises(self.conn.test_service.CustomException) as cm:
            self.conn.test_service.throw_custom_exception()
        self.assertTrue(str(cm.exception).startswith("A custom kRPC exception"))

    def test_client_members(self) -> None:
        self.assertSetEqual(
            set(
                [
                    "krpc",
                    "test_service",
                    # Owns the class that an extension member of TestService returns
                    "test_service2",
                    # The server-side benchmarks the test server exposes. It has no
                    # pre-generated stubs, so this is also the client building a service
                    # from the definitions the server hands over.
                    "benchmark",
                    "stream",
                    "add_stream",
                    "stream_update_condition",
                    "wait_for_stream_update",
                    "add_stream_update_callback",
                    "remove_stream_update_callback",
                    "get_call",
                    "close",
                    "ui",
                    "drawing",
                    "kerbal_alarm_clock",
                    "lidar",
                    "infernal_robotics",
                    "remote_tech",
                    "space_center",
                    "docking_camera",
                    "debug",
                ]
            ),
            set(x for x in dir(self.conn) if not x.startswith("_")),
        )

    def test_krpc_service_members(self) -> None:
        self.assertSetEqual(
            set(
                [
                    "get_client_id",
                    "get_client_name",
                    "get_services",
                    "get_status",
                    "add_stream",
                    "start_stream",
                    "set_stream_rate",
                    "remove_stream",
                    "add_event",
                    "hold_tick",
                    "release_tick",
                    "next_tick",
                    "game_scene",
                    "current_game_scene",
                    "GameScene",
                    "paused",
                    "clients",
                    "Expression",
                    "Type",
                    "InvalidOperationException",
                    "ArgumentException",
                    "ArgumentNullException",
                    "ArgumentOutOfRangeException",
                    "ObjectDestroyedException",
                ]
            ),
            set(x for x in dir(self.conn.krpc) if not x.startswith("_")),
        )

    def test_test_service_service_members(self) -> None:
        self.assertSetEqual(
            set(
                [
                    "float_to_string",
                    "double_to_string",
                    "int32_to_string",
                    "int64_to_string",
                    "bool_to_string",
                    "string_to_int32",
                    "bytes_to_hex_string",
                    "add_multiple_values",
                    "string_property",
                    "string_property_private_get",
                    "string_property_private_set",
                    "create_test_object",
                    "echo_test_object",
                    "object_property",
                    "return_null_when_not_allowed",
                    "not_nullable_object",
                    "echo_nullable_string",
                    "echo_nullable_list",
                    "echo_nullable_int",
                    "nullable_object",
                    "empty_list_default",
                    "TestClass",
                    "optional_arguments",
                    "TestEnum",
                    "enum_return",
                    "enum_echo",
                    "enum_default_arg",
                    "enum_list_default",
                    "blocking_procedure",
                    "increment_list",
                    "increment_dictionary",
                    "increment_set",
                    "increment_tuple",
                    "increment_nested_collection",
                    "tuple_default",
                    "dictionary_default",
                    "list_default",
                    "set_default",
                    "add_to_object_list",
                    "counter",
                    "TestStruct",
                    "TestNestedStruct",
                    "TestNullableStruct",
                    "TestNestedNullableStruct",
                    "echo_nested_nullable_struct",
                    "echo_list_of_nullable_ints",
                    "echo_list_of_nullable_objects",
                    "echo_dictionary_of_nullable_objects",
                    "echo_tuple_with_a_nullable_object",
                    "echo_nested_list_of_nullable_objects",
                    "struct_echo",
                    "nullable_struct_echo",
                    "nested_struct_echo",
                    "increment_list_of_structs",
                    "struct_default",
                    "struct_echo_nullable",
                    "counter_struct",
                    "CustomException",
                    "throw_custom_exception",
                    "reset_custom_exception_later",
                    "throw_custom_exception_later",
                    "throw_invalid_operation_exception",
                    "throw_invalid_operation_exception_later",
                    "reset_invalid_operation_exception_later",
                    "throw_argument_exception",
                    "throw_argument_null_exception",
                    "throw_argument_out_of_range_exception",
                    "on_timer",
                    "on_timer_using_lambda",
                    "deprecated_procedure",
                    "deprecated_procedure_no_message",
                    "deprecated_property",
                    "DeprecatedClass",
                    "DeprecatedEnum",
                    "DeprecatedException",
                    "DeprecatedStruct",
                    "double_special_defaults",
                    "float_special_defaults",
                    "int32_special_defaults",
                    "int64_special_defaults",
                    "uint32_special_defaults",
                    "uint64_special_defaults",
                ]
            ),
            set(x for x in dir(self.conn.test_service) if not x.startswith("_")),
        )

    def test_test_service_test_class_members(self) -> None:
        self.assertSetEqual(
            set(
                [
                    "get_value",
                    "float_to_string",
                    "object_to_string",
                    "echo_nullable_object",
                    "int_property",
                    "object_property",
                    "string_property_private_get",
                    "string_property_private_set",
                    "optional_arguments",
                    "static_method",
                    "static_nullable_object",
                    # Added by extension members, which the client picks up from the
                    # server's definitions
                    "extension_method",
                    "extension_method_returning_class_from_other_service",
                    "extension_property",
                    "extension_read_write_property",
                ]
            ),
            set(
                x
                for x in dir(self.conn.test_service.TestClass)
                if not x.startswith("_")
            ),
        )

    def test_test_service_enum_members(self) -> None:
        self.assertSetEqual(
            set(["value_a", "value_b", "value_c"]),
            set(
                x for x in dir(self.conn.test_service.TestEnum) if not x.startswith("_")
            ),
        )
        self.assertEqual(0, self.conn.test_service.TestEnum.value_a.value)
        self.assertEqual(1, self.conn.test_service.TestEnum.value_b.value)
        self.assertEqual(2, self.conn.test_service.TestEnum.value_c.value)

    def test_line_endings(self) -> None:
        strings = [
            "foo\nbar",
            "foo\rbar",
            "foo\n\rbar",
            "foo\r\nbar",
            "foo\x10bar",
            "foo\x13bar",
            "foo\x10\x13bar",
            "foo\x13\x10bar",
        ]
        for string in strings:
            self.conn.test_service.string_property = string
            self.assertEqual(string, self.conn.test_service.string_property)

    def test_types_from_different_connections(self) -> None:
        conn1 = self.connect()
        conn2 = self.connect()
        # self.assertNotEqual(
        #     conn1.test_service.TestClass, conn2.test_service.TestClass)
        obj2 = conn2.test_service.TestClass(self.conn, 0)
        obj1 = conn1._types.coerce_to(
            obj2, conn1._types.class_type("TestService", "TestClass")
        )
        self.assertEqual(obj1, obj2)
        # self.assertNotEqual(type(obj1), type(obj2))
        # self.assertEqual(type(obj1), conn1.test_service.TestClass)
        # self.assertEqual(type(obj2), conn2.test_service.TestClass)

    # def test_thread_safe(self) -> None:
    #     thread_count = 32
    #     repeats = 100
    #
    #     latch = threading.Condition()
    #     count = thread_count
    #
    #     def thread_main() -> None:
    #         for _ in range(repeats):
    #             self.assertEqual(
    #                 "False", self.conn.test_service.bool_to_string(False))
    #             self.assertEqual(
    #                 12345, self.conn.test_service.string_to_int32("12345"))
    #         with latch:
    #             count -= 1
    #             if count <= 0:
    #                 latch.notify_all()
    #
    #     for _ in range(thread_count):
    #         thread = threading.Thread(target=thread_main)
    #         thread.daemon = True
    #         thread.start()
    #
    #     with latch:
    #         while count > 0:
    #             latch.wait(10)
    #     self.assertEqual(0, count)


if __name__ == "__main__":
    unittest.main()
