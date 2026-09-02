#pragma once

#include <cstddef>
#include <cstdint>
#include <map>
#include <optional>
#include <set>
#include <string>
#include <tuple>
#include <utility>
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
void decode(std::optional<T>& value, const std::string& data, Client* client = nullptr);
template <typename T>
void decode(std::vector<T>& list, const std::string& data, Client* client = nullptr);
template <typename T>
void decode(std::set<T>& set, const std::string& data, Client* client = nullptr);
template <typename K, typename V>
void decode(std::map<K, V>& dictionary, const std::string& data, Client* client = nullptr);

template <typename T>
void decode_item(std::optional<T>& value, const std::string& data, Client* client);
template <typename T>
void decode_item(T& value, const std::string& data, Client* client);

uint32_t decode_size(const std::string& data);

/** Read the size prefix a message is preceded by out of a buffer. Returns false if the buffer
    does not hold a complete prefix yet, leaving size and prefix_length untouched. */
bool decode_size_prefix(const char* data, size_t length, uint32_t* size, size_t* prefix_length);

template <typename T>
inline void decode(Object<T>& object, const std::string& data, Client* client) {
  uint64_t id;
  decode(id, data, client);
  object._client = client;
  object._id = id;
}

template <typename... Ts>
inline void decode(std::tuple<Ts...>& tuple, const std::string& data, Client* client) {
  // See the note on the list below for why the message this arrives in is kept between calls.
  static thread_local krpc::schema::Tuple tupleMessage;
  if (!tupleMessage.ParseFromString(data)) throw EncodingError("Failed to decode message");
  int index = 0;
  std::apply([&](Ts&... args) { (decode_item(args, tupleMessage.items(index++), client), ...); },
             tuple);
}

template <typename T>
inline void decode(std::optional<T>& value, const std::string& data, Client* client) {
  // Only called for a present value; a null value is signaled by is_null and leaves the
  // optional empty.
  T inner;
  decode(inner, data, client);
  value = inner;
}

// Decode one of the values a collection or a structure holds, where a nullable position is
// a std::optional.
template <typename T>
inline void decode_item(std::optional<T>& value, const std::string& data, Client* client) {
  // The presence bool is one byte, as a bool value always is, and is read from it directly
  // rather than through a copy of it
  if (data.empty()) throw EncodingError("Failed to decode message");
  if (data[0] == 0) {
    value.reset();
    return;
  }
  T inner;
  decode(inner, data.substr(1), client);
  value = inner;
}

template <typename T>
inline void decode_item(T& value, const std::string& data, Client* client) {
  decode(value, data, client);
}

// A collection arrives as a message holding its items, one encoded value each. That message is
// kept between calls: parsing into it reuses the storage the items it held last time have,
// where a message made for the call goes to the allocator for every item and gives it all back
// again. It is one per thread, as the thread that reads stream updates decodes them while the
// program is decoding what it asked for. It is one per element type, which makes it safe to
// reuse: reaching this function again while it is running takes a collection of itself, which
// is not a type that can be written.
template <typename T>
inline void decode(std::vector<T>& list, const std::string& data, Client* client) {
  static thread_local krpc::schema::List listMessage;
  if (!listMessage.ParseFromString(data)) throw EncodingError("Failed to decode message");
  list.clear();
  // The number of items is known before any of them are decoded, so the list is given room for
  // all of them up front.
  list.reserve(listMessage.items_size());
  for (int i = 0; i < listMessage.items_size(); i++) {
    T value;
    decode_item(value, listMessage.items(i), client);
    list.push_back(std::move(value));
  }
}

template <typename T>
inline void decode(std::set<T>& set, const std::string& data, Client* client) {
  static thread_local krpc::schema::Set setMessage;
  if (!setMessage.ParseFromString(data)) throw EncodingError("Failed to decode message");
  set.clear();
  for (int i = 0; i < setMessage.items_size(); i++) {
    T value;
    decode(value, setMessage.items(i), client);
    set.insert(std::move(value));
  }
}

template <typename K, typename V>
inline void decode(std::map<K, V>& dictionary, const std::string& data, Client* client) {
  static thread_local krpc::schema::Dictionary dictionaryMessage;
  if (!dictionaryMessage.ParseFromString(data)) throw EncodingError("Failed to decode message");
  dictionary.clear();
  for (int i = 0; i < dictionaryMessage.entries_size(); i++) {
    const schema::DictionaryEntry& entry = dictionaryMessage.entries(i);
    K key;
    V value;
    decode(key, entry.key(), client);
    decode_item(value, entry.value(), client);
    dictionary[std::move(key)] = std::move(value);
  }
}

}  // namespace decoder
}  // namespace krpc
