#include "krpc/encoder.hpp"

#include <google/protobuf/io/coded_stream.h>
#include <google/protobuf/message_lite.h>
#include <google/protobuf/wire_format_lite.h>

#include <cstddef>
#include <cstdint>
#include <string>

#include "krpc/error.hpp"

namespace krpc {
namespace encoder {

static const size_t LITTLE_ENDIAN_32_LENGTH = 4;
static const size_t LITTLE_ENDIAN_64_LENGTH = 8;

std::string encode(double value) {
  uint64_t value2 = google::protobuf::internal::WireFormatLite::EncodeDouble(value);
  std::string data(LITTLE_ENDIAN_64_LENGTH, 0);
  (void)google::protobuf::io::CodedOutputStream::WriteLittleEndian64ToArray(
    value2, reinterpret_cast<uint8_t*>(&data[0]));
  return data;
}

std::string encode(float value) {
  uint32_t value2 = google::protobuf::internal::WireFormatLite::EncodeFloat(value);
  std::string data(LITTLE_ENDIAN_32_LENGTH, 0);
  (void)google::protobuf::io::CodedOutputStream::WriteLittleEndian32ToArray(
    value2, reinterpret_cast<uint8_t*>(&data[0]));
  return data;
}

std::string encode(int32_t value) {
  uint32_t zigZagValue = google::protobuf::internal::WireFormatLite::ZigZagEncode32(value);
  size_t length = google::protobuf::io::CodedOutputStream::VarintSize32(zigZagValue);
  std::string data(length, 0);
  (void)google::protobuf::io::CodedOutputStream::WriteVarint32ToArray(
    zigZagValue, reinterpret_cast<uint8_t*>(&data[0]));
  return data;
}

std::string encode(int64_t value) {
  uint64_t zigZagValue = google::protobuf::internal::WireFormatLite::ZigZagEncode64(value);
  size_t length = google::protobuf::io::CodedOutputStream::VarintSize64(zigZagValue);
  std::string data(length, 0);
  (void)google::protobuf::io::CodedOutputStream::WriteVarint64ToArray(
    zigZagValue, reinterpret_cast<uint8_t*>(&data[0]));
  return data;
}

std::string encode(uint32_t value) {
  size_t length = google::protobuf::io::CodedOutputStream::VarintSize32(value);
  std::string data(length, 0);
  (void)google::protobuf::io::CodedOutputStream::WriteVarint32ToArray(
    value, reinterpret_cast<uint8_t*>(&data[0]));
  return data;
}

std::string encode(uint64_t value) {
  size_t length = google::protobuf::io::CodedOutputStream::VarintSize64(value);
  std::string data(length, 0);
  (void)google::protobuf::io::CodedOutputStream::WriteVarint64ToArray(
    value, reinterpret_cast<uint8_t*>(&data[0]));
  return data;
}

std::string encode(bool value) {
  uint32_t value2 = (value ? 1 : 0);
  size_t length = google::protobuf::io::CodedOutputStream::VarintSize32(value2);
  std::string data(length, 0);
  (void)google::protobuf::io::CodedOutputStream::WriteVarint32ToArray(
    value, reinterpret_cast<uint8_t*>(&data[0]));
  return data;
}

std::string encode(const char* value) {
  return encode(std::string(value));
}

std::string encode(const std::string& value) {
  size_t length = value.size();
  size_t header_length = google::protobuf::io::CodedOutputStream::VarintSize64(length);
  std::string data(header_length + length, 0);
  (void)google::protobuf::io::CodedOutputStream::WriteVarint64ToArray(
    length, reinterpret_cast<uint8_t*>(&data[0]));
  (void)google::protobuf::io::CodedOutputStream::WriteStringToArray(
    value, reinterpret_cast<uint8_t*>(&data[header_length]));
  return data;
}

std::string encode(const google::protobuf::MessageLite& message) {
  std::string data;
  if (!message.SerializeToString(&data))
    throw EncodingError("Failed to encode message");
  return data;
}

std::string encode_message_with_size(const google::protobuf::MessageLite& message) {
  size_t length = message.ByteSizeLong();
  size_t header_length = google::protobuf::io::CodedOutputStream::VarintSize64(length);
  std::string data(header_length + length, 0);
  (void)google::protobuf::io::CodedOutputStream::WriteVarint64ToArray(
    length, reinterpret_cast<uint8_t*>(&data[0]));
  if (!message.SerializeWithCachedSizesToArray(
        reinterpret_cast<uint8_t*>(&data[header_length])))
    throw EncodingError("Failed to encode message with size");
  return data;
}

}  // namespace encoder
}  // namespace krpc
