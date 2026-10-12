#include <gtest/gtest-message.h>
#include <gtest/gtest-test-part.h>

#include <atomic>
#include <cmath>
#include <map>
#include <optional>
#include <set>
#include <stdexcept>
#include <string>
#include <tuple>
#include <vector>

#include "gtest/gtest.h"
#include "krpc/error.hpp"
#include "krpc/event.hpp"
#include "krpc/function_stream.hpp"
#include "krpc/services/krpc.hpp"
#include "krpc/stream.hpp"
#include "server_test.hpp"
#include "services/test_service.hpp"

class test_function_stream : public server_test {};

typedef krpc::services::KRPC::Expression Expr;
typedef krpc::services::KRPC::Type KType;

TEST_F(test_function_stream, test_function_stream) {
  test_service.set_string_property("foo");
  auto expr = Expr::call(conn, test_service.string_property_call());
  auto stream = krpc::add_stream<std::string>(expr);
  ASSERT_EQ("foo", stream());
  stream.remove();
}

TEST_F(test_function_stream, test_computed_value) {
  auto counter = Expr::call(conn, test_service.counter_call("CppFunctionStream.Computed", 1));
  // int RPC result * double constant promotes to double
  auto expr = Expr::multiply(conn, counter, Expr::constant_double(conn, 0.5));
  auto stream = krpc::add_stream<double>(expr);
  auto value = stream();
  ASSERT_GE(value, 0.5);
  ASSERT_EQ(0, std::fmod(value * 2, 1));
  stream.remove();
}

TEST_F(test_function_stream, test_callback) {
  auto counter = Expr::call(conn, test_service.counter_call("CppFunctionStream.Callback", 1));
  auto expr = Expr::multiply(conn, counter, Expr::constant_double(conn, 0.5));
  auto stream = krpc::add_stream<double>(expr);
  std::atomic<double> value{0};
  stream.add_callback([&value](double x) { value = x; });
  while (value == 0) {
  }
  ASSERT_GE(value, 0.5);
  ASSERT_EQ(0, std::fmod(value * 2, 1));
  stream.remove();
}

TEST_F(test_function_stream, test_event_from_a_function) {
  auto event = krpc::add_event(Expr::constant_bool(conn, true));
  event.acquire();
  event.wait();
  event.release();
  event.remove();
}

TEST_F(test_function_stream, test_events_and_streams_over_one_function) {
  // Each add creates its own stream, and removing one leaves the other live
  auto never = Expr::constant_bool(conn, false);
  auto first = krpc::add_event(never);
  auto second = krpc::add_event(never);
  ASSERT_NE(first, second);
  first.remove();
  first.acquire();
  ASSERT_THROW(first.wait(), krpc::StreamError);
  first.release();
  second.acquire();
  ASSERT_FALSE(second.wait(0.1));
  second.release();
  second.remove();
  test_service.set_string_property("live");
  auto value = Expr::call(conn, test_service.string_property_call());
  auto stream1 = krpc::add_stream<std::string>(value);
  auto stream2 = krpc::add_stream<std::string>(value);
  ASSERT_NE(stream1, stream2);
  stream1.remove();
  ASSERT_THROW(stream1(), krpc::StreamError);
  ASSERT_EQ("live", stream2());
  stream2.remove();
}

TEST_F(test_function_stream, test_removing_a_stream_again_through_a_copy) {
  test_service.set_string_property("live");
  auto value = Expr::call(conn, test_service.string_property_call());
  auto first = krpc::add_stream<std::string>(value);
  auto copy = first;
  first.remove();
  // The server can give the new stream the removed one's id
  auto second = krpc::add_stream<std::string>(value);
  copy.remove();
  ASSERT_EQ("live", second());
  second.remove();
}

TEST_F(test_function_stream, test_per_element_call) {
  std::vector<krpc::services::TestService::TestClass> objs;
  std::vector<Expr> constants;
  for (int i = 0; i < 3; i++) {
    auto obj = test_service.create_test_object("expr" + std::to_string(i));
    obj.set_int_property(i + 1);
    objs.push_back(obj);
    constants.push_back(Expr::constant_object(conn, obj._id));
  }
  auto objects = Expr::create_list(conn, constants);
  auto param = Expr::parameter(conn, "x", KType::class_type(conn, "TestService", "TestClass"));
  auto get_property = Expr::call_with_arguments(conn, objs[0].int_property_call(),
                                                std::map<int32_t, Expr>({{0, param}}));
  auto selected = Expr::to_list(
      conn,
      Expr::select(conn, objects, Expr::lambda(conn, std::vector<Expr>({param}), get_property)));
  auto stream = krpc::add_stream<std::vector<int32_t>>(selected);
  ASSERT_EQ(std::vector<int32_t>({1, 2, 3}), stream());
  stream.remove();
}

TEST_F(test_function_stream, test_struct) {
  auto call = test_service.counter_struct_call("CppFunctionStream.Struct");
  auto value = Expr::call(conn, call);
  auto stream = krpc::add_stream<krpc::services::TestService::TestStruct>(value);
  ASSERT_EQ("CppFunctionStream.Struct", stream().string_field);
  stream.remove();
  auto field =
      krpc::add_stream<std::string>(Expr::get_field(conn, Expr::call(conn, call), "StringField"));
  ASSERT_EQ("CppFunctionStream.Struct", field());
  field.remove();
}

TEST_F(test_function_stream, test_create_struct) {
  auto value = Expr::create_struct(
      conn, KType::struct_type(conn, "TestService", "TestStruct"),
      std::vector<Expr>(
          {Expr::constant_int(conn, 3), Expr::constant_string(conn, "built"),
           Expr::cast(conn, Expr::constant_int(conn, 0),
                      KType::enumeration_type(conn, "TestService", "TestEnum")),
           Expr::create_list(conn, std::vector<Expr>({Expr::constant_int(conn, 7)}))}));
  auto result = krpc::run<krpc::services::TestService::TestStruct>(value);
  ASSERT_EQ(3, result.int_field);
  ASSERT_EQ("built", result.string_field);
  ASSERT_EQ(std::vector<int32_t>({7}), result.list_field);
}

TEST_F(test_function_stream, test_run_function) {
  auto obj = test_service.create_test_object("run");
  obj.set_int_property(20);
  auto expr =
      Expr::multiply(conn, Expr::call(conn, obj.int_property_call()), Expr::constant_int(conn, 2));
  ASSERT_EQ(40, krpc::run<int32_t>(expr));
}

TEST_F(test_function_stream, test_run_function_with_arguments) {
  auto x = Expr::parameter(conn, "x", KType::double_(conn));
  auto n = Expr::parameter(conn, "n", KType::int_(conn));
  auto function = Expr::lambda(conn, {x, n}, Expr::multiply(conn, x, n));
  ASSERT_EQ(7.5, krpc::run<double>(function, 2.5, 3));
  // An int widens to a double parameter
  ASSERT_EQ(-8.0, krpc::run<double>(function, 2, -4));
  ASSERT_THROW(krpc::run<double>(function, 1.0), std::invalid_argument);
  ASSERT_THROW(krpc::run<double>(function, 1.0, 2.5), std::invalid_argument);
  // A number converts to a wider type
  ASSERT_EQ(6.0, krpc::run<double>(function, static_cast<short>(2), static_cast<char>(3)));
  auto l = Expr::parameter(conn, "l", KType::long_(conn));
  auto long_identity = Expr::lambda(conn, {l}, l);
  ASSERT_EQ(7, krpc::run<int64_t>(long_identity, static_cast<uint32_t>(7)));
  ASSERT_EQ(8, krpc::run<int64_t>(long_identity, 8LL));
  // An integer that is not negative converts to an unsigned parameter
  auto u = Expr::parameter(conn, "u", KType::u_int(conn));
  auto unsigned_identity = Expr::lambda(conn, {u}, u);
  ASSERT_EQ(5u, krpc::run<uint32_t>(unsigned_identity, 5));
  ASSERT_EQ(5u, krpc::run<uint32_t>(unsigned_identity, static_cast<int16_t>(5)));
  // An integer converts to an unsigned parameter at least as wide
  ASSERT_THROW(krpc::run<uint32_t>(unsigned_identity, static_cast<int64_t>(5)),
               std::invalid_argument);
  ASSERT_THROW(krpc::run<uint32_t>(unsigned_identity, static_cast<uint64_t>(5)),
               std::invalid_argument);
  auto ul = Expr::parameter(conn, "ul", KType::u_long(conn));
  auto unsigned_long_identity = Expr::lambda(conn, {ul}, ul);
  ASSERT_EQ(5u, krpc::run<uint64_t>(unsigned_long_identity, static_cast<int64_t>(5)));
  ASSERT_EQ(5u, krpc::run<uint64_t>(unsigned_long_identity, static_cast<uint32_t>(5)));
  ASSERT_THROW(krpc::run<uint64_t>(unsigned_long_identity, static_cast<int64_t>(-5)),
               std::invalid_argument);
  try {
    krpc::run<uint32_t>(unsigned_identity, -5);
    FAIL();
  } catch (const std::invalid_argument& e) {
    ASSERT_EQ(std::string("Argument 1 does not match the type of its parameter"), e.what());
  }
}

TEST_F(test_function_stream, test_run_function_with_a_float_parameter) {
  auto f = Expr::parameter(conn, "f", KType::float_(conn));
  auto float_identity = Expr::lambda(conn, {f}, f);
  ASSERT_EQ(2.5f, krpc::run<float>(float_identity, 2.5f));
  ASSERT_EQ(2.0f, krpc::run<float>(float_identity, 2));
  // A double does not convert to a float implicitly
  ASSERT_THROW(krpc::run<float>(float_identity, 2.5), std::invalid_argument);
}

TEST_F(test_function_stream, test_run_function_checks_an_enumeration_argument_by_kind) {
  typedef krpc::services::TestService::TestEnum TestEnum;
  auto e = Expr::parameter(conn, "e", KType::enumeration_type(conn, "TestService", "TestEnum"));
  auto enum_identity = Expr::lambda(conn, {e}, e);
  ASSERT_EQ(TestEnum::value_c, krpc::run<TestEnum>(enum_identity, TestEnum::value_c));
  // Another service's enumeration passes as its value, which is 1 for TypeCode::double_
  ASSERT_EQ(TestEnum::value_b,
            krpc::run<TestEnum>(enum_identity, krpc::services::KRPC::TypeCode::double_));
  ASSERT_THROW(krpc::run<TestEnum>(enum_identity, 1), std::invalid_argument);
}

TEST_F(test_function_stream, test_run_function_checks_the_argument_count) {
  auto x = Expr::parameter(conn, "x", KType::double_(conn));
  auto identity = Expr::lambda(conn, {x}, x);
  try {
    krpc::run<double>(identity);
    FAIL();
  } catch (const std::invalid_argument& e) {
    ASSERT_EQ(std::string("The function takes 1 argument, got 0"), e.what());
  }
}

TEST_F(test_function_stream, test_run_function_with_collection_arguments) {
  auto ints = Expr::parameter(conn, "l", KType::list_type(conn, KType::int_(conn)));
  auto count_ints = Expr::lambda(conn, {ints}, Expr::count(conn, ints));
  // A number a collection holds converts as a number argument does
  ASSERT_EQ(3, krpc::run<int32_t>(count_ints, std::vector<int16_t>({1, 2, 3})));
  ASSERT_THROW(krpc::run<int32_t>(count_ints, std::vector<int64_t>({1})), std::invalid_argument);
  ASSERT_THROW(krpc::run<int32_t>(count_ints, std::vector<std::string>({"a"})),
               std::invalid_argument);
  auto longs = Expr::parameter(
      conn, "d", KType::dictionary_type(conn, KType::string(conn), KType::long_(conn)));
  auto count_longs = Expr::lambda(conn, {longs}, Expr::count(conn, longs));
  ASSERT_EQ(1, krpc::run<int32_t>(count_longs, std::map<std::string, long long>({{"a", 1}})));
  auto nullable = Expr::parameter(
      conn, "n", KType::list_type(conn, KType::nullable_type(conn, KType::int_(conn))));
  auto count_nullable = Expr::lambda(conn, {nullable}, Expr::count(conn, nullable));
  ASSERT_EQ(2, krpc::run<int32_t>(count_nullable,
                                  std::vector<std::optional<int16_t>>({1, std::nullopt})));
  ASSERT_THROW(krpc::run<int32_t>(count_ints, std::vector<std::optional<int32_t>>{1}),
               std::invalid_argument);
  auto pair = Expr::parameter(conn, "t",
                              KType::tuple_type(conn, {KType::double_(conn), KType::string(conn)}));
  auto tuple_identity = Expr::lambda(conn, {pair}, pair);
  ASSERT_EQ(std::make_tuple(2.0, std::string("a")),
            (krpc::run<std::tuple<double, std::string>>(
                tuple_identity, std::make_tuple(static_cast<int16_t>(2), "a"))));
  ASSERT_THROW((krpc::run<std::tuple<double, std::string>>(tuple_identity, std::make_tuple(2.0))),
               std::invalid_argument);
}

TEST_F(test_function_stream, test_run_function_with_object_and_null_arguments) {
  auto obj = test_service.create_test_object("argument");
  obj.set_int_property(4);
  auto param = Expr::parameter(conn, "obj", KType::class_type(conn, "TestService", "TestClass"));
  auto label = Expr::parameter(conn, "label", KType::nullable_type(conn, KType::string(conn)));
  auto get_property = Expr::call_with_arguments(conn, obj.int_property_call(), {{0, param}});
  auto function = Expr::lambda(conn, {param, label},
                               Expr::conditional(conn, Expr::is_null(conn, label), get_property,
                                                 Expr::constant_int(conn, 0)));
  ASSERT_EQ(4, krpc::run<int32_t>(function, obj, std::nullopt));
  ASSERT_EQ(4, krpc::run<int32_t>(function, obj, nullptr));
  ASSERT_EQ(0, krpc::run<int32_t>(function, obj, "label"));
  ASSERT_EQ(0, krpc::run<int32_t>(function, obj, std::optional<std::string>("label")));
}

TEST_F(test_function_stream, test_run_function_with_arguments_for_its_effects) {
  auto id = Expr::parameter(conn, "id", KType::string(conn));
  auto count = Expr::lambda(
      conn, {id}, Expr::call_with_arguments(conn, test_service.counter_call("", 1), {{0, id}}));
  auto before = test_service.counter("CppRunFunctionEffects", 1);
  krpc::run(count, "CppRunFunctionEffects");
  ASSERT_EQ(before + 2, test_service.counter("CppRunFunctionEffects", 1));
}

TEST_F(test_function_stream, test_nullable_elements) {
  std::vector<std::optional<int32_t>> ints = {5, std::nullopt};
  auto expr = Expr::call(conn, test_service.echo_list_of_nullable_ints_call(ints));
  ASSERT_EQ(ints, krpc::run<std::vector<std::optional<int32_t>>>(expr));
  // An element that can be null is sent with a flag saying whether it is
  ASSERT_THROW(krpc::run<std::vector<int32_t>>(expr), std::invalid_argument);
}

TEST_F(test_function_stream, test_nullable_object_elements) {
  typedef krpc::services::TestService::TestClass TestClass;
  auto obj = test_service.create_test_object("nullableelements");
  std::vector<std::optional<TestClass>> objects{obj, {}};
  auto expr = Expr::call(conn, test_service.echo_list_of_nullable_objects_call(objects));
  ASSERT_EQ(objects, krpc::run<std::vector<std::optional<TestClass>>>(expr));
  ASSERT_THROW(krpc::run<std::vector<TestClass>>(expr), std::invalid_argument);
  auto stream = krpc::add_stream<std::vector<std::optional<TestClass>>>(expr);
  ASSERT_EQ(objects, stream());
  stream.remove();
}

TEST_F(test_function_stream, test_run_function_null_result) {
  typedef krpc::services::TestService::TestClass TestClass;
  auto obj = test_service.create_test_object("runnull");
  auto expr = Expr::call(conn, obj.object_property_call());
  // The property is nullable and unset, so the function's value is null
  ASSERT_FALSE(krpc::run<std::optional<TestClass>>(expr).has_value());
  ASSERT_THROW(krpc::run<TestClass>(expr), std::runtime_error);
  auto stream = krpc::add_stream<std::optional<TestClass>>(expr);
  ASSERT_FALSE(stream().has_value());
  stream.remove();
  auto non_optional = krpc::add_stream<TestClass>(expr);
  ASSERT_THROW(non_optional(), std::runtime_error);
  non_optional.remove();
  auto other = test_service.create_test_object("runnull2");
  obj.set_object_property(other);
  ASSERT_EQ(other, krpc::run<std::optional<TestClass>>(expr).value());
}

TEST_F(test_function_stream, test_null_read_in_a_callback) {
  typedef krpc::services::TestService::TestClass TestClass;
  auto obj = test_service.create_test_object("callbacknull");
  auto expr = Expr::call(conn, obj.object_property_call());
  auto stream = krpc::add_stream<TestClass>(expr);
  ASSERT_THROW(stream(), std::runtime_error);
  // A callback added to a stream with a value is called with it straight away
  std::atomic<bool> called{false};
  testing::internal::CaptureStderr();
  stream.add_callback([&called](TestClass) { called = true; });
  auto output = testing::internal::GetCapturedStderr();
  ASSERT_FALSE(called);
  ASSERT_NE(std::string::npos, output.find("use a std::optional for the result"));
  stream.remove();
}

TEST_F(test_function_stream, test_run_function_side_effects) {
  auto obj = test_service.create_test_object("runeffect");
  obj.set_int_property(1);
  krpc::schema::ProcedureCall call;
  call.set_service("TestService");
  call.set_procedure("TestClass_set_IntProperty");
  auto expr =
      Expr::call_with_arguments(conn, call,
                                std::map<int32_t, Expr>({{0, Expr::constant_object(conn, obj._id)},
                                                         {1, Expr::constant_int(conn, 42)}}));
  krpc::run(expr);
  ASSERT_EQ(42, obj.int_property());
}

TEST_F(test_function_stream, test_run_function_checks_the_template_type) {
  auto obj = test_service.create_test_object("runcheck");
  obj.set_int_property(3);
  auto value = Expr::call(conn, obj.int_property_call());
  ASSERT_THROW(krpc::run<std::string>(value), std::invalid_argument);
  ASSERT_THROW(krpc::add_stream<std::vector<int32_t>>(value), std::invalid_argument);
  ASSERT_EQ(3, krpc::run<int32_t>(value));
  krpc::schema::ProcedureCall call;
  call.set_service("TestService");
  call.set_procedure("TestClass_set_IntProperty");
  auto effect =
      Expr::call_with_arguments(conn, call,
                                std::map<int32_t, Expr>({{0, Expr::constant_object(conn, obj._id)},
                                                         {1, Expr::constant_int(conn, 4)}}));
  ASSERT_THROW(krpc::run<int32_t>(effect), std::invalid_argument);
  ASSERT_THROW(krpc::add_stream<int32_t>(effect), std::invalid_argument);
  ASSERT_EQ(3, obj.int_property());
}

TEST_F(test_function_stream, test_template_type_of_a_collection) {
  auto one = Expr::constant_int(conn, 1);
  auto name = Expr::constant_string(conn, "a");
  auto tuple = Expr::create_tuple(conn, std::vector<Expr>({one, name}));
  ASSERT_EQ(std::make_tuple(1, std::string("a")),
            (krpc::run<std::tuple<int32_t, std::string>>(tuple)));
  ASSERT_THROW(krpc::run<std::tuple<int32_t>>(tuple), std::invalid_argument);
  ASSERT_THROW((krpc::run<std::tuple<std::string, int32_t>>(tuple)), std::invalid_argument);
  auto set = Expr::create_set(conn, std::set<Expr>({one}));
  ASSERT_EQ(std::set<int32_t>({1}), krpc::run<std::set<int32_t>>(set));
  ASSERT_THROW(krpc::run<std::vector<int32_t>>(set), std::invalid_argument);
  auto map = Expr::create_dictionary(conn, std::vector<Expr>({name}), std::vector<Expr>({one}));
  ASSERT_EQ((std::map<std::string, int32_t>({{"a", 1}})),
            (krpc::run<std::map<std::string, int32_t>>(map)));
  ASSERT_THROW((krpc::run<std::map<int32_t, std::string>>(map)), std::invalid_argument);
  auto enumeration =
      Expr::cast(conn, one, KType::enumeration_type(conn, "TestService", "TestEnum"));
  ASSERT_EQ(krpc::services::TestService::TestEnum::value_b,
            krpc::run<krpc::services::TestService::TestEnum>(enumeration));
  ASSERT_THROW(krpc::run<int32_t>(enumeration), std::invalid_argument);
}

TEST_F(test_function_stream, test_return_type) {
  auto expr = Expr::constant_double(conn, 1.5);
  ASSERT_EQ(krpc::services::KRPC::TypeCode::double_, expr.return_type().code());
  auto obj = test_service.create_test_object("expr_return_type");
  auto obj_type = Expr::constant_object(conn, obj._id).return_type();
  ASSERT_EQ(krpc::services::KRPC::TypeCode::class_, obj_type.code());
  ASSERT_EQ("TestService", obj_type.service());
  ASSERT_EQ("TestClass", obj_type.name());
}

TEST_F(test_function_stream, test_function_stream_without_a_stream_connection) {
  // A stream port of zero connects without streams, which connect_local cannot express
  if (get_rpc_path() != nullptr) GTEST_SKIP();
  auto client = krpc::connect("NoStreams", "localhost", get_rpc_port(), 0);
  auto expr = Expr::constant_int(client, 1);
  ASSERT_THROW(krpc::add_stream<int32_t>(expr), krpc::StreamError);
  krpc::services::TestService service(&client);
  ASSERT_THROW(service.string_property_stream(), krpc::StreamError);
}
