#pragma once

#include <cstdint>
#include <map>
#include <optional>
#include <set>
#include <stdexcept>
#include <string>
#include <tuple>
#include <type_traits>
#include <utility>
#include <vector>

#include "krpc/object.hpp"
#include "krpc/services/krpc.hpp"
#include "krpc/stream.hpp"

namespace krpc {

namespace function_stream_detail {

template <typename T>
struct type_tag {};

template <typename T>
struct is_optional : std::false_type {};
template <typename T>
struct is_optional<std::optional<T>> : std::true_type {};

inline bool describes(type_tag<double>, const schema::Type& type) {
  return type.code() == schema::Type::DOUBLE;
}
inline bool describes(type_tag<float>, const schema::Type& type) {
  return type.code() == schema::Type::FLOAT;
}
inline bool describes(type_tag<int32_t>, const schema::Type& type) {
  return type.code() == schema::Type::SINT32;
}
inline bool describes(type_tag<int64_t>, const schema::Type& type) {
  return type.code() == schema::Type::SINT64;
}
inline bool describes(type_tag<uint32_t>, const schema::Type& type) {
  return type.code() == schema::Type::UINT32;
}
inline bool describes(type_tag<uint64_t>, const schema::Type& type) {
  return type.code() == schema::Type::UINT64;
}
inline bool describes(type_tag<bool>, const schema::Type& type) {
  return type.code() == schema::Type::BOOL;
}
// A std::string holds both strings and bytes
inline bool describes(type_tag<std::string>, const schema::Type& type) {
  return type.code() == schema::Type::STRING || type.code() == schema::Type::BYTES;
}

template <typename T>
bool describes(type_tag<T>, const schema::Type& type);

template <typename T>
bool describes(type_tag<std::optional<T>>, const schema::Type& type) {
  return describes(type_tag<T>{}, type);
}

template <typename T>
bool describes(type_tag<std::vector<T>>, const schema::Type& type) {
  return type.code() == schema::Type::LIST && type.types_size() == 1 &&
         describes(type_tag<T>{}, type.types(0));
}

template <typename T>
bool describes(type_tag<std::set<T>>, const schema::Type& type) {
  return type.code() == schema::Type::SET && type.types_size() == 1 &&
         describes(type_tag<T>{}, type.types(0));
}

template <typename K, typename V>
bool describes(type_tag<std::map<K, V>>, const schema::Type& type) {
  return type.code() == schema::Type::DICTIONARY && type.types_size() == 2 &&
         describes(type_tag<K>{}, type.types(0)) && describes(type_tag<V>{}, type.types(1));
}

template <typename... Ts, size_t... Is>
bool describes_elements(const schema::Type& type, std::index_sequence<Is...>) {
  return (describes(type_tag<Ts>{}, type.types(static_cast<int>(Is))) && ...);
}

template <typename... Ts>
bool describes(type_tag<std::tuple<Ts...>>, const schema::Type& type) {
  return type.code() == schema::Type::TUPLE &&
         type.types_size() == static_cast<int>(sizeof...(Ts)) &&
         describes_elements<Ts...>(type, std::index_sequence_for<Ts...>{});
}

// A class, enumeration or structure a service defines. Only the kind is compared, as the
// generated types do not carry their names
template <typename T>
bool describes(type_tag<T>, const schema::Type& type) {
  static_assert(std::is_class_v<T> || std::is_enum_v<T>,
                "The template type must be one a server side function can evaluate to");
  if constexpr (std::is_base_of_v<Object<T>, T>)
    return type.code() == schema::Type::CLASS;
  else if constexpr (std::is_enum_v<T>)
    return type.code() == schema::Type::ENUMERATION;
  else
    return type.code() == schema::Type::STRUCT;
}

// Taken by value, as the generated accessors are not const
inline void build_type(services::KRPC::Type remote, schema::Type* type) {
  type->set_code(static_cast<schema::Type::TypeCode>(remote.code()));
  auto service = remote.service();
  if (!service.empty()) {
    type->set_service(service);
    type->set_name(remote.name());
  }
  for (const auto& element : remote.types()) build_type(element, type->add_types());
}

/**
 * The type of the values a server side function evaluates to, with a code of NONE for one
 * that evaluates to no value. Introspected on the server once per function.
 */
inline schema::Type return_type(const services::KRPC::Expression& function) {
  schema::Type type;
  if (function._client->get_function_return_type(function._id, &type)) return type;
  services::KRPC::Expression expression = function;
  if (expression.has_return_type()) build_type(expression.return_type(), &type);
  function._client->set_function_return_type(function._id, type);
  return type;
}

/** Throw unless the template type describes the values the function evaluates to. */
template <typename T>
void check_return_type(const services::KRPC::Expression& function) {
  auto type = return_type(function);
  if (type.code() == schema::Type::NONE)
    throw std::invalid_argument(
        "The function does not evaluate to a value; run it without a template type");
  if (!describes(type_tag<T>{}, type))
    throw std::invalid_argument(
        "The template type does not match the type of the values the function evaluates to");
}

}  // namespace function_stream_detail

/**
 * Create a stream from a server side function. On each update, the value of
 * the stream is the result of evaluating the function on the server.
 * The template type must correspond to the function's return type.
 */
template <typename T>
inline Stream<T> add_function_stream(const services::KRPC::Expression& function) {
  function_stream_detail::check_return_type<T>(function);
  services::KRPC krpc_service(function._client);
  krpc::schema::Stream stream = krpc_service.add_function_stream(function, false);
  return Stream<T>(function._client, stream.id());
}

/**
 * Run a function on the server, within a single physics tick, and return the
 * value it produces. The template type must correspond to the function's
 * return type, and must be a std::optional for a function whose value is null.
 */
template <typename T>
inline T run_function(const services::KRPC::Expression& function) {
  function_stream_detail::check_return_type<T>(function);
  services::KRPC krpc_service(function._client);
  auto data = krpc_service.run_function(function);
  // A null value is signaled out-of-band by is_null
  T result{};
  if (!data) {
    if constexpr (!function_stream_detail::is_optional<T>::value)
      throw std::runtime_error(
          "The function produced a null value; use a std::optional for the result");
  } else {
    // Unqualified, so that argument dependent lookup finds the overload for a type a
    // service defines at the point this template is used
    using decoder::decode;
    decode(result, *data, function._client);
  }
  return result;
}

/**
 * Run a function with no result on the server, within a single physics tick,
 * for its effects.
 */
inline void run_function(const services::KRPC::Expression& function) {
  services::KRPC krpc_service(function._client);
  krpc_service.run_function(function);
}

}  // namespace krpc
