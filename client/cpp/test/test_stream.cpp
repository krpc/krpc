#include <gtest/gtest-message.h>
#include <gtest/gtest-test-part.h>

#include <atomic>
#include <chrono>  // NOLINT(build/c++11)
#include <cstddef>
#include <string>
#include <thread>  // NOLINT(build/c++11)
#include <vector>
// IWYU pragma: no_include <ext/alloc_traits.h>

#include "gtest/gtest.h"

#include "krpc/client.hpp"
#include "krpc/services/krpc.hpp"
#include "krpc/stream.hpp"

#include "server_test.hpp"
#include "services/test_service.hpp"

namespace krpc {
class StreamError;
}

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
  auto x = test_service.counter_stream("test_stream.test_counter");
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
  ASSERT_EQ("42", s0());
  ASSERT_EQ("42", s1());

  auto s2 = test_service.int32_to_string_stream(43);
  ASSERT_NE(s0, s2);
  ASSERT_EQ("42", s0());
  ASSERT_EQ("42", s1());
  ASSERT_EQ("43", s2());
  wait();
  ASSERT_EQ("42", s0());
  ASSERT_EQ("42", s1());
  ASSERT_EQ("43", s2());
}

TEST_F(test_stream, test_invalid_operation_exception_immediately) {
  auto s = test_service.throw_invalid_operation_exception_stream();
  ASSERT_THROW(s(), krpc::services::KRPC::InvalidOperationException);
}

TEST_F(test_stream, test_invalid_operation_exception_later) {
  test_service.reset_invalid_operation_exception_later();
  auto s = test_service.throw_invalid_operation_exception_later_stream();
  ASSERT_EQ(0, s());
  ASSERT_THROW({
      while (true) {
        wait();
        s();
      }
    },
    krpc::services::KRPC::InvalidOperationException);
}

TEST_F(test_stream, test_custom_exception_immediately) {
  auto s = test_service.throw_custom_exception_stream();
  ASSERT_THROW(s(), krpc::services::TestService::CustomException);
  try {
    s();
  } catch(krpc::services::TestService::CustomException& e) {
    ASSERT_STREQ(e.what(),
      "A custom kRPC exception");
  }
}

TEST_F(test_stream, test_custom_exception_later) {
  test_service.reset_custom_exception_later();
  auto s = test_service.throw_custom_exception_later_stream();
  ASSERT_EQ(0, s());
  ASSERT_THROW({
      while (true) {
        wait();
        s();
      }
    },
    krpc::services::TestService::CustomException);
  try {
    s();
  } catch(krpc::services::TestService::CustomException& e) {
    ASSERT_STREQ(e.what(),
      "A custom kRPC exception");
  }
}

TEST_F(test_stream, test_yield_exception) {
  auto s = test_service.blocking_procedure_stream(10);
  for (auto i = 0; i < 100; i++) {
    ASSERT_EQ(55, s());
    wait();
  }
}

TEST_F(test_stream, test_wait) {
  auto x = test_service.counter_stream("test_stream.test_wait", 10);
  x.acquire();
  auto count = x();
  ASSERT_LT(count, 10);
  while (count < 10) {
    x.wait();
    count += 1;
    ASSERT_EQ(count, x());
  }
  x.release();
}

TEST_F(test_stream, test_wait_timeout_short) {
  auto x = test_service.counter_stream("test_stream.test_wait_timeout_short", 10);
  x.acquire();
  auto count = x();
  x.wait(0);
  ASSERT_EQ(count, x());
  x.release();
}

TEST_F(test_stream, test_wait_timeout_long) {
  auto x = test_service.counter_stream("test_stream.test_wait_timeout_long", 10);
  x.acquire();
  auto count = x();
  ASSERT_LT(count, 10);
  while (count < 10) {
    x.wait(10);
    count += 1;
    ASSERT_EQ(count, x());
  }
  x.release();
}

TEST_F(test_stream, test_callback) {
  std::atomic<int> test_callback_value(-1);
  std::atomic_flag error;
  error.test_and_set();
  std::atomic_flag stop;
  stop.test_and_set();

  auto callback = [&test_callback_value, &error, &stop] (int x) {
    if (x > 5) {
      stop.clear();
    } else if (test_callback_value+1 != x) {
      error.clear();
      stop.clear();
    } else {
      test_callback_value++;
    }
  };

  auto x = test_service.counter_stream("test_stream.test_callback", 10);
  x.add_callback(callback);
  x.start();
  while (stop.test_and_set()) {
  }
  x.remove();
  ASSERT_TRUE(error.test_and_set());
  ASSERT_EQ(test_callback_value, 5);
}

TEST_F(test_stream, test_remove_callback) {
  std::atomic_flag called1;
  called1.test_and_set();
  std::atomic_flag called2;
  called2.test_and_set();

  auto callback1 = [&called1] (int x) {
    called1.clear();
  };

  auto callback2 = [&called2] (int x) {
    called2.clear();
  };

  auto x = test_service.counter_stream("test_stream.test_remove_callback", 10);
  x.add_callback(callback1);
  auto callback2_tag = x.add_callback(callback2);
  x.remove_callback(callback2_tag);
  x.start();
  while (called1.test_and_set()) {
  }
  x.remove();
  ASSERT_TRUE(called2.test_and_set());
}

TEST_F(test_stream, test_rate) {
  std::atomic<int> test_rate_value(0);
  std::atomic_flag error;
  error.test_and_set();
  std::atomic_flag stop;
  stop.test_and_set();

  auto callback = [&test_rate_value, &error, &stop] (int x) {
    if (x > 5) {
      stop.clear();
    } else if (test_rate_value+1 != x) {
      error.clear();
      stop.clear();
    } else {
      test_rate_value++;
    }
  };

  auto x = test_service.counter_stream("test_stream.test_rate");
  x.add_callback(callback);
  x.set_rate(5);
  x.start();
  auto start = std::chrono::system_clock::now();
  while (stop.test_and_set()) {
  }
  auto end = std::chrono::system_clock::now();
  std::chrono::duration<double> elapsed = end - start;
  ASSERT_GT(elapsed.count(), 1.0);
  ASSERT_LT(elapsed.count(), 1.2);
  x.remove();
  ASSERT_TRUE(error.test_and_set());
  ASSERT_EQ(test_rate_value, 5);
}

TEST_F(test_stream, test_stream_freeze) {
  auto s0 = test_service.counter_stream("test_stream.test_stream_freeze.0");
  auto s1 = test_service.counter_stream("test_stream.test_stream_freeze.1");
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
  for (size_t i = 0; i < 100; i++)
    streams.push_back(
      test_service.counter_stream(
        "test_stream.test_stream_freeze_many."+std::to_string(i)));
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

// FIXME: reenable test
// TEST_F(test_stream, test_stream_stop_while_frozen) {
//   auto s = test_service.counter_stream("test_stream.test_stream_stop_while_frozen");
//   conn.freeze_streams();
// }

TEST_F(test_stream, test_default_constructable) {
  krpc::Stream<int> s;
  ASSERT_FALSE(s);
}

TEST_F(test_stream, test_assignable) {
  krpc::Stream<int> s;
  ASSERT_FALSE(s);
  s = test_service.counter_stream("test_stream.test_assignable");
  ASSERT_TRUE(s);
  wait();
  ASSERT_NE(s(), 0);
}

TEST_F(test_stream, test_equality) {
  krpc::Stream<int> s1;
  krpc::Stream<int> s2;
  ASSERT_TRUE(s1 == s2);
  ASSERT_FALSE(s1 != s2);
  ASSERT_TRUE(s2 == s1);
  ASSERT_FALSE(s2 != s1);
  s1 = test_service.counter_stream("test_stream.test_assignable");
  ASSERT_FALSE(s1 == s2);
  ASSERT_TRUE(s1 != s2);
  ASSERT_FALSE(s2 == s1);
  ASSERT_TRUE(s2 != s1);
  s2 = test_service.counter_stream("test_stream.test_assignable");
  ASSERT_TRUE(s1 == s2);
  ASSERT_FALSE(s1 != s2);
  ASSERT_TRUE(s2 == s1);
  ASSERT_FALSE(s2 != s1);
}
