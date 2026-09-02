#include <gmock/gmock-matchers.h>
#include <gtest/gtest-message.h>
#include <gtest/gtest-test-part.h>

#include <atomic>
#include <cstdint>
#include <exception>
#include <iosfwd>
#include <map>
#include <memory>
#include <optional>
#include <set>
#include <stdexcept>
#include <string>
#include <thread>  // NOLINT(build/c++11)
#include <tuple>
#include <type_traits>
#include <unordered_set>
#include <vector>

#include "gtest/gtest.h"
#include "krpc.hpp"
#include "krpc/platform.hpp"
#include "krpc/services/krpc.hpp"
#include "server_test.hpp"
#include "services/test_service.hpp"

class test_client : public server_test {};

// The version is three dot separated runs of digits, checked by hand. gtest falls back to
// a regular expression syntax of its own where it cannot use POSIX ones, and the two have
// no spelling of a digit in common: [0-9] is only understood by the former, \d only by the
// latter.
static bool is_version(const char* value) {
  int parts = 1;
  int digits = 0;
  for (; *value != '\0'; value++) {
    if (*value == '.') {
      if (digits == 0) return false;
      digits = 0;
      parts++;
    } else if (*value >= '0' && *value <= '9') {
      digits++;
    } else {
      return false;
    }
  }
  return parts == 3 && digits > 0;
}

TEST_F(test_client, test_default_ctor) { krpc::Client client; }

TEST_F(test_client, test_shared_ptr) {
  auto client = std::make_shared<krpc::Client>(connect());
  krpc::services::KRPC krpc(client.get());
  krpc::schema::Status status = krpc.get_status();
  ASSERT_TRUE(is_version(status.version().c_str())) << status.version();
  client.reset();
}

TEST_F(test_client, test_std_container) {
  std::vector<krpc::Client> clients;
  clients.push_back(connect());
  krpc::services::KRPC krpc(&(clients[0]));
  krpc::schema::Status status = krpc.get_status();
  ASSERT_TRUE(is_version(status.version().c_str())) << status.version();
}

TEST_F(test_client, test_version) {
  krpc::schema::Status status = krpc.get_status();
  ASSERT_TRUE(is_version(status.version().c_str())) << status.version();
}

// These connect by port, so they are skipped where the server is listening on socket paths:
// there is no port to get wrong then, and the one they would fall back on is a guess that
// says nothing about the client.
TEST_F(test_client, test_wrong_rpc_port) {
  if (get_rpc_path() != nullptr) GTEST_SKIP() << "the server is listening on socket paths";
  ASSERT_THROW(krpc::connect("C++ClientTestWrongRpcPort", "localhost", unused_port(),
                             get_stream_port(), connect_timeout),
               std::exception);
}

TEST_F(test_client, test_wrong_stream_port) {
  if (get_rpc_path() != nullptr) GTEST_SKIP() << "the server is listening on socket paths";
  ASSERT_THROW(krpc::connect("C++ClientTestWrongStreamPort", "localhost", get_rpc_port(),
                             unused_port(), connect_timeout),
               std::exception);
}

TEST_F(test_client, test_wrong_rpc_server) {
  auto fn = [this]() { connect("C++ClientTestWrongRpcServer", "stream", "stream"); };
  ASSERT_THROW(fn(), krpc::ConnectionError);
  try {
    fn();
  } catch (krpc::ConnectionError& e) {
    ASSERT_STREQ(e.what(),
                 "Connection request was for the rpc server, but this is the stream server. "
                 "Did you connect to the wrong port number or socket path?");
  }
}

TEST_F(test_client, test_wrong_stream_server) {
  auto fn = [this]() { connect("C++ClientTestWrongStreamServer", "rpc", "rpc"); };
  ASSERT_THROW(fn(), krpc::ConnectionError);
  try {
    fn();
  } catch (krpc::ConnectionError& e) {
    ASSERT_STREQ(e.what(),
                 "Connection request was for the stream server, but this is the rpc server. "
                 "Did you connect to the wrong port number or socket path?");
  }
}

TEST_F(test_client, test_value_parameters) {
  ASSERT_EQ("3.14159", test_service.float_to_string(3.14159f));
  // Modern .NET formats doubles with the shortest round-trippable
  // representation, unlike .NET Framework/mono
  ASSERT_EQ("3.141590118408203", test_service.double_to_string(3.14159f));
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
  std::string prefix("TestService::TestClass<");
  ASSERT_TRUE(!stream.str().compare(0, prefix.size(), prefix));
  ASSERT_EQ("value=jeb", object.get_value());
}

TEST_F(test_client, test_class_none_value) {
  std::optional<krpc::services::TestService::TestClass> none;
  ASSERT_FALSE(test_service.echo_test_object(none).has_value());
  krpc::services::TestService::TestClass object = test_service.create_test_object("bob");
  ASSERT_EQ("bobnull", object.object_to_string(none));
  test_service.set_object_property(none);
  ASSERT_FALSE(test_service.object_property().has_value());
}

TEST_F(test_client, test_nullable_non_class_values) {
  // Nullable value-type, string and collection parameters and return values
  ASSERT_EQ(42, test_service.echo_nullable_int(42).value());
  ASSERT_FALSE(test_service.echo_nullable_int(std::nullopt).has_value());
  ASSERT_EQ("foo", test_service.echo_nullable_string("foo").value());
  ASSERT_FALSE(test_service.echo_nullable_string(std::nullopt).has_value());
  std::vector<int32_t> list = {1, 2, 3};
  ASSERT_EQ(list, test_service.echo_nullable_list(list).value());
  ASSERT_FALSE(test_service.echo_nullable_list(std::nullopt).has_value());
}

// The C++ type of a parameter carries whether it can hold null, so a null at a parameter
// that is not nullable is rejected by the compiler rather than by the server
static_assert(std::is_invocable_v<decltype(&krpc::services::TestService::echo_test_object),
                                  krpc::services::TestService&, std::nullopt_t>);
static_assert(!std::is_invocable_v<decltype(&krpc::services::TestService::not_nullable_object),
                                   krpc::services::TestService&, std::nullopt_t>);

TEST_F(test_client, test_nullable_class_method) {
  std::optional<krpc::services::TestService::TestClass> none;
  krpc::services::TestService::TestClass obj = test_service.create_test_object("jeb");
  krpc::services::TestService::TestClass obj2 = test_service.create_test_object("bob");
  ASSERT_EQ(obj2, obj.echo_nullable_object(obj2));
  ASSERT_FALSE(obj.echo_nullable_object(none).has_value());
}

TEST_F(test_client, test_nullable_class_static_method) {
  std::optional<krpc::services::TestService::TestClass> none;
  krpc::services::TestService::TestClass obj = test_service.create_test_object("jeb");
  ASSERT_EQ(obj, krpc::services::TestService::TestClass::static_nullable_object(conn, obj));
  auto result = krpc::services::TestService::TestClass::static_nullable_object(conn, none);
  ASSERT_FALSE(result.has_value());
}

TEST_F(test_client, test_nullable_property) {
  std::optional<krpc::services::TestService::TestClass> none;
  krpc::services::TestService::TestClass obj = test_service.create_test_object("jeb");
  // ObjectProperty is nullable and its setter accepts null
  test_service.set_object_property(none);
  ASSERT_FALSE(test_service.object_property().has_value());
  // NullableObject is nullable for reads, but its setter guards against null, so writing
  // null raises the server's ArgumentNullException
  test_service.set_nullable_object(obj);
  ASSERT_EQ(obj, test_service.nullable_object());
  ASSERT_THROW(test_service.set_nullable_object(none), krpc::services::KRPC::ArgumentNullException);
}

TEST_F(test_client, test_empty_collection_default) {
  // An empty-collection default is distinguishable from no default: the argument can be
  // omitted and the empty list is used.
  ASSERT_EQ(std::vector<std::string>(), test_service.empty_list_default());
  std::vector<std::string> list = {"foo", "bar"};
  ASSERT_EQ(list, test_service.empty_list_default(list));
}

TEST_F(test_client, test_class_methods) {
  krpc::services::TestService::TestClass obj = test_service.create_test_object("bob");
  ASSERT_EQ("value=bob", obj.get_value());
  ASSERT_EQ("bob3.14159", obj.float_to_string(3.14159));
  krpc::services::TestService::TestClass obj2 = test_service.create_test_object("bill");
  ASSERT_EQ("bobbill", obj.object_to_string(obj2));
}

TEST_F(test_client, test_class_static_methods) {
  ASSERT_EQ("jeb", krpc::services::TestService::TestClass::static_method(conn));
  ASSERT_EQ("jebbobbill",
            krpc::services::TestService::TestClass::static_method(conn, "bob", "bill"));
}

TEST_F(test_client, test_class_properties) {
  krpc::services::TestService::TestClass object = test_service.create_test_object("jeb");
  object.set_int_property(0);
  ASSERT_EQ(0, object.int_property());
  object.set_int_property(42);
  ASSERT_EQ(42, object.int_property());
  krpc::services::TestService::TestClass object2 = test_service.create_test_object("kermin");
  object.set_object_property(object2);
  ASSERT_EQ(object2, object.object_property());
  object.set_string_property_private_get("bob");
  ASSERT_EQ("bob", object.string_property_private_set());
}

TEST_F(test_client, test_optional_arguments) {
  ASSERT_EQ("jebfoobarnull", test_service.optional_arguments("jeb"));
  ASSERT_EQ("jebbobbillnull", test_service.optional_arguments("jeb", "bob", "bill"));
  krpc::services::TestService::TestClass obj = test_service.create_test_object("kermin");
  ASSERT_EQ("jebbobbillkermin", test_service.optional_arguments("jeb", "bob", "bill", obj));
}

TEST_F(test_client, test_blocking_procedure) {
  ASSERT_EQ(0, test_service.blocking_procedure(0, 0));
  ASSERT_EQ(1, test_service.blocking_procedure(1, 0));
  ASSERT_EQ(1 + 2, test_service.blocking_procedure(2));
  int expected = 0;
  for (int i = 1; i <= 42; i++) expected += i;
  ASSERT_EQ(expected, test_service.blocking_procedure(42));
}

TEST_F(test_client, test_enums) {
  ASSERT_EQ(krpc::services::TestService::TestEnum::value_b, test_service.enum_return());
  ASSERT_EQ(krpc::services::TestService::TestEnum::value_a,
            test_service.enum_echo(krpc::services::TestService::TestEnum::value_a));
  ASSERT_EQ(krpc::services::TestService::TestEnum::value_b,
            test_service.enum_echo(krpc::services::TestService::TestEnum::value_b));
  ASSERT_EQ(krpc::services::TestService::TestEnum::value_c,
            test_service.enum_echo(krpc::services::TestService::TestEnum::value_c));
  ASSERT_EQ(krpc::services::TestService::TestEnum::value_a,
            test_service.enum_default_arg(krpc::services::TestService::TestEnum::value_a));
  ASSERT_EQ(krpc::services::TestService::TestEnum::value_c, test_service.enum_default_arg());
  ASSERT_EQ(krpc::services::TestService::TestEnum::value_b,
            test_service.enum_default_arg(krpc::services::TestService::TestEnum::value_b));
  std::vector<krpc::services::TestService::TestEnum> enums = {
      krpc::services::TestService::TestEnum::value_b,
      krpc::services::TestService::TestEnum::value_c};
  ASSERT_EQ(enums, test_service.enum_list_default());
  enums = {krpc::services::TestService::TestEnum::value_a,
           krpc::services::TestService::TestEnum::value_b};
  ASSERT_EQ(enums, test_service.enum_list_default(enums));
}

TEST_F(test_client, test_collections) {
  ASSERT_EQ(std::vector<int>(), test_service.increment_list(std::vector<int>()));
  {
    std::vector<int> l1;
    l1.push_back(0);
    l1.push_back(1);
    l1.push_back(2);
    std::vector<int> l2;
    l2.push_back(1);
    l2.push_back(2);
    l2.push_back(3);
    ASSERT_EQ(l2, test_service.increment_list(l1));
  }
  {
    std::map<std::string, int> m;
    ASSERT_EQ(m, test_service.increment_dictionary(m));
  }
  {
    std::map<std::string, int> m1;
    m1["a"] = 0;
    m1["b"] = 1;
    m1["c"] = 2;
    std::map<std::string, int> m2;
    m2["a"] = 1;
    m2["b"] = 2;
    m2["c"] = 3;
    ASSERT_EQ(m2, test_service.increment_dictionary(m1));
  }
  {
    std::set<int> s;
    ASSERT_EQ(s, test_service.increment_set(s));
  }
  {
    std::set<int> s1;
    s1.insert(0);
    s1.insert(1);
    s1.insert(2);
    std::set<int> s2;
    s2.insert(1);
    s2.insert(2);
    s2.insert(3);
    ASSERT_EQ(s2, test_service.increment_set(s1));
  }
  {
    std::tuple<int, int> t1(1, 2);
    std::tuple<int, int> t2(2, 3);
    ASSERT_EQ(t2, test_service.increment_tuple(t1));
  }
}

TEST_F(test_client, test_nested_collections) {
  {
    std::map<std::string, std::vector<int32_t>> m;
    ASSERT_EQ(m, test_service.increment_nested_collection(m));
  }
  {
    std::map<std::string, std::vector<int32_t>> m1;
    m1["a"] = std::vector<int>();
    m1["a"].push_back(0);
    m1["a"].push_back(1);
    m1["b"] = std::vector<int>();
    m1["c"] = std::vector<int>();
    m1["c"].push_back(2);
    std::map<std::string, std::vector<int32_t>> m2;
    m2["a"] = std::vector<int>();
    m2["a"].push_back(1);
    m2["a"].push_back(2);
    m2["b"] = std::vector<int>();
    m2["c"] = std::vector<int>();
    m2["c"].push_back(3);
    ASSERT_EQ(m2, test_service.increment_nested_collection(m1));
  }
}

TEST_F(test_client, test_collections_of_objects) {
  typedef std::vector<krpc::services::TestService::TestClass> ListType;
  ListType l1;
  ListType l2 = test_service.add_to_object_list(l1, "jeb");
  ASSERT_EQ(1u, l2.size());
  ASSERT_EQ("value=jeb", l2[0].get_value());
  ListType l3 = test_service.add_to_object_list(l2, "bob");
  ASSERT_EQ(2u, l3.size());
  ASSERT_EQ("value=jeb", l3[0].get_value());
  ASSERT_EQ("value=bob", l3[1].get_value());
}

TEST_F(test_client, test_structs) {
  krpc::services::TestService::TestStruct value(
      42, "jeb", krpc::services::TestService::TestEnum::value_b, {1, 2, 3});
  auto result = test_service.struct_echo(value);
  ASSERT_EQ(value, result);
  ASSERT_EQ(42, result.int_field);
  ASSERT_EQ("jeb", result.string_field);
  ASSERT_EQ(krpc::services::TestService::TestEnum::value_b, result.enum_field);
  ASSERT_EQ(std::vector<int32_t>({1, 2, 3}), result.list_field);
}

TEST_F(test_client, test_nested_structs) {
  auto obj = test_service.create_test_object("bob");
  krpc::services::TestService::TestNestedStruct value(
      krpc::services::TestService::TestStruct(1, "jeb",
                                              krpc::services::TestService::TestEnum::value_a, {}),
      obj, "bill");
  auto result = test_service.nested_struct_echo(value);
  ASSERT_EQ(value, result);
  ASSERT_EQ(1, result.struct_field.int_field);
  ASSERT_EQ(obj, result.object_field);
  ASSERT_EQ("bill", result.string_field);
}

TEST_F(test_client, test_collections_of_structs) {
  std::vector<krpc::services::TestService::TestStruct> values{
      krpc::services::TestService::TestStruct(0, "jeb",
                                              krpc::services::TestService::TestEnum::value_c, {}),
      krpc::services::TestService::TestStruct(1, "bob",
                                              krpc::services::TestService::TestEnum::value_c, {})};
  auto result = test_service.increment_list_of_structs(values);
  ASSERT_EQ(2, result.size());
  ASSERT_EQ(1, result[0].int_field);
  ASSERT_EQ(2, result[1].int_field);
}

TEST_F(test_client, test_nullable_struct_fields) {
  auto obj = test_service.create_test_object("jeb");
  krpc::services::TestService::TestNullableStruct value(
      1, 2, krpc::services::TestService::TestEnum::value_b, "jeb", obj);
  auto result = test_service.nullable_struct_echo(value);
  ASSERT_EQ(value, result);
  ASSERT_EQ(2, result.nullable_int_field);
  ASSERT_EQ(krpc::services::TestService::TestEnum::value_b, result.nullable_enum_field);
  ASSERT_EQ("jeb", result.nullable_string_field);
  ASSERT_EQ(obj, result.nullable_object_field);
}

TEST_F(test_client, test_null_struct_fields) {
  // A position inside a value is an optional, and an empty one is null
  krpc::services::TestService::TestNullableStruct value(1, {}, {}, {}, {});
  auto result = test_service.nullable_struct_echo(value);
  ASSERT_EQ(1, result.int_field);
  ASSERT_FALSE(result.nullable_int_field.has_value());
  ASSERT_FALSE(result.nullable_enum_field.has_value());
  ASSERT_FALSE(result.nullable_string_field.has_value());
  ASSERT_FALSE(result.nullable_object_field.has_value());
}

TEST_F(test_client, test_nullable_list_elements) {
  std::vector<std::optional<int32_t>> ints{1, {}, 3};
  ASSERT_EQ(ints, test_service.echo_list_of_nullable_ints(ints));
  auto obj = test_service.create_test_object("jeb");
  std::vector<std::optional<krpc::services::TestService::TestClass>> objects{obj, {}};
  ASSERT_EQ(objects, test_service.echo_list_of_nullable_objects(objects));
}

TEST_F(test_client, test_nullable_dictionary_values) {
  auto obj = test_service.create_test_object("jeb");
  std::map<std::string, std::optional<krpc::services::TestService::TestClass>> value{{"a", obj},
                                                                                     {"b", {}}};
  ASSERT_EQ(value, test_service.echo_dictionary_of_nullable_objects(value));
}

TEST_F(test_client, test_nullable_tuple_item) {
  auto obj = test_service.create_test_object("jeb");
  std::tuple<int32_t, std::optional<krpc::services::TestService::TestClass>> present{1, obj};
  ASSERT_EQ(present, test_service.echo_tuple_with_a_nullable_object(present));
  std::tuple<int32_t, std::optional<krpc::services::TestService::TestClass>> absent{1, {}};
  ASSERT_EQ(absent, test_service.echo_tuple_with_a_nullable_object(absent));
}

TEST_F(test_client, test_nullable_nested_list_elements) {
  auto obj = test_service.create_test_object("jeb");
  std::vector<std::vector<std::optional<krpc::services::TestService::TestClass>>> value{{obj, {}},
                                                                                        {}};
  ASSERT_EQ(value, test_service.echo_nested_list_of_nullable_objects(value));
}

TEST_F(test_client, test_struct_default_value) {
  krpc::services::TestService::TestStruct value(
      42, "jeb", krpc::services::TestService::TestEnum::value_b, {1, 2, 3});
  ASSERT_EQ(value, test_service.struct_default());
}

TEST_F(test_client, test_struct_comparison) {
  typedef krpc::services::TestService::TestStruct Struct;
  Struct a(1, "jeb", krpc::services::TestService::TestEnum::value_a, {1, 2});
  Struct b(1, "jeb", krpc::services::TestService::TestEnum::value_a, {1, 2});
  Struct c(2, "jeb", krpc::services::TestService::TestEnum::value_a, {1, 2});

  ASSERT_EQ(a, b);
  ASSERT_NE(a, c);
  // Ordered by the fields in turn, as a tuple of the same values is
  ASSERT_LT(a, c);
  ASSERT_GT(c, a);
  ASSERT_LE(a, b);
  ASSERT_GE(a, b);

  std::set<Struct> set{c, a, b};
  ASSERT_EQ(2u, set.size());
  ASSERT_EQ(a, *set.begin());

  // A structure is a type of our own, so it gets a std::hash and needs nothing further to be
  // the key of an unordered container
  std::unordered_set<Struct> hashed{a, b, c};
  ASSERT_EQ(2u, hashed.size());
  ASSERT_EQ(std::hash<Struct>()(a), std::hash<Struct>()(b));
  // A collection of structures hashes too, through krpc::hash
  ASSERT_EQ(krpc::hash_value(std::vector<Struct>{a}), krpc::hash_value(std::vector<Struct>{b}));
}

TEST_F(test_client, test_collections_default_values) {
  std::tuple<int, bool> t{1, false};
  ASSERT_EQ(t, test_service.tuple_default());
  std::vector<int> l{1, 2, 3};
  ASSERT_EQ(l, test_service.list_default());
  std::set<int> s{1, 2, 3};
  ASSERT_EQ(s, test_service.set_default());
  std::map<int, bool> m{{1, false}, {2, true}};
  ASSERT_EQ(m, test_service.dictionary_default());
}

TEST_F(test_client, test_test_service_enum_members) {
  ASSERT_EQ(0, static_cast<int>(krpc::services::TestService::TestEnum::value_a));
  ASSERT_EQ(1, static_cast<int>(krpc::services::TestService::TestEnum::value_b));
  ASSERT_EQ(2, static_cast<int>(krpc::services::TestService::TestEnum::value_c));
}

// An error naming a service whose generated header was never instantiated has no registered
// thrower, and must be reported and not looked up past the end of the map. This client
// deliberately never constructs the TestService object, so nothing registers its exceptions.
TEST_F(test_client, test_unknown_exception_type) {
  krpc::Client client = connect("C++ClientTestUnknownExceptionType");
  try {
    client.invoke("TestService", "ThrowCustomException");
    FAIL() << "expected an exception";
  } catch (const krpc::RPCError& exn) {
    ASSERT_THAT(std::string(exn.what()), testing::HasSubstr("TestService.CustomException"));
    ASSERT_THAT(std::string(exn.what()), testing::HasSubstr("A custom kRPC exception"));
  }
}

TEST_F(test_client, test_invalid_operation_exception) {
  ASSERT_THROW(test_service.throw_invalid_operation_exception(),
               krpc::services::KRPC::InvalidOperationException);
  try {
    test_service.throw_invalid_operation_exception();
  } catch (std::runtime_error& e) {
    EXPECT_THAT(e.what(), testing::HasSubstr("Invalid operation"));
  }
}

TEST_F(test_client, test_argument_exception) {
  ASSERT_THROW(test_service.throw_argument_exception(), krpc::services::KRPC::ArgumentException);
  try {
    test_service.throw_argument_exception();
  } catch (krpc::services::KRPC::ArgumentException& e) {
    EXPECT_THAT(e.what(), testing::HasSubstr("Invalid argument"));
  }
}

TEST_F(test_client, test_argument_null_exception) {
  ASSERT_THROW(test_service.throw_argument_null_exception(""),
               krpc::services::KRPC::ArgumentNullException);
  try {
    test_service.throw_argument_null_exception("");
  } catch (krpc::services::KRPC::ArgumentNullException& e) {
    // The parameter name formatting differs between .NET Framework/mono
    // ("Parameter name: foo") and modern .NET ("(Parameter 'foo')")
    EXPECT_THAT(e.what(), testing::HasSubstr("Value cannot be null."));
    EXPECT_THAT(e.what(), testing::HasSubstr("foo"));
  }
}

TEST_F(test_client, test_argument_out_of_range_exception) {
  ASSERT_THROW(test_service.throw_argument_out_of_range_exception(0),
               krpc::services::KRPC::ArgumentOutOfRangeException);
  try {
    test_service.throw_argument_out_of_range_exception(0);
  } catch (krpc::services::KRPC::ArgumentOutOfRangeException& e) {
    EXPECT_THAT(e.what(),
                testing::HasSubstr("Specified argument was out of the range of valid values."));
    EXPECT_THAT(e.what(), testing::HasSubstr("foo"));
  }
}

TEST_F(test_client, test_custom_exception) {
  ASSERT_THROW(test_service.throw_custom_exception(), krpc::services::TestService::CustomException);
  try {
    test_service.throw_custom_exception();
  } catch (krpc::services::TestService::CustomException& e) {
    EXPECT_THAT(e.what(), testing::HasSubstr("A custom kRPC exception"));
  }
}

TEST_F(test_client, test_line_endings) {
  std::vector<std::string> strings;
  strings.push_back("foo\nbar");
  strings.push_back("foo\rbar");
  strings.push_back("foo\n\rbar");
  strings.push_back("foo\r\nbar");
  strings.push_back(
      "foo"
      "\x10"
      "bar");
  strings.push_back(
      "foo"
      "\x13"
      "bar");
  strings.push_back(
      "foo"
      "\x10\x13"
      "bar");
  strings.push_back(
      "foo"
      "\x13\x10"
      "bar");
  for (std::vector<std::string>::const_iterator i = strings.begin(); i != strings.end(); i++) {
    test_service.set_string_property(*i);
    ASSERT_EQ(*i, test_service.string_property());
  }
}

TEST_F(test_client, test_thread_safe) {
  const int thread_count = 2;
  const int repeats = 1000;

  std::atomic_int count;
  count = thread_count;

  std::vector<std::thread> threads;
  for (int i = 0; i < thread_count; i++)
    threads.push_back(std::thread(
        [this](std::atomic_int* count) {
          for (int j = 0; j < repeats; j++) {
            ASSERT_EQ("False", test_service.bool_to_string(false));
            ASSERT_EQ(12345, test_service.string_to_int32("12345"));
          }
          (*count)--;
        },
        &count));

  for (auto& t : threads) t.join();
  ASSERT_EQ(count, 0);
}
