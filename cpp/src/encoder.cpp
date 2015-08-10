#include "krpc/encoder.hpp"
#include <google/protobuf/io/coded_stream.h>

namespace pb = google::protobuf;

namespace krpc {

  const size_t Encoder::RPC_HELLO_MESSAGE_LENGTH = 12;
  const char Encoder::RPC_HELLO_MESSAGE[] = {
    0x48, 0x45, 0x4C, 0x4C,
    0x4F, 0x2D, 0x52, 0x50,
    0x43, 0x00, 0x00, 0x00
  };

  const size_t Encoder::STREAM_HELLO_MESSAGE_LENGTH = 12;
  const char Encoder::STREAM_HELLO_MESSAGE[] = {
    0x48, 0x45, 0x4C, 0x4C,
    0x4F, 0x2D, 0x53, 0x54,
    0x52, 0x45, 0x41, 0x4D
  };

  std::string Encoder::client_name(const std::string& name) {
    std::string result(name);
    result.resize(32);
    return result;
  }

  std::string Encoder::encode(pb::uint32 value) {
    size_t length = pb::io::CodedOutputStream::VarintSize32(value);
    std::string data(length, 0);
    pb::io::CodedOutputStream::WriteVarint32ToArray(value, (pb::uint8*)&data[0]);
    return data;
  }

  std::string Encoder::encode(const std::string& value) {
    size_t length = value.size();
    size_t header_length = pb::io::CodedOutputStream::VarintSize64(length);
    std::string data(header_length + length, 0);
    pb::io::CodedOutputStream::WriteVarint64ToArray(length, (pb::uint8*)&data[0]);
    pb::io::CodedOutputStream::WriteStringToArray(value, (pb::uint8*)&data[header_length]);
    return data;
  }

  std::string Encoder::encode(const pb::Message& message) {
    std::string data;
    message.SerializeToString(&data);
    return data;
  }

  std::string Encoder::encode_delimited(pb::uint32 value) {
    size_t length = pb::io::CodedOutputStream::VarintSize32(value);
    size_t header_length = pb::io::CodedOutputStream::VarintSize64(length);
    std::string data(header_length + length, 0);
    pb::io::CodedOutputStream::WriteVarint64ToArray(length, (pb::uint8*)&data[0]);
    pb::io::CodedOutputStream::WriteVarint32ToArray(value, (pb::uint8*)&data[header_length]);
    return data;
  }

  std::string Encoder::encode_delimited(const std::string& value) {
    size_t length = value.size();
    size_t header_length = pb::io::CodedOutputStream::VarintSize64(length);
    std::string data(header_length + length, 0);
    pb::io::CodedOutputStream::WriteVarint64ToArray(length, (pb::uint8*)&data[0]);
    pb::io::CodedOutputStream::WriteStringToArray(value, (pb::uint8*)&data[header_length]);
    return data;
  }

  std::string Encoder::encode_delimited(const pb::Message& message) {
    size_t length = message.ByteSize();
    size_t header_length = pb::io::CodedOutputStream::VarintSize64(length);
    std::string data(header_length + length, 0);
    pb::io::CodedOutputStream::WriteVarint64ToArray(length, (pb::uint8*)&data[0]);
    message.SerializeWithCachedSizesToArray((pb::uint8*)&data[header_length]);
    return data;
  }

}
