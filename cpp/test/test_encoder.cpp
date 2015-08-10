#include <gtest/gtest.h>
#include <gmock/gmock.h>
#include <krpc/encoder.hpp>
#include <krpc/platform.hpp>
#include <krpc/KRPC.pb.h>

TEST(test_encoder, test_rpc_hello_message) {
  std::string message(krpc::Encoder::RPC_HELLO_MESSAGE, krpc::Encoder::RPC_HELLO_MESSAGE_LENGTH);
  EXPECT_EQ(12, message.size());
  EXPECT_EQ("48454c4c4f2d525043000000", krpc::platform::hexlify(message));
}

TEST(test_encoder, test_stream_hello_message) {
  std::string message(krpc::Encoder::STREAM_HELLO_MESSAGE, krpc::Encoder::STREAM_HELLO_MESSAGE_LENGTH);
  EXPECT_EQ(12, message.size());
  EXPECT_EQ("48454c4c4f2d53545245414d", krpc::platform::hexlify(message));
}

TEST(test_encoder, test_client_name) {
  std::string message = krpc::Encoder::client_name("foo");
  EXPECT_EQ(32, message.size());
  EXPECT_EQ("666f6f" + std::string(29*2, '0'), krpc::platform::hexlify(message));
}

TEST(test_encoder, test_empty_client_name) {
  std::string message = krpc::Encoder::client_name("");
  EXPECT_EQ(32, message.size());
  EXPECT_EQ(std::string(32*2, '0'), krpc::platform::hexlify(message));
}

TEST(test_encoder, test_long_client_name) {
  std::string message = krpc::Encoder::client_name(std::string(33, 'a'));
  EXPECT_EQ(32, message.size());
  std::string expected;
  for (int i = 0; i < 32; i++)
    expected += "61";
  EXPECT_EQ(expected, krpc::platform::hexlify(message));
}

TEST(test_encoder, test_encode_message) {
  krpc::Request request;
  request.set_service("ServiceName");
  request.set_procedure("ProcedureName");
  //data = krpc::Encoder::encode(request, self.types.as_type('KRPC.Request'));
  std::string data;
  request.SerializeToString(&data);
  std::string expected = "0a0b536572766963654e616d65120d50726f6365647572654e616d65";
  EXPECT_EQ(expected, krpc::platform::hexlify(data));
}

TEST(test_encoder, test_encode_value) {
  std::string data = krpc::Encoder::encode((unsigned int)300);
  EXPECT_EQ("ac02", krpc::platform::hexlify(data));
}

TEST(test_encoder, test_encode_string) {
  std::string data = krpc::Encoder::encode("foo");
  EXPECT_EQ("03666f6f", krpc::platform::hexlify(data));
}

TEST(test_encoder, test_encode_unicode_string) {
  krpc::platform::unhexlify("6a");
  std::string in = krpc::platform::unhexlify("e284a2");
  std::string data = krpc::Encoder::encode(in);
  EXPECT_EQ("03e284a2", krpc::platform::hexlify(data));
}

TEST(test_encoder, test_encode_message_delimited) {
  krpc::Request request;
  request.set_service("ServiceName");
  request.set_procedure("ProcedureName");
  std::string data = krpc::Encoder::encode_delimited(request);
  std::string expected = "1c0a0b536572766963654e616d65120d50726f6365647572654e616d65";
  EXPECT_EQ(expected, krpc::platform::hexlify(data));
}

TEST(test_encoder, test_encode_value_delimited) {
  std::string data = krpc::Encoder::encode_delimited(300);
  EXPECT_EQ("02ac02", krpc::platform::hexlify(data));
}

TEST(test_encoder, test_encode_class) {
  //typ = self.types.as_type('Class(ServiceName.ClassName)')
  //class_type = typ.python_type
  //self.assertTrue(issubclass(class_type, ClassBase))
  //value = class_type(300)
  //self.assertEqual(300, value._object_id)
  //data = Encoder.encode(value, typ)
  //self.assertEqual('ac02', hexlify(data))
}

TEST(test_encoder, test_encode_class_none) {
  //typ = self.types.as_type('Class(ServiceName.ClassName)')
  //value = None
  //data = Encoder.encode(value, typ)
  //self.assertEqual('00', hexlify(data))
}
