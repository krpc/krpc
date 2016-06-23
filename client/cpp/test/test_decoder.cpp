#include <gtest/gtest.h>

#include <string>
#include <utility>

#include <krpc/decoder.hpp>
#include <krpc/platform.hpp>

#include "services/test_service.hpp"

namespace pb = google::protobuf;

TEST(test_decoder, test_decode_message) {
  std::string message = "0a0b536572766963654e616d65120d50726f6365647572654e616d65";
  krpc::schema::Request request;
  krpc::decoder::decode(request, krpc::platform::unhexlify(message));
  ASSERT_EQ("ServiceName", request.service());
  ASSERT_EQ("ProcedureName", request.procedure());
}

TEST(test_decoder, test_decode_value) {
  unsigned int value;
  krpc::decoder::decode(value, krpc::platform::unhexlify("ac02"));
  ASSERT_EQ(300, value);
}

TEST(test_decoder, test_decode_unicode_string) {
  std::string value;
  krpc::decoder::decode(value, krpc::platform::unhexlify("03e284a2"));
  ASSERT_EQ(krpc::platform::unhexlify("e284a2"), value);
}

TEST(test_decoder, test_decode_size_and_position) {
  std::string message = "1c";
  std::pair<pb::uint32, pb::uint32> result =
    krpc::decoder::decode_size_and_position(krpc::platform::unhexlify(message));
  ASSERT_EQ(28, result.first);
  ASSERT_EQ(1, result.second);
}

TEST(test_decoder, test_decode_class) {
  krpc::services::TestService::TestClass object;
  krpc::decoder::decode(object, krpc::platform::unhexlify("ac02"));
  ASSERT_EQ(krpc::services::TestService::TestClass(nullptr, 300), object);
}

TEST(test_decoder, test_decode_class_none) {
  krpc::services::TestService::TestClass object;
  krpc::decoder::decode(object, krpc::platform::unhexlify("00"));
  ASSERT_EQ(krpc::services::TestService::TestClass(), object);
}

TEST(test_decoder, test_guid) {
  ASSERT_EQ(
    "6f271b39-00dd-4de4-9732-f0d3a68838df",
    krpc::decoder::guid(krpc::platform::unhexlify("391b276fdd00e44d9732f0d3a68838df")));
}
