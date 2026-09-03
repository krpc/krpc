#include <gtest/gtest-message.h>
#include <gtest/gtest-test-part.h>

#include <map>
#include <optional>
#include <set>
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

class test_function_stream : public server_test {};

typedef krpc::services::KRPC::Expression Expr;
typedef krpc::services::KRPC::Type KType;

TEST_F(test_function_stream, test_function_stream) {
  test_service.set_string_property("foo");
  auto expr = Expr::call(conn, test_service.string_property_call());
  auto stream = krpc::add_function_stream<std::string>(expr);
  ASSERT_EQ("foo", stream());
  stream.remove();
}

TEST_F(test_function_stream, test_computed_value) {
  auto counter = Expr::call(conn, test_service.counter_call("CppFunctionStream.Computed", 1));
  // int RPC result * double constant promotes to double
  auto expr = Expr::multiply(conn, counter, Expr::constant_double(conn, 0.5));
  auto stream = krpc::add_function_stream<double>(expr);
  ASSERT_GT(stream(), 0);
  stream.remove();
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
  auto stream = krpc::add_function_stream<std::vector<int32_t>>(selected);
  ASSERT_EQ(std::vector<int32_t>({1, 2, 3}), stream());
  stream.remove();
}

TEST_F(test_function_stream, test_struct) {
  auto call = test_service.counter_struct_call("CppFunctionStream.Struct");
  auto value = Expr::call(conn, call);
  auto stream = krpc::add_function_stream<krpc::services::TestService::TestStruct>(value);
  ASSERT_EQ("CppFunctionStream.Struct", stream().string_field);
  stream.remove();
  auto field = krpc::add_function_stream<std::string>(
      Expr::get_field(conn, Expr::call(conn, call), "StringField"));
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
  auto result = krpc::run_function<krpc::services::TestService::TestStruct>(value);
  ASSERT_EQ(3, result.int_field);
  ASSERT_EQ("built", result.string_field);
  ASSERT_EQ(std::vector<int32_t>({7}), result.list_field);
}

TEST_F(test_function_stream, test_run_function) {
  auto obj = test_service.create_test_object("run");
  obj.set_int_property(20);
  auto expr =
      Expr::multiply(conn, Expr::call(conn, obj.int_property_call()), Expr::constant_int(conn, 2));
  ASSERT_EQ(40, krpc::run_function<int32_t>(expr));
}

TEST_F(test_function_stream, test_run_function_null_result) {
  typedef krpc::services::TestService::TestClass TestClass;
  auto obj = test_service.create_test_object("runnull");
  auto expr = Expr::call(conn, obj.object_property_call());
  // The property is nullable and unset, so the function's value is null
  ASSERT_FALSE(krpc::run_function<std::optional<TestClass>>(expr).has_value());
  ASSERT_THROW(krpc::run_function<TestClass>(expr), std::runtime_error);
  auto stream = krpc::add_function_stream<std::optional<TestClass>>(expr);
  ASSERT_FALSE(stream().has_value());
  stream.remove();
  auto other = test_service.create_test_object("runnull2");
  obj.set_object_property(other);
  ASSERT_EQ(other, krpc::run_function<std::optional<TestClass>>(expr).value());
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
  krpc::run_function(expr);
  ASSERT_EQ(42, obj.int_property());
}

TEST_F(test_function_stream, test_run_function_checks_the_template_type) {
  auto obj = test_service.create_test_object("runcheck");
  obj.set_int_property(3);
  auto value = Expr::call(conn, obj.int_property_call());
  ASSERT_THROW(krpc::run_function<std::string>(value), std::invalid_argument);
  ASSERT_THROW(krpc::add_function_stream<std::vector<int32_t>>(value), std::invalid_argument);
  ASSERT_EQ(3, krpc::run_function<int32_t>(value));
  krpc::schema::ProcedureCall call;
  call.set_service("TestService");
  call.set_procedure("TestClass_set_IntProperty");
  auto effect =
      Expr::call_with_arguments(conn, call,
                                std::map<int32_t, Expr>({{0, Expr::constant_object(conn, obj._id)},
                                                         {1, Expr::constant_int(conn, 4)}}));
  ASSERT_THROW(krpc::run_function<int32_t>(effect), std::invalid_argument);
  ASSERT_EQ(3, obj.int_property());
}

TEST_F(test_function_stream, test_template_type_of_a_collection) {
  auto one = Expr::constant_int(conn, 1);
  auto name = Expr::constant_string(conn, "a");
  auto tuple = Expr::create_tuple(conn, std::vector<Expr>({one, name}));
  ASSERT_EQ(std::make_tuple(1, std::string("a")),
            (krpc::run_function<std::tuple<int32_t, std::string>>(tuple)));
  ASSERT_THROW(krpc::run_function<std::tuple<int32_t>>(tuple), std::invalid_argument);
  ASSERT_THROW((krpc::run_function<std::tuple<std::string, int32_t>>(tuple)),
               std::invalid_argument);
  auto set = Expr::create_set(conn, std::set<Expr>({one}));
  ASSERT_EQ(std::set<int32_t>({1}), krpc::run_function<std::set<int32_t>>(set));
  ASSERT_THROW(krpc::run_function<std::vector<int32_t>>(set), std::invalid_argument);
  auto map = Expr::create_dictionary(conn, std::vector<Expr>({name}), std::vector<Expr>({one}));
  ASSERT_EQ((std::map<std::string, int32_t>({{"a", 1}})),
            (krpc::run_function<std::map<std::string, int32_t>>(map)));
  ASSERT_THROW((krpc::run_function<std::map<int32_t, std::string>>(map)), std::invalid_argument);
  auto enumeration =
      Expr::cast(conn, one, KType::enumeration_type(conn, "TestService", "TestEnum"));
  ASSERT_EQ(krpc::services::TestService::TestEnum::value_b,
            krpc::run_function<krpc::services::TestService::TestEnum>(enumeration));
  ASSERT_THROW(krpc::run_function<int32_t>(enumeration), std::invalid_argument);
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
