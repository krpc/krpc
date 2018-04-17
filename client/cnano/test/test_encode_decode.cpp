#include <gtest/gtest-message.h>
#include <gtest/gtest-test-part.h>

#include <krpc_cnano.h>
#include <krpc_cnano/decoder.h>
#include <krpc_cnano/encoder.h>
#include <krpc_cnano/services/krpc.h>

#include <algorithm>
#include <cmath>
#include <limits>
#include <map>
#include <set>
#include <string>
#include <vector>

#include "gtest/gtest.h"

#include "services/test_service.h"
#include "testing_tools.hpp"

static pb_istream_t create_istream(uint8_t * buffer, std::string data) {
  unhexlify(buffer, data);
  return pb_istream_from_buffer(buffer, data.size()/2);
}

static pb_ostream_t create_ostream(uint8_t * buffer, size_t size) {
  return pb_ostream_from_buffer(buffer, size);
}

template <typename T> krpc_error_t encode_value(pb_ostream_t * stream, T value) {
  return KRPC_ERROR_ENCODING_FAILED;
}

template <> krpc_error_t encode_value<int32_t>(pb_ostream_t * stream, int32_t value) {
  return krpc_encode_int32(stream, value);
}

template <> krpc_error_t encode_value<int64_t>(pb_ostream_t * stream, int64_t value) {
  return krpc_encode_int64(stream, value);
}

template <> krpc_error_t encode_value<uint32_t>(pb_ostream_t * stream, uint32_t value) {
  return krpc_encode_uint32(stream, value);
}

template <> krpc_error_t encode_value<uint64_t>(pb_ostream_t * stream, uint64_t value) {
  return krpc_encode_uint64(stream, value);
}

template <> krpc_error_t encode_value<bool>(pb_ostream_t * stream, bool value) {
  return krpc_encode_bool(stream, value);
}

template <typename T> krpc_error_t encode_size_value(size_t * size, T value) {
  return KRPC_ERROR_ENCODING_FAILED;
}

template <> krpc_error_t encode_size_value<int32_t>(size_t * size, int32_t value) {
  return krpc_encode_size_int32(size, value);
}

template <> krpc_error_t encode_size_value<int64_t>(size_t * size, int64_t value) {
  return krpc_encode_size_int64(size, value);
}

template <> krpc_error_t encode_size_value<uint32_t>(size_t * size, uint32_t value) {
  return krpc_encode_size_uint32(size, value);
}

template <> krpc_error_t encode_size_value<uint64_t>(size_t * size, uint64_t value) {
  return krpc_encode_size_uint64(size, value);
}

template <> krpc_error_t encode_size_value<bool>(size_t * size, bool value) {
  return krpc_encode_size_bool(size, value);
}

template <typename T> krpc_error_t decode_value(pb_istream_t * stream, T * value) {
  return KRPC_ERROR_ENCODING_FAILED;
}

template <> krpc_error_t decode_value<int32_t>(pb_istream_t * stream, int32_t * value) {
  return krpc_decode_int32(stream, value);
}

template <> krpc_error_t decode_value<int64_t>(pb_istream_t * stream, int64_t * value) {
  return krpc_decode_int64(stream, value);
}

template <> krpc_error_t decode_value<uint32_t>(pb_istream_t * stream, uint32_t * value) {
  return krpc_decode_uint32(stream, value);
}

template <> krpc_error_t decode_value<uint64_t>(pb_istream_t * stream, uint64_t * value) {
  return krpc_decode_uint64(stream, value);
}

template <> krpc_error_t decode_value<bool>(pb_istream_t * stream, bool * value) {
  return krpc_decode_bool(stream, value);
}

template<typename T> void test_value(T decoded, std::string encoded) {
  uint8_t data[8];
  {
    size_t size = 0;
    ASSERT_EQ(KRPC_OK, encode_size_value<T>(&size, decoded));
    ASSERT_EQ(size, encoded.size()/2);
  }
  {
    pb_ostream_t stream = create_ostream(data, sizeof(data));
    ASSERT_EQ(KRPC_OK, encode_value<T>(&stream, decoded));
    ASSERT_EQ(encoded, hexlify(data, stream.bytes_written));
  }
  {
    pb_istream_t stream = create_istream(data, encoded);
    T value = 0;
    ASSERT_EQ(KRPC_OK, decode_value<T>(&stream, &value));
    ASSERT_EQ(decoded, value);
  }
}

void test_float(float decoded, std::string encoded) {
  uint8_t data[4];
  {
    size_t size = 0;
    ASSERT_EQ(KRPC_OK, krpc_encode_size_float(&size, decoded));
    ASSERT_EQ(size, encoded.size()/2);
  }
  {
    pb_ostream_t stream = create_ostream(data, sizeof(data));
    ASSERT_EQ(KRPC_OK, krpc_encode_float(&stream, decoded));
    ASSERT_EQ(encoded, hexlify(data, stream.bytes_written));
  }
  {
    pb_istream_t stream = create_istream(data, encoded);
    float value = 0;
    ASSERT_EQ(KRPC_OK, krpc_decode_float(&stream, &value));
    if (!std::isnan(decoded))
      ASSERT_FLOAT_EQ(decoded, value);
    else
      ASSERT_TRUE(std::isnan(value));
  }
}

void test_double(double decoded, std::string encoded) {
  uint8_t data[8];
  {
    size_t size = 0;
    ASSERT_EQ(KRPC_OK, krpc_encode_size_double(&size, decoded));
    ASSERT_EQ(size, encoded.size()/2);
  }
  {
    pb_ostream_t stream = create_ostream(data, sizeof(data));
    ASSERT_EQ(KRPC_OK, krpc_encode_double(&stream, decoded));
    ASSERT_EQ(encoded, hexlify(data, stream.bytes_written));
  }
  {
    pb_istream_t stream = create_istream(data, encoded);
    double value = 0;
    ASSERT_EQ(KRPC_OK, krpc_decode_double(&stream, &value));
    if (!std::isnan(decoded))
      ASSERT_DOUBLE_EQ(decoded, value);
    else
      ASSERT_TRUE(std::isnan(value));
  }
}

void test_string(std::string decoded, std::string encoded) {
  const char * string = decoded.c_str();
  uint8_t data[256];
  {
    size_t size = 0;
    ASSERT_EQ(KRPC_OK, krpc_encode_size_string(&size, string));
    ASSERT_EQ(size, encoded.size()/2);
  }
  {
    pb_ostream_t stream = create_ostream(data, sizeof(data));
    ASSERT_EQ(KRPC_OK, krpc_encode_string(&stream, string));
    ASSERT_EQ(encoded, hexlify(data, stream.bytes_written));
  }
  {
    pb_istream_t stream = create_istream(data, encoded);
    char * value = nullptr;
    ASSERT_EQ(KRPC_OK, krpc_decode_string(&stream, &value));
    ASSERT_STREQ(decoded.c_str(), value);
    krpc_free(value);
  }
}

void test_bytes(std::string decoded, std::string encoded) {
  uint8_t data[256];
  krpc_bytes_t bytes = KRPC_BYTES_FROM_BUFFER((uint8_t*)decoded.c_str(), decoded.size());
  {
    size_t size = 0;
    ASSERT_EQ(KRPC_OK, krpc_encode_size_bytes(&size, bytes));
    ASSERT_EQ(size, encoded.size()/2);
  }
  {
    pb_ostream_t stream = create_ostream(data, sizeof(data));
    ASSERT_EQ(KRPC_OK, krpc_encode_bytes(&stream, bytes));
    ASSERT_EQ(encoded, hexlify(data, stream.bytes_written));
  }
  {
    pb_istream_t stream = create_istream(data, encoded);
    krpc_bytes_t value = KRPC_BYTES_NULL;
    ASSERT_EQ(KRPC_OK, krpc_decode_bytes(&stream, &value));
    ASSERT_EQ(decoded.size(), value.size);
    if (decoded.size() == 0) {
      ASSERT_EQ(NULL, value.size);
    } else {
      for (size_t i = 0; i < decoded.size(); i++)
        ASSERT_EQ((uint8_t)(decoded[i]), value.data[i]);
    }
    KRPC_FREE_BYTES(value);
  }
}

template <typename T> void test_list(const std::vector<T>& decoded, std::string encoded) {
  assert(false);
}

template <> void test_list(const std::vector<int32_t>& decoded, std::string encoded) {
  uint8_t data[256];
  {
    krpc_list_int32_t value;
    value.size = decoded.size();
    value.items = new int32_t[decoded.size()];
    for (size_t i = 0; i < value.size; i++)
      value.items[i] = decoded[i];

    size_t size = 0;
    ASSERT_EQ(KRPC_OK, krpc_encode_size_list_int32(&size, &value));
    ASSERT_EQ(size, encoded.size()/2);

    pb_ostream_t stream = create_ostream(data, sizeof(data));
    ASSERT_EQ(KRPC_OK, krpc_encode_list_int32(&stream, &value));
    ASSERT_EQ(encoded, hexlify(data, stream.bytes_written));

    delete[] value.items;
  }

  {
    pb_istream_t stream = create_istream(data, encoded);
    krpc_list_int32_t value = KRPC_NULL_LIST;
    ASSERT_EQ(KRPC_OK, krpc_decode_list_int32(&stream, &value));
    ASSERT_EQ(decoded.size(), value.size);
    for (size_t i = 0; i < decoded.size(); i++)
      ASSERT_EQ(decoded[i], value.items[i]);
    KRPC_FREE_LIST(value);
  }
}

template <> void test_list(
  const std::vector<krpc_TestService_TestClass_t>& decoded, std::string encoded) {
  uint8_t data[256];
  {
    krpc_list_object_t value;
    value.size = decoded.size();
    value.items = new krpc_TestService_TestClass_t[decoded.size()];
    for (size_t i = 0; i < value.size; i++)
      value.items[i] = decoded[i];

    size_t size = 0;
    ASSERT_EQ(KRPC_OK, krpc_encode_size_list_object(&size, &value));
    ASSERT_EQ(size, encoded.size()/2);

    pb_ostream_t stream = create_ostream(data, sizeof(data));
    ASSERT_EQ(KRPC_OK, krpc_encode_list_object(&stream, &value));
    ASSERT_EQ(encoded, hexlify(data, stream.bytes_written));

    delete[] value.items;
  }
  {
    pb_istream_t stream = create_istream(data, encoded);
    krpc_list_object_t value = KRPC_NULL_LIST;
    ASSERT_EQ(KRPC_OK, krpc_decode_list_object(&stream, &value));
    ASSERT_EQ(decoded.size(), value.size);
    for (size_t i = 0; i < decoded.size(); i++)
      ASSERT_EQ(decoded[i], value.items[i]);
    KRPC_FREE_LIST(value);
  }
}

template <typename K, typename V> void test_dictionary(
  const std::map<K, V>& decoded, std::string encoded) {
  assert(false);
}

template <> void test_dictionary(
  const std::map<std::string, int32_t>& decoded, std::string encoded) {
  uint8_t data[256];
  {
    krpc_dictionary_string_int32_t value;
    value.size = decoded.size();
    value.entries = new krpc_dictionary_entry_string_int32_t[decoded.size()];
    size_t i = 0;
    for (auto entry : decoded) {
      auto str = new char[entry.first.size()+1];
      strncpy(str, entry.first.c_str(), entry.first.size());
      str[entry.first.size()] = '\0';
      value.entries[i].key = str;
      value.entries[i].value = entry.second;
      i++;
    }

    size_t size = 0;
    ASSERT_EQ(KRPC_OK, krpc_encode_size_dictionary_string_int32(&size, &value));
    ASSERT_EQ(size, encoded.size()/2);

    pb_ostream_t stream = create_ostream(data, sizeof(data));
    ASSERT_EQ(KRPC_OK, krpc_encode_dictionary_string_int32(&stream, &value));
    ASSERT_EQ(encoded, hexlify(data, stream.bytes_written));

    for (i = 0; i < decoded.size(); i++)
      delete[] value.entries[i].key;
    delete[] value.entries;
  }
  {
    pb_istream_t stream = create_istream(data, encoded);
    krpc_dictionary_string_int32_t value = KRPC_NULL_DICTIONARY;
    ASSERT_EQ(KRPC_OK, krpc_decode_dictionary_string_int32(&stream, &value));
    ASSERT_EQ(decoded.size(), value.size);
    std::map<std::string, int32_t> actual;
    for (size_t i = 0; i < value.size; i++) {
      auto entry = value.entries+i;
      actual[std::string(entry->key)] = entry->value;
    }
    ASSERT_EQ(decoded.size(), actual.size());
    ASSERT_TRUE(std::equal(decoded.begin(), decoded.end(), actual.begin()));
    for (size_t i = 0; i < value.size; i++)
      krpc_free(value.entries[i].key);
    KRPC_FREE_DICTIONARY(value);
  }
}

template <typename T> void test_set(const std::set<T>& decoded, std::string encoded) {
  assert(false);
}

template <> void test_set(const std::set<int32_t>& decoded, std::string encoded) {
  uint8_t data[256];
  {
    krpc_set_int32_t value;
    value.size = decoded.size();
    value.items = new int32_t[decoded.size()];
    size_t i = 0;
    for (auto x : decoded) {
      value.items[i] = x;
      i++;
    }

    size_t size = 0;
    ASSERT_EQ(KRPC_OK, krpc_encode_size_set_int32(&size, &value));
    ASSERT_EQ(size, encoded.size()/2);

    pb_ostream_t stream = create_ostream(data, sizeof(data));
    ASSERT_EQ(KRPC_OK, krpc_encode_set_int32(&stream, &value));
    ASSERT_EQ(encoded, hexlify(data, stream.bytes_written));

    delete[] value.items;
  }
  {
    pb_istream_t stream = create_istream(data, encoded);
    krpc_set_int32_t value = KRPC_NULL_SET;
    ASSERT_EQ(KRPC_OK, krpc_decode_set_int32(&stream, &value));
    ASSERT_EQ(decoded.size(), value.size);
    std::set<int32_t> actual;
    for (size_t i = 0; i < value.size; i++)
      actual.insert(value.items[i]);
    ASSERT_EQ(decoded.size(), actual.size());
    ASSERT_TRUE(std::equal(decoded.begin(), decoded.end(), actual.begin()));
    KRPC_FREE_SET(value);
  }
}

void test_tuple_int32_int64(const krpc_tuple_int32_int64_t& decoded, std::string encoded) {
  uint8_t data[256];
  {
    size_t size = 0;
    ASSERT_EQ(KRPC_OK, krpc_encode_size_tuple_int32_int64(&size, &decoded));
    ASSERT_EQ(size, encoded.size()/2);

    pb_ostream_t stream = create_ostream(data, sizeof(data));
    ASSERT_EQ(KRPC_OK, krpc_encode_tuple_int32_int64(&stream, &decoded));
    ASSERT_EQ(encoded, hexlify(data, stream.bytes_written));
  }
  {
    pb_istream_t stream = create_istream(data, encoded);
    krpc_tuple_int32_int64_t value = { 0, 0 };
    ASSERT_EQ(KRPC_OK, krpc_decode_tuple_int32_int64(&stream, &value));
    ASSERT_EQ(decoded.e0, value.e0);
    ASSERT_EQ(decoded.e1, value.e1);
  }
}

void test_tuple_int32_bool(const krpc_tuple_int32_bool_t& decoded, std::string encoded) {
  uint8_t data[256];
  {
    size_t size = 0;
    ASSERT_EQ(KRPC_OK, krpc_encode_size_tuple_int32_bool(&size, &decoded));
    ASSERT_EQ(size, encoded.size()/2);

    pb_ostream_t stream = create_ostream(data, sizeof(data));
    ASSERT_EQ(KRPC_OK, krpc_encode_tuple_int32_bool(&stream, &decoded));
    ASSERT_EQ(encoded, hexlify(data, stream.bytes_written));
  }
  {
    pb_istream_t stream = create_istream(data, encoded);
    krpc_tuple_int32_bool_t value = { 0, false };
    ASSERT_EQ(KRPC_OK, krpc_decode_tuple_int32_bool(&stream, &value));
    ASSERT_EQ(decoded.e0, value.e0);
    ASSERT_EQ(decoded.e1, value.e1);
  }
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

TEST(test_encode_decode, test_sint32) {
  test_value<int32_t>(0, "00");
  test_value<int32_t>(1, "02");
  test_value<int32_t>(42, "54");
  test_value<int32_t>(300, "d804");
  test_value<int32_t>(-33, "41");
  test_value<int32_t>(2147483647, "feffffff0f");
  test_value<int32_t>(-2147483648, "ffffffff0f");
}

TEST(test_encode_decode, test_sint64) {
  test_value<int64_t>(0, "00");
  test_value<int64_t>(1, "02");
  test_value<int64_t>(42, "54");
  test_value<int64_t>(300, "d804");
  test_value<int64_t>(1234567890000L, "a091d89fee47");
  test_value<int64_t>(-33, "41");
}

TEST(test_encode_decode, test_uint32) {
  test_value<uint32_t>(0, "00");
  test_value<uint32_t>(1, "01");
  test_value<uint32_t>(42, "2a");
  test_value<uint32_t>(300, "ac02");
  test_value<uint32_t>(std::numeric_limits<uint32_t>::max(), "ffffffff0f");
}

TEST(test_encode_decode, test_uint64) {
  test_value<uint64_t>(0, "00");
  test_value<uint64_t>(1, "01");
  test_value<uint64_t>(42, "2a");
  test_value<uint64_t>(300, "ac02");
  test_value<uint64_t>(1234567890000L, "d088ec8ff723");
}

TEST(test_encode_decode, test_bool) {
  test_value<bool>(true,  "01");
  test_value<bool>(false, "00");
}

TEST(test_encode_decode, test_string) {
  test_string("", "00");
  test_string("testing", "0774657374696e67");
  test_string("One small step for Kerbal-kind!",
              "1f4f6e6520736d616c6c207374657020666f72204b657262616c2d6b696e6421");
  test_string("\xe2\x84\xa2", "03e284a2");
  test_string("Mystery Goo\xe2\x84\xa2 Containment Unit",
              "1f4d79737465727920476f6fe284a220436f6e7461696e6d656e7420556e6974");
}

TEST(test_encode_decode, test_bytes) {
  test_bytes("", "00");
  test_bytes("\xba\xda\x55", "03bada55");
  test_bytes("\xde\xad\xbe\xef", "04deadbeef");
  test_bytes(std::string("\xde\xad\xbe\xef\x00", 5), "05deadbeef00");
}

TEST(test_encode_decode, test_list) {
  test_list(std::vector<int32_t>(), "");
  {
    std::vector<int32_t> l;
    l.push_back(1);
    test_list(l, "0a0102");
  }
  {
    std::vector<int32_t> l;
    l.push_back(1);
    l.push_back(2);
    l.push_back(3);
    l.push_back(4);
    test_list(l, "0a01020a01040a01060a0108");
  }
}

TEST(test_encode_decode, test_dictionary) {
  test_dictionary(std::map<std::string, int32_t>(), "");
  {
    std::map<std::string, int32_t> d;
    d[""] = 0;
    test_dictionary(d, "0a060a0100120100");
  }
  {
    std::map<std::string, int32_t> d;
    d["foo"] = 42;
    d["bar"] = 365;
    d["baz"] = 3;
    test_dictionary(d, "0a0a0a04036261721202da050a090a040362617a1201060a090a0403666f6f120154");
  }
}

TEST(test_encode_decode, test_set) {
  test_set(std::set<int32_t>(), "");
  {
    std::set<int32_t> s;
    s.insert(1);
    test_set(s, "0a0102");
  }
  {
    std::set<int32_t> s;
    s.insert(1);
    s.insert(2);
    s.insert(3);
    s.insert(4);
    test_set(s, "0a01020a01040a01060a0108");
  }
}

TEST(test_encode_decode, test_tuple) {
  test_tuple_int32_int64({1, 0}, "0a01020a0100");
  test_tuple_int32_int64({42, 123456789L}, "0a01540a04aab4de75");
  test_tuple_int32_bool({1, true}, "0a01020a0101");
  test_tuple_int32_bool({-1, false}, "0a01010a0100");
}

TEST(test_encode_decode, test_list_of_objects) {
  test_list(std::vector<krpc_TestService_TestClass_t>(), "");
  {
    std::vector<krpc_TestService_TestClass_t> l;
    l.push_back(1);
    l.push_back(2);
    l.push_back(3);
    test_list(l, "0a01010a01020a0103");
  }
}
