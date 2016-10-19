#include <gmock/gmock.h>
#include <gtest/gtest.h>

#include <string>
#include <vector>

#include <krpc/platform.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/stream.hpp>

#include "server_test.hpp"
#include "services/test_service.hpp"

class test_stream: public server_test {
};

static void wait() {
  std::this_thread::sleep_for(std::chrono::milliseconds(50));
}

TEST_F(test_stream, test_method) {
  krpc::Stream<std::string> x = test_service.float_to_string_stream(3.14159);
  for (int i = 0; i < 5; i++) {
    ASSERT_EQ("3.14159", x());
    wait();
  }
}

TEST_F(test_stream, test_property) {
  test_service.set_string_property("foo");
  krpc::Stream<std::string> x = test_service.string_property_stream();
  for (int i = 0; i < 5; i++) {
    ASSERT_EQ("foo", x());
    wait();
  }
}

TEST_F(test_stream, test_class_method) {
  auto obj = test_service.create_test_object("bob");
  auto x = obj.float_to_string_stream(3.14159);
  for (int i = 0; i < 5; i++) {
    ASSERT_EQ("bob3.14159", x());
    wait();
  }
}

TEST_F(test_stream, test_class_static_method) {
  auto x = krpc::services::TestService::TestClass::static_method_stream(conn, "foo");
  for (int i = 0; i < 5; i++) {
    ASSERT_EQ("jebfoo", x());
    wait();
  }
}

TEST_F(test_stream, test_class_property) {
  auto obj = test_service.create_test_object("jeb");
  obj.set_int_property(42);
  auto x = obj.int_property_stream();
  for (int i = 0; i < 5; i++) {
    ASSERT_EQ(42, x());
    wait();
  }
}

TEST_F(test_stream, test_counter) {
  int count = -1;
  auto x = test_service.counter_stream();
  for (int i = 0; i < 5; i++) {
    ASSERT_LT(count, x());
    count = x();
    wait();
  }
}

TEST_F(test_stream, test_nested) {
  auto x0 = test_service.float_to_string_stream(0.123);
  auto x1 = test_service.float_to_string_stream(1.234);
  for (int i = 0; i < 5; i++) {
    ASSERT_EQ("0.123", x0());
    ASSERT_EQ("1.234", x1());
    wait();
  }
}

TEST_F(test_stream, test_interleaved) {
  auto s0 = test_service.int32_to_string_stream(0);
  ASSERT_EQ("0", s0());

  wait();
  ASSERT_EQ("0", s0());

  auto s1 = test_service.int32_to_string_stream(1);
  ASSERT_EQ("0", s0());
  ASSERT_EQ("1", s1());

  wait();
  ASSERT_EQ("0", s0());
  ASSERT_EQ("1", s1());

  s1.remove();
  ASSERT_EQ("0", s0());
  ASSERT_THROW(s1(), krpc::StreamError);

  wait();
  ASSERT_EQ("0", s0());
  ASSERT_THROW(s1(), krpc::StreamError);

  auto s2 = test_service.int32_to_string_stream(2);
  ASSERT_EQ("0", s0());
  ASSERT_THROW(s1(), krpc::StreamError);
  ASSERT_EQ("2", s2());

  wait();
  ASSERT_EQ("0", s0());
  ASSERT_THROW(s1(), krpc::StreamError);
  ASSERT_EQ("2", s2());

  s0.remove();
  ASSERT_THROW(s0(), krpc::StreamError);
  ASSERT_THROW(s1(), krpc::StreamError);
  ASSERT_EQ("2", s2());

  wait();
  ASSERT_THROW(s0(), krpc::StreamError);
  ASSERT_THROW(s1(), krpc::StreamError);
  ASSERT_EQ("2", s2());

  s2.remove();
  ASSERT_THROW(s0(), krpc::StreamError);
  ASSERT_THROW(s1(), krpc::StreamError);
  ASSERT_THROW(s2(), krpc::StreamError);

  wait();
  ASSERT_THROW(s0(), krpc::StreamError);
  ASSERT_THROW(s1(), krpc::StreamError);
  ASSERT_THROW(s2(), krpc::StreamError);
}

TEST_F(test_stream, test_remove_stream_twice) {
  auto s = test_service.int32_to_string_stream(0);
  ASSERT_EQ("0", s());

  wait();
  ASSERT_EQ("0", s());

  s.remove();
  ASSERT_THROW(s(), krpc::StreamError);

  s.remove();
  ASSERT_THROW(s(), krpc::StreamError);
}

TEST_F(test_stream, test_add_stream_twice) {
  auto s0 = test_service.int32_to_string_stream(42);
  ASSERT_EQ("42", s0());

  wait();
  ASSERT_EQ("42", s0());

  auto s1 = test_service.int32_to_string_stream(42);
  ASSERT_EQ(s0, s1);
  ASSERT_EQ("42", s0());
  ASSERT_EQ("42", s1());

  wait();
  ASSERT_EQ(s0, s1);
  ASSERT_EQ("42", s0());
  ASSERT_EQ("42", s1());
}

TEST_F(test_stream, test_stream_freeze) {
  auto s0 = test_service.counter_stream(0);
  auto s1 = test_service.counter_stream(1);
  auto x0 = s0();
  auto x1 = s1();
  wait();
  ASSERT_NE(x0, s0());
  ASSERT_NE(x1, s1());
  conn.freeze_streams();
  x0 = s0();
  x1 = s1();
  wait();
  ASSERT_EQ(x0, s0());
  ASSERT_EQ(x1, s1());
  conn.thaw_streams();
  wait();
  ASSERT_NE(x0, s0());
  ASSERT_NE(x1, s1());
}

TEST_F(test_stream, test_stream_freeze_many) {
  std::vector<krpc::Stream<int>> streams;
  for (size_t i = 0; i < 1000; i++)
    streams.push_back(test_service.counter_stream(i));
  std::vector<int> values;
  for (auto stream : streams)
    values.push_back(stream());
  wait();
  for (size_t i = 0; i < streams.size(); i++)
    ASSERT_NE(values[i], streams[i]());
  conn.freeze_streams();
  for (size_t i = 0; i < streams.size(); i++)
    values[i] = streams[i]();
  wait();
  for (size_t i = 0; i < streams.size(); i++)
    ASSERT_EQ(values[i], streams[i]());
  conn.thaw_streams();
  wait();
  for (size_t i = 0; i < streams.size(); i++)
    ASSERT_NE(values[i], streams[i]());
}

TEST_F(test_stream, test_stream_stop_while_frozen) {
  auto s = test_service.counter_stream(0);
  conn.freeze_streams();
}

TEST_F(test_stream, test_default_constructable) {
  krpc::Stream<int> s;
  ASSERT_FALSE(s);
}

TEST_F(test_stream, test_assignable) {
  krpc::Stream<int> s;
  ASSERT_FALSE(s);
  s = test_service.counter_stream(0);
  ASSERT_TRUE(s);
  wait();
  ASSERT_NE(s(), 0);
}
