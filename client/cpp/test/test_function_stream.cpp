#include <gtest/gtest-message.h>
#include <gtest/gtest-test-part.h>

#include <cstdint>
#include <map>
#include <optional>
#include <stdexcept>
#include <string>
#include <tuple>
#include <vector>

#include "gtest/gtest.h"
#include "krpc/function_stream.hpp"
#include "krpc/services/krpc.hpp"
#include "krpc/stream.hpp"
#include "server_test.hpp"
#include "services/test_service.hpp"

// How the C++ type of an argument converts to the type of a function's parameter. The
// conformance suite covers the rest of server side functions, except a function with no
// value, as the testees build calls only of procedures with a value.
class test_function_stream : public server_test {};

typedef krpc::services::KRPC::Expression Expr;
typedef krpc::services::KRPC::Type KType;

// A function that returns its one parameter, of the given type
static Expr identity(krpc::Client& conn, const KType& type) {
  auto x = Expr::parameter(conn, "x", type);
  return Expr::lambda(conn, {x}, x);
}

TEST_F(test_function_stream, test_a_number_argument_widens_to_its_parameter) {
  auto to_double = identity(conn, KType::double_(conn));
  ASSERT_EQ(2.0, krpc::run<double>(to_double, 2));
  ASSERT_EQ(2.0, krpc::run<double>(to_double, static_cast<short>(2)));
  ASSERT_EQ(3, krpc::run<int32_t>(identity(conn, KType::int_(conn)), static_cast<char>(3)));
  auto to_long = identity(conn, KType::long_(conn));
  ASSERT_EQ(7, krpc::run<int64_t>(to_long, static_cast<uint32_t>(7)));
  ASSERT_EQ(8, krpc::run<int64_t>(to_long, 8LL));
  ASSERT_EQ(2.0f, krpc::run<float>(identity(conn, KType::float_(conn)), 2));
}

TEST_F(test_function_stream, test_a_number_argument_does_not_narrow_to_its_parameter) {
  ASSERT_THROW(krpc::run<float>(identity(conn, KType::float_(conn)), 2.5), std::invalid_argument);
  auto to_unsigned = identity(conn, KType::u_int(conn));
  ASSERT_THROW(krpc::run<uint32_t>(to_unsigned, static_cast<int64_t>(5)), std::invalid_argument);
  ASSERT_THROW(krpc::run<uint32_t>(to_unsigned, static_cast<uint64_t>(5)), std::invalid_argument);
}

TEST_F(test_function_stream,
       test_a_signed_argument_converts_to_an_unsigned_parameter_if_not_negative) {
  auto to_unsigned = identity(conn, KType::u_int(conn));
  ASSERT_EQ(5u, krpc::run<uint32_t>(to_unsigned, 5));
  ASSERT_EQ(5u, krpc::run<uint32_t>(to_unsigned, static_cast<int16_t>(5)));
  auto to_unsigned_long = identity(conn, KType::u_long(conn));
  ASSERT_EQ(5u, krpc::run<uint64_t>(to_unsigned_long, static_cast<int64_t>(5)));
  ASSERT_EQ(5u, krpc::run<uint64_t>(to_unsigned_long, static_cast<uint32_t>(5)));
  ASSERT_THROW(krpc::run<uint64_t>(to_unsigned_long, static_cast<int64_t>(-5)),
               std::invalid_argument);
  try {
    krpc::run<uint32_t>(to_unsigned, -5);
    FAIL();
  } catch (const std::invalid_argument& e) {
    ASSERT_EQ(std::string("Argument 1 does not match the type of its parameter"), e.what());
  }
}

TEST_F(test_function_stream, test_an_enumeration_parameter_takes_an_enumeration_of_any_service) {
  typedef krpc::services::TestService::TestEnum TestEnum;
  auto to_enum = identity(conn, KType::enumeration_type(conn, "TestService", "TestEnum"));
  ASSERT_EQ(TestEnum::value_c, krpc::run<TestEnum>(to_enum, TestEnum::value_c));
  // Another service's enumeration passes as its value, which is 1 for TypeCode::double_
  ASSERT_EQ(TestEnum::value_b,
            krpc::run<TestEnum>(to_enum, krpc::services::KRPC::TypeCode::double_));
  ASSERT_THROW(krpc::run<TestEnum>(to_enum, 1), std::invalid_argument);
}

TEST_F(test_function_stream, test_a_collection_argument_converts_its_items_as_numbers) {
  auto ints = Expr::parameter(conn, "l", KType::list_type(conn, KType::int_(conn)));
  auto count_ints = Expr::lambda(conn, {ints}, Expr::count(conn, ints));
  ASSERT_EQ(3, krpc::run<int32_t>(count_ints, std::vector<int16_t>({1, 2, 3})));
  ASSERT_THROW(krpc::run<int32_t>(count_ints, std::vector<int64_t>({1})), std::invalid_argument);
  auto longs = Expr::parameter(
      conn, "d", KType::dictionary_type(conn, KType::string(conn), KType::long_(conn)));
  auto count_longs = Expr::lambda(conn, {longs}, Expr::count(conn, longs));
  ASSERT_EQ(1, krpc::run<int32_t>(count_longs, std::map<std::string, long long>({{"a", 1}})));
  auto to_pair =
      identity(conn, KType::tuple_type(conn, {KType::double_(conn), KType::string(conn)}));
  ASSERT_EQ(std::make_tuple(2.0, std::string("a")),
            (krpc::run<std::tuple<double, std::string>>(
                to_pair, std::make_tuple(static_cast<int16_t>(2), "a"))));
}

TEST_F(test_function_stream, test_an_optional_item_type_needs_a_nullable_item_parameter) {
  auto nullable = Expr::parameter(
      conn, "n", KType::list_type(conn, KType::nullable_type(conn, KType::int_(conn))));
  auto count_nullable = Expr::lambda(conn, {nullable}, Expr::count(conn, nullable));
  ASSERT_EQ(2, krpc::run<int32_t>(count_nullable,
                                  std::vector<std::optional<int16_t>>({1, std::nullopt})));
  auto ints = Expr::parameter(conn, "l", KType::list_type(conn, KType::int_(conn)));
  auto count_ints = Expr::lambda(conn, {ints}, Expr::count(conn, ints));
  // Rejected by the item type, though no item is null
  ASSERT_THROW(krpc::run<int32_t>(count_ints, std::vector<std::optional<int32_t>>{1}),
               std::invalid_argument);
}

TEST_F(test_function_stream, test_a_null_argument_is_nullptr_or_nullopt) {
  auto label = Expr::parameter(conn, "label", KType::nullable_type(conn, KType::string(conn)));
  auto is_null = Expr::lambda(conn, {label}, Expr::is_null(conn, label));
  ASSERT_TRUE(krpc::run<bool>(is_null, std::nullopt));
  ASSERT_TRUE(krpc::run<bool>(is_null, nullptr));
  ASSERT_TRUE(krpc::run<bool>(is_null, std::optional<std::string>()));
  ASSERT_FALSE(krpc::run<bool>(is_null, "label"));
  ASSERT_FALSE(krpc::run<bool>(is_null, std::optional<std::string>("label")));
}

TEST_F(test_function_stream, test_run_function_with_arguments_for_its_effects) {
  auto id = Expr::parameter(conn, "id", KType::string(conn));
  auto count = Expr::lambda(
      conn, {id}, Expr::call_with_arguments(conn, test_service.counter_call("", 1), {{0, id}}));
  auto before = test_service.counter("CppRunFunctionEffects", 1);
  krpc::run(count, "CppRunFunctionEffects");
  ASSERT_EQ(before + 2, test_service.counter("CppRunFunctionEffects", 1));
}

TEST_F(test_function_stream, test_run_function_side_effects) {
  auto obj = test_service.create_test_object("runeffect");
  obj.set_int_property(1);
  krpc::schema::ProcedureCall call;
  call.set_service("TestService");
  call.set_procedure("TestClass_set_IntProperty");
  auto expr = Expr::call_with_arguments(
      conn, call,
      std::map<int32_t, Expr>({{0, Expr::constant_object(conn, krpc::get_object_id(obj))},
                               {1, Expr::constant_int(conn, 42)}}));
  krpc::run(expr);
  ASSERT_EQ(42, obj.int_property());
}

TEST_F(test_function_stream, test_a_template_type_for_a_function_with_no_value_throws) {
  auto obj = test_service.create_test_object("runcheck");
  obj.set_int_property(3);
  krpc::schema::ProcedureCall call;
  call.set_service("TestService");
  call.set_procedure("TestClass_set_IntProperty");
  auto effect = Expr::call_with_arguments(
      conn, call,
      std::map<int32_t, Expr>({{0, Expr::constant_object(conn, krpc::get_object_id(obj))},
                               {1, Expr::constant_int(conn, 4)}}));
  ASSERT_THROW(krpc::run<int32_t>(effect), std::invalid_argument);
  ASSERT_THROW(krpc::add_stream<int32_t>(effect), std::invalid_argument);
  ASSERT_EQ(3, obj.int_property());
}
