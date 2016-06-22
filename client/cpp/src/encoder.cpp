#include "krpc/encoder.hpp"

#include <google/protobuf/io/coded_stream.h>
#include <google/protobuf/wire_format_lite.h>

#include <string>

namespace pb = google::protobuf;

namespace krpc {
namespace encoder {

static const size_t LITTLE_ENDIAN_32_LENGTH = 4;
static const size_t LITTLE_ENDIAN_64_LENGTH = 8;

std::string client_name(const std::string& name) {
  std::string result(name);
  result.resize(32);
  return result;
}

std::string encode(float value) {
  pb::uint32 value2 = pb::internal::WireFormatLite::EncodeFloat(value);
  std::string data(LITTLE_ENDIAN_32_LENGTH, 0);
  pb::io::CodedOutputStream::WriteLittleEndian32ToArray(value2, (pb::uint8*)&data[0]);
  return data;
}

std::string encode(double value) {
  pb::uint64 value2 = pb::internal::WireFormatLite::EncodeDouble(value);
  std::string data(LITTLE_ENDIAN_64_LENGTH, 0);
  pb::io::CodedOutputStream::WriteLittleEndian64ToArray(value2, (pb::uint8*)&data[0]);
  return data;
}

std::string encode(pb::int32 value) {
  size_t length = pb::io::CodedOutputStream::VarintSize32SignExtended(value);
  std::string data(length, 0);
  pb::io::CodedOutputStream::WriteVarint32SignExtendedToArray(value, (pb::uint8*)&data[0]);
  return data;
}

std::string encode(pb::int64 value) {
  pb::uint64 value2 = static_cast<pb::uint64>(value);
  size_t length = pb::io::CodedOutputStream::VarintSize64(value2);
  std::string data(length, 0);
  pb::io::CodedOutputStream::WriteVarint64ToArray(value2, (pb::uint8*)&data[0]);
  return data;
}

std::string encode(pb::uint32 value) {
  size_t length = pb::io::CodedOutputStream::VarintSize32(value);
  std::string data(length, 0);
  pb::io::CodedOutputStream::WriteVarint32ToArray(value, (pb::uint8*)&data[0]);
  return data;
}

std::string encode(pb::uint64 value) {
  size_t length = pb::io::CodedOutputStream::VarintSize64(value);
  std::string data(length, 0);
  pb::io::CodedOutputStream::WriteVarint64ToArray(value, (pb::uint8*)&data[0]);
  return data;
}

std::string encode(bool value) {
  pb::uint32 value2 = (value ? 1 : 0);
  size_t length = pb::io::CodedOutputStream::VarintSize32(value2);
  std::string data(length, 0);
  pb::io::CodedOutputStream::WriteVarint32ToArray(value, (pb::uint8*)&data[0]);
  return data;
}

std::string encode(const char* value) {
  return encode(std::string(value));
}

std::string encode(const std::string& value) {
  size_t length = value.size();
  size_t header_length = pb::io::CodedOutputStream::VarintSize64(length);
  std::string data(header_length + length, 0);
  pb::io::CodedOutputStream::WriteVarint64ToArray(length, (pb::uint8*)&data[0]);
  pb::io::CodedOutputStream::WriteStringToArray(value, (pb::uint8*)&data[header_length]);
  return data;
}

std::string encode(const pb::Message& message) {
  std::string data;
  if (!message.SerializeToString(&data))
    throw EncodeFailed("Failed to encode message");
  return data;
}

std::string encode_delimited(const pb::Message& message) {
  size_t length = message.ByteSize();
  size_t header_length = pb::io::CodedOutputStream::VarintSize64(length);
  std::string data(header_length + length, 0);
  pb::io::CodedOutputStream::WriteVarint64ToArray(length, (pb::uint8*)&data[0]);
  if (!message.SerializeWithCachedSizesToArray((pb::uint8*)&data[header_length]))
    throw EncodeFailed("Failed to encode delimited message");
  return data;
}

}  // namespace encoder
}  // namespace krpc
