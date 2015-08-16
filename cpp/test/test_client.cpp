#include <gtest/gtest.h>
#include <gmock/gmock.h>
#include <boost/algorithm/string/predicate.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/services/test_service.hpp>
#include <krpc/platform.hpp>
#include "server_test.hpp"

class test_client: public server_test {
};

TEST_F(test_client, test_version) {
  krpc::schema::Status status = krpc.get_status();
  ASSERT_EQ("0.1.11", status.version());
}

TEST_F(test_client, test_error) {
  ASSERT_THROW(test_service.throw_argument_exception(), krpc::RPCError);
  try {
    test_service.throw_argument_exception();
  } catch(boost::exception& e) {
    std::string msg = *boost::get_error_info<krpc::error_description>(e);
    ASSERT_EQ("Invalid argument", msg);
  }
  ASSERT_THROW(test_service.throw_invalid_operation_exception(), krpc::RPCError);
  try {
    test_service.throw_invalid_operation_exception();
  } catch(boost::exception& e) {
    std::string msg = *boost::get_error_info<krpc::error_description>(e);
    ASSERT_EQ("Invalid operation", msg);
  }
}

TEST_F(test_client, test_value_parameters) {
  ASSERT_EQ("3.14159", test_service.float_to_string(3.14159f));
  ASSERT_EQ("3.1415901184082", test_service.double_to_string(3.14159f));
  ASSERT_EQ("42", test_service.int32_to_string(42));
  ASSERT_EQ("123456789000", test_service.int64_to_string(123456789000));
  ASSERT_EQ("True", test_service.bool_to_string(true));
  ASSERT_EQ("False", test_service.bool_to_string(false));
  ASSERT_EQ(12345, test_service.string_to_int32("12345"));
  ASSERT_EQ("deadbeef", test_service.bytes_to_hex_string(krpc::platform::unhexlify("deadbeef")));
}

TEST_F(test_client, test_multiple_value_parameters) {
  ASSERT_EQ("3.14159", test_service.add_multiple_values(0.14159, 1, 2));
}

TEST_F(test_client, test_properties) {
  test_service.set_string_property("foo");
  ASSERT_EQ("foo", test_service.string_property());
  ASSERT_EQ("foo", test_service.string_property_private_set());
  test_service.set_string_property_private_get("foo");
  krpc::services::TestService::TestClass object = test_service.create_test_object("bar");
  test_service.set_object_property(object);
  ASSERT_EQ(object, test_service.object_property());
}

TEST_F(test_client, test_class_as_return_value) {
  krpc::services::TestService::TestClass object = test_service.create_test_object("jeb");
  std::stringstream stream;
  stream << object;
  ASSERT_TRUE(boost::starts_with(stream.str(), "TestService::TestClass<"));
}

TEST_F(test_client, test_class_none_value) {
  krpc::services::TestService::TestClass none(conn); //TODO: remove conn
  ASSERT_EQ(none, test_service.echo_test_object(none));
  krpc::services::TestService::TestClass object = test_service.create_test_object("bob");
  ASSERT_EQ("bobnull", object.object_to_string(none));
  test_service.set_object_property(none);
  ASSERT_EQ(none, test_service.object_property());
}

TEST_F(test_client, test_class_methods) {
  krpc::services::TestService::TestClass obj = test_service.create_test_object("bob");
  ASSERT_EQ("value=bob", obj.get_value());
  ASSERT_EQ("bob3.14159", obj.float_to_string(3.14159));
  krpc::services::TestService::TestClass obj2 = test_service.create_test_object("bill");
  ASSERT_EQ("bobbill", obj.object_to_string(obj2));
}

//TEST_F(test_client, test_class_static_methods) {
//  ASSERT_EQ("jeb", TestClass.static_method());
//  ASSERT_EQ("jebbobbill", TestClass.static_method("bob", "bill"));
//}

TEST_F(test_client, test_class_properties) {
  krpc::services::TestService::TestClass object = test_service.create_test_object("jeb");
  object.set_int_property(0);
  ASSERT_EQ(0, object.int_property());
  object.set_int_property(42);
  ASSERT_EQ(42, object.int_property());
  krpc::services::TestService::TestClass object2 = test_service.create_test_object("kermin");
  object.set_object_property(object2);
  ASSERT_EQ(object2, object.object_property());
}

TEST_F(test_client, test_optional_arguments) {
  ASSERT_EQ("jebfoobarbaz", test_service.optional_arguments("jeb"));
  ASSERT_EQ("jebbobbillbaz", test_service.optional_arguments("jeb", "bob", "bill"));
}

//def test_named_parameters(self):
//    self.assertEqual('1234', self.conn.test_service.optional_arguments(x='1', y='2', z='3', another_parameter='4'))
//    self.assertEqual('2413', self.conn.test_service.optional_arguments(z='1', x='2', another_parameter='3', y='4'))
//    self.assertEqual('1243', self.conn.test_service.optional_arguments('1', '2', another_parameter='3', z='4'))
//    self.assertEqual('123baz', self.conn.test_service.optional_arguments('1', '2', z='3'))
//    self.assertEqual('12bar3', self.conn.test_service.optional_arguments('1', '2', another_parameter='3'))
//    self.assertRaises(TypeError, self.conn.test_service.optional_arguments, '1', '2', '3', '4', another_parameter='5')
//    self.assertRaises(TypeError, self.conn.test_service.optional_arguments, '1', '2', '3', y='4')
//    self.assertRaises(TypeError, self.conn.test_service.optional_arguments, '1', foo='4')
//
//    obj = self.conn.test_service.create_test_object('jeb')
//    self.assertEqual('1234', obj.optional_arguments(x='1', y='2', z='3', another_parameter='4'))
//    self.assertEqual('2413', obj.optional_arguments(z='1', x='2', another_parameter='3', y='4'))
//    self.assertEqual('1243', obj.optional_arguments('1', '2', another_parameter='3', z='4'))
//    self.assertEqual('123baz', obj.optional_arguments('1', '2', z='3'))
//    self.assertEqual('12bar3', obj.optional_arguments('1', '2', another_parameter='3'))
//    self.assertRaises(TypeError, obj.optional_arguments, '1', '2', '3', '4', another_parameter='5')
//    self.assertRaises(TypeError, obj.optional_arguments, '1', '2', '3', y='4')
//    self.assertRaises(TypeError, obj.optional_arguments, '1', foo='4')

//def test_blocking_procedure(self):
//    self.assertEqual(0, self.conn.test_service.blocking_procedure(0,0))
//    self.assertEqual(1, self.conn.test_service.blocking_procedure(1,0))
//    self.assertEqual(1+2, self.conn.test_service.blocking_procedure(2))
//    self.assertEqual(sum(x for x in range(1,43)), self.conn.test_service.blocking_procedure(42))

//def test_too_many_arguments(self):
//    self.assertRaises(TypeError, self.conn.test_service.optional_arguments, '1', '2', '3', '4', '5')
//    obj = self.conn.test_service.create_test_object('jeb')
//    self.assertRaises(TypeError, obj.optional_arguments, '1', '2', '3', '4', '5')

//def test_protobuf_enums(self):
//    self.assertEqual(TestSchema.a, self.conn.test_service.enum_return())
//    self.assertEqual(TestSchema.a, self.conn.test_service.enum_echo(TestSchema.a))
//    self.assertEqual(TestSchema.b, self.conn.test_service.enum_echo(TestSchema.b))
//    self.assertEqual(TestSchema.c, self.conn.test_service.enum_echo(TestSchema.c))
//
//    self.assertEqual(TestSchema.a, self.conn.test_service.enum_default_arg(TestSchema.a))
//    self.assertEqual(TestSchema.c, self.conn.test_service.enum_default_arg())
//    self.assertEqual(TestSchema.b, self.conn.test_service.enum_default_arg(TestSchema.b))

//def test_enums(self):
//    enum = self.conn.test_service.CSharpEnum
//    self.assertEqual(enum.value_b, self.conn.test_service.c_sharp_enum_return())
//    self.assertEqual(enum.value_a, self.conn.test_service.c_sharp_enum_echo(enum.value_a))
//    self.assertEqual(enum.value_b, self.conn.test_service.c_sharp_enum_echo(enum.value_b))
//    self.assertEqual(enum.value_c, self.conn.test_service.c_sharp_enum_echo(enum.value_c))
//
//    self.assertEqual(enum.value_a, self.conn.test_service.c_sharp_enum_default_arg(enum.value_a))
//    self.assertEqual(enum.value_c, self.conn.test_service.c_sharp_enum_default_arg())
//    self.assertEqual(enum.value_b, self.conn.test_service.c_sharp_enum_default_arg(enum.value_b))

//def test_invalid_enum(self):
//    self.assertRaises(ValueError, self.conn.test_service.CSharpEnum, 9999)

//def test_collections(self):
//    self.assertEqual([], self.conn.test_service.increment_list([]))
//    self.assertEqual([1,2,3], self.conn.test_service.increment_list([0,1,2]))
//    self.assertEqual({}, self.conn.test_service.increment_dictionary({}))
//    self.assertEqual({'a': 1, 'b': 2, 'c': 3}, self.conn.test_service.increment_dictionary({'a': 0, 'b': 1, 'c': 2}))
//    self.assertEqual(set(), self.conn.test_service.increment_set(set()))
//    self.assertEqual(set([1,2,3]), self.conn.test_service.increment_set(set([0,1,2])))
//    self.assertEqual((2,3), self.conn.test_service.increment_tuple((1,2)))
//    self.assertRaises(TypeError, self.conn.test_service.increment_list, None)
//    self.assertRaises(TypeError, self.conn.test_service.increment_set, None)
//    self.assertRaises(TypeError, self.conn.test_service.increment_dictionary, None)

//def test_nested_collections(self):
//    self.assertEqual({}, self.conn.test_service.increment_nested_collection({}))
//    self.assertEqual({'a': [1, 2], 'b': [], 'c': [3]},
//                     self.conn.test_service.increment_nested_collection({'a': [0, 1], 'b': [], 'c': [2]}))

//def test_collections_of_objects(self):
//    l = self.conn.test_service.add_to_object_list([], "jeb")
//    self.assertEqual(1, len(l))
//    self.assertEqual("value=jeb", l[0].get_value())
//    l = self.conn.test_service.add_to_object_list(l, "bob")
//    self.assertEqual(2, len(l))
//    self.assertEqual("value=jeb", l[0].get_value())
//    self.assertEqual("value=bob", l[1].get_value())

//def test_test_service_enum_members(self):
//    self.assertSetEqual(
//        set(['value_a','value_b','value_c']),
//        set(filter(lambda x: not x.startswith('_'), dir(self.conn.test_service.CSharpEnum))))
//    self.assertEqual (0, self.conn.test_service.CSharpEnum.value_a.value)
//    self.assertEqual (1, self.conn.test_service.CSharpEnum.value_b.value)
//    self.assertEqual (2, self.conn.test_service.CSharpEnum.value_c.value)

//def test_line_endings(self):
//    strings = [
//        'foo\nbar',
//        'foo\rbar',
//        'foo\n\rbar',
//        'foo\r\nbar',
//        'foo\x10bar',
//        'foo\x13bar',
//        'foo\x10\x13bar',
//        'foo\x13\x10bar'
//    ]
//    for string in strings:
//        self.conn.test_service.string_property = string
//        self.assertEqual(string, self.conn.test_service.string_property)

//def test_types_from_different_connections(self):
//    conn1 = self.connect()
//    conn2 = self.connect()
//    self.assertNotEqual(conn1.test_service.TestClass, conn2.test_service.TestClass)
//    obj2 = conn2.test_service.TestClass(0)
//    obj1 = conn1._types.coerce_to(obj2, conn1._types.as_type('Class(TestService.TestClass)'))
//    self.assertEqual(obj1, obj2)
//    self.assertNotEqual(type(obj1), type(obj2))
//    self.assertEqual(type(obj1), conn1.test_service.TestClass)
//    self.assertEqual(type(obj2), conn2.test_service.TestClass)
