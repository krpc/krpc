// Server side functions, streamed and run at the types the driver names.
#pragma once

#include <any>
#include <cstdint>
#include <krpc/function_stream.hpp>
#include <krpc/services/krpc.hpp>
#include <map>
#include <optional>
#include <set>
#include <stdexcept>
#include <string>
#include <tuple>
#include <type_traits>
#include <variant>
#include <vector>

#include "dispatch_values.hpp"
#include "services/test_service.hpp"
#include "test_service_dispatch.hpp"

namespace krpc_conformance {
namespace functions {

using Expression = krpc::services::KRPC::Expression;

using RunFunction = krpc::conformance::RunFunction;

using TestClass = krpc::services::TestService::TestClass;

// An argument to a function, by the case of the Value that carries it
using Argument = std::variant<int32_t, bool, std::string, double, TestClass, std::nullopt_t>;

inline Argument argument(const Value& value) {
  switch (value.value_case()) {
    case Value::kSint32:
      return value.sint32();
    case Value::kBool:
      return value.bool_();
    case Value::kString:
      return value.string();
    case Value::kDouble:
      return value.double_();
    case Value::kObject:
      return std::any_cast<TestClass>(Handles::instance().object(value.object().handle()).value);
    case Value::kNull:
      return std::nullopt;
    default:
      throw std::invalid_argument("No function argument for the value");
  }
}

template <typename Tag>
StreamHandle add_stream(const Expression& function) {
  return result_stream(krpc::add_stream<typename Tag::type>(function), Tag::result);
}

template <typename Tag, typename... Args>
Value run_with(const Expression& function, const Args&... args) {
  if constexpr (std::is_void_v<Tag>) {
    krpc::run(function, args...);
    return result_none();
  } else {
    return Tag::result(krpc::run<typename Tag::type>(function, args...));
  }
}

// Runs a function with arguments converted by their Value case. Each combination of argument
// types is a separate instantiation, so only a function of sint32 value takes these.
template <typename Tag>
Value run_basic(const Expression& function, const std::vector<Argument>& args) {
  switch (args.size()) {
    case 1:
      return std::visit([&](const auto& a) { return run_with<Tag>(function, a); }, args[0]);
    case 2:
      return std::visit([&](const auto& a, const auto& b) { return run_with<Tag>(function, a, b); },
                        args[0], args[1]);
    default:
      throw std::invalid_argument("The testee runs a function with at most two arguments");
  }
}

template <typename F>
bool with_type(const std::string& type, F&& f);

/** Runs a function with no arguments, with one argument of a type the testee knows, or with
    sint32 value and arguments of the basic types. */
template <typename Tag>
Value run(krpc::Client& client, const Expression& function, const RunFunction& command) {
  const auto& args = command.arguments();
  if (args.empty()) return run_with<Tag>(function);
  if (args.size() == 1 && command.argument_types_size() == 1) {
    std::optional<Value> result;
    auto known = with_type(command.argument_types(0), [&](auto arg) {
      using Arg = decltype(arg);
      result = run_with<Tag>(function, Arg::arg(client, args.Get(0)));
    });
    if (known) return *result;
  }
  if constexpr (std::is_same_v<Tag, Sint32>) {
    std::vector<Argument> basic;
    for (const auto& x : args) basic.push_back(argument(x));
    return run_basic<Tag>(function, basic);
  }
  throw Unsupported("A function of type " + command.type() + " with these arguments");
}

/** Calls the function with the tag for a type the driver names, and returns whether the type is
    one the testee knows. */
template <typename F>
bool with_type(const std::string& type, F&& f) {
  if (type == "double")
    f(Double());
  else if (type == "float")
    f(Float());
  else if (type == "sint32")
    f(Sint32());
  else if (type == "nullable sint32")
    f(Optional<Sint32>());
  else if (type == "sint64")
    f(Sint64());
  else if (type == "uint32")
    f(Uint32());
  else if (type == "uint64")
    f(Uint64());
  else if (type == "bool")
    f(Bool());
  else if (type == "string")
    f(String());
  else if (type == "bytes")
    f(Bytes());
  else if (type == "TestClass")
    f(Object<TestClass>());
  else if (type == "TestEnum")
    f(Enum<krpc::services::TestService::TestEnum>());
  else if (type == "TestStruct")
    f(TestService_TestStruct());
  else if (type == "list(sint32)")
    f(List<Sint32>());
  else if (type == "list(sint64)")
    f(List<Sint64>());
  else if (type == "list(nullable sint32)")
    f(List<Optional<Sint32>>());
  else if (type == "list(string)")
    f(List<String>());
  else if (type == "list(nullable TestClass)")
    f(List<Optional<Object<TestClass>>>());
  else if (type == "set(sint32)")
    f(Set<Sint32>());
  else if (type == "tuple(sint32)")
    f(Tuple<Sint32>());
  else if (type == "tuple(sint32,bool)")
    f(Tuple<Sint32, Bool>());
  else if (type == "dictionary(string,list(sint32))")
    f(Dictionary<String, List<Sint32>>());
  else if (type == "dictionary(string,nullable TestClass)")
    f(Dictionary<String, Optional<Object<TestClass>>>());
  else if (type == "tuple(sint32,nullable TestClass)")
    f(Tuple<Sint32, Optional<Object<TestClass>>>());
  else
    return false;
  return true;
}

}  // namespace functions
}  // namespace krpc_conformance
