#ifndef HEADER_KRPC_DECODER
#define HEADER_KRPC_DECODER

#include "krpc/KRPC.pb.h"
#include "krpc/object.hpp"
#include <boost/exception/all.hpp>
#include <google/protobuf/message.h>
#include <boost/tuple/tuple.hpp>
#include <string>

namespace krpc {

  struct DecodeFailed : virtual boost::exception, virtual std::exception {};

  class Decoder {
  public:
    static const char OK_MESSAGE[];
    static const size_t OK_MESSAGE_LENGTH;
    static const size_t GUID_LENGTH;

    static std::string guid(const std::string& data);

    static void decode(float& value, const std::string& data);
    static void decode(double& value, const std::string& data);
    static void decode(google::protobuf::int32& value, const std::string& data);
    static void decode(google::protobuf::int64& value, const std::string& data);
    static void decode(google::protobuf::uint32& value, const std::string& data);
    static void decode(google::protobuf::uint64& value, const std::string& data);
    static void decode(bool& value, const std::string& data);
    static void decode(std::string& value, const std::string& data);
    static void decode(google::protobuf::Message& message, const std::string& data);
    template <typename T> static void decode(Object<T>& object, const std::string& data);

    template <typename T>
    static void decode(std::vector<T>& list, const std::string& data);
    template <typename K, typename V>
    static void decode(std::map<K,V>& dictionary, const std::string& data);
    template <typename T>
    static void decode(std::set<T>& set, const std::string& data);

    template <typename T0>
    static void decode(boost::tuple<T0>& tuple, const std::string& data);
    template <typename T0, typename T1>
    static void decode(boost::tuple<T0,T1>& tuple, const std::string& data);
    template <typename T0, typename T1, typename T2>
    static void decode(boost::tuple<T0,T1,T2>& tuple, const std::string& data);
    template <typename T0, typename T1, typename T2, typename T3>
    static void decode(boost::tuple<T0,T1,T2,T3>& tuple, const std::string& data);
    template <typename T0, typename T1, typename T2, typename T3, typename T4>
    static void decode(boost::tuple<T0,T1,T2,T3,T4>& tuple, const std::string& data);

    static void decode_delimited(google::protobuf::Message& message, const std::string& data);

    static std::pair<google::protobuf::uint32, google::protobuf::uint32> decode_size_and_position(const std::string& data);
  };

  template <typename T>
  inline void Decoder::decode(Object<T>& object, const std::string& data) {
    google::protobuf::uint64 id;
    decode(id, data);
    object.id = id;
  }

  template <typename T>
  inline void Decoder::decode(std::vector<T>& list, const std::string& data) {
    list.clear();
    krpc::schema::List listMessage;
    listMessage.ParseFromString(data);
    for (int i = 0; i < listMessage.items_size(); i++) {
      T value;
      decode(value, listMessage.items(i));
      list.push_back(value);
    }
  }

  template <typename K, typename V>
  inline void Decoder::decode(std::map<K,V>& dictionary, const std::string& data) {
    dictionary.clear();
    krpc::schema::Dictionary dictionaryMessage;
    dictionaryMessage.ParseFromString(data);
    for (int i = 0; i < dictionaryMessage.entries_size(); i++) {
      const schema::DictionaryEntry& entry = dictionaryMessage.entries(i);
      K key;
      V value;
      decode(key, entry.key());
      decode(value, entry.value());
      dictionary[key] = value;
    }
  }

  template <typename T>
  inline void Decoder::decode(std::set<T>& set, const std::string& data) {
    set.clear();
    krpc::schema::Set setMessage;
    setMessage.ParseFromString(data);
    for (int i = 0; i < setMessage.items_size(); i++) {
      T value;
      decode(value, setMessage.items(i));
      set.insert(value);
    }
  }

  template <typename T0>
  inline void Decoder::decode(boost::tuple<T0>& tuple, const std::string& data) {
    krpc::schema::Tuple tupleMessage;
    tupleMessage.ParseFromString(data);
    decode(boost::get<0>(tuple), tupleMessage.items(0));
  }

  template <typename T0, typename T1>
  inline void Decoder::decode(boost::tuple<T0,T1>& tuple, const std::string& data) {
    krpc::schema::Tuple tupleMessage;
    tupleMessage.ParseFromString(data);
    decode(boost::get<0>(tuple), tupleMessage.items(0));
    decode(boost::get<1>(tuple), tupleMessage.items(1));
  }

  template <typename T0, typename T1, typename T2>
  inline void Decoder::decode(boost::tuple<T0,T1,T2>& tuple, const std::string& data) {
    krpc::schema::Tuple tupleMessage;
    tupleMessage.ParseFromString(data);
    decode(boost::get<0>(tuple), tupleMessage.items(0));
    decode(boost::get<1>(tuple), tupleMessage.items(1));
    decode(boost::get<2>(tuple), tupleMessage.items(2));
  }

  template <typename T0, typename T1, typename T2, typename T3>
  inline void Decoder::decode(boost::tuple<T0,T1,T2,T3>& tuple, const std::string& data) {
    krpc::schema::Tuple tupleMessage;
    tupleMessage.ParseFromString(data);
    decode(boost::get<0>(tuple), tupleMessage.items(0));
    decode(boost::get<1>(tuple), tupleMessage.items(1));
    decode(boost::get<2>(tuple), tupleMessage.items(2));
    decode(boost::get<3>(tuple), tupleMessage.items(3));
  }

  template <typename T0, typename T1, typename T2, typename T3, typename T4>
  inline void Decoder::decode(boost::tuple<T0,T1,T2,T3,T4>& tuple, const std::string& data) {
    krpc::schema::Tuple tupleMessage;
    tupleMessage.ParseFromString(data);
    decode(boost::get<0>(tuple), tupleMessage.items(0));
    decode(boost::get<1>(tuple), tupleMessage.items(1));
    decode(boost::get<2>(tuple), tupleMessage.items(2));
    decode(boost::get<3>(tuple), tupleMessage.items(3));
    decode(boost::get<4>(tuple), tupleMessage.items(4));
  }

}

#endif
