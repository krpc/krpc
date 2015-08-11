#include <gtest/gtest.h>
#include <gmock/gmock.h>
#include <krpc/encoder.hpp>
#include <krpc/decoder.hpp>
#include <krpc/platform.hpp>

namespace pb = google::protobuf;

template<typename T> void test(T decoded, std::string encoded) {
  ASSERT_EQ(encoded, krpc::platform::hexlify(krpc::Encoder::encode(decoded)));
  T value = 0;
  krpc::Decoder::decode(value, krpc::platform::unhexlify(encoded));
  ASSERT_EQ(decoded, value);
}

void test_float(float decoded, std::string encoded) {
  ASSERT_EQ(encoded, krpc::platform::hexlify(krpc::Encoder::encode(decoded)));
  float value = 0;
  krpc::Decoder::decode(value, krpc::platform::unhexlify(encoded));
  if (!isnan(decoded))
    ASSERT_FLOAT_EQ(decoded, value);
  else
    ASSERT_TRUE(isnan(value));
}

void test_double(double decoded, std::string encoded) {
  ASSERT_EQ(encoded, krpc::platform::hexlify(krpc::Encoder::encode(decoded)));
  double value = 0;
  krpc::Decoder::decode(value, krpc::platform::unhexlify(encoded));
  if (!isnan(decoded))
    ASSERT_DOUBLE_EQ(decoded, value);
  else
    ASSERT_TRUE(isnan(value));
}

void test_string(std::string decoded, std::string encoded) {
  ASSERT_EQ(encoded, krpc::platform::hexlify(krpc::Encoder::encode(decoded)));
  std::string value;
  krpc::Decoder::decode(value, krpc::platform::unhexlify(encoded));
  ASSERT_EQ(decoded, value);
}

void test_bytes(std::string decoded, std::string encoded) {
  test_string(decoded, encoded);
}

template <typename T> void test_list(const std::vector<T>& decoded, std::string encoded) {
  ASSERT_EQ(encoded, krpc::platform::hexlify(krpc::Encoder::encode(decoded)));
  std::vector<T> value;
  krpc::Decoder::decode(value, krpc::platform::unhexlify(encoded));
  ASSERT_EQ(decoded.size(), value.size());
  for (unsigned int i = 0; i < decoded.size(); i++)
    ASSERT_EQ(decoded[i], value[i]);
}

TEST(test_encode_decode, test_double) {
  test_double(0.0, "0000000000000000");
  test_double(-1.0, "000000000000f0bf");
  test_double(3.14159265359, "ea2e4454fb210940");
  test_double(std::numeric_limits<double>::infinity(), "000000000000f07f");
  test_double(-std::numeric_limits<double>::infinity(), "000000000000f0ff");
  test_double(std::numeric_limits<double>::quiet_NaN(), "000000000000f87f");
  test_double(std::numeric_limits<double>::signaling_NaN(), "000000000000f47f");
}

TEST(test_encode_decode, test_float) {
  test_float(3.14159265359f, "db0f4940");
  test_float(-1.0f, "000080bf");
  test_float(0.0f, "00000000");
  test_float(std::numeric_limits<float>::infinity(), "0000807f");
  test_float(-std::numeric_limits<float>::infinity(), "000080ff");
  test_float(std::numeric_limits<float>::quiet_NaN(), "0000c07f");
  test_float(std::numeric_limits<float>::signaling_NaN(), "0000a07f");
}

TEST(test_encode_decode, test_int32) {
  test<pb::int32>(0, "00");
  test<pb::int32>(1, "01");
  test<pb::int32>(42, "2a");
  test<pb::int32>(300, "ac02");
  test<pb::int32>(-33, "dfffffffffffffffff01");
  test<pb::int32>(std::numeric_limits<pb::int32>::max(), "ffffffff07"); //should be ffffffffffffffff7f ?
  test<pb::int32>(std::numeric_limits<pb::int32>::min(), "80808080f8ffffffff01"); //should be 80808080808080808001 ?
}

TEST(test_encode_decode, test_int64) {
  test<pb::int64>(0, "00");
  test<pb::int64>(1, "01");
  test<pb::int64>(42, "2a");
  test<pb::int64>(300, "ac02");
  test<pb::int64>(1234567890000L, "d088ec8ff723");
  test<pb::int64>(-33, "dfffffffffffffffff01");
}

TEST(test_encode_decode, test_uint32) {
  test<pb::uint32>(0, "00");
  test<pb::uint32>(1, "01");
  test<pb::uint32>(42, "2a");
  test<pb::uint32>(300, "ac02");
  test<pb::uint32>(std::numeric_limits<pb::uint32>::max(), "ffffffff0f"); //should be ffffffffffffffff7f ?
}

TEST(test_encode_decode, test_uint64) {
  test<pb::uint64>(0, "00");
  test<pb::uint64>(1, "01");
  test<pb::uint64>(42, "2a");
  test<pb::uint64>(300, "ac02");
  test<pb::uint64>(1234567890000L, "d088ec8ff723");
}

TEST(test_encode_decode, test_bool) {
  test<bool>(true,  "01");
  test<bool>(false, "00");
}

TEST(test_encode_decode, test_string) {
  test_string("", "00");
  test_string("testing", "0774657374696e67");
  test_string("One small step for Kerbal-kind!", "1f4f6e6520736d616c6c207374657020666f72204b657262616c2d6b696e6421");
  test_string(krpc::platform::unhexlify("e284a2"), "03e284a2");
  test_string("Mystery Goo" + krpc::platform::unhexlify("e284a2") + " Containment Unit", "1f4d79737465727920476f6fe284a220436f6e7461696e6d656e7420556e6974");
}

TEST(test_encode_decode, test_bytes) {
  test_bytes("", "00");
  test_bytes(krpc::platform::unhexlify("bada55"), "03bada55");
  test_bytes(krpc::platform::unhexlify("deadbeef"), "04deadbeef");
}

TEST(test_encode_decode, test_list) {
  test_list(std::vector<pb::uint32>(), "");
  {
    std::vector<pb::uint32> l;
    l.push_back(1);
    test_list(l, "0a0101");
  }
  {
    std::vector<pb::uint32> l;
    l.push_back(1);
    l.push_back(2);
    l.push_back(3);
    l.push_back(4);
    test_list(l, "0a01010a01020a01030a0104");
  }
}

//test_dictionary
//test_set
//test_tuple
