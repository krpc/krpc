#pragma once

#include <cstddef>
#include <cstdint>
#include <map>
#include <set>
#include <string>
#include <tuple>
#include <vector>

#include "krpc/error.hpp"
#include "krpc/krpc.pb.hpp"

namespace google {
namespace protobuf {
class MessageLite;
}
}  // namespace google

namespace krpc {

class Client;
class Event;
template <typename T>
class Object;

namespace decoder {

const size_t GUID_LENGTH = 16;

std::string guid(const std::string& data);

void decode(double& value, const std::string& data, Client* client = nullptr);
void decode(float& value, const std::string& data, Client* client = nullptr);
void decode(int32_t& value, const std::string& data, Client* client = nullptr);
void decode(int64_t& value, const std::string& data, Client* client = nullptr);
void decode(uint32_t& value, const std::string& data, Client* client = nullptr);
void decode(uint64_t& value, const std::string& data, Client* client = nullptr);
void decode(bool& value, const std::string& data, Client* client = nullptr);
void decode(std::string& value, const std::string& data, Client* client = nullptr);
void decode(Event& event, const std::string& data, Client* client = nullptr);
void decode(google::protobuf::MessageLite& message, const std::string& data,
            Client* client = nullptr);

template <typename T>
void decode(Object<T>& object, const std::string& data, Client* client = nullptr);

template <typename... Ts>
void decode(std::tuple<Ts...>& tuple, const std::string& data, Client* client = nullptr);

template <typename T>
void decode(std::vector<T>& list, const std::string& data, Client* client = nullptr);
template <typename T>
void decode(std::set<T>& set, const std::string& data, Client* client = nullptr);
template <typename K, typename V>
void decode(std::map<K, V>& dictionary, const std::string& data, Client* client = nullptr);

uint32_t decode_size(const std::string& data);

template <typename T>
inline void decode(Object<T>& object, const std::string& data, Client* client) {
  uint64_t id;
  decode(id, data, client);
  object._client = client;
  object._id = id;
}

template <typename... Ts>
inline void decode(std::tuple<Ts...>& tuple, const std::string& data, Client* client) {
  krpc::schema::Tuple tupleMessage;
  if (!tupleMessage.ParseFromString(data)) throw EncodingError("Failed to decode message");
  int index = 0;
  std::apply([&](Ts&... args) { (decode(args, tupleMessage.items(index++), client), ...); }, tuple);
}

template <typename T>
inline void decode(std::vector<T>& list, const std::string& data, Client* client) {
  list.clear();
  krpc::schema::List listMessage;
  if (!listMessage.ParseFromString(data)) throw EncodingError("Failed to decode message");
  for (int i = 0; i < listMessage.items_size(); i++) {
    T value;
    decode(value, listMessage.items(i), client);
    list.push_back(value);
  }
}

template <typename T>
inline void decode(std::set<T>& set, const std::string& data, Client* client) {
  set.clear();
  krpc::schema::Set setMessage;
  if (!setMessage.ParseFromString(data)) throw EncodingError("Failed to decode message");
  for (int i = 0; i < setMessage.items_size(); i++) {
    T value;
    decode(value, setMessage.items(i), client);
    set.insert(value);
  }
}

template <typename K, typename V>
inline void decode(std::map<K, V>& dictionary, const std::string& data, Client* client) {
  dictionary.clear();
  krpc::schema::Dictionary dictionaryMessage;
  if (!dictionaryMessage.ParseFromString(data)) throw EncodingError("Failed to decode message");
  for (int i = 0; i < dictionaryMessage.entries_size(); i++) {
    const schema::DictionaryEntry& entry = dictionaryMessage.entries(i);
    K key;
    V value;
    decode(key, entry.key(), client);
    decode(value, entry.value(), client);
    dictionary[key] = value;
  }
}

}  // namespace decoder
}  // namespace krpc
