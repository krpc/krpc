#include "krpc/decoder.hpp"

#include <google/protobuf/io/coded_stream.h>
#include <google/protobuf/message.h>
#include <google/protobuf/wire_format_lite.h>

#include <string>

#include "krpc/error.hpp"
#include "krpc/event.hpp"
#include "krpc/platform.hpp"

namespace pb = google::protobuf;

namespace krpc {
namespace decoder {

std::string guid(const std::string& data) {
  if (data.size() != 16)
    throw EncodingError("GUID is not 16 characters");
  return
    platform::hexlify(std::string(data.rbegin() + 12, data.rend()))        + "-" +
    platform::hexlify(std::string(data.rbegin() + 10, data.rbegin() + 12)) + "-" +
    platform::hexlify(std::string(data.rbegin() + 8,  data.rbegin() + 10)) + "-" +
    platform::hexlify(std::string(data.begin()  + 8,  data.begin()  + 10)) + "-" +
    platform::hexlify(std::string(data.begin()  + 10, data.end()));
}

void decode(double& value, const std::string& data, Client* client) {
  pb::io::CodedInputStream stream((pb::uint8*)&data[0], data.size());
  pb::uint64 value2 = 0;
  if (!stream.ReadLittleEndian64(&value2))
    throw EncodingError("Failed to decode double");
  value = pb::internal::WireFormatLite::DecodeDouble(value2);
}

void decode(float& value, const std::string& data, Client* client) {
  pb::io::CodedInputStream stream((pb::uint8*)(&data[0]), data.size());
  pb::uint32 value2;
  if (!stream.ReadLittleEndian32(&value2))
    throw EncodingError("Failed to decode float");
  value = pb::internal::WireFormatLite::DecodeFloat(value2);
}

void decode(pb::int32& value, const std::string& data, Client* client) {
  pb::io::CodedInputStream stream((pb::uint8*)&data[0], data.size());
  pb::uint32 zigZagValue = 0;
  if (!stream.ReadVarint32(&zigZagValue))
    throw EncodingError("Failed to decode sint32");
  value = pb::internal::WireFormatLite::ZigZagDecode32(zigZagValue);
}

void decode(pb::int64& value, const std::string& data, Client* client) {
  pb::io::CodedInputStream stream((pb::uint8*)&data[0], data.size());
  pb::uint64 zigZagValue = 0;
  if (!stream.ReadVarint64(&zigZagValue))
    throw EncodingError("Failed to decode sint64");
  value = pb::internal::WireFormatLite::ZigZagDecode64(zigZagValue);
}

void decode(pb::uint32& value, const std::string& data, Client* client) {
  pb::io::CodedInputStream stream((pb::uint8*)&data[0], data.size());
  if (!stream.ReadVarint32(&value))
    throw EncodingError("Failed to decode uint32");
}

void decode(pb::uint64& value, const std::string& data, Client* client) {
  pb::io::CodedInputStream stream((pb::uint8*)&data[0], data.size());
  if (!stream.ReadVarint64(&value))
    throw EncodingError("Failed to decode uint64");
}

void decode(bool& value, const std::string& data, Client* client) {
  pb::io::CodedInputStream stream((pb::uint8*)&data[0], data.size());
  pb::uint64 value2 = 0;
  if (!stream.ReadVarint64(&value2))
    throw EncodingError("Failed to decode bool");
  value = (value2 != 0);
}

void decode(std::string& value, const std::string& data, Client* client) {
  pb::io::CodedInputStream stream((pb::uint8*)&data[0], data.size());
  pb::uint64 length;
  if (!stream.ReadVarint64(&length))
    throw EncodingError("Failed to decode string (length)");
  if (!stream.ReadString(&value, static_cast<int>(length)))
    throw EncodingError("Failed to decode string");
}

void decode(Event& event, const std::string& data, Client* client) {
  krpc::schema::Event message;
  if (!message.ParseFromString(data))
    throw EncodingError("Failed to decode message");
  event = Event(client, message);
}

void decode(pb::Message& message, const std::string& data, Client* client) {
  if (!message.ParseFromString(data))
    throw EncodingError("Failed to decode message");
}

pb::uint32 decode_size(const std::string& data) {
  pb::uint32 result;
  pb::io::CodedInputStream stream((pb::uint8*)&data[0], data.size());
  if (!stream.ReadVarint32(&result))
    throw EncodingError("Failed to decode size");
  return result;
}

}  // namespace decoder
}  // namespace krpc
