#include <gtest/gtest-message.h>
#include <gtest/gtest-test-part.h>

#include <string>

#include "gtest/gtest.h"

#include "krpc/encoder.hpp"
#include "krpc/krpc.pb.hpp"
#include "krpc/platform.hpp"

#include "services/test_service.hpp"

TEST(test_encoder, test_encode_message) {
  krpc::schema::ProcedureCall call;
  call.set_service("ServiceName");
  call.set_procedure("ProcedureName");
  std::string data = krpc::encoder::encode(call);
  std::string expected = "0a0b536572766963654e616d65120d50726f6365647572654e616d65";
  ASSERT_EQ(expected, krpc::platform::hexlify(data));
}

TEST(test_encoder, test_encode_value) {
  std::string data = krpc::encoder::encode((unsigned int)300);
  ASSERT_EQ("ac02", krpc::platform::hexlify(data));
}

TEST(test_encoder, test_encode_string) {
  std::string data = krpc::encoder::encode("foo");
  ASSERT_EQ("03666f6f", krpc::platform::hexlify(data));
}

TEST(test_encoder, test_encode_unicode_string) {
  krpc::platform::unhexlify("6a");
  std::string in = krpc::platform::unhexlify("e284a2");
  std::string data = krpc::encoder::encode(in);
  ASSERT_EQ("03e284a2", krpc::platform::hexlify(data));
}

TEST(test_encoder, test_encode_message_with_size) {
  krpc::schema::ProcedureCall call;
  call.set_service("ServiceName");
  call.set_procedure("ProcedureName");
  std::string data = krpc::encoder::encode_message_with_size(call);
  std::string expected = "1c0a0b536572766963654e616d65120d50726f6365647572654e616d65";
  ASSERT_EQ(expected, krpc::platform::hexlify(data));
}

TEST(test_encoder, test_encode_class) {
  krpc::services::TestService::TestClass value(nullptr, 300);
  std::string data = krpc::encoder::encode(value);
  ASSERT_EQ("ac02", krpc::platform::hexlify(data));
}

TEST(test_encoder, test_encode_class_none) {
  krpc::services::TestService::TestClass value;
  std::string data = krpc::encoder::encode(value);
  ASSERT_EQ("00", krpc::platform::hexlify(data));
}
