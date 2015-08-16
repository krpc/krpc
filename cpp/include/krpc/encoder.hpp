#ifndef HEADER_KRPC_ENCODER
#define HEADER_KRPC_ENCODER

#include "krpc/KRPC.pb.h"
#include "krpc/object.hpp"
#include <boost/exception/all.hpp>
#include <google/protobuf/message.h>
#include <string>

namespace krpc {

  struct EncodeFailed : virtual boost::exception, virtual std::exception {};

  class Encoder {
  public:
    static const char RPC_HELLO_MESSAGE[];
    static const size_t RPC_HELLO_MESSAGE_LENGTH;
    static const char STREAM_HELLO_MESSAGE[];
    static const size_t STREAM_HELLO_MESSAGE_LENGTH;
    static std::string client_name(const std::string& name);

    static std::string encode(float value);
    static std::string encode(double value);
    static std::string encode(google::protobuf::int32 value);
    static std::string encode(google::protobuf::int64 value);
    static std::string encode(google::protobuf::uint32 value);
    static std::string encode(google::protobuf::uint64 value);
    static std::string encode(bool value);
    static std::string encode(const char* value);
    static std::string encode(const std::string& value);
    static std::string encode(const google::protobuf::Message& message);
    template <typename T> static std::string encode(const Object<T>& object);

    template <typename T> static std::string encode(const std::vector<T>& list);
    template <typename K, typename V> static std::string encode(const std::map<K,V>& dictionary);
    template <typename T> static std::string encode(const std::set<T>& set);

    template <typename T0>
    static std::string encode(const boost::tuple<T0>& tuple);
    template <typename T0, typename T1>
    static std::string encode(const boost::tuple<T0,T1>& tuple);
    template <typename T0, typename T1, typename T2>
    static std::string encode(const boost::tuple<T0,T1,T2>& tuple);
    template <typename T0, typename T1, typename T2, typename T3>
    static std::string encode(const boost::tuple<T0,T1,T2,T3>& tuple);
    template <typename T0, typename T1, typename T2, typename T3, typename T4>
    static std::string encode(const boost::tuple<T0,T1,T2,T3,T4>& tuple);

    static std::string encode_delimited(const google::protobuf::Message& message);
  };

  template <typename T>
  inline std::string Encoder::encode(const Object<T>& object) {
    return encode(object.id);
  }

  template <typename T>
  inline std::string Encoder::encode(const std::vector<T>& list) {
    krpc::schema::List listMessage;
    for (typename std::vector<T>::const_iterator x = list.begin(); x != list.end(); x++)
      listMessage.add_items(encode(*x));
    return encode(listMessage);
  }

  template <typename K, typename V>
  inline std::string Encoder::encode(const std::map<K,V>& dictionary) {
    krpc::schema::Dictionary dictionaryMessage;
    for (typename std::map<K,V>::const_iterator x = dictionary.begin(); x != dictionary.end(); x++) {
      schema::DictionaryEntry* entry = dictionaryMessage.add_entries();
      entry->set_key(encode(x->first));
      entry->set_value(encode(x->second));
    }
    return encode(dictionaryMessage);
  }

  template <typename T>
  inline std::string Encoder::encode(const std::set<T>& set) {
    krpc::schema::Set setMessage;
    for (typename std::set<T>::const_iterator x = set.begin(); x != set.end(); x++)
      setMessage.add_items(encode(*x));
    return encode(setMessage);
  }

  template <typename T0>
  inline std::string Encoder::encode(const boost::tuple<T0>& tuple) {
    krpc::schema::Tuple tupleMessage;
    tupleMessage.add_items(encode(boost::get<0>(tuple)));
    return encode(tupleMessage);
  }

  template <typename T0, typename T1>
  inline std::string Encoder::encode(const boost::tuple<T0,T1>& tuple) {
    krpc::schema::Tuple tupleMessage;
    tupleMessage.add_items(encode(boost::get<0>(tuple)));
    tupleMessage.add_items(encode(boost::get<1>(tuple)));
    return encode(tupleMessage);
  }

  template <typename T0, typename T1, typename T2>
  inline std::string Encoder::encode(const boost::tuple<T0,T1,T2>& tuple) {
    krpc::schema::Tuple tupleMessage;
    tupleMessage.add_items(encode(boost::get<0>(tuple)));
    tupleMessage.add_items(encode(boost::get<1>(tuple)));
    tupleMessage.add_items(encode(boost::get<2>(tuple)));
    return encode(tupleMessage);
  }

  template <typename T0, typename T1, typename T2, typename T3>
  inline std::string Encoder::encode(const boost::tuple<T0,T1,T2,T3>& tuple) {
    krpc::schema::Tuple tupleMessage;
    tupleMessage.add_items(encode(boost::get<0>(tuple)));
    tupleMessage.add_items(encode(boost::get<1>(tuple)));
    tupleMessage.add_items(encode(boost::get<2>(tuple)));
    tupleMessage.add_items(encode(boost::get<3>(tuple)));
    return encode(tupleMessage);
  }

  template <typename T0, typename T1, typename T2, typename T3, typename T4>
  inline std::string Encoder::encode(const boost::tuple<T0,T1,T2,T3,T4>& tuple) {
    krpc::schema::Tuple tupleMessage;
    tupleMessage.add_items(encode(boost::get<0>(tuple)));
    tupleMessage.add_items(encode(boost::get<1>(tuple)));
    tupleMessage.add_items(encode(boost::get<2>(tuple)));
    tupleMessage.add_items(encode(boost::get<3>(tuple)));
    tupleMessage.add_items(encode(boost::get<4>(tuple)));
    return encode(tupleMessage);
  }

}

#endif
