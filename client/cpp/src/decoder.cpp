#include "krpc/decoder.hpp"

#include <google/protobuf/io/coded_stream.h>
#include <google/protobuf/wire_format_lite.h>

#include <string>
#include <utility>

#include "krpc/platform.hpp"

namespace pb = google::protobuf;

namespace krpc {
namespace decoder {

std::string guid(const std::string& data) {
  if (data.size() != 16)
    throw DecodeFailed("GUID is not 16 characters");
  return
    platform::hexlify(std::string(data.rbegin() + 12, data.rend()))        + "-" +
    platform::hexlify(std::string(data.rbegin() + 10, data.rbegin() + 12)) + "-" +
    platform::hexlify(std::string(data.rbegin() + 8,  data.rbegin() + 10)) + "-" +
    platform::hexlify(std::string(data.begin()  + 8,  data.begin()  + 10)) + "-" +
    platform::hexlify(std::string(data.begin()  + 10, data.end()));
}

void decode(float& value, const std::string& data, Client* client) {
  pb::io::CodedInputStream stream((pb::uint8*)(&data[0]), data.size());
  pb::uint32 value2;
  if (!stream.ReadLittleEndian32(&value2))
    throw DecodeFailed("Failed to decode float");
  value = pb::internal::WireFormatLite::DecodeFloat(value2);
}

void decode(double& value, const std::string& data, Client* client) {
  pb::io::CodedInputStream stream((pb::uint8*)&data[0], data.size());
  pb::uint64 value2 = 0;
  if (!stream.ReadLittleEndian64(&value2))
    throw DecodeFailed("Failed to decode double");
  value = pb::internal::WireFormatLite::DecodeDouble(value2);
}

void decode(pb::int32& value, const std::string& data, Client* client) {
  pb::io::CodedInputStream stream((pb::uint8*)&data[0], data.size());
  pb::uint32 value2 = 0;
  if (!stream.ReadVarint32(&value2))
    throw DecodeFailed("Failed to decode int32");
  value = static_cast<pb::int32>(value2);
}

void decode(pb::int64& value, const std::string& data, Client* client) {
  pb::io::CodedInputStream stream((pb::uint8*)&data[0], data.size());
  pb::uint64 value2 = 0;
  if (!stream.ReadVarint64(&value2))
    throw DecodeFailed("Failed to decode int64");
  value = static_cast<pb::int64>(value2);
}

void decode(pb::uint32& value, const std::string& data, Client* client) {
  pb::io::CodedInputStream stream((pb::uint8*)&data[0], data.size());
  if (!stream.ReadVarint32(&value))
    throw DecodeFailed("Failed to decode uint32");
}

void decode(bool& value, const std::string& data, Client* client) {
  pb::io::CodedInputStream stream((pb::uint8*)&data[0], data.size());
  pb::uint64 value2 = 0;
  if (!stream.ReadVarint64(&value2))
    throw DecodeFailed("Failed to decode bool");
  value = (value2 != 0);
}

void decode(pb::uint64& value, const std::string& data, Client* client) {
  pb::io::CodedInputStream stream((pb::uint8*)&data[0], data.size());
  if (!stream.ReadVarint64(&value))
    throw DecodeFailed("Failed to decode uint64");
}

void decode(std::string& value, const std::string& data, Client* client) {
  pb::io::CodedInputStream stream((pb::uint8*)&data[0], data.size());
  pb::uint64 length;
  if (!stream.ReadVarint64(&length))
    throw DecodeFailed("Failed to decode string (length)");
  if (!stream.ReadString(&value, static_cast<int>(length)))
    throw DecodeFailed("Failed to decode string");
}

void decode(google::protobuf::Message& message, const std::string& data, Client* client) {
  if (!message.ParseFromString(data))
    throw DecodeFailed("Failed to decode message");
}

std::pair<pb::uint32, pb::uint32> decode_size_and_position(const std::string& data) {
  std::pair<pb::uint32, pb::uint32> result;
  pb::io::CodedInputStream stream((pb::uint8*)&data[0], data.size());
  if (!stream.ReadVarint32(&(result.first)))
    throw DecodeFailed("Failed to decode size");
  result.second = stream.CurrentPosition();
  return result;
}

}  // namespace decoder
}  // namespace krpc
