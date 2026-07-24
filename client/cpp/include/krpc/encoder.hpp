#pragma once

#include <cstdint>
#include <map>
#include <optional>
#include <set>
#include <string>
#include <tuple>
#include <utility>
#include <vector>

#include "krpc/krpc.pb.hpp"

namespace google {
namespace protobuf {
class MessageLite;
}
}  // namespace google

namespace krpc {

template <typename T>
class Object;

namespace encoder {

std::string encode(float value);
std::string encode(double value);
std::string encode(int32_t value);
std::string encode(int64_t value);
std::string encode(uint32_t value);
std::string encode(uint64_t value);
std::string encode(bool value);
std::string encode(const char* value);
std::string encode(const std::string& value);
std::string encode(const google::protobuf::MessageLite& message);
template <typename T>
std::string encode(const Object<T>& object);

template <typename T>
std::string encode(const std::vector<T>& list);
template <typename K, typename V>
std::string encode(const std::map<K, V>& dictionary);
template <typename T>
std::string encode(const std::set<T>& set);

template <typename... Ts>
std::string encode(const std::tuple<Ts...>& tuple);

std::string encode_message_with_size(const google::protobuf::MessageLite& message);

template <typename T>
inline std::string encode(const Object<T>& object) {
  return encode(object._id);
}

template <typename T>
inline std::string encode(const std::vector<T>& list) {
  krpc::schema::List listMessage;
  for (typename std::vector<T>::const_iterator x = list.begin(); x != list.end(); ++x)
    listMessage.add_items(encode(*x));
  return encode(listMessage);
}

template <typename K, typename V>
inline std::string encode(const std::map<K, V>& dictionary) {
  krpc::schema::Dictionary dictionaryMessage;
  for (typename std::map<K, V>::const_iterator x = dictionary.begin(); x != dictionary.end(); ++x) {
    schema::DictionaryEntry* entry = dictionaryMessage.add_entries();
    entry->set_key(encode(x->first));
    entry->set_value(encode(x->second));
  }
  return encode(dictionaryMessage);
}

template <typename T>
inline std::string encode(const std::set<T>& set) {
  krpc::schema::Set setMessage;
  for (typename std::set<T>::const_iterator x = set.begin(); x != set.end(); ++x)
    setMessage.add_items(encode(*x));
  return encode(setMessage);
}

template <typename... Ts>
inline std::string encode(const std::tuple<Ts...>& tuple) {
  krpc::schema::Tuple tupleMessage;
  std::apply([&tupleMessage](const Ts&... args) { (tupleMessage.add_items(encode(args)), ...); },
             tuple);
  return encode(tupleMessage);
}

// An encoded argument value together with whether the argument is null. A null argument
// is signaled by is_null on the wire and leaves the value field unset.
struct Value {
  std::string value;
  bool is_null;
  // Implicit so a plain encoded value (the common, non-null case) becomes a Value.
  Value(std::string value) : value(std::move(value)), is_null(false) {}  // NOLINT(runtime/explicit)
  static Value null() {
    Value value("");
    value.is_null = true;
    return value;
  }
};

// Encode a nullable non-class argument: an empty optional is null, otherwise the
// contained value is encoded as its underlying type.
template <typename T>
inline Value encode_nullable(const std::optional<T>& value) {
  if (value) return Value(encode(*value));
  return Value::null();
}

// Encode a nullable class argument: an object with id 0 is the null representation.
template <typename T>
inline Value encode_nullable(const Object<T>& object) {
  if (object._id == 0) return Value::null();
  return Value(encode(object));
}

}  // namespace encoder
}  // namespace krpc
