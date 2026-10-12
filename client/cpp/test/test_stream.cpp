#include <gtest/gtest-message.h>
#include <gtest/gtest-test-part.h>

#include <asio/io_context.hpp>
#include <asio/ip/address.hpp>
#include <asio/ip/tcp.hpp>
#include <atomic>
#include <chrono>  // NOLINT(build/c++11)
#include <cstddef>
#include <cstdint>
#include <exception>
#include <functional>
#include <future>
#include <memory>
#include <mutex>  // NOLINT(build/c++11)
#include <optional>
#include <string>
#include <thread>  // NOLINT(build/c++11)
#include <vector>
// IWYU pragma: no_include <ext/alloc_traits.h>

#include "gtest/gtest.h"
#include "krpc/client.hpp"
#include "krpc/encoder.hpp"
#include "krpc/error.hpp"
#include "krpc/services/krpc.hpp"
#include "krpc/stream.hpp"
#include "krpc/stream_impl.hpp"
#include "krpc/stream_manager.hpp"
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
  ASSERT_EQ("3.14159", x());
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

// The server stops sending updates for a stream removed by id after its handles are gone
TEST_F(test_stream, test_remove_stream_with_no_handles) {
  auto client = connect_client();
  krpc::services::TestService service(client.get());
  auto stream =
      client->add_stream(service.counter_call("test_stream.test_remove_stream_with_no_handles"));
  auto id = stream->get_id();
  stream.reset();
  client->remove_stream(id);
  std::atomic<int> updates(0);
  client->add_stream_update_callback([&updates] { updates++; });
  // An update sent before the removal can still arrive
  wait();
  updates.store(0);
  wait();
  ASSERT_EQ(0, updates.load());
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

TEST_F(test_stream, test_read_straight_after_add) {
  auto x = test_service.int32_to_string_stream(48);
  ASSERT_EQ("48", x());
}

TEST_F(test_stream, test_callback_added_after_the_first_value) {
  auto x = test_service.int32_to_string_stream(49);
  ASSERT_EQ("49", x());
  std::vector<std::string> values;
  x.add_callback([&values](const std::string& value) { values.push_back(value); });
  ASSERT_EQ(std::vector<std::string>{"49"}, values);
}

// A stream manager on a connection to a server that sends nothing, so the only results it
// applies are those a test passes to update
class idle_stream_manager {
 public:
  explicit idle_stream_manager(krpc::Client* client)
      : acceptor(io_context, asio::ip::tcp::endpoint(asio::ip::make_address("127.0.0.1"), 0)),
        socket(io_context) {
    auto connection =
        std::make_shared<krpc::Connection>("127.0.0.1", acceptor.local_endpoint().port());
    connection->connect();
    acceptor.accept(socket);
    manager = krpc::StreamManager::create(client, connection);
  }
  asio::io_context io_context;
  asio::ip::tcp::acceptor acceptor;
  asio::ip::tcp::socket socket;
  std::shared_ptr<krpc::StreamManager> manager;
};

static krpc::schema::ProcedureResult result_value(const std::string& value) {
  krpc::schema::ProcedureResult result;
  result.set_value(value);
  return result;
}

TEST_F(test_stream, test_result_before_the_add_reply_is_kept) {
  idle_stream_manager idle(&conn);
  auto& manager = *idle.manager;
  manager.note_issued(3);
  // One for an id noted but not registered, and one for an id not yet noted
  manager.update(3, result_value("\x2a"));
  manager.update(4, result_value("\x2b"));
  bool is_null = false;
  ASSERT_EQ("\x2a", manager.get_stream(3)->get_data(&is_null));
  ASSERT_EQ("\x2b", manager.get_stream(4)->get_data(&is_null));
}

// Only the latest value is kept, as a new stream has no callbacks to give the others to
TEST_F(test_stream, test_kept_value_is_the_latest) {
  idle_stream_manager idle(&conn);
  auto& manager = *idle.manager;
  manager.note_issued(3);
  manager.update(3, result_value("\x2a"));
  manager.update(3, result_value("\x2b"));
  auto stream = manager.get_stream(3);
  bool is_null = false;
  ASSERT_EQ("\x2b", stream->get_data(&is_null));
  ASSERT_EQ(1, stream->get_version());
}

TEST_F(test_stream, test_result_after_a_kept_ending_is_dropped) {
  idle_stream_manager idle(&conn);
  auto& manager = *idle.manager;
  manager.note_issued(3);
  manager.update(3, result_value("\x2a"));
  krpc::schema::ProcedureResult error;
  error.mutable_error()->set_description("ended");
  manager.update(3, error);
  manager.update(3, result_value("\x2b"));
  auto stream = manager.get_stream(3);
  ASSERT_TRUE(stream->has_ended());
  ASSERT_EQ(1, stream->get_version());
  bool is_null = false;
  ASSERT_THROW(stream->get_data(&is_null), krpc::RPCError);
}

// The first call of a new callback is skipped if an update has already passed it a later value
TEST_F(test_stream, test_callback_skips_an_older_value) {
  krpc::StreamImpl stream(&conn, 1, std::make_shared<std::recursive_mutex>());
  krpc::StreamImpl::Callbacks callbacks;
  uint64_t version = 0;
  uint64_t fired = 0;
  {
    std::lock_guard<std::mutex> held(stream.get_condition_mutex());
    ASSERT_TRUE(stream.update_and_notify("a", false, &callbacks, &version, &fired));
  }
  std::vector<std::string> values;
  stream.add_callback([&values](const std::string& value, bool) { values.push_back(value); });
  {
    std::lock_guard<std::mutex> held(stream.get_condition_mutex());
    ASSERT_TRUE(stream.update_and_notify("b", false, &callbacks, &version, &fired));
  }
  stream.call_callbacks(callbacks, "b", false, version, fired);
  // A first call with the earlier value, made after the update
  stream.call_callbacks(callbacks, "a", false, 1, fired);
  ASSERT_EQ((std::vector<std::string>{"a", "b"}), values);
}

TEST_F(test_stream, test_result_for_an_ended_stream_is_dropped) {
  idle_stream_manager idle(&conn);
  auto& manager = *idle.manager;
  manager.note_issued(5);
  manager.get_stream(5);
  manager.update(2, result_value("\x2a"));
  ASSERT_FALSE(manager.get_stream(2)->has_updated());
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
  conn.wait_for_stream_update();
  auto count = x();
  conn.wait_for_stream_update(0);
  ASSERT_EQ(count, x());
  conn.release_stream_update();
}

TEST_F(test_stream, test_wait_update_timeout_long) {
  auto x = test_service.counter_stream("test_stream.test_wait_update_timeout_long", 10);
  conn.acquire_stream_update();
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

// Counts consecutive values passed to a callback. The stream can update before the callback
// is added, so counting starts from the first value passed.
class callback_counter {
 public:
  callback_counter() {
    error.test_and_set();
    stop.test_and_set();
  }
  void operator()(int x) {
    if (count > 0 && last + 1 != x) {
      error.clear();
      stop.clear();
    }
    last = x;
    count++;
    if (count > 5) stop.clear();
  }
  std::atomic<int> last{0};
  std::atomic<int> count{0};
  std::atomic_flag error;
  std::atomic_flag stop;
};

TEST_F(test_stream, test_callback) {
  callback_counter counter;
  auto x = test_service.counter_stream("test_stream.test_callback", 10);
  x.add_callback([&counter](int value) { counter(value); });
  while (counter.stop.test_and_set()) {
  }
  x.remove();
  ASSERT_TRUE(counter.error.test_and_set());
}

// A callback that throws must not escape the update thread: an exception leaving it calls
// std::terminate and ends the process, not merely the stream updates.
// A callback outlives the stream object it was added through
TEST_F(test_stream, test_callback_after_the_handle_is_moved_and_destroyed) {
  std::atomic<int> moved_calls(0);
  std::atomic<int> destroyed_calls(0);
  auto x = test_service.counter_stream("test_stream.test_callback_after_the_handle_is_moved");
  {
    auto y = x;
    y.add_callback([&moved_calls](int) { moved_calls++; });
    auto z = std::move(y);
    auto w = x;
    w.add_callback([&destroyed_calls](int) { destroyed_calls++; });
  }
  moved_calls.store(0);
  destroyed_calls.store(0);
  ASSERT_TRUE(eventually([&] { return moved_calls.load() > 0 && destroyed_calls.load() > 0; }));
  x.remove();
}

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
  while (called1.test_and_set()) {
  }
  x.remove();
  ASSERT_TRUE(called2.test_and_set());
}

TEST_F(test_stream, test_update_callback) {
  std::atomic_flag stop;
  stop.test_and_set();

  auto callback = [&stop]() { stop.clear(); };

  conn.add_stream_update_callback(callback);
  auto x = test_service.counter_stream("test_stream.test_update_callback", 10);
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

  conn.add_stream_update_callback(callback1);
  auto callback2_tag = conn.add_stream_update_callback(callback2);
  conn.remove_stream_update_callback(callback2_tag);
  auto x = test_service.counter_stream("test_stream.test_remove_update_callback", 10);
  while (called1.test_and_set()) {
  }
  x.remove();
  ASSERT_TRUE(called2.test_and_set());
}

TEST_F(test_stream, test_rate) {
  callback_counter counter;
  auto x = test_service.counter_stream("test_stream.test_rate");
  x.set_rate(5);
  x.add_callback([&counter](int value) { counter(value); });
  auto start = std::chrono::system_clock::now();
  while (counter.stop.test_and_set()) {
  }
  auto end = std::chrono::system_clock::now();
  std::chrono::duration<double> elapsed = end - start;
  // Lower bound checks the rate limit was honoured; the upper bound is a
  // generous hang detector, kept loose so the test does not flake under
  // parallel load.
  ASSERT_GT(elapsed.count(), 1.0);
  ASSERT_LT(elapsed.count(), 3.0);
  x.remove();
  ASSERT_TRUE(counter.error.test_and_set());
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

// The update thread has values for the stream while the closing thread holds its condition
TEST_F(test_stream, test_close_while_holding_condition) {
  std::shared_ptr<krpc::Client> client = connect_client();
  krpc::services::TestService service(client.get());
  auto x = service.counter_stream("test_stream.test_close_while_holding_condition");
  ASSERT_GT(x(), 0);
  std::promise<void> closed;
  auto done = closed.get_future();
  // Owns what it uses, so that it can be left running if the close hangs
  std::thread closer([x, client = std::move(client), closed = std::move(closed)]() mutable {
    x.acquire();
    wait();
    client.reset();
    x.release();
    closed.set_value();
  });
  if (done.wait_for(std::chrono::seconds(10)) != std::future_status::ready) {
    closer.detach();
    FAIL() << "Closing the client while holding a stream's condition did not return";
  }
  closer.join();
  ASSERT_THROW(x(), krpc::ConnectionError);
}

// Streams keep working through a copy of the client once the client that connected is gone
TEST_F(test_stream, test_stream_after_the_original_client_is_destroyed) {
  auto client = connect_client();
  auto copy = std::make_unique<krpc::Client>(*client);
  client.reset();
  krpc::services::TestService service(copy.get());
  auto x = service.counter_stream("test_stream.test_stream_after_the_original_client_is_destroyed");
  auto value = x();
  ASSERT_TRUE(eventually([&x, value] { return x() > value; }));
  auto s = service.throw_invalid_operation_exception_stream();
  ASSERT_THROW(s(), krpc::services::KRPC::InvalidOperationException);
  x.remove();
}

// A thread holding the condition through one stream object can use another
TEST_F(test_stream, test_another_handle_while_holding_condition) {
  auto x = test_service.int32_to_string_stream(50);
  x.acquire();
  auto y = x;
  // Waits for the stream's first value
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

// A lock taken through the condition lock counts as a hold of the stream object
TEST_F(test_stream, test_wait_after_locking_the_condition_lock) {
  auto x = test_service.counter_stream("test_stream.test_wait_after_locking_the_condition_lock");
  ASSERT_GT(x(), 0);
  x.get_condition_lock().lock();
  ASSERT_TRUE(x.wait(5));
  x.release();
  {
    auto y = x;
    y.get_condition_lock().lock();
    ASSERT_TRUE(y.wait(5));
  }
  // Both locks are released, so another thread can take the condition
  std::thread other([x]() mutable {
    x.acquire();
    x.release();
  });
  other.join();
  x.remove();
}

// A callback is not called once its stream has ended
TEST_F(test_stream, test_callback_after_the_stream_ends) {
  krpc::StreamImpl stream(&conn, 1, std::make_shared<std::recursive_mutex>());
  std::vector<std::string> values;
  stream.add_callback([&values](const std::string& value, bool) { values.push_back(value); });
  krpc::StreamImpl::Callbacks callbacks;
  uint64_t version = 0;
  uint64_t fired = 0;
  {
    std::lock_guard<std::mutex> held(stream.get_condition_mutex());
    ASSERT_TRUE(stream.update_and_notify("a", false, &callbacks, &version, &fired));
  }
  stream.end(std::make_exception_ptr(krpc::StreamError("ended")));
  stream.call_callbacks(callbacks, "a", false, version, fired);
  ASSERT_TRUE(values.empty());
}

// A value that fails to decode is thrown to the reader, and the stream stays live
TEST_F(test_stream, test_value_that_fails_to_decode) {
  // An int32 is too short to decode as a double
  krpc::Stream<double> x(&conn,
                         test_service.counter_call("test_stream.test_value_that_fails_to_decode"));
  std::atomic<int> calls(0);
  std::atomic<int> ended(0);
  x.add_callback([&calls](double) { calls++; });
  x.add_end_callback([&ended](const std::exception_ptr&) { ended++; });
  ASSERT_THROW(x(), krpc::EncodingError);
  x.acquire();
  for (int i = 0; i < 3; i++) ASSERT_TRUE(x.wait(5));
  x.release();
  ASSERT_THROW(x(), krpc::EncodingError);
  ASSERT_EQ(0, calls.load());
  ASSERT_EQ(0, ended.load());
  x.remove();
}

// RemoveStream is sent for an id the client noted but never wrapped in a stream
TEST_F(test_stream, test_remove_a_stream_never_wrapped) {
  auto id =
      krpc.add_stream(test_service.counter_call("test_stream.test_remove_a_stream_never_wrapped"))
          .id();
  wait();
  conn.remove_stream(id);
  // A result already sent arrives before this
  wait();
  krpc::Stream<int32_t> x(&conn, id);
  x.acquire();
  ASSERT_FALSE(x.wait(0.5));
  x.release();
}

// Removing an id the client noted drops the result kept for it, however the removal is sent
TEST_F(test_stream, test_remove_drops_a_kept_result) {
  const std::string name = "test_stream.test_remove_drops_a_kept_result";
  std::vector<std::function<void(uint64_t)>> removals = {
      [this](uint64_t id) { krpc.remove_stream(id); },
      [this](uint64_t id) { conn.invoke("KRPC", "RemoveStream", {krpc::encoder::encode_arg(id)}); },
      [this](uint64_t id) {
        conn.invoke(conn.build_request("KRPC", "RemoveStream", {krpc::encoder::encode_arg(id)}));
      }};
  for (const auto& remove : removals) {
    auto id = krpc.add_stream(test_service.counter_call(name)).id();
    // A result already sent arrives before this
    wait();
    remove(id);
    wait();
    ASSERT_FALSE(conn.get_stream(id)->has_updated());
  }
}

// RemoveStream is sent for an id the manager has not noted, such as one another manager noted
TEST_F(test_stream, test_remove_a_stream_not_noted) {
  const std::string name = "test_stream.test_remove_a_stream_not_noted";
  auto id = krpc.add_stream(test_service.counter_call(name)).id();
  idle_stream_manager idle(&conn);
  idle.manager->remove_stream(id);
  wait();
  // Only these calls count once the stream is removed
  auto count = test_service.counter(name);
  wait();
  ASSERT_EQ(count + 1, test_service.counter(name));
}

// Runs fn on a thread of its own. Returns false if it does not return within ten seconds, and
// leaves it running, so fn owns what it uses. A std::function is not used, as moving one can copy
// what it holds.
template <typename F>
static bool returns(F fn) {
  std::promise<void> returned;
  auto done = returned.get_future();
  std::thread thread([fn = std::move(fn), returned = std::move(returned)]() mutable {
    fn();
    returned.set_value();
  });
  if (done.wait_for(std::chrono::seconds(10)) != std::future_status::ready) {
    thread.detach();
    return false;
  }
  thread.join();
  return true;
}

// A thread holding one stream's condition while it waits on another lets the first go once the
// other ends
template <typename F>
static void hold_one_wait_on_another(krpc::Stream<int32_t> held, krpc::Stream<std::string> waited,
                                     F close) {
  std::atomic_bool waiting(false);
  std::thread waiter([held, waited, &waiting]() mutable {
    held.acquire();
    waiting.store(true);
    EXPECT_THROW(waited.wait(), krpc::ConnectionError);
    held.release();
  });
  ASSERT_TRUE(server_test::eventually([&waiting] { return waiting.load(); }));
  ASSERT_TRUE(returns(std::move(close)));
  waiter.join();
  ASSERT_TRUE(server_test::eventually([held]() mutable {
    try {
      held();
    } catch (const krpc::ConnectionError&) {
      return true;
    }
    return false;
  }));
}

TEST_F(test_stream, test_close_while_a_thread_holds_one_stream_and_waits_on_another) {
  std::shared_ptr<krpc::Client> client = connect_client();
  krpc::services::TestService service(client.get());
  auto held = service.counter_stream("test_stream.test_close_while_a_thread_holds_one_stream");
  auto waited = service.int32_to_string_stream(52);
  ASSERT_EQ("52", waited());
  // The close is given the last copy of the client
  hold_one_wait_on_another(held, waited,
                           [client = std::move(client)]() mutable { client.reset(); });
}

TEST_F(test_stream, test_connection_failure_while_a_thread_holds_one_stream_and_waits_on_another) {
  std::function<void()> shutdown;
  auto client = connect_client(&shutdown);
  krpc::services::TestService service(client.get());
  auto held = service.counter_stream("test_stream.test_connection_failure_while_a_thread_holds");
  auto waited = service.int32_to_string_stream(53);
  ASSERT_EQ("53", waited());
  hold_one_wait_on_another(held, waited, shutdown);
}

// Closing from a callback on the update thread does not wait for that thread
TEST_F(test_stream, test_remove_after_locking_the_condition_lock) {
  auto x = test_service.int32_to_string_stream(42);
  ASSERT_EQ("42", x());
  ASSERT_TRUE(returns([x]() mutable {
    x.get_condition_lock().lock();
    x.remove();
    x.get_condition_lock().unlock();
  }));
  ASSERT_THROW(x(), krpc::StreamError);
}

// The thread adding a callback holds a condition that the callback, running on the update
// thread with a later value, waits for. The first call is skipped rather than waiting for it.
TEST_F(test_stream, test_add_callback_while_holding_a_condition_the_callback_waits_for) {
  auto held = test_service.int32_to_string_stream(42);
  auto x = test_service.counter_stream(
      "test_stream.test_add_callback_while_holding_a_condition_the_callback_waits_for");
  ASSERT_EQ("42", held());
  x();
  ASSERT_TRUE(returns([held, x]() mutable {
    for (int i = 0; i < 200; i++) {
      held.acquire();
      auto tag = x.add_callback([held](int32_t) mutable {
        auto handle = held;
        handle.acquire();
        handle.release();
      });
      held.release();
      x.remove_callback(tag);
    }
  }));
}

TEST_F(test_stream, test_close_from_a_callback) {
  auto client = connect_client();
  krpc::services::TestService service(client.get());
  auto x = service.counter_stream("test_stream.test_close_from_a_callback");
  std::atomic_bool closed(false);
  x.add_callback([&client, &closed](int32_t) {
    if (closed.load()) return;
    client.reset();
    closed.store(true);
  });
  ASSERT_TRUE(eventually([&closed] { return closed.load(); }));
  ASSERT_THROW(x(), krpc::ConnectionError);
}

TEST_F(test_stream, test_close_from_an_end_callback) {
  auto client = connect_client();
  krpc::services::TestService service(client.get());
  auto x = service.counter_stream("test_stream.test_close_from_an_end_callback");
  // Ended by the server, on the update thread
  auto event = service.on_timer(100);
  std::atomic_bool closed(false);
  event.add_end_callback([&client, &closed](const std::exception_ptr&) {
    client.reset();
    closed.store(true);
  });
  ASSERT_TRUE(eventually([&closed] { return closed.load(); }));
  ASSERT_THROW(x(), krpc::ConnectionError);
}

// The closing thread holds a stream's condition that a callback on the update thread waits for
TEST_F(test_stream, test_close_while_holding_a_condition_a_callback_waits_for) {
  std::shared_ptr<krpc::Client> client = connect_client();
  krpc::services::TestService service(client.get());
  auto x = service.counter_stream("test_stream.test_close_while_holding_a_condition");
  auto y = service.int32_to_string_stream(54);
  ASSERT_EQ("54", y());
  auto blocked = std::make_shared<std::atomic_bool>(false);
  x.add_callback([y, blocked](int32_t) mutable {
    blocked->store(true);
    try {
      y.wait(0);
    } catch (const krpc::ConnectionError&) {  // NOLINT(bugprone-empty-catch): closed
    }
  });
  // The closer has the last copy of the client
  ASSERT_TRUE(returns([y, blocked, client = std::move(client)]() mutable {
    y.acquire();
    // The callback's next call waits for the condition
    blocked->store(false);
    while (!blocked->load()) std::this_thread::sleep_for(std::chrono::milliseconds(1));
    wait();
    client.reset();
    y.release();
  }));
  ASSERT_THROW(y(), krpc::ConnectionError);
}
