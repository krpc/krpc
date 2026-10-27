// The client's encoder and decoder, for each codec type the driver names.
#pragma once

#include <cstdint>
#include <functional>
#include <map>
#include <stdexcept>
#include <string>

#include "dispatch_values.hpp"
#include "test_service_dispatch.hpp"

namespace krpc_conformance {

struct CodecEntry {
  std::function<std::string(const Value&)> encode;
  std::function<Value(const std::string&)> decode;
};

/** The codec of a tag's C type, given the client functions that encode and decode it */
template <typename Tag, typename Encode, typename Decode>
CodecEntry codec_entry(Encode encode, Decode decode) {
  return {[encode](const Value& value) {
            auto x = Tag::arg(value);
            pb_ostream_t sizing = PB_OSTREAM_SIZING;
            check(encode(&sizing, x));
            std::string buffer(sizing.bytes_written, '\0');
            pb_ostream_t stream =
                pb_ostream_from_buffer(reinterpret_cast<pb_byte_t*>(buffer.data()), buffer.size());
            check(encode(&stream, x));
            return buffer;
          },
          [decode](const std::string& data) {
            typename Tag::type x{};
            pb_istream_t stream = pb_istream_from_buffer(
                reinterpret_cast<const pb_byte_t*>(data.data()), data.size());
            check(decode(&stream, &x));
            return Tag::result(x);
          }};
}

// The client functions for a type passed by value, and for one passed by pointer
#define KRPC_CONFORMANCE_BY_VALUE(name)                                              \
  [](pb_ostream_t* stream, const auto& x) { return krpc_encode_##name(stream, x); }, \
      [](pb_istream_t* stream, auto* x) { return krpc_decode_##name(stream, x); }
#define KRPC_CONFORMANCE_BY_POINTER(name)                                             \
  [](pb_ostream_t* stream, const auto& x) { return krpc_encode_##name(stream, &x); }, \
      [](pb_istream_t* stream, auto* x) { return krpc_decode_##name(stream, x); }

inline const CodecEntry& codec(const std::string& type) {
  using NullableObject = Nullable<krpc_nullable_object_t, Object>;
  using ListOfNullableObjects = List<krpc_list_nullable_object_t, NullableObject>;
  static const std::map<std::string, CodecEntry> entries = {
      {"double", codec_entry<Double>(KRPC_CONFORMANCE_BY_VALUE(double))},
      {"float", codec_entry<Float>(KRPC_CONFORMANCE_BY_VALUE(float))},
      {"sint32", codec_entry<Sint32>(KRPC_CONFORMANCE_BY_VALUE(int32))},
      {"sint64", codec_entry<Sint64>(KRPC_CONFORMANCE_BY_VALUE(int64))},
      {"uint32", codec_entry<Uint32>(KRPC_CONFORMANCE_BY_VALUE(uint32))},
      {"uint64", codec_entry<Uint64>(KRPC_CONFORMANCE_BY_VALUE(uint64))},
      {"bool", codec_entry<Bool>(KRPC_CONFORMANCE_BY_VALUE(bool))},
      {"string", codec_entry<String>(KRPC_CONFORMANCE_BY_VALUE(string))},
      {"bytes", codec_entry<Bytes>(KRPC_CONFORMANCE_BY_VALUE(bytes))},
      {"TestEnum", codec_entry<Enum<krpc_enum_t>>(KRPC_CONFORMANCE_BY_VALUE(enum))},
      {"TestClass", codec_entry<Object>(KRPC_CONFORMANCE_BY_VALUE(object))},
      {"list(sint32)",
       codec_entry<List<krpc_list_int32_t, Sint32>>(KRPC_CONFORMANCE_BY_POINTER(list_int32))},
      {"set(sint32)",
       codec_entry<Set<krpc_set_int32_t, Sint32>>(KRPC_CONFORMANCE_BY_POINTER(set_int32))},
      {"dictionary(string,sint32)",
       codec_entry<Dictionary<krpc_dictionary_string_int32_t, String, Sint32>>(
           KRPC_CONFORMANCE_BY_POINTER(dictionary_string_int32))},
      {"tuple(sint32,bool)",
       codec_entry<tuple_int32_bool>(KRPC_CONFORMANCE_BY_POINTER(tuple_int32_bool))},
      {"list(list(nullable TestClass))",
       codec_entry<List<krpc_list_list_nullable_object_t, ListOfNullableObjects>>(
           KRPC_CONFORMANCE_BY_POINTER(list_list_nullable_object))},
      {"list(nullable sint32)",
       codec_entry<List<krpc_list_nullable_int32_t, Nullable<krpc_nullable_int32_t, Sint32>>>(
           KRPC_CONFORMANCE_BY_POINTER(list_nullable_int32))},
      {"dictionary(string,nullable TestClass)",
       codec_entry<Dictionary<krpc_dictionary_string_nullable_object_t, String, NullableObject>>(
           KRPC_CONFORMANCE_BY_POINTER(dictionary_string_nullable_object))},
      {"tuple(sint32,nullable TestClass)",
       codec_entry<tuple_int32_nullable_object>(
           KRPC_CONFORMANCE_BY_POINTER(tuple_int32_nullable_object))},
      {"list(nullable TestClass)",
       codec_entry<ListOfNullableObjects>(KRPC_CONFORMANCE_BY_POINTER(list_nullable_object))},
      {"tuple(sint32,sint64)",
       codec_entry<tuple_int32_int64>(KRPC_CONFORMANCE_BY_POINTER(tuple_int32_int64))},
      {"list(string)", codec_entry<List<krpc_list_string_t, NotNull<String>>>(
                           KRPC_CONFORMANCE_BY_POINTER(list_string))},
      {"TestStruct",
       codec_entry<TestService_TestStruct>(KRPC_CONFORMANCE_BY_POINTER(TestService_TestStruct))},
      {"TestNullableStruct", codec_entry<TestService_TestNullableStruct>(
                                 KRPC_CONFORMANCE_BY_POINTER(TestService_TestNullableStruct))},
  };
  auto entry = entries.find(type);
  if (entry == entries.end()) throw Unsupported("The testee has no codec type " + type);
  return entry->second;
}

#undef KRPC_CONFORMANCE_BY_VALUE
#undef KRPC_CONFORMANCE_BY_POINTER

}  // namespace krpc_conformance
