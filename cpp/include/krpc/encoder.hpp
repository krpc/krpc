#ifndef HEADER_KRPC_ENCODER
#define HEADER_KRPC_ENCODER

#include <google/protobuf/message.h>
#include <string>

namespace krpc {

  class Encoder {
  public:
    static const char RPC_HELLO_MESSAGE[];
    static const size_t RPC_HELLO_MESSAGE_LENGTH;
    static const char STREAM_HELLO_MESSAGE[];
    static const size_t STREAM_HELLO_MESSAGE_LENGTH;
    static std::string client_name(const std::string& name);

    static std::string encode(google::protobuf::uint32 value);
    static std::string encode(const std::string& value);
    static std::string encode(const google::protobuf::Message& value);

    static std::string encode_delimited(google::protobuf::uint32 value);
    static std::string encode_delimited(const std::string& value);
    static std::string encode_delimited(const google::protobuf::Message& value);
  };

}

#endif
