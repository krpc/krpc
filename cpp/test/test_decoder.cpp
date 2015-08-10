#include <gtest/gtest.h>
#include <gmock/gmock.h>
#include <krpc/decoder.hpp>
#include <krpc/platform.hpp>
#include <krpc/KRPC.pb.h>

namespace pb = google::protobuf;

TEST(test_decoder, test_decode_message) {
  std::string message = "0a0b536572766963654e616d65120d50726f6365647572654e616d65";
  krpc::Request request;
  krpc::Decoder::decode(request, krpc::platform::unhexlify(message));
  EXPECT_EQ("ServiceName", request.service());
  EXPECT_EQ("ProcedureName", request.procedure());
}

TEST(test_decoder, test_decode_value) {
  unsigned int value;
  krpc::Decoder::decode(value, krpc::platform::unhexlify("ac02"));
  EXPECT_EQ(300, value);
}

TEST(test_decoder, test_decode_unicode_string) {
  std::string value;
  krpc::Decoder::decode(value, krpc::platform::unhexlify("03e284a2"));
  EXPECT_EQ(krpc::platform::unhexlify("e284a2"), value);
}

TEST(test_decoder, test_decode_size_and_position) {
  std::string message = "1c";
  std::pair<pb::uint32,pb::uint32> result = krpc::Decoder::decode_size_and_position(krpc::platform::unhexlify(message));
  EXPECT_EQ(28, result.first);
  EXPECT_EQ(1, result.second);
}

TEST(test_decoder, test_decode_message_delimited) {
  std::string message = "1c0a0b536572766963654e616d65120d50726f6365647572654e616d65";
  krpc::Request request;
  krpc::Decoder::decode_delimited(request, krpc::platform::unhexlify(message));
  EXPECT_EQ("ServiceName", request.service());
  EXPECT_EQ("ProcedureName", request.procedure());
}

TEST(test_decoder, test_decode_value_delimited) {
  pb::uint32 value;
  krpc::Decoder::decode_delimited(value, krpc::platform::unhexlify("02ac02"));
  EXPECT_EQ(300, value);
}

TEST(test_decoder, test_decode_class) {
  //typ = self.types.as_type('Class(ServiceName.ClassName)')
  //value = Decoder.decode(unhexlify('ac02'), typ)
  //self.assertTrue(isinstance(value, typ.python_type))
  //self.assertEqual(300, value._object_id)
}

TEST(test_decoder, test_decode_class_none) {
  //typ = self.types.as_type('Class(ServiceName.ClassName)')
  //value = Decoder.decode(unhexlify('00'), typ)
  //self.assertIsNone(value)
}

TEST(test_decoder, test_guid) {
  EXPECT_EQ(
    "6f271b39-00dd-4de4-9732-f0d3a68838df",
    krpc::Decoder::guid(krpc::platform::unhexlify("391b276fdd00e44d9732f0d3a68838df")));
}
