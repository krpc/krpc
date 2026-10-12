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

#include "krpc/event.hpp"
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

// An element that can be null is encoded with a flag saying whether it is null, which a
// std::optional element decodes
template <typename T>
bool describes_element(const schema::Type& type) {
  return is_optional<T>::value == type.nullable() && describes(type_tag<T>{}, type);
}

template <typename T>
bool describes(type_tag<std::vector<T>>, const schema::Type& type) {
  return type.code() == schema::Type::LIST && type.types_size() == 1 &&
         describes_element<T>(type.types(0));
}

template <typename T>
bool describes(type_tag<std::set<T>>, const schema::Type& type) {
  return type.code() == schema::Type::SET && type.types_size() == 1 &&
         describes_element<T>(type.types(0));
}

template <typename K, typename V>
bool describes(type_tag<std::map<K, V>>, const schema::Type& type) {
  return type.code() == schema::Type::DICTIONARY && type.types_size() == 2 &&
         describes_element<K>(type.types(0)) && describes_element<V>(type.types(1));
}

template <typename... Ts, size_t... Is>
bool describes_elements(const schema::Type& type, std::index_sequence<Is...>) {
  return (describes_element<Ts>(type.types(static_cast<int>(Is))) && ...);
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
  type->set_nullable(remote.nullable());
}

// The client's function type caches, which are private to Client
struct function_type_cache {
  static bool get(const Client& client, uint64_t id, schema::Type* type) {
    return client.get_function_return_type(id, type);
  }
  static void set(Client* client, uint64_t id, const schema::Type& type) {
    client->set_function_return_type(id, type);
  }
  static bool get(const Client& client, uint64_t id, std::vector<schema::Type>* types) {
    return client.get_function_parameter_types(id, types);
  }
  static void set(Client* client, uint64_t id, const std::vector<schema::Type>& types) {
    client->set_function_parameter_types(id, types);
  }
};

/**
 * The type of the values a server side function evaluates to, with a code of NONE for one
 * that evaluates to no value. Cached, as introspecting it makes a round trip per property.
 */
inline schema::Type return_type(const services::KRPC::Expression& function) {
  schema::Type type;
  if (function_type_cache::get(*function._client, function._id, &type)) return type;
  services::KRPC::Expression expression = function;
  if (expression.has_return_type()) build_type(expression.return_type(), &type);
  function_type_cache::set(function._client, function._id, type);
  return type;
}

/** The types of a server side function's parameters. Cached, as for the return type. */
inline std::vector<schema::Type> parameter_types(const services::KRPC::Expression& function) {
  std::vector<schema::Type> types;
  if (function_type_cache::get(*function._client, function._id, &types)) return types;
  services::KRPC::Expression expression = function;
  for (const auto& remote : expression.parameter_types()) build_type(remote, &types.emplace_back());
  function_type_cache::set(function._client, function._id, types);
  return types;
}

/**
 * Encode a number at a numeric parameter's type. Any number converts to a double, and an integer
 * or a float to a float, as C++ converts it implicitly, which can round. An integer converts to
 * a signed parameter that holds every value of its type, and an integer that is not negative to
 * an unsigned parameter at least as wide. Returns std::nullopt for a number the parameter cannot
 * take.
 */
template <typename N>
std::optional<std::string> encode_number(N value, const schema::Type& type) {
  using encoder::encode;
  constexpr bool integral = std::is_integral_v<N>;
  constexpr bool is_signed = std::is_signed_v<N>;
  switch (type.code()) {
    case schema::Type::DOUBLE:
      return encode(static_cast<double>(value));
    case schema::Type::FLOAT:
      if constexpr (integral || std::is_same_v<N, float>) return encode(static_cast<float>(value));
      break;
    case schema::Type::SINT32:
      if constexpr (integral && sizeof(N) <= sizeof(int32_t) &&
                    (is_signed || sizeof(N) < sizeof(int32_t)))
        return encode(static_cast<int32_t>(value));
      break;
    case schema::Type::SINT64:
      if constexpr (integral && (is_signed || sizeof(N) < sizeof(int64_t)))
        return encode(static_cast<int64_t>(value));
      break;
    case schema::Type::UINT32:
      if constexpr (integral && sizeof(N) <= sizeof(uint32_t)) {
        if constexpr (is_signed) {
          if (value < 0) break;
        }
        return encode(static_cast<uint32_t>(value));
      }
      break;
    case schema::Type::UINT64:
      if constexpr (integral) {
        if constexpr (is_signed) {
          if (value < 0) break;
        }
        return encode(static_cast<uint64_t>(value));
      }
      break;
    default:
      break;
  }
  return std::nullopt;
}

// Encode a value at a type, with a number at any position converting as encode_number converts
// it. Returns std::nullopt for a value the type cannot take.
template <typename A>
std::optional<std::string> encode_value(const A& value, const schema::Type& type);
template <typename T>
std::optional<std::string> encode_value(const std::vector<T>& list, const schema::Type& type);
template <typename T>
std::optional<std::string> encode_value(const std::set<T>& set, const schema::Type& type);
template <typename K, typename V>
std::optional<std::string> encode_value(const std::map<K, V>& dictionary, const schema::Type& type);
template <typename... Ts>
std::optional<std::string> encode_value(const std::tuple<Ts...>& tuple, const schema::Type& type);

// One of the values a collection holds. A nullable position carries a presence bool before
// its value.
template <typename A>
std::optional<std::string> encode_element(const A& value, const schema::Type& type) {
  using encoder::encode;
  if constexpr (is_optional<A>::value) {
    if (!type.nullable()) return std::nullopt;
    if (!value) return encode(false);
    auto encoded = encode_value(*value, type);
    if (!encoded) return std::nullopt;
    return encode(true) + *encoded;
  } else {
    auto encoded = encode_value(value, type);
    if (!encoded || !type.nullable()) return encoded;
    return encode(true) + *encoded;
  }
}

template <typename A>
std::optional<std::string> encode_value(const A& value, const schema::Type& type) {
  using decayed = std::decay_t<A>;
  if constexpr (std::is_convertible_v<decayed, std::string> &&
                !std::is_same_v<decayed, std::string>) {
    return encode_value(std::string(value), type);
  } else if constexpr (std::is_arithmetic_v<decayed> && !std::is_same_v<decayed, bool>) {
    return encode_number(value, type);
  } else {
    if (!describes(type_tag<decayed>{}, type)) return std::nullopt;
    // Unqualified, so that argument dependent lookup finds the overload for a type a
    // service defines
    using encoder::encode;
    return encode(value);
  }
}

template <typename T>
std::optional<std::string> encode_value(const std::vector<T>& list, const schema::Type& type) {
  if (type.code() != schema::Type::LIST || type.types_size() != 1) return std::nullopt;
  schema::List message;
  for (const auto& item : list) {
    auto encoded = encode_element(item, type.types(0));
    if (!encoded) return std::nullopt;
    message.add_items(*encoded);
  }
  return encoder::encode(message);
}

template <typename T>
std::optional<std::string> encode_value(const std::set<T>& set, const schema::Type& type) {
  if (type.code() != schema::Type::SET || type.types_size() != 1) return std::nullopt;
  schema::Set message;
  for (const auto& item : set) {
    auto encoded = encode_element(item, type.types(0));
    if (!encoded) return std::nullopt;
    message.add_items(*encoded);
  }
  return encoder::encode(message);
}

template <typename K, typename V>
std::optional<std::string> encode_value(const std::map<K, V>& dictionary,
                                        const schema::Type& type) {
  if (type.code() != schema::Type::DICTIONARY || type.types_size() != 2) return std::nullopt;
  schema::Dictionary message;
  for (const auto& [key, value] : dictionary) {
    auto encoded_key = encode_element(key, type.types(0));
    auto encoded_value = encode_element(value, type.types(1));
    if (!encoded_key || !encoded_value) return std::nullopt;
    auto* entry = message.add_entries();
    entry->set_key(*encoded_key);
    entry->set_value(*encoded_value);
  }
  return encoder::encode(message);
}

template <typename... Ts>
std::optional<std::string> encode_value(const std::tuple<Ts...>& tuple, const schema::Type& type) {
  if (type.code() != schema::Type::TUPLE || type.types_size() != static_cast<int>(sizeof...(Ts)))
    return std::nullopt;
  schema::Tuple message;
  int position = 0;
  auto add = [&type, &message, &position](const auto& item) {
    auto encoded = encode_element(item, type.types(position++));
    if (encoded) message.add_items(*encoded);
    return encoded.has_value();
  };
  bool matches = std::apply([&add](const Ts&... items) { return (add(items) && ...); }, tuple);
  if (!matches) return std::nullopt;
  return encoder::encode(message);
}

/**
 * Encode an argument of a call to a server side function, at its parameter's type. A number,
 * also one a collection holds, converts as encode_number converts it, and a string literal is
 * a string. Throws std::invalid_argument for an argument the parameter cannot take, naming its
 * position from 1.
 */
template <typename A>
std::optional<std::string> encode_argument(const A& value, const schema::Type& type,
                                           size_t position) {
  using decayed = std::decay_t<A>;
  if constexpr (std::is_same_v<decayed, std::nullptr_t> ||
                std::is_same_v<decayed, std::nullopt_t>) {
    return std::nullopt;
  } else if constexpr (is_optional<decayed>::value) {
    if (!value) return std::nullopt;
    return encode_argument(*value, type, position);
  } else {
    auto encoded = encode_value(value, type);
    if (!encoded)
      throw std::invalid_argument("Argument " + std::to_string(position) +
                                  " does not match the type of its parameter");
    return encoded;
  }
}

/**
 * Encode the arguments of a call to a server side function, one per parameter. Throws
 * std::invalid_argument if the number of arguments differs from the number of parameters.
 */
template <typename... Args>
std::vector<std::optional<std::string>> encode_arguments(const services::KRPC::Expression& function,
                                                         const Args&... args) {
  std::vector<std::optional<std::string>> encoded;
  auto types = parameter_types(function);
  if (types.size() != sizeof...(Args))
    throw std::invalid_argument("The function takes " + std::to_string(types.size()) +
                                (types.size() == 1 ? " argument" : " arguments") + ", got " +
                                std::to_string(sizeof...(Args)));
  [[maybe_unused]] size_t position = 0;
  ((encoded.push_back(encode_argument(args, types[position], position + 1)), ++position), ...);
  return encoded;
}

/** Throw unless the template type describes the values the function evaluates to. */
template <typename T>
void check_return_type(const services::KRPC::Expression& function) {
  auto type = return_type(function);
  if (type.code() == schema::Type::NONE)
    throw std::invalid_argument(
        "The function does not evaluate to a value. Call run without a "
        "template type");
  if (!describes(type_tag<T>{}, type))
    throw std::invalid_argument(
        "The template type does not match the type of the values the function evaluates to");
}

// Constructs a function stream, whose constructor is private to Stream
struct function_streams {
  template <typename T>
  static Stream<T> create(Client* client, uint64_t id, bool null_is_an_error) {
    return Stream<T>(client, id, null_is_an_error);
  }
};

}  // namespace function_stream_detail

/**
 * Create a stream from a server side function, evaluated on each update. Each call creates a new
 * stream. Throws std::invalid_argument if the template type does not describe the function's
 * value, StreamError if the client has no stream connection, and services::KRPC::ArgumentException
 * if the function has parameters. Reading a null value throws std::runtime_error unless the
 * template type is a std::optional. In a callback, that error is written to stderr and the
 * callback does not run.
 */
template <typename T>
inline Stream<T> add_stream(const services::KRPC::Expression& function) {
  function._client->check_stream_connection();
  function_stream_detail::check_return_type<T>(function);
  services::KRPC krpc_service(function._client);
  krpc::schema::Stream stream = krpc_service.add_function_stream(function);
  return function_stream_detail::function_streams::create<T>(
      function._client, stream.id(), !function_stream_detail::is_optional<T>::value);
}

/**
 * Create an event from a server side function that evaluates to a boolean value. Each call
 * creates a new event. Throws StreamError if the client has no stream connection, and
 * services::KRPC::ArgumentException if the function does not evaluate to a boolean value or has
 * parameters.
 */
inline Event add_event(const services::KRPC::Expression& function) {
  function._client->check_stream_connection();
  services::KRPC krpc_service(function._client);
  return krpc_service.add_event(function);
}

/**
 * Run a server side function within a single physics tick, and return its value, or run it
 * for its effects when the template type is void. The arguments are passed to the function's
 * parameters, with std::nullopt or nullptr for a null. Throws std::invalid_argument if the
 * template type does not describe the function's value, the number of arguments differs from the
 * number of parameters, or an argument does not match its parameter, and std::runtime_error for a
 * null value unless the template type is a std::optional. The server checks the class of an object
 * argument, and a null for a parameter that is not nullable, and throws
 * services::KRPC::ArgumentException for a mismatch.
 */
template <typename T = void, typename... Args>
inline T run(const services::KRPC::Expression& function, const Args&... args) {
  if constexpr (!std::is_void_v<T>) function_stream_detail::check_return_type<T>(function);
  auto arguments = function_stream_detail::encode_arguments(function, args...);
  services::KRPC krpc_service(function._client);
  [[maybe_unused]] auto data = krpc_service.run_function(function, arguments);
  if constexpr (!std::is_void_v<T>) {
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
}

}  // namespace krpc
