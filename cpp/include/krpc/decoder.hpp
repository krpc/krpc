#ifndef HEADER_KRPC_DECODER
#define HEADER_KRPC_DECODER

#include <boost/exception/all.hpp>
#include <google/protobuf/message.h>
#include <string>

namespace krpc {

  struct DecodeFailed : virtual boost::exception, virtual std::exception {};

  class Decoder {
  public:
    static const char OK_MESSAGE[];
    static const size_t OK_MESSAGE_LENGTH;
    static const size_t GUID_LENGTH;

    static std::string guid(const std::string& data);

    static void decode(google::protobuf::uint32& value, const std::string& data);
    static void decode(std::string& value, const std::string& data);
    static void decode(google::protobuf::Message& message, const std::string& data);

    static void decode_delimited(google::protobuf::uint32& value, const std::string& data);
    static void decode_delimited(google::protobuf::Message& message, const std::string& data);

    static std::pair<google::protobuf::uint32, google::protobuf::uint32> decode_size_and_position(const std::string& data);
  };

}

#endif
