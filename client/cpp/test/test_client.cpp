#include <gmock/gmock.h>
#include <gtest/gtest.h>

#include <map>
#include <set>
#include <string>
#include <tuple>
#include <vector>

#include <krpc/platform.hpp>
#include <krpc/services/krpc.hpp>

#include "server_test.hpp"
#include "services/test_service.hpp"

class test_client: public server_test {
};

TEST_F(test_client, test_default_ctor) {
  krpc::Client client;
}

TEST_F(test_client, test_shared_ptr) {
  auto client = std::make_shared<krpc::Client>(
    "C++ClientTest", "localhost", get_rpc_port(), get_stream_port());
  krpc::services::KRPC krpc(client.get());
  krpc::schema::Status status = krpc.get_status();
  ASSERT_THAT(status.version(), testing::MatchesRegex("[0-9]+\\.[0-9]+\\.[0-9]+"));
  client.reset();
}

TEST_F(test_client, test_std_container) {
  std::vector<krpc::Client> clients;
  clients.push_back(krpc::connect("C++ClientTest", "localhost", get_rpc_port(), get_stream_port()));
  krpc::services::KRPC krpc(&(clients[0]));
  krpc::schema::Status status = krpc.get_status();
  ASSERT_THAT(status.version(), testing::MatchesRegex("[0-9]+\\.[0-9]+\\.[0-9]+"));
}

TEST_F(test_client, test_version) {
  krpc::schema::Status status = krpc.get_status();
  ASSERT_THAT(status.version(), testing::MatchesRegex("[0-9]+\\.[0-9]+\\.[0-9]+"));
}

TEST_F(test_client, test_current_game_scene) {
  krpc::services::KRPC::GameScene scene = krpc.current_game_scene();
  ASSERT_EQ(krpc::services::KRPC::GameScene::space_center, scene);
}

TEST_F(test_client, test_error) {
  ASSERT_THROW(test_service.throw_argument_exception(), krpc::RPCError);
  try {
    test_service.throw_argument_exception();
  } catch(krpc::RPCError& e) {
    EXPECT_THAT(e.what(), testing::HasSubstr("Invalid argument"));
  }
  ASSERT_THROW(test_service.throw_invalid_operation_exception(), krpc::RPCError);
  try {
    test_service.throw_invalid_operation_exception();
  } catch(krpc::RPCError& e) {
    EXPECT_THAT(e.what(), testing::HasSubstr("Invalid operation"));
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
  std::string prefix("TestService::TestClass<");
  ASSERT_TRUE(!stream.str().compare(0, prefix.size(), prefix));
  ASSERT_EQ("value=jeb", object.get_value());
}

TEST_F(test_client, test_class_none_value) {
  krpc::services::TestService::TestClass none;
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
}

TEST_F(test_client, test_optional_arguments) {
  ASSERT_EQ("jebfoobarbaz", test_service.optional_arguments("jeb"));
  ASSERT_EQ("jebbobbillbaz", test_service.optional_arguments("jeb", "bob", "bill"));
}

TEST_F(test_client, test_blocking_procedure) {
  ASSERT_EQ(0, test_service.blocking_procedure(0, 0));
  ASSERT_EQ(1, test_service.blocking_procedure(1, 0));
  ASSERT_EQ(1+2, test_service.blocking_procedure(2));
  int expected = 0;
  for (int i = 1; i <= 42; i++)
    expected += i;
  ASSERT_EQ(expected, test_service.blocking_procedure(42));
}

TEST_F(test_client, test_enums) {
  ASSERT_EQ(krpc::services::TestService::TestEnum::value_b,
            test_service.enum_return());
  ASSERT_EQ(krpc::services::TestService::TestEnum::value_a,
            test_service.enum_echo(krpc::services::TestService::TestEnum::value_a));
  ASSERT_EQ(krpc::services::TestService::TestEnum::value_b,
            test_service.enum_echo(krpc::services::TestService::TestEnum::value_b));
  ASSERT_EQ(krpc::services::TestService::TestEnum::value_c,
            test_service.enum_echo(krpc::services::TestService::TestEnum::value_c));
  ASSERT_EQ(krpc::services::TestService::TestEnum::value_a,
            test_service.enum_default_arg(krpc::services::TestService::TestEnum::value_a));
  ASSERT_EQ(krpc::services::TestService::TestEnum::value_c,
            test_service.enum_default_arg());
  ASSERT_EQ(krpc::services::TestService::TestEnum::value_b,
            test_service.enum_default_arg(krpc::services::TestService::TestEnum::value_b));
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
    std::map<std::string, std::vector<google::protobuf::int32>> m;
    ASSERT_EQ(m, test_service.increment_nested_collection(m));
  }
  {
    std::map<std::string, std::vector<google::protobuf::int32>> m1;
    m1["a"] = std::vector<int>();
    m1["a"].push_back(0);
    m1["a"].push_back(1);
    m1["b"] = std::vector<int>();
    m1["c"] = std::vector<int>();
    m1["c"].push_back(2);
    std::map<std::string, std::vector<google::protobuf::int32>> m2;
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
  ASSERT_EQ(1, l2.size());
  ASSERT_EQ("value=jeb", l2[0].get_value());
  ListType l3 = test_service.add_to_object_list(l2, "bob");
  ASSERT_EQ(2, l3.size());
  ASSERT_EQ("value=jeb", l3[0].get_value());
  ASSERT_EQ("value=bob", l3[1].get_value());
}

TEST_F(test_client, test_collections_default_values) {
  std::tuple<int, bool> t {1, false};
  ASSERT_EQ(t, test_service.tuple_default());
  std::vector<int> l {1, 2, 3};
  ASSERT_EQ(l, test_service.list_default());
  std::set<int> s {1, 2, 3};
  ASSERT_EQ(s, test_service.set_default());
  std::map<int, bool> m {{1, false}, {2, true}};
  ASSERT_EQ(m, test_service.dictionary_default());
}

TEST_F(test_client, test_test_service_enum_members) {
  ASSERT_EQ(0, static_cast<int>(krpc::services::TestService::TestEnum::value_a));
  ASSERT_EQ(1, static_cast<int>(krpc::services::TestService::TestEnum::value_b));
  ASSERT_EQ(2, static_cast<int>(krpc::services::TestService::TestEnum::value_c));
}

TEST_F(test_client, test_line_endings) {
  std::vector<std::string> strings;
  strings.push_back("foo\nbar");
  strings.push_back("foo\rbar");
  strings.push_back("foo\n\rbar");
  strings.push_back("foo\r\nbar");
  strings.push_back("foo""\x10""bar");
  strings.push_back("foo""\x13""bar");
  strings.push_back("foo""\x10\x13""bar");
  strings.push_back("foo""\x13\x10""bar");
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
    threads.push_back(
      std::thread(
        [this](std::atomic_int* count) {
          for (int j = 0; j < repeats; j++) {
            ASSERT_EQ("False", test_service.bool_to_string(false));
            ASSERT_EQ(12345, test_service.string_to_int32("12345"));
          }
          (*count)--;
        },
        &count));

  for (auto& t : threads)
    t.join();
  ASSERT_EQ(count, 0);
}
