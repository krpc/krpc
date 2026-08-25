#include "krpc/decoder.hpp"

#include <google/protobuf/io/coded_stream.h>
#include <google/protobuf/message_lite.h>
#include <google/protobuf/wire_format_lite.h>

#include <algorithm>
#include <cstddef>
#include <cstdint>
#include <string>

#include "krpc/error.hpp"
#include "krpc/event.hpp"
#include "krpc/platform.hpp"

namespace krpc {
namespace decoder {

namespace {

const size_t LITTLE_ENDIAN_32_LENGTH = 4;
const size_t LITTLE_ENDIAN_64_LENGTH = 8;

// The most bytes a varint can take: seven bits of a 64 bit value in each.
const size_t MAX_VARINT_LENGTH = 10;

// Read a varint out of a buffer, and return how many bytes it took, or zero if the buffer does
// not hold a whole one yet. A varint carries seven bits of the value in each byte, with the
// top bit saying whether another byte follows.
//
// Read here and not through a coded stream. A value is a few bytes long, and a collection
// arrives a value at a time, so setting a stream up to read one costs more than reading it.
size_t read_varint(const char* data, size_t length, uint64_t* value) {
  uint64_t result = 0;
  size_t whole = std::min(length, MAX_VARINT_LENGTH);
  for (size_t i = 0; i < whole; i++) {
    uint8_t byte = static_cast<uint8_t>(data[i]);
    result |= static_cast<uint64_t>(byte & 0x7f) << (7 * i);
    if ((byte & 0x80) == 0) {
      *value = result;
      return i + 1;
    }
  }
  // A varint that has not ended by its last byte is not one, however much data follows it.
  if (length >= MAX_VARINT_LENGTH) throw EncodingError("Failed to decode a varint");
  return 0;
}

// Read a varint that the data has to hold, and return how many bytes it took. What is being
// decoded is named for the error, which is all the caller could have said about it anyway.
size_t read_varint(const std::string& data, uint64_t* value, const char* what) {
  size_t length = read_varint(data.data(), data.size(), value);
  if (length == 0) throw EncodingError(std::string("Failed to decode ") + what);
  return length;
}

}  // namespace

std::string guid(const std::string& data) {
  if (data.size() != 16) throw EncodingError("GUID is not 16 characters");
  return platform::hexlify(std::string(data.rbegin() + 12, data.rend())) + "-" +
         platform::hexlify(std::string(data.rbegin() + 10, data.rbegin() + 12)) + "-" +
         platform::hexlify(std::string(data.rbegin() + 8, data.rbegin() + 10)) + "-" +
         platform::hexlify(std::string(data.begin() + 8, data.begin() + 10)) + "-" +
         platform::hexlify(std::string(data.begin() + 10, data.end()));
}

void decode(double& value, const std::string& data, Client* client) {
  if (data.size() < LITTLE_ENDIAN_64_LENGTH) throw EncodingError("Failed to decode double");
  uint64_t value2 = 0;
  (void)google::protobuf::io::CodedInputStream::ReadLittleEndian64FromArray(
      reinterpret_cast<const uint8_t*>(data.data()), &value2);
  value = google::protobuf::internal::WireFormatLite::DecodeDouble(value2);
}

void decode(float& value, const std::string& data, Client* client) {
  if (data.size() < LITTLE_ENDIAN_32_LENGTH) throw EncodingError("Failed to decode float");
  uint32_t value2 = 0;
  (void)google::protobuf::io::CodedInputStream::ReadLittleEndian32FromArray(
      reinterpret_cast<const uint8_t*>(data.data()), &value2);
  value = google::protobuf::internal::WireFormatLite::DecodeFloat(value2);
}

void decode(int32_t& value, const std::string& data, Client* client) {
  uint64_t zigZagValue = 0;
  read_varint(data, &zigZagValue, "sint32");
  value = google::protobuf::internal::WireFormatLite::ZigZagDecode32(
      static_cast<uint32_t>(zigZagValue));
}

void decode(int64_t& value, const std::string& data, Client* client) {
  uint64_t zigZagValue = 0;
  read_varint(data, &zigZagValue, "sint64");
  value = google::protobuf::internal::WireFormatLite::ZigZagDecode64(zigZagValue);
}

void decode(uint32_t& value, const std::string& data, Client* client) {
  uint64_t value2 = 0;
  read_varint(data, &value2, "uint32");
  value = static_cast<uint32_t>(value2);
}

void decode(uint64_t& value, const std::string& data, Client* client) {
  read_varint(data, &value, "uint64");
}

void decode(bool& value, const std::string& data, Client* client) {
  uint64_t value2 = 0;
  read_varint(data, &value2, "bool");
  value = (value2 != 0);
}

void decode(std::string& value, const std::string& data, Client* client) {
  uint64_t length = 0;
  size_t header_length = read_varint(data, &length, "string (length)");
  if (data.size() - header_length < length) throw EncodingError("Failed to decode string");
  value.assign(data, header_length, length);
}

void decode(Event& event, const std::string& data, Client* client) {
  krpc::schema::Event message;
  if (!message.ParseFromString(data)) throw EncodingError("Failed to decode message");
  event = Event(client, message);
}

void decode(google::protobuf::MessageLite& message, const std::string& data, Client* client) {
  if (!message.ParseFromString(data)) throw EncodingError("Failed to decode message");
}

uint32_t decode_size(const std::string& data) {
  uint32_t size = 0;
  size_t prefix_length = 0;
  if (!decode_size_prefix(data.data(), data.size(), &size, &prefix_length))
    throw EncodingError("Failed to decode size");
  return size;
}

bool decode_size_prefix(const char* data, size_t length, uint32_t* size, size_t* prefix_length) {
  uint64_t value = 0;
  size_t read = read_varint(data, length, &value);
  if (read == 0) return false;
  *size = static_cast<uint32_t>(value);
  *prefix_length = read;
  return true;
}

}  // namespace decoder
}  // namespace krpc
