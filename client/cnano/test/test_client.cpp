#include <gtest/gtest.h>
#include <krpc_cnano.h>
#include <krpc_cnano/functions.h>
#include <krpc_cnano/services/krpc.h>
#include <krpc_cnano/utils.h>

#include <cstdint>
#include <cstdlib>
#include <iostream>

#include "services/test_service.h"

// The port the test harness passes in, or the server's default when the binary is run directly
static uint16_t server_port() {
  const char* port = std::getenv("KRPC_TEST_TCPIP_PORT");
  return port == nullptr ? 50000 : static_cast<uint16_t>(std::strtol(port, nullptr, 10));
}

class test_client : public ::testing::Test {
 public:
  test_client() {
    if (KRPC_OK != krpc_open_tcp(&conn, "127.0.0.1", server_port())) exit(1);
    if (KRPC_OK != krpc_connect(conn, "TestClientCNano")) exit(1);
  }

  ~test_client() override {
    if (KRPC_OK != krpc_disconnect(conn)) exit(1);
  }

  krpc_connection_t conn;
};

TEST_F(test_client, test_optional_return_value) {
  ASSERT_EQ(KRPC_OK, krpc_KRPC_GetStatus(conn, nullptr));
}

// The ids in the generated macros name the server's types
static void expect_type(krpc_connection_t conn, krpc_KRPC_Type_t type, const char* name) {
  char* value = nullptr;
  ASSERT_EQ(KRPC_OK, krpc_KRPC_Type_Service(conn, &value, type));
  EXPECT_STREQ("TestService", value);
  krpc_free(value);
  value = nullptr;
  ASSERT_EQ(KRPC_OK, krpc_KRPC_Type_Name(conn, &value, type));
  EXPECT_STREQ(name, value);
  krpc_free(value);
}

TEST_F(test_client, test_type_ids) {
  krpc_KRPC_Type_t type;
  ASSERT_EQ(KRPC_OK, krpc_KRPC_Type_ClassTypeById(conn, &type, KRPC_CLASS_TESTSERVICE_TESTCLASS));
  expect_type(conn, type, "TestClass");
  ASSERT_EQ(KRPC_OK,
            krpc_KRPC_Type_EnumerationTypeById(conn, &type, KRPC_ENUMERATION_TESTSERVICE_TESTENUM));
  expect_type(conn, type, "TestEnum");
  ASSERT_EQ(KRPC_OK,
            krpc_KRPC_Type_StructTypeById(conn, &type, KRPC_STRUCT_TESTSERVICE_TESTSTRUCT));
  expect_type(conn, type, "TestStruct");
}

// The field positions in the generated macros match the server's order of the fields
class test_field_positions : public test_client {
 public:
  test_field_positions() {
    krpc_KRPC_Type_t type;
    EXPECT_EQ(KRPC_OK,
              krpc_KRPC_Type_StructTypeById(conn, &type, KRPC_STRUCT_TESTSERVICE_TESTSTRUCT));
    krpc_KRPC_Expression_t list_items[1];
    EXPECT_EQ(KRPC_OK, krpc_KRPC_Expression_ConstantInt(conn, &list_items[0], 7));
    krpc_list_object_t list_values = {1, list_items};
    krpc_KRPC_Expression_t fields[4];
    EXPECT_EQ(KRPC_OK, krpc_KRPC_Expression_ConstantInt(conn, &fields[0], 42));
    EXPECT_EQ(KRPC_OK, krpc_KRPC_Expression_ConstantString(conn, &fields[1], "foo"));
    EXPECT_EQ(KRPC_OK, krpc_KRPC_Expression_ConstantEnumById(conn, &fields[2],
                                                             KRPC_ENUMERATION_TESTSERVICE_TESTENUM,
                                                             KRPC_TESTSERVICE_TESTENUM_VALUEC));
    EXPECT_EQ(KRPC_OK, krpc_KRPC_Expression_CreateList(conn, &fields[3], &list_values));
    krpc_list_object_t field_values = {4, fields};
    EXPECT_EQ(KRPC_OK, krpc_KRPC_Expression_CreateStruct(conn, &value, type, &field_values));
  }

  // Runs a function that reads the field at the given position, and returns its result
  pb_istream_t field(int32_t index) {
    krpc_KRPC_Expression_t expr;
    EXPECT_EQ(KRPC_OK, krpc_KRPC_Expression_GetFieldByIndex(conn, &expr, value, index));
    KRPC_FREE_BYTES(result);
    result = KRPC_BYTES_NULL;
    EXPECT_EQ(KRPC_OK, krpc_run_function(conn, &result, nullptr, expr, nullptr));
    return pb_istream_from_buffer(result.data, result.size);
  }

  ~test_field_positions() override { KRPC_FREE_BYTES(result); }

  krpc_KRPC_Expression_t value;
  krpc_bytes_t result = KRPC_BYTES_NULL;
};

TEST_F(test_field_positions, test_field_positions) {
  pb_istream_t stream = field(KRPC_FIELD_TESTSERVICE_TESTSTRUCT_INTFIELD);
  int32_t int_value;
  ASSERT_EQ(KRPC_OK, krpc_decode_int32(&stream, &int_value));
  EXPECT_EQ(42, int_value);

  stream = field(KRPC_FIELD_TESTSERVICE_TESTSTRUCT_STRINGFIELD);
  char* string_value = nullptr;
  ASSERT_EQ(KRPC_OK, krpc_decode_string(&stream, &string_value));
  EXPECT_STREQ("foo", string_value);
  krpc_free(string_value);

  stream = field(KRPC_FIELD_TESTSERVICE_TESTSTRUCT_ENUMFIELD);
  krpc_enum_t enum_value;
  ASSERT_EQ(KRPC_OK, krpc_decode_enum(&stream, &enum_value));
  EXPECT_EQ(KRPC_TESTSERVICE_TESTENUM_VALUEC, enum_value);

  stream = field(KRPC_FIELD_TESTSERVICE_TESTSTRUCT_LISTFIELD);
  krpc_list_int32_t list_value = KRPC_NULL_LIST;
  ASSERT_EQ(KRPC_OK, krpc_decode_list_int32(&stream, &list_value));
  ASSERT_EQ(1u, list_value.size);
  EXPECT_EQ(7, list_value.items[0]);
  KRPC_FREE_LIST(list_value);
}

// The float conversions the AVR build encodes and decodes with
static void test_float32_to_float64(uint32_t float32, uint64_t float64) {
  uint64_t result;
  krpc_float32_to_float64(&float32, &result);
  ASSERT_EQ(result, float64);
}

static void test_float64_to_float32(uint64_t float64, uint32_t float32) {
  uint32_t result;
  krpc_float64_to_float32(&float64, &result);
  if (result != float32) std::cout << std::hex << result << std::endl;
  ASSERT_EQ(result, float32);
}

TEST(test_float_conversion, test_float32_to_float64) {
  test_float32_to_float64(0x3f800000, 0x3ff0000000000000);  // 1
  test_float32_to_float64(0x40000000, 0x4000000000000000);  // 2
  test_float32_to_float64(0xc0000000, 0xc000000000000000);  // -2
  test_float32_to_float64(0x40490fdb, 0x400921fb60000000);  // pi
  test_float32_to_float64(0x3eaaaaab, 0x3fd5555560000000);  // 1/3
  test_float32_to_float64(0x7f7fffff, 0x47efffffe0000000);  // max value
  test_float32_to_float64(0x00800000, 0x3810000000000000);  // min value
  test_float32_to_float64(0x80800000, 0xb810000000000000);  // -min value
  test_float32_to_float64(0xff7fffff, 0xc7efffffe0000000);  // -max value
  test_float32_to_float64(0x00000000, 0x0000000000000000);  // 0
  test_float32_to_float64(0x80000000, 0x8000000000000000);  // -0
  test_float32_to_float64(0x7f800000, 0x7ff0000000000000);  // +infinity
  test_float32_to_float64(0xff800000, 0xfff0000000000000);  // -infinity
  test_float32_to_float64(0x7fc00000, 0x7ff8000000000000);  // quiet NaN
  test_float32_to_float64(0x7fa00000, 0x7ff4000000000000);  // signalling NaN
}

TEST(test_float_conversion, test_float64_to_float32) {
  test_float64_to_float32(0x3ff0000000000000, 0x3f800000);  // 1
  test_float64_to_float32(0x3ff0000000000001, 0x3f800000);  // smallest number > 1 -> 1
  test_float64_to_float32(0x4000000000000000, 0x40000000);  // 2
  test_float64_to_float32(0xc000000000000000, 0xc0000000);  // -2
  test_float64_to_float32(0x400921fb54442d18, 0x40490fda);  // pi
  test_float64_to_float32(0x3fd5555555555555, 0x3eaaaaaa);  // 1/3
  test_float64_to_float32(0x7fefffffffffffff, 0x7f800000);  // max value -> inf
  test_float64_to_float32(0x0010000000000000, 0x00000000);  // min value -> 0
  test_float64_to_float32(0x8010000000000000, 0x80000000);  // -min value -> -0
  test_float64_to_float32(0xffefffffffffffff, 0xff800000);  // -max value -> -inf
  test_float64_to_float32(0x0000000000000000, 0x00000000);  // 0
  test_float64_to_float32(0x8000000000000000, 0x80000000);  // -0
  test_float64_to_float32(0x7ff0000000000000, 0x7f800000);  // +infinity
  test_float64_to_float32(0xfff0000000000000, 0xff800000);  // -infinity
  test_float64_to_float32(0x7ff8000000000000, 0x7fc00000);  // quiet NaN
  test_float64_to_float32(0x7ff4000000000000, 0x7fa00000);  // signalling NaN
}
