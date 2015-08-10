#include "krpc/decoder.hpp"
#include "krpc/platform.hpp"
#include <google/protobuf/io/coded_stream.h>

namespace pb = google::protobuf;

namespace krpc {

  const char Decoder::OK_MESSAGE[] = { 0x4F, 0x4B };
  const size_t Decoder::OK_MESSAGE_LENGTH = 2;
  const size_t Decoder::GUID_LENGTH = 16;

  std::string Decoder::guid(const std::string& data) {
    if (data.size() != 16)
      BOOST_THROW_EXCEPTION(DecodeFailed());
    return
      platform::hexlify(std::string(data.rbegin() + 12, data.rend()))        + "-" +
      platform::hexlify(std::string(data.rbegin() + 10, data.rbegin() + 12)) + "-" +
      platform::hexlify(std::string(data.rbegin() + 8,  data.rbegin() + 10)) + "-" +
      platform::hexlify(std::string(data.begin()  + 8,  data.begin()  + 10)) + "-" +
      platform::hexlify(std::string(data.begin()  + 10, data.end()));
  }

  void Decoder::decode(pb::uint32& value, const std::string& data) {
    pb::io::CodedInputStream stream((pb::uint8*)&data[0], data.size());
    if (!stream.ReadVarint32(&value))
      BOOST_THROW_EXCEPTION(DecodeFailed());
  }

  void Decoder::decode(std::string& value, const std::string& data) {
    pb::io::CodedInputStream stream((pb::uint8*)&data[0], data.size());
    pb::uint64 length;
    if (!stream.ReadVarint64(&length))
      BOOST_THROW_EXCEPTION(DecodeFailed());
    if (!stream.ReadString(&value, length))
      BOOST_THROW_EXCEPTION(DecodeFailed());
  }

  void Decoder::decode(google::protobuf::Message& message, const std::string& data) {
    if (!message.ParseFromString(data))
      BOOST_THROW_EXCEPTION(DecodeFailed());
  }

  void Decoder::decode_delimited(pb::uint32& value, const std::string& data) {
    pb::io::CodedInputStream stream((pb::uint8*)&data[0], data.size());
    pb::uint64 length;
    if (!stream.ReadVarint64(&length))
      BOOST_THROW_EXCEPTION(DecodeFailed());
    if (!stream.ReadVarint32(&value))
      BOOST_THROW_EXCEPTION(DecodeFailed());
    //TODO: check that length bytes were read to decode the delimited message
  }

  void Decoder::decode_delimited(pb::Message& message, const std::string& data) {
    pb::io::CodedInputStream stream((pb::uint8*)&data[0], data.size());
    pb::uint64 length;
    if (!stream.ReadVarint64(&length))
      BOOST_THROW_EXCEPTION(DecodeFailed());
    if (!message.ParseFromCodedStream(&stream))
      BOOST_THROW_EXCEPTION(DecodeFailed());
    //TODO: check that length bytes were read to decode the delimited message
  }

  std::pair<pb::uint32, pb::uint32> Decoder::decode_size_and_position(const std::string& data) {
    std::pair<pb::uint32, pb::uint32> result;
    pb::io::CodedInputStream stream((pb::uint8*)&data[0], data.size());
    if (!stream.ReadVarint32(&(result.first)))
      BOOST_THROW_EXCEPTION(DecodeFailed());
    result.second = stream.CurrentPosition();
    return result;
  }

}
