// The client's encoder and decoder, for each codec type the driver names.
#pragma once

#include <functional>
#include <map>
#include <stdexcept>
#include <string>

#include "dispatch_values.hpp"
#include "test_service_dispatch.hpp"

namespace krpc_conformance {

struct CodecEntry {
  std::function<std::string(krpc::Client&, const Value&)> encode;
  std::function<Value(krpc::Client&, const std::string&)> decode;
};

template <typename Tag>
CodecEntry codec_entry() {
  return {[](krpc::Client& client, const Value& value) {
            return krpc::encoder::encode(Tag::arg(client, value));
          },
          [](krpc::Client& client, const std::string& data) {
            typename Tag::type value{};
            krpc::decoder::decode(value, data, &client);
            return Tag::result(value);
          }};
}

inline const CodecEntry& codec(const std::string& type) {
  using TestClass = Object<krpc::services::TestService::TestClass>;
  static const std::map<std::string, CodecEntry> entries = {
      {"double", codec_entry<Double>()},
      {"float", codec_entry<Float>()},
      {"sint32", codec_entry<Sint32>()},
      {"sint64", codec_entry<Sint64>()},
      {"uint32", codec_entry<Uint32>()},
      {"uint64", codec_entry<Uint64>()},
      {"bool", codec_entry<Bool>()},
      {"string", codec_entry<String>()},
      {"bytes", codec_entry<Bytes>()},
      {"TestEnum", codec_entry<Enum<krpc::services::TestService::TestEnum>>()},
      {"TestClass", codec_entry<TestClass>()},
      {"list(sint32)", codec_entry<List<Sint32>>()},
      {"set(sint32)", codec_entry<Set<Sint32>>()},
      {"dictionary(string,sint32)", codec_entry<Dictionary<String, Sint32>>()},
      {"tuple(sint32,bool)", codec_entry<Tuple<Sint32, Bool>>()},
      {"list(list(nullable TestClass))", codec_entry<List<List<Optional<TestClass>>>>()},
      {"list(nullable sint32)", codec_entry<List<Optional<Sint32>>>()},
      {"list(nullable bool)", codec_entry<List<Optional<Bool>>>()},
      {"list(nullable bytes)", codec_entry<List<Optional<Bytes>>>()},
      {"dictionary(string,nullable TestClass)",
       codec_entry<Dictionary<String, Optional<TestClass>>>()},
      {"tuple(sint32,nullable TestClass)", codec_entry<Tuple<Sint32, Optional<TestClass>>>()},
      {"list(nullable TestClass)", codec_entry<List<Optional<TestClass>>>()},
      {"tuple(uint32)", codec_entry<Tuple<Uint32>>()},
      {"tuple(uint32,string,bool)", codec_entry<Tuple<Uint32, String, Bool>>()},
      {"tuple(sint32,sint64)", codec_entry<Tuple<Sint32, Sint64>>()},
      {"tuple(sint32,nullable string)", codec_entry<Tuple<Sint32, Optional<String>>>()},
      {"dictionary(string,nullable sint32)", codec_entry<Dictionary<String, Optional<Sint32>>>()},
      {"dictionary(string,list(uint32))", codec_entry<Dictionary<String, List<Uint32>>>()},
      {"list(nullable list(uint32))", codec_entry<List<Optional<List<Uint32>>>>()},
      {"list(nullable TestStruct)", codec_entry<List<Optional<TestService_TestStruct>>>()},
      {"list(string)", codec_entry<List<String>>()},
      {"TestStruct", codec_entry<TestService_TestStruct>()},
      {"TestNullableStruct", codec_entry<TestService_TestNullableStruct>()},
  };
  auto entry = entries.find(type);
  if (entry == entries.end()) throw Unsupported("The testee has no codec type " + type);
  return entry->second;
}

}  // namespace krpc_conformance
