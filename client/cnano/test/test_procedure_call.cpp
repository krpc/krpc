#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>

#include <vector>

#include "gtest/gtest.h"
#include "memory.h"

namespace {

using memory::allocations_left;
using memory::live_blocks;

// An encode callback that fails in the sizing pass
bool fail_to_size(pb_ostream_t* /*stream*/, const pb_field_t* /*field*/, void* const* /*arg*/) {
  return false;
}

// An encode callback that fails only when writing, once the buffer for the value is allocated
bool fail_to_write(pb_ostream_t* stream, const pb_field_t* field, void* const* /*arg*/) {
  if (stream->callback != nullptr) return false;
  return pb_encode_tag_for_field(stream, field) && pb_encode_varint(stream, 1);
}

class ProcedureCallTest : public ::testing::Test {
 protected:
  void SetUp() override {
    live_blocks = 0;
    allocations_left = -1;
  }

  krpc_schema_ProcedureCall call = krpc_schema_ProcedureCall_init_default;
};

TEST_F(ProcedureCallTest, EncodesIdsAndArguments) {
  const uint32_t value = 7;
  ASSERT_EQ(KRPC_OK, krpc_procedure_call(&call, 1, 2, 2));
  ASSERT_EQ(KRPC_OK, krpc_procedure_call_argument(&call, 0, &krpc_encode_callback_uint32, &value));
  ASSERT_EQ(KRPC_OK, krpc_procedure_call_null_argument(&call, 1));

  std::vector<pb_byte_t> buffer(32);
  pb_ostream_t stream = pb_ostream_from_buffer(buffer.data(), buffer.size());
  ASSERT_TRUE(pb_encode(&stream, krpc_schema_ProcedureCall_fields, &call));
  buffer.resize(stream.bytes_written);

  const std::vector<pb_byte_t> expected = {
      0x1a, 0x03, 0x12, 0x01, 0x07,        // Argument 0, with its value
      0x1a, 0x04, 0x08, 0x01, 0x18, 0x01,  // Argument 1, with is_null
      0x20, 0x01,                          // service_id
      0x28, 0x02,                          // procedure_id
  };
  EXPECT_EQ(expected, buffer);

  EXPECT_EQ(KRPC_OK, krpc_free_procedure_call(&call));
  EXPECT_EQ(0, live_blocks);
}

TEST_F(ProcedureCallTest, CallWithNoArguments) {
  ASSERT_EQ(KRPC_OK, krpc_procedure_call(&call, 1, 2, 0));
  EXPECT_EQ(KRPC_OK, krpc_free_procedure_call(&call));
  EXPECT_EQ(0, live_blocks);
}

TEST_F(ProcedureCallTest, PositionOutOfRange) {
  const uint32_t value = 7;
  ASSERT_EQ(KRPC_OK, krpc_procedure_call(&call, 1, 2, 1));
  EXPECT_EQ(KRPC_ERROR_ARGUMENT,
            krpc_procedure_call_argument(&call, 1, &krpc_encode_callback_uint32, &value));
  EXPECT_EQ(KRPC_ERROR_ARGUMENT, krpc_procedure_call_null_argument(&call, 1));
  EXPECT_EQ(KRPC_OK, krpc_free_procedure_call(&call));
  EXPECT_EQ(0, live_blocks);
}

TEST_F(ProcedureCallTest, ArgumentOfACallNotFilledIn) {
  const uint32_t value = 7;
  EXPECT_EQ(KRPC_ERROR_ARGUMENT,
            krpc_procedure_call_argument(&call, 0, &krpc_encode_callback_uint32, &value));
  EXPECT_EQ(KRPC_ERROR_ARGUMENT, krpc_procedure_call_null_argument(&call, 0));
}

TEST_F(ProcedureCallTest, EncodeFailsPartWay) {
  const uint32_t value = 7;
  ASSERT_EQ(KRPC_OK, krpc_procedure_call(&call, 1, 2, 3));
  ASSERT_EQ(KRPC_OK, krpc_procedure_call_argument(&call, 0, &krpc_encode_callback_uint32, &value));
  EXPECT_EQ(KRPC_ERROR_ENCODING, krpc_procedure_call_argument(&call, 1, &fail_to_size, nullptr));
  EXPECT_EQ(KRPC_ERROR_ENCODING, krpc_procedure_call_argument(&call, 2, &fail_to_write, nullptr));
  EXPECT_EQ(KRPC_OK, krpc_free_procedure_call(&call));
  EXPECT_EQ(0, live_blocks);
}

TEST_F(ProcedureCallTest, EncodingFailsWithAnArgumentNotFilledIn) {
  const uint32_t value = 7;
  ASSERT_EQ(KRPC_OK, krpc_procedure_call(&call, 1, 2, 2));
  ASSERT_EQ(KRPC_OK, krpc_procedure_call_argument(&call, 1, &krpc_encode_callback_uint32, &value));

  std::vector<pb_byte_t> buffer(32);
  pb_ostream_t stream = pb_ostream_from_buffer(buffer.data(), buffer.size());
  EXPECT_FALSE(pb_encode(&stream, krpc_schema_ProcedureCall_fields, &call));

  EXPECT_EQ(KRPC_OK, krpc_free_procedure_call(&call));
  EXPECT_EQ(0, live_blocks);
}

TEST_F(ProcedureCallTest, FillingInAnArgumentAgainReplacesIt) {
  const uint32_t first = 7;
  const uint32_t second = 8;
  ASSERT_EQ(KRPC_OK, krpc_procedure_call(&call, 1, 2, 1));
  ASSERT_EQ(KRPC_OK, krpc_procedure_call_argument(&call, 0, &krpc_encode_callback_uint32, &first));
  ASSERT_EQ(KRPC_OK, krpc_procedure_call_argument(&call, 0, &krpc_encode_callback_uint32, &second));

  std::vector<pb_byte_t> buffer(32);
  pb_ostream_t stream = pb_ostream_from_buffer(buffer.data(), buffer.size());
  ASSERT_TRUE(pb_encode(&stream, krpc_schema_ProcedureCall_fields, &call));
  buffer.resize(stream.bytes_written);

  const std::vector<pb_byte_t> expected = {
      0x1a, 0x03, 0x12, 0x01, 0x08,  // Argument 0, with the second value
      0x20, 0x01,                    // service_id
      0x28, 0x02,                    // procedure_id
  };
  EXPECT_EQ(expected, buffer);

  EXPECT_EQ(KRPC_OK, krpc_free_procedure_call(&call));
  EXPECT_EQ(0, live_blocks);
}

TEST_F(ProcedureCallTest, FailingToFillInAnArgumentAgainLeavesItUnfilled) {
  const uint32_t value = 7;
  ASSERT_EQ(KRPC_OK, krpc_procedure_call(&call, 1, 2, 1));
  ASSERT_EQ(KRPC_OK, krpc_procedure_call_argument(&call, 0, &krpc_encode_callback_uint32, &value));
  EXPECT_EQ(KRPC_ERROR_ENCODING, krpc_procedure_call_argument(&call, 0, &fail_to_size, nullptr));

  std::vector<pb_byte_t> buffer(32);
  pb_ostream_t stream = pb_ostream_from_buffer(buffer.data(), buffer.size());
  EXPECT_FALSE(pb_encode(&stream, krpc_schema_ProcedureCall_fields, &call));

  EXPECT_EQ(KRPC_OK, krpc_free_procedure_call(&call));
  EXPECT_EQ(0, live_blocks);
}

TEST_F(ProcedureCallTest, NullingAnArgumentFreesItsValue) {
  const uint32_t value = 7;
  ASSERT_EQ(KRPC_OK, krpc_procedure_call(&call, 1, 2, 1));
  ASSERT_EQ(KRPC_OK, krpc_procedure_call_argument(&call, 0, &krpc_encode_callback_uint32, &value));
  ASSERT_EQ(KRPC_OK, krpc_procedure_call_null_argument(&call, 0));
  // The call and its arguments array remain
  EXPECT_EQ(2, live_blocks);

  std::vector<pb_byte_t> buffer(32);
  pb_ostream_t stream = pb_ostream_from_buffer(buffer.data(), buffer.size());
  ASSERT_TRUE(pb_encode(&stream, krpc_schema_ProcedureCall_fields, &call));
  buffer.resize(stream.bytes_written);

  const std::vector<pb_byte_t> expected = {
      0x1a, 0x02, 0x18, 0x01,  // Argument 0, with is_null
      0x20, 0x01,              // service_id
      0x28, 0x02,              // procedure_id
  };
  EXPECT_EQ(expected, buffer);

  EXPECT_EQ(KRPC_OK, krpc_free_procedure_call(&call));
  EXPECT_EQ(0, live_blocks);
}

// A generated _call function fails with each of its allocations in turn: the call, its arguments
// and the value of each of the two arguments
TEST_F(ProcedureCallTest, GeneratedCallOutOfMemory) {
  const int allocations = 4;
  for (int i = 0; i < allocations; i++) {
    krpc_schema_ProcedureCall failed = krpc_schema_ProcedureCall_init_default;
    allocations_left = i;
    EXPECT_EQ(KRPC_ERROR_NO_MEMORY, krpc_KRPC_Expression_Add_call(&failed, 1, 2)) << i;
    allocations_left = -1;
    EXPECT_EQ(KRPC_OK, krpc_free_procedure_call(&failed)) << i;
    EXPECT_EQ(0, live_blocks) << i;
  }

  allocations_left = allocations;
  EXPECT_EQ(KRPC_OK, krpc_KRPC_Expression_Add_call(&call, 1, 2));
  allocations_left = -1;
  EXPECT_EQ(KRPC_OK, krpc_free_procedure_call(&call));
  EXPECT_EQ(0, live_blocks);
}

TEST_F(ProcedureCallTest, FreeACallNotFilledIn) {
  EXPECT_EQ(KRPC_OK, krpc_free_procedure_call(&call));
}

TEST_F(ProcedureCallTest, FreeTwice) {
  ASSERT_EQ(KRPC_OK, krpc_procedure_call(&call, 1, 2, 1));
  EXPECT_EQ(KRPC_OK, krpc_free_procedure_call(&call));
  EXPECT_EQ(KRPC_OK, krpc_free_procedure_call(&call));
  EXPECT_EQ(0, live_blocks);
}

}  // namespace
