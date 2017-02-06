#include <gtest/gtest.h>

#include <string>

#include <krpc/client.hpp>
#include <krpc/encoder.hpp>
#include <krpc/platform.hpp>

#include "services/test_service.hpp"

TEST(test_encoder, test_rpc_hello_message) {
  std::string message(krpc::encoder::RPC_HELLO_MESSAGE,
                      krpc::encoder::RPC_HELLO_MESSAGE_LENGTH);
  ASSERT_EQ(12, message.size());
  ASSERT_EQ("48454c4c4f2d525043000000", krpc::platform::hexlify(message));
}

TEST(test_encoder, test_stream_hello_message) {
  std::string message(krpc::encoder::STREAM_HELLO_MESSAGE,
                      krpc::encoder::STREAM_HELLO_MESSAGE_LENGTH);
  ASSERT_EQ(12, message.size());
  ASSERT_EQ("48454c4c4f2d53545245414d", krpc::platform::hexlify(message));
}

TEST(test_encoder, test_client_name) {
  std::string message = krpc::encoder::client_name("foo");
  ASSERT_EQ(32, message.size());
  ASSERT_EQ("666f6f" + std::string(29*2, '0'), krpc::platform::hexlify(message));
}

TEST(test_encoder, test_empty_client_name) {
  std::string message = krpc::encoder::client_name("");
  ASSERT_EQ(32, message.size());
  ASSERT_EQ(std::string(32*2, '0'), krpc::platform::hexlify(message));
}

TEST(test_encoder, test_long_client_name) {
  std::string message = krpc::encoder::client_name(std::string(33, 'a'));
  ASSERT_EQ(32, message.size());
  std::string expected;
  for (int i = 0; i < 32; i++)
    expected += "61";
  ASSERT_EQ(expected, krpc::platform::hexlify(message));
}

TEST(test_encoder, test_encode_message) {
  krpc::schema::Request request;
  request.set_service("ServiceName");
  request.set_procedure("ProcedureName");
  std::string data = krpc::encoder::encode(request);
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

TEST(test_encoder, test_encode_message_delimited) {
  krpc::schema::Request request;
  request.set_service("ServiceName");
  request.set_procedure("ProcedureName");
  std::string data = krpc::encoder::encode_delimited(request);
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
