#ifndef HEADER_KRPC_DECODER
#define HEADER_KRPC_DECODER

#include "krpc/KRPC.pb.h"
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

    static void decode(float& value, const std::string& data);
    static void decode(double& value, const std::string& data);
    static void decode(google::protobuf::int32& value, const std::string& data);
    static void decode(google::protobuf::int64& value, const std::string& data);
    static void decode(google::protobuf::uint32& value, const std::string& data);
    static void decode(google::protobuf::uint64& value, const std::string& data);
    static void decode(bool& value, const std::string& data);
    static void decode(std::string& value, const std::string& data);
    static void decode(google::protobuf::Message& message, const std::string& data);

    template <typename T> static void decode(std::vector<T>& value, const std::string& data);

    static void decode_delimited(google::protobuf::uint32& value, const std::string& data);
    static void decode_delimited(google::protobuf::Message& message, const std::string& data);

    static std::pair<google::protobuf::uint32, google::protobuf::uint32> decode_size_and_position(const std::string& data);
  };

  template <typename T> inline void Decoder::decode(std::vector<T>& value, const std::string& data) {
    value.clear();
    krpc::schema::List list;
    list.ParseFromString(data);
    for (int i = 0; i < list.items_size(); i++) {
      T x;
      decode(x, list.items(i));
      value.push_back(x);
    }
  }

}

#endif
