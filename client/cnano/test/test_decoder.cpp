#include <gtest/gtest-message.h>
#include <gtest/gtest-test-part.h>

#include <krpc_cnano.h>
#include <krpc_cnano/encoder.h>
#include <krpc_cnano/services/krpc.h>

#include <string>

#include "gtest/gtest.h"

#include "services/test_service.h"
#include "testing_tools.hpp"

static pb_istream_t create_stream(uint8_t * buffer, std::string data) {
  unhexlify(buffer, data);
  return pb_istream_from_buffer(buffer, data.size()/2);
}

TEST(test_decoder, test_decode_value) {
  uint8_t data[2];
  pb_istream_t stream = create_stream(data, "ac02");
  unsigned int value = 0;
  ASSERT_EQ(KRPC_OK, krpc_decode_uint32(&stream, &value));
  ASSERT_EQ(300, value);
}

TEST(test_decoder, test_decode_unicode_string) {
  uint8_t data[4];
  pb_istream_t stream = create_stream(data, "03e284a2");
  char * value = nullptr;
  ASSERT_EQ(KRPC_OK, krpc_decode_string(&stream, &value));
  ASSERT_STREQ("\xe2\x84\xa2", value);
  krpc_free(value);
}

TEST(test_decoder, test_decode_object) {
  uint8_t data[2];
  pb_istream_t stream = create_stream(data, "ac02");
  krpc_TestService_TestClass_t object = 0;
  ASSERT_EQ(KRPC_OK, krpc_decode_object(&stream, &object));
  ASSERT_EQ(300, object);
}

TEST(test_decoder, test_decode_object_none) {
  uint8_t data[1];
  pb_istream_t stream = create_stream(data, "00");
  krpc_TestService_TestClass_t object = 42;
  ASSERT_EQ(KRPC_OK, krpc_decode_object(&stream, &object));
  ASSERT_EQ(0, object);
}
