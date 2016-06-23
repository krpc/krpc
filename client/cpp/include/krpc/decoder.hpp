#pragma once

#include <google/protobuf/message.h>

#include <map>
#include <set>
#include <string>
#include <tuple>
#include <utility>
#include <vector>

#include "krpc/krpc.pb.hpp"
#include "krpc/object.hpp"

namespace krpc {
namespace decoder {

class DecodeFailed : public std::runtime_error {
 public:
  explicit DecodeFailed(const std::string& msg) : std::runtime_error(msg) {}
};

const char OK_MESSAGE[] = { 0x4F, 0x4B };
const size_t OK_MESSAGE_LENGTH = 2;
const size_t GUID_LENGTH = 16;

std::string guid(const std::string& data);

void decode(float& value, const std::string& data, Client * client = nullptr);
void decode(double& value, const std::string& data, Client * client = nullptr);
void decode(google::protobuf::int32& value, const std::string& data, Client * client = nullptr);
void decode(google::protobuf::int64& value, const std::string& data, Client * client = nullptr);
void decode(google::protobuf::uint32& value, const std::string& data, Client * client = nullptr);
void decode(google::protobuf::uint64& value, const std::string& data, Client * client = nullptr);
void decode(bool& value, const std::string& data, Client * client = nullptr);
void decode(std::string& value, const std::string& data, Client * client = nullptr);
void decode(google::protobuf::Message& message, const std::string& data, Client * client = nullptr);
template <typename T> void decode(Object<T>& object, const std::string& data, Client * client = nullptr);

template <typename T> void decode(std::vector<T>& list, const std::string& data, Client * client = nullptr);
template <typename K, typename V> void decode(
  std::map<K, V>& dictionary, const std::string& data, Client * client = nullptr);
template <typename T> void decode(std::set<T>& set, const std::string& data, Client * client = nullptr);

template <typename T0> void decode(
  std::tuple<T0>& tuple, const std::string& data, Client * client = nullptr);
template <typename T0, typename T1> void decode(
  std::tuple<T0, T1>& tuple, const std::string& data, Client * client = nullptr);
template <typename T0, typename T1, typename T2> void decode(
  std::tuple<T0, T1, T2>& tuple, const std::string& data, Client * client = nullptr);
template <typename T0, typename T1, typename T2, typename T3> void decode(
  std::tuple<T0, T1, T2, T3>& tuple, const std::string& data, Client * client = nullptr);
template <typename T0, typename T1, typename T2, typename T3, typename T4> void decode(
  std::tuple<T0, T1, T2, T3, T4>& tuple, const std::string& data, Client * client = nullptr);

std::pair<google::protobuf::uint32, google::protobuf::uint32> decode_size_and_position(const std::string& data);

template <typename T>
inline void decode(Object<T>& object, const std::string& data, Client * client) {
  google::protobuf::uint64 id;
  decode(id, data, client);
  object._client = client;
  object._id = id;
}

template <typename T>
inline void decode(std::vector<T>& list, const std::string& data, Client * client) {
  list.clear();
  krpc::schema::List listMessage;
  listMessage.ParseFromString(data);
  for (int i = 0; i < listMessage.items_size(); i++) {
    T value;
    decode(value, listMessage.items(i), client);
    list.push_back(value);
  }
}

template <typename K, typename V> inline void decode(
  std::map<K, V>& dictionary, const std::string& data, Client * client) {
  dictionary.clear();
  krpc::schema::Dictionary dictionaryMessage;
  dictionaryMessage.ParseFromString(data);
  for (int i = 0; i < dictionaryMessage.entries_size(); i++) {
    const schema::DictionaryEntry& entry = dictionaryMessage.entries(i);
    K key;
    V value;
    decode(key, entry.key(), client);
    decode(value, entry.value(), client);
    dictionary[key] = value;
  }
}

template <typename T> inline void decode(std::set<T>& set, const std::string& data, Client * client) {
  set.clear();
  krpc::schema::Set setMessage;
  setMessage.ParseFromString(data);
  for (int i = 0; i < setMessage.items_size(); i++) {
    T value;
    decode(value, setMessage.items(i), client);
    set.insert(value);
  }
}

template <typename T0>
inline void decode(std::tuple<T0>& tuple, const std::string& data, Client * client) {
  krpc::schema::Tuple tupleMessage;
  tupleMessage.ParseFromString(data);
  decode(std::get<0>(tuple), tupleMessage.items(0), client);
}

template <typename T0, typename T1>
inline void decode(std::tuple<T0, T1>& tuple, const std::string& data, Client * client) {
  krpc::schema::Tuple tupleMessage;
  tupleMessage.ParseFromString(data);
  decode(std::get<0>(tuple), tupleMessage.items(0), client);
  decode(std::get<1>(tuple), tupleMessage.items(1), client);
}

template <typename T0, typename T1, typename T2>
inline void decode(std::tuple<T0, T1, T2>& tuple, const std::string& data, Client * client) {
  krpc::schema::Tuple tupleMessage;
  tupleMessage.ParseFromString(data);
  decode(std::get<0>(tuple), tupleMessage.items(0), client);
  decode(std::get<1>(tuple), tupleMessage.items(1), client);
  decode(std::get<2>(tuple), tupleMessage.items(2), client);
}

template <typename T0, typename T1, typename T2, typename T3>
inline void decode(std::tuple<T0, T1, T2, T3>& tuple, const std::string& data, Client * client) {
  krpc::schema::Tuple tupleMessage;
  tupleMessage.ParseFromString(data);
  decode(std::get<0>(tuple), tupleMessage.items(0), client);
  decode(std::get<1>(tuple), tupleMessage.items(1), client);
  decode(std::get<2>(tuple), tupleMessage.items(2), client);
  decode(std::get<3>(tuple), tupleMessage.items(3), client);
}

template <typename T0, typename T1, typename T2, typename T3, typename T4>
inline void decode(std::tuple<T0, T1, T2, T3, T4>& tuple, const std::string& data, Client * client) {
  krpc::schema::Tuple tupleMessage;
  tupleMessage.ParseFromString(data);
  decode(std::get<0>(tuple), tupleMessage.items(0), client);
  decode(std::get<1>(tuple), tupleMessage.items(1), client);
  decode(std::get<2>(tuple), tupleMessage.items(2), client);
  decode(std::get<3>(tuple), tupleMessage.items(3), client);
  decode(std::get<4>(tuple), tupleMessage.items(4), client);
}

}  // namespace decoder
}  // namespace krpc
