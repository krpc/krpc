#pragma once

#include <google/protobuf/message.h>

#include <map>
#include <set>
#include <string>
#include <tuple>
#include <vector>

#include "krpc/krpc.pb.hpp"
#include "krpc/object.hpp"

namespace krpc {
namespace encoder {

class EncodeFailed : std::runtime_error {
 public:
  explicit EncodeFailed(const std::string& msg) : std::runtime_error(msg) {}
};

const char RPC_HELLO_MESSAGE[] = {
  0x48, 0x45, 0x4C, 0x4C,
  0x4F, 0x2D, 0x52, 0x50,
  0x43, 0x00, 0x00, 0x00
};
const size_t RPC_HELLO_MESSAGE_LENGTH = 12;
const char STREAM_HELLO_MESSAGE[] = {
  0x48, 0x45, 0x4C, 0x4C,
  0x4F, 0x2D, 0x53, 0x54,
  0x52, 0x45, 0x41, 0x4D
};
const size_t STREAM_HELLO_MESSAGE_LENGTH = 12;
std::string client_name(const std::string& name);

std::string encode(float value);
std::string encode(double value);
std::string encode(google::protobuf::int32 value);
std::string encode(google::protobuf::int64 value);
std::string encode(google::protobuf::uint32 value);
std::string encode(google::protobuf::uint64 value);
std::string encode(bool value);
std::string encode(const char* value);
std::string encode(const std::string& value);
std::string encode(const google::protobuf::Message& message);
template <typename T> std::string encode(const Object<T>& object);

template <typename T> std::string encode(const std::vector<T>& list);
template <typename K, typename V> std::string encode(const std::map<K, V>& dictionary);
template <typename T> std::string encode(const std::set<T>& set);

/*[[[cog
import cog
import itertools
for n in range(1,int(nargs)+1):
    cog.out("""
    template <""" + ', '.join('typename T%d' % i for i in range(n)) + """>
    std::string encode(const std::tuple<""" + ', '.join('T%d' % i for i in range(n)) + """>& tuple);""")
]]]*/

template <typename T0>
std::string encode(const std::tuple<T0>& tuple);
template <typename T0, typename T1>
std::string encode(const std::tuple<T0, T1>& tuple);
template <typename T0, typename T1, typename T2>
std::string encode(const std::tuple<T0, T1, T2>& tuple);
template <typename T0, typename T1, typename T2, typename T3>
std::string encode(const std::tuple<T0, T1, T2, T3>& tuple);
template <typename T0, typename T1, typename T2, typename T3, typename T4>
std::string encode(const std::tuple<T0, T1, T2, T3, T4>& tuple);
// [[[end]]]

std::string encode_delimited(const google::protobuf::Message& message);

template <typename T>
inline std::string encode(const Object<T>& object) {
  return encode(object._id);
}

template <typename T>
inline std::string encode(const std::vector<T>& list) {
  krpc::schema::List listMessage;
  for (typename std::vector<T>::const_iterator x = list.begin(); x != list.end(); x++)
    listMessage.add_items(encode(*x));
  return encode(listMessage);
}

template <typename K, typename V>
inline std::string encode(const std::map<K, V>& dictionary) {
  krpc::schema::Dictionary dictionaryMessage;
  for (typename std::map<K, V>::const_iterator x = dictionary.begin(); x != dictionary.end(); x++) {
    schema::DictionaryEntry* entry = dictionaryMessage.add_entries();
    entry->set_key(encode(x->first));
    entry->set_value(encode(x->second));
  }
  return encode(dictionaryMessage);
}

template <typename T>
inline std::string encode(const std::set<T>& set) {
  krpc::schema::Set setMessage;
  for (typename std::set<T>::const_iterator x = set.begin(); x != set.end(); x++)
    setMessage.add_items(encode(*x));
  return encode(setMessage);
}

/*[[[cog
import cog
import itertools
for n in range(1,int(nargs)+1):
    cog.out("""
template <""" + ', '.join('typename T%d' % i for i in range(n)) + """>
inline std::string encode(const std::tuple<""" + ', '.join('T%d' % i for i in range(n)) + """>& tuple) {
  krpc::schema::Tuple tupleMessage;
""")
    for i in range(n):
        cog.outl('  tupleMessage.add_items(encode(std::get<%d>(tuple)));' % i)
    cog.out("""  return encode(tupleMessage);
}
""")
]]]*/

template <typename T0>
inline std::string encode(const std::tuple<T0>& tuple) {
  krpc::schema::Tuple tupleMessage;
  tupleMessage.add_items(encode(std::get<0>(tuple)));
  return encode(tupleMessage);
}

template <typename T0, typename T1>
inline std::string encode(const std::tuple<T0, T1>& tuple) {
  krpc::schema::Tuple tupleMessage;
  tupleMessage.add_items(encode(std::get<0>(tuple)));
  tupleMessage.add_items(encode(std::get<1>(tuple)));
  return encode(tupleMessage);
}

template <typename T0, typename T1, typename T2>
inline std::string encode(const std::tuple<T0, T1, T2>& tuple) {
  krpc::schema::Tuple tupleMessage;
  tupleMessage.add_items(encode(std::get<0>(tuple)));
  tupleMessage.add_items(encode(std::get<1>(tuple)));
  tupleMessage.add_items(encode(std::get<2>(tuple)));
  return encode(tupleMessage);
}

template <typename T0, typename T1, typename T2, typename T3>
inline std::string encode(const std::tuple<T0, T1, T2, T3>& tuple) {
  krpc::schema::Tuple tupleMessage;
  tupleMessage.add_items(encode(std::get<0>(tuple)));
  tupleMessage.add_items(encode(std::get<1>(tuple)));
  tupleMessage.add_items(encode(std::get<2>(tuple)));
  tupleMessage.add_items(encode(std::get<3>(tuple)));
  return encode(tupleMessage);
}

template <typename T0, typename T1, typename T2, typename T3, typename T4>
inline std::string encode(const std::tuple<T0, T1, T2, T3, T4>& tuple) {
  krpc::schema::Tuple tupleMessage;
  tupleMessage.add_items(encode(std::get<0>(tuple)));
  tupleMessage.add_items(encode(std::get<1>(tuple)));
  tupleMessage.add_items(encode(std::get<2>(tuple)));
  tupleMessage.add_items(encode(std::get<3>(tuple)));
  tupleMessage.add_items(encode(std::get<4>(tuple)));
  return encode(tupleMessage);
}
// [[[end]]]

}  // namespace encoder
}  // namespace krpc
