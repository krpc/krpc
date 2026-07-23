#pragma once

#include <cstdint>
#include <map>
#include <set>
#include <string>
#include <tuple>
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

}  // namespace encoder
}  // namespace krpc
