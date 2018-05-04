#include <gtest/gtest-message.h>
#include <gtest/gtest-test-part.h>

#include <krpc_cnano/utils.h>

#include "gtest/gtest.h"

static void test_float32_to_float64(uint32_t float32, uint64_t float64) {
  uint64_t result;
  krpc_float32_to_float64(&float32, &result);
  ASSERT_EQ(result, float64);
}

static void test_float64_to_float32(uint64_t float64, uint32_t float32) {
  uint32_t result;
  krpc_float64_to_float32(&float64, &result);
  if (result != float32)
    std::cout << std::hex << result << std::endl;
  ASSERT_EQ(result, float32);
}

TEST(test_utils, test_float32_to_float64) {
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

TEST(test_utils, test_float64_to_float32) {
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
