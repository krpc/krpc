#include <gtest/gtest-message.h>
#include <gtest/gtest-test-part.h>

#include <atomic>
#include <chrono>  // NOLINT(build/c++11)
#include <cstddef>
#include <exception>
#include <functional>
#include <memory>
#include <optional>
#include <string>
#include <thread>  // NOLINT(build/c++11)
#include <vector>
// IWYU pragma: no_include <ext/alloc_traits.h>

#include "gtest/gtest.h"
#include "krpc/client.hpp"
#include "krpc/error.hpp"
#include "krpc/services/krpc.hpp"
#include "krpc/stream.hpp"
#include "server_test.hpp"
#include "services/test_service.hpp"

class test_stream : public server_test {};

static void wait() { std::this_thread::sleep_for(std::chrono::milliseconds(50)); }

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

TEST_F(test_stream, test_enumeration) {
  auto x = test_service.enum_return_stream();
  for (int i = 0; i < 5; i++) {
    ASSERT_EQ(krpc::services::TestService::TestEnum::value_b, x());
    wait();
  }
}

TEST_F(test_stream, test_struct) {
  auto x = test_service.struct_echo_stream(krpc::services::TestService::TestStruct(
      1, "jeb", krpc::services::TestService::TestEnum::value_c, {1, 2}));
  for (int i = 0; i < 5; i++) {
    ASSERT_EQ("jeb", x().string_field);
    wait();
  }
}

TEST_F(test_stream, test_nullable) {
  auto x = test_service.echo_nullable_int_stream(std::nullopt);
  auto y = test_service.echo_nullable_string_stream(std::nullopt);
  for (int i = 0; i < 5; i++) {
    ASSERT_FALSE(x().has_value());
    ASSERT_FALSE(y().has_value());
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

// Removing a stream ends it, and another handle to the same stream throws the ending in
// place of the value it last received.
TEST_F(test_stream, test_remove_errors_another_handle) {
  auto x = test_service.int32_to_string_stream(42);
  krpc::Stream<std::string> y = x;
  ASSERT_EQ("42", x());
  ASSERT_EQ("42", y());
  x.remove();
  ASSERT_THROW(y(), krpc::StreamError);
}

// No further update arrives for a removed stream, so removing one has to wake a thread
// already waiting on it. This waits on a stream whose value never changes, which gets
// exactly one update. A regression leaves the waiting thread blocked and the test times out.
TEST_F(test_stream, test_remove_wakes_a_waiting_thread) {
  krpc::Stream<std::string> x = test_service.float_to_string_stream(3.14159);
  krpc::Stream<std::string> y = x;
  x.start();
  std::atomic_bool waiting(false);
  std::thread waiter([&y, &waiting] {
    y.acquire();
    waiting.store(true);
    // The wait wakes, and throws as the stream was removed
    EXPECT_THROW(y.wait(), krpc::StreamError);
    y.release();
  });
  // The waiter holds the condition until it enters its wait, so the removal below cannot
  // notify before then
  ASSERT_TRUE(eventually([&waiting] { return waiting.load(); }));
  x.remove();
  waiter.join();
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

TEST_F(test_stream, test_removing_a_stream_again_through_another_handle) {
  auto first = test_service.int32_to_string_stream(46);
  auto second = test_service.int32_to_string_stream(46);
  first.remove();
  // The server can give the new stream the removed one's id
  auto third = test_service.int32_to_string_stream(46);
  second.remove();
  ASSERT_EQ("46", third());
  third.remove();
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
  ASSERT_THROW(
      {
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
  } catch (krpc::services::TestService::CustomException& e) {
    ASSERT_STREQ(e.what(), "A custom kRPC exception");
  }
}

TEST_F(test_stream, test_custom_exception_later) {
  test_service.reset_custom_exception_later();
  auto s = test_service.throw_custom_exception_later_stream();
  ASSERT_EQ(0, s());
  ASSERT_THROW(
      {
        while (true) {
          wait();
          s();
        }
      },
      krpc::services::TestService::CustomException);
  try {
    s();
  } catch (krpc::services::TestService::CustomException& e) {
    ASSERT_STREQ(e.what(), "A custom kRPC exception");
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
  ASSERT_FALSE(x.wait(0));
  ASSERT_EQ(count, x());
  x.release();
}

TEST_F(test_stream, test_wait_timeout_long) {
  auto x = test_service.counter_stream("test_stream.test_wait_timeout_long", 10);
  x.acquire();
  auto count = x();
  ASSERT_LT(count, 10);
  while (count < 10) {
    ASSERT_TRUE(x.wait(10));
    count += 1;
    ASSERT_EQ(count, x());
  }
  x.release();
}

// The update thread must not hold the update lock while waiting for a stream's condition.
// This test holds the condition, which is how waiting for an update is documented to work,
// and then calls something needing the update lock. A regression deadlocks the two, and the
// test times out.
TEST_F(test_stream, test_remove_callback_while_holding_condition) {
  auto x = test_service.counter_stream("test_stream.test_remove_callback_while_holding_condition");
  auto tag = x.add_callback([](int) {});
  x.start();
  x.acquire();
  x.wait();
  // Let the update thread reach the next update and block on the condition we hold
  std::this_thread::sleep_for(std::chrono::milliseconds(200));
  x.remove_callback(tag);
  x.release();
}

// These tests wait for an update first, to synchronize on an update message
// boundary before reading their baseline count; each subsequent wait then
// corresponds to exactly one update message, which advances the counter value
// by exactly one (the counter's divisor slows value changes to one per ten
// server ticks, so each change is sent in its own update message).

TEST_F(test_stream, test_wait_update) {
  auto x = test_service.counter_stream("test_stream.test_wait_update", 10);
  conn.acquire_stream_update();
  x.start(false);
  conn.wait_for_stream_update();
  auto count = x();
  ASSERT_LT(count, 10);
  while (count < 10) {
    conn.wait_for_stream_update();
    count += 1;
    ASSERT_EQ(count, x());
  }
  conn.release_stream_update();
}

TEST_F(test_stream, test_wait_update_timeout_short) {
  auto x = test_service.counter_stream("test_stream.test_wait_update_timeout_short", 10);
  conn.acquire_stream_update();
  x.start(false);
  conn.wait_for_stream_update();
  auto count = x();
  conn.wait_for_stream_update(0);
  ASSERT_EQ(count, x());
  conn.release_stream_update();
}

TEST_F(test_stream, test_wait_update_timeout_long) {
  auto x = test_service.counter_stream("test_stream.test_wait_update_timeout_long", 10);
  conn.acquire_stream_update();
  x.start(false);
  conn.wait_for_stream_update();
  auto count = x();
  ASSERT_LT(count, 10);
  while (count < 10) {
    conn.wait_for_stream_update(10);
    count += 1;
    ASSERT_EQ(count, x());
  }
  conn.release_stream_update();
}

TEST_F(test_stream, test_callback) {
  std::atomic<int> test_callback_value(-1);
  std::atomic_flag error;
  error.test_and_set();
  std::atomic_flag stop;
  stop.test_and_set();

  auto callback = [&test_callback_value, &error, &stop](int x) {
    if (x > 5) {
      stop.clear();
    } else if (test_callback_value + 1 != x) {
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

// A callback that throws must not escape the update thread: an exception leaving it calls
// std::terminate and ends the process, not merely the stream updates.
TEST_F(test_stream, test_callback_that_throws) {
  std::atomic_flag called;
  called.test_and_set();
  std::atomic_flag stop;
  stop.test_and_set();

  auto x = test_service.counter_stream("test_stream.test_callback_that_throws", 10);
  x.add_callback([&called](int value) {
    called.clear();
    throw std::runtime_error("callback failed");
  });
  x.add_callback([&stop](int value) {
    if (value > 5) stop.clear();
  });
  x.start();
  while (stop.test_and_set()) {
  }
  x.remove();
  ASSERT_FALSE(called.test_and_set());
}

// An errored stream has no value to give a callback, and reading one rethrows the error. That
// must not escape the update thread either.
TEST_F(test_stream, test_callback_on_errored_stream) {
  std::atomic_flag called;
  called.test_and_set();

  auto x = test_service.throw_invalid_operation_exception_stream();
  x.add_callback([&called](int value) { called.clear(); });
  x.start(false);
  for (int i = 0; i < 5; i++) wait();

  // The stream still reports its error to a reader, and the callback was never given a value
  ASSERT_THROW(x(), krpc::services::KRPC::InvalidOperationException);
  ASSERT_TRUE(called.test_and_set());
}

TEST_F(test_stream, test_remove_callback) {
  std::atomic_flag called1;
  called1.test_and_set();
  std::atomic_flag called2;
  called2.test_and_set();

  auto callback1 = [&called1](int x) { called1.clear(); };

  auto callback2 = [&called2](int x) { called2.clear(); };

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

TEST_F(test_stream, test_update_callback) {
  std::atomic_flag stop;
  stop.test_and_set();

  auto callback = [&stop]() { stop.clear(); };

  auto x = test_service.counter_stream("test_stream.test_update_callback", 10);
  conn.add_stream_update_callback(callback);
  x.start();
  while (stop.test_and_set()) {
  }
  x.remove();
}

TEST_F(test_stream, test_remove_update_callback) {
  std::atomic_flag called1;
  called1.test_and_set();
  std::atomic_flag called2;
  called2.test_and_set();

  auto callback1 = [&called1]() { called1.clear(); };

  auto callback2 = [&called2]() { called2.clear(); };

  auto x = test_service.counter_stream("test_stream.test_remove_update_callback", 10);
  conn.add_stream_update_callback(callback1);
  auto callback2_tag = conn.add_stream_update_callback(callback2);
  conn.remove_stream_update_callback(callback2_tag);
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

  auto callback = [&test_rate_value, &error, &stop](int x) {
    if (x > 5) {
      stop.clear();
    } else if (test_rate_value + 1 != x) {
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
  // Lower bound checks the rate limit was honoured; the upper bound is a
  // generous hang detector, kept loose so the test does not flake under
  // parallel load.
  ASSERT_GT(elapsed.count(), 1.0);
  ASSERT_LT(elapsed.count(), 3.0);
  x.remove();
  ASSERT_TRUE(error.test_and_set());
  ASSERT_EQ(test_rate_value, 5);
}

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

TEST_F(test_stream, test_stream_without_a_stream_connection) {
  // A stream port of zero connects without streams, which connect_local cannot express
  if (get_rpc_path() != nullptr) GTEST_SKIP();
  auto client = krpc::connect("NoStreams", "localhost", get_rpc_port(), 0);
  krpc::services::TestService service(&client);
  ASSERT_THROW(service.string_property_stream(), krpc::StreamError);
}

TEST_F(test_stream, test_error_ends_the_stream) {
  auto x = test_service.throw_invalid_operation_exception_stream();
  std::atomic<int> ended(0);
  auto count_ending = [&ended](const std::exception_ptr& ending) {
    ended++;
    try {
      std::rethrow_exception(ending);
    } catch (const krpc::services::KRPC::InvalidOperationException&) {
    } catch (...) {
      ADD_FAILURE() << "the stream ended with the wrong exception";
    }
  };
  x.add_end_callback(count_ending);
  x.acquire();
  EXPECT_THROW(x.wait(), krpc::services::KRPC::InvalidOperationException);
  EXPECT_THROW(x.wait(1), krpc::services::KRPC::InvalidOperationException);
  x.release();
  ASSERT_THROW(x(), krpc::services::KRPC::InvalidOperationException);
  // The end callbacks run after the waiters wake
  ASSERT_TRUE(eventually([&ended] { return ended.load() == 1; }));
  // Added after the end, so called straight away
  x.add_end_callback(count_ending);
  ASSERT_EQ(2, ended.load());
  // Already removed by the server, so this does nothing
  x.remove();
  ASSERT_THROW(x(), krpc::services::KRPC::InvalidOperationException);
}

TEST_F(test_stream, test_end_callback_on_remove) {
  auto x = test_service.int32_to_string_stream(46);
  ASSERT_EQ("46", x());
  std::atomic<int> ended(0);
  x.add_end_callback([&ended](const std::exception_ptr& ending) {
    ended++;
    EXPECT_THROW(std::rethrow_exception(ending), krpc::StreamError);
  });
  x.remove();
  ASSERT_EQ(1, ended.load());
}

TEST_F(test_stream, test_remove_end_callback) {
  auto x = test_service.int32_to_string_stream(48);
  std::atomic<int> ended(0);
  auto tag = x.add_end_callback([&ended](const std::exception_ptr&) { ended++; });
  x.remove_end_callback(tag);
  x.remove();
  ASSERT_EQ(0, ended.load());
}

TEST_F(test_stream, test_remove_while_holding_condition) {
  auto x = test_service.int32_to_string_stream(49);
  ASSERT_EQ("49", x());
  x.acquire();
  x.remove();
  EXPECT_THROW(x.wait(), krpc::StreamError);
  x.release();
}

TEST_F(test_stream, test_close_wakes_a_waiting_thread) {
  auto client = connect_client();
  krpc::services::TestService service(client.get());
  auto x = service.int32_to_string_stream(47);
  ASSERT_EQ("47", x());
  std::atomic_bool waiting(false);
  std::thread waiter([&x, &waiting] {
    x.acquire();
    waiting.store(true);
    EXPECT_THROW(x.wait(), krpc::ConnectionError);
    x.release();
  });
  ASSERT_TRUE(eventually([&waiting] { return waiting.load(); }));
  client.reset();
  waiter.join();
  ASSERT_THROW(x(), krpc::ConnectionError);
}

TEST_F(test_stream, test_connection_failure_wakes_a_waiting_thread) {
  std::function<void()> shutdown;
  auto client = connect_client(&shutdown);
  krpc::services::TestService service(client.get());
  auto x = service.int32_to_string_stream(47);
  ASSERT_EQ("47", x());
  std::atomic<int> ended(0);
  x.add_end_callback([&ended](const std::exception_ptr&) { ended++; });
  std::atomic_bool waiting(false);
  std::thread waiter([&x, &waiting] {
    x.acquire();
    waiting.store(true);
    EXPECT_THROW(x.wait(), krpc::ConnectionError);
    x.release();
  });
  ASSERT_TRUE(eventually([&waiting] { return waiting.load(); }));
  // Stands in for the server closing the connection
  shutdown();
  waiter.join();
  // The end callbacks run after the waiters wake
  ASSERT_TRUE(eventually([&ended] { return ended.load() == 1; }));
  ASSERT_THROW(x(), krpc::ConnectionError);
}

// A thread holding the condition through one stream object can use another
TEST_F(test_stream, test_another_handle_while_holding_condition) {
  auto x = test_service.int32_to_string_stream(50);
  x.acquire();
  auto y = x;
  // Starts the stream and waits for its first value
  ASSERT_EQ("50", y());
  y.remove();
  EXPECT_THROW(x.wait(), krpc::StreamError);
  x.release();
}

// A thread can hold the condition through two stream objects at once
TEST_F(test_stream, test_acquire_through_two_handles) {
  auto x = test_service.int32_to_string_stream(51);
  ASSERT_EQ("51", x());
  auto y = x;
  x.acquire();
  y.acquire();
  x.release();
  y.remove();
  EXPECT_THROW(y.wait(), krpc::StreamError);
  y.release();
  // Both holds are released, so another thread can take the condition
  std::thread other([&x] {
    x.acquire();
    x.release();
  });
  other.join();
}

// A thread waiting on the condition directly keeps its hold while another thread takes it
TEST_F(test_stream, test_remove_after_waiting_on_the_condition) {
  auto x = test_service.counter_stream("test_stream.test_remove_after_waiting_on_the_condition");
  ASSERT_GT(x(), 0);
  x.acquire();
  std::atomic_bool done(false);
  std::thread other([x, &done]() mutable {
    x.acquire();
    x.release();
    done.store(true);
  });
  // The counter's updates wake the wait, which then checks whether the other thread is done
  x.get_condition().wait(x.get_condition_lock(), [&done] { return done.load(); });
  other.join();
  x.remove();
  EXPECT_THROW(x.wait(), krpc::StreamError);
  x.release();
}
