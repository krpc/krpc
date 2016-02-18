#include <gtest/gtest.h>
#include <gmock/gmock.h>
#include <krpc/platform.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/stream.hpp>
#include "services/test_service.hpp"
#include "server_test.hpp"

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
