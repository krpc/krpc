#ifndef HEADER_KRPC_ENCODER
#define HEADER_KRPC_ENCODER

#include "krpc/KRPC.pb.h"
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
    static std::string encode(const std::string& value);
    static std::string encode(const google::protobuf::Message& value);

    template <typename T> static std::string encode(const std::vector<T>& value);

    static std::string encode_delimited(google::protobuf::uint32 value);
    static std::string encode_delimited(const std::string& value);
    static std::string encode_delimited(const google::protobuf::Message& value);
  };

  template <typename T> inline std::string Encoder::encode(const std::vector<T>& value) {
    krpc::schema::List list;
    for (typename std::vector<T>::const_iterator x = value.begin(); x != value.end(); x++)
      list.add_items(encode(*x));
    return encode(list);
  }

}

#endif
