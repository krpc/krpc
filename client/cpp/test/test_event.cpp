#include <gtest/gtest-message.h>
#include <gtest/gtest-test-part.h>

#include <atomic>
#include <chrono>  // NOLINT(build/c++11)
#include <cstdint>
#include <exception>
#include <functional>
#include <future>
#include <memory>
#include <mutex>  // NOLINT(build/c++11)
#include <string>
#include <thread>  // NOLINT(build/c++11)
// IWYU pragma: no_include <ext/alloc_traits.h>

#include "gtest/gtest.h"
#include "krpc/client.hpp"
#include "krpc/decoder.hpp"
#include "krpc/encoder.hpp"
#include "krpc/error.hpp"
#include "krpc/event.hpp"
#include "krpc/krpc.pb.hpp"
#include "krpc/services/krpc.hpp"
#include "krpc/stream.hpp"
#include "krpc/stream_impl.hpp"
#include "server_test.hpp"
#include "services/test_service.hpp"

class test_event : public server_test {};

// An event from TestService.OnTimer, and the state of its stream, which counts its firings
struct timer_event {
  krpc::Event event;
  std::shared_ptr<krpc::StreamImpl> stream;
};

static timer_event on_timer(krpc::Client* client, uint32_t milliseconds, uint32_t repeats) {
  auto result =
      client->invoke("TestService", "OnTimer",
                     {krpc::encoder::encode_arg(milliseconds), krpc::encoder::encode_arg(repeats)},
                     krpc::Client::Returns::event);
  krpc::schema::Event message;
  krpc::decoder::decode(message, result.value(), client);
  timer_event timer{krpc::Event(client, message), client->get_stream(message.stream().id())};
  return timer;
}

TEST_F(test_event, test_event) {
  auto event = test_service.on_timer(200);
  event.acquire();
  auto start_time = std::chrono::high_resolution_clock::now();
  ASSERT_TRUE(event.wait());
  auto end_time = std::chrono::high_resolution_clock::now();
  std::chrono::duration<double> duration = end_time - start_time;
  // Lower bound is a correctness check (the event must not fire before its
  // timer); the upper bound is a generous hang detector, kept loose so the
  // test does not flake under parallel load.
  ASSERT_GT(duration.count(), 0.15);
  ASSERT_LT(duration.count(), 2);
  event.release();
}

TEST_F(test_event, test_event_timeout_short) {
  auto event = test_service.on_timer(200);
  event.acquire();
  auto start_time = std::chrono::high_resolution_clock::now();
  ASSERT_FALSE(event.wait(0.1));
  auto end_time = std::chrono::high_resolution_clock::now();
  std::chrono::duration<double> duration = end_time - start_time;
  ASSERT_LT(duration.count(), 0.2);
  ASSERT_GT(duration.count(), 0.08);
  ASSERT_TRUE(event.wait());
  event.release();
}

TEST_F(test_event, test_event_timeout_long) {
  auto event = test_service.on_timer(200);
  event.acquire();
  auto start_time = std::chrono::high_resolution_clock::now();
  ASSERT_TRUE(event.wait(1));
  auto end_time = std::chrono::high_resolution_clock::now();
  std::chrono::duration<double> duration = end_time - start_time;
  ASSERT_GT(duration.count(), 0.15);
  ASSERT_LT(duration.count(), 2);
  event.release();
}

TEST_F(test_event, test_event_loop) {
  auto start_time = std::chrono::high_resolution_clock::now();
  auto event = test_service.on_timer(200, 5);
  event.acquire();
  auto repeat = 0;
  while (true) {
    ASSERT_TRUE(event.wait());
    auto end_time = std::chrono::high_resolution_clock::now();
    std::chrono::duration<double> duration = end_time - start_time;
    repeat++;
    ASSERT_GT(duration.count(), 0.2 * repeat - 0.05);
    ASSERT_LT(duration.count(), 0.2 * repeat + 2);
    if (repeat == 5) break;
  }
  event.release();
}

TEST_F(test_event, test_event_callback) {
  auto event = test_service.on_timer(200);
  std::atomic_flag called;
  called.test_and_set();
  event.add_callback([&called]() { called.clear(); });
  auto start_time = std::chrono::high_resolution_clock::now();
  while (called.test_and_set()) {
  }
  auto end_time = std::chrono::high_resolution_clock::now();
  std::chrono::duration<double> duration = end_time - start_time;
  ASSERT_GT(duration.count(), 0.15);
  ASSERT_LT(duration.count(), 2);
}

TEST_F(test_event, test_event_callback_timeout) {
  auto event = test_service.on_timer(1000);
  std::atomic_flag called;
  called.test_and_set();
  event.add_callback([&called]() { called.clear(); });
  auto start_time = std::chrono::high_resolution_clock::now();
  while (called.test_and_set()) {
    auto time = std::chrono::high_resolution_clock::now();
    std::chrono::duration<double> duration = time - start_time;
    if (duration.count() >= 0.1) break;
  }
  auto end_time = std::chrono::high_resolution_clock::now();
  std::chrono::duration<double> duration = end_time - start_time;
  // Loop breaks on its own 0.1s timeout, before the 1000ms event fires.
  ASSERT_GT(duration.count(), 0.05);
  ASSERT_LT(duration.count(), 1);
  ASSERT_TRUE(called.test_and_set());
}

TEST_F(test_event, test_event_callback_loop) {
  auto event = test_service.on_timer(200, 5);
  std::atomic<int> count(0);
  event.add_callback([&count]() { count++; });
  auto start_time = std::chrono::high_resolution_clock::now();
  while (count < 5) {
  }
  auto end_time = std::chrono::high_resolution_clock::now();
  std::chrono::duration<double> duration = end_time - start_time;
  ASSERT_GT(duration.count(), 0.9);
  ASSERT_LT(duration.count(), 3);
  ASSERT_EQ(count, 5);
}

// A callback outlives the event object it was added through
TEST_F(test_event, test_callback_after_the_event_is_moved_and_destroyed) {
  std::atomic<int> calls(0);
  krpc::Event kept;
  {
    auto event = test_service.on_timer(200);
    event.add_callback([&calls] { calls++; });
    kept = event;
    auto moved = std::move(event);
  }
  ASSERT_TRUE(eventually([&calls] { return calls.load() > 0; }));
}

TEST_F(test_event, test_event_remove_callback) {
  auto event = test_service.on_timer(200);
  std::atomic_flag called1;
  called1.test_and_set();
  std::atomic_flag called2;
  called2.test_and_set();
  event.add_callback([&called1]() { called1.clear(); });
  auto called2_tag = event.add_callback([&called2]() { called2.clear(); });
  event.remove_callback(called2_tag);
  auto start_time = std::chrono::high_resolution_clock::now();
  while (called1.test_and_set()) {
  }
  auto end_time = std::chrono::high_resolution_clock::now();
  std::chrono::duration<double> duration = end_time - start_time;
  ASSERT_GT(duration.count(), 0.15);
  ASSERT_LT(duration.count(), 2);
  ASSERT_TRUE(called2.test_and_set());
}

TEST_F(test_event, test_custom_event) {
  typedef krpc::services::KRPC::Expression Expr;
  auto counter = Expr::call(conn, test_service.counter_call("test_event.test_custom_event"));
  auto expr = Expr::equal(
      conn, Expr::multiply(conn, Expr::constant_int(conn, 2), Expr::constant_int(conn, 10)),
      counter);
  auto event = krpc.add_event(expr);
  event.acquire();
  event.wait();
  // The event fires when the server-side counter reaches 20. The counter
  // increments on every expression evaluation, so the value read back is >= 21
  // (20 at the trigger, plus this read); the exact figure depends on how many
  // more times the server evaluated the expression first, so assert the lower
  // bound to avoid flaking under load.
  ASSERT_GE(test_service.counter("test_event.test_custom_event"), 21);
  event.release();
}

TEST_F(test_event, test_default_constructable) {
  krpc::Event e;
  ASSERT_FALSE(e);
}

TEST_F(test_event, test_assignable) {
  krpc::Event e;
  ASSERT_FALSE(e);
  e = test_service.on_timer(100);
  ASSERT_TRUE(e);
  e.acquire();
  ASSERT_TRUE(e.wait());
  e.release();
}

TEST_F(test_event, test_equality) {
  krpc::Event e1;
  krpc::Event e2;
  ASSERT_TRUE(e1 == e2);
  ASSERT_FALSE(e1 != e2);
  ASSERT_TRUE(e2 == e1);
  ASSERT_FALSE(e2 != e1);
  e1 = test_service.on_timer(100);
  ASSERT_FALSE(e1 == e2);
  ASSERT_TRUE(e1 != e2);
  ASSERT_FALSE(e2 == e1);
  ASSERT_TRUE(e2 != e1);
  e2 = e1;
  ASSERT_TRUE(e1 == e2);
  ASSERT_FALSE(e1 != e2);
  ASSERT_TRUE(e2 == e1);
  ASSERT_FALSE(e2 != e1);
}

TEST_F(test_event, test_server_removed_event) {
  auto event = test_service.on_timer(100);
  std::atomic<int> ended(0);
  std::atomic_bool removed(false);
  event.add_end_callback([&ended, &removed](const std::exception_ptr& ending) {
    try {
      std::rethrow_exception(ending);
    } catch (const krpc::services::KRPC::StreamRemovedException&) {
      removed.store(true);
    } catch (...) {  // NOLINT(bugprone-empty-catch): the flag stays false
    }
    ended++;
  });
  event.acquire();
  ASSERT_TRUE(event.wait());
  EXPECT_THROW(event.wait(), krpc::services::KRPC::StreamRemovedException);
  EXPECT_THROW(event.wait(0.1), krpc::services::KRPC::StreamRemovedException);
  event.release();
  // The end callbacks run after the waiters wake
  ASSERT_TRUE(eventually([&ended] { return ended.load() == 1; }));
  ASSERT_TRUE(removed.load());
}

TEST_F(test_event, test_firing_before_wait) {
  auto event = test_service.on_timer(100);
  std::this_thread::sleep_for(std::chrono::milliseconds(500));
  event.acquire();
  auto start_time = std::chrono::steady_clock::now();
  ASSERT_TRUE(event.wait(1));
  std::chrono::duration<double> duration = std::chrono::steady_clock::now() - start_time;
  ASSERT_LT(duration.count(), 0.1);
  event.release();
}

TEST_F(test_event, test_firings_coalesce) {
  auto event = test_service.on_timer(50, 3);
  std::this_thread::sleep_for(std::chrono::milliseconds(500));
  event.acquire();
  ASSERT_TRUE(event.wait());
  EXPECT_THROW(event.wait(), krpc::services::KRPC::StreamRemovedException);
  event.release();
}

// Each thread waits on its own copy of the event object, as each has its own lock
TEST_F(test_event, test_two_threads_wait_on_one_event) {
  auto event = test_service.on_timer(1000);
  std::atomic<int> woke(0);
  auto wait = [&woke](krpc::Event copy) {
    copy.acquire();
    if (copy.wait(5)) woke++;
    copy.release();
  };
  std::thread first(wait, event);
  std::thread second(wait, event);
  first.join();
  second.join();
  ASSERT_EQ(2, woke.load());
}

TEST_F(test_event, test_error_ends_the_event) {
  typedef krpc::services::KRPC::Expression Expr;
  auto expr =
      Expr::equal(conn, Expr::call(conn, test_service.throw_invalid_operation_exception_call()),
                  Expr::constant_int(conn, 0));
  auto event = krpc.add_event(expr);
  event.acquire();
  EXPECT_THROW(event.wait(), krpc::services::KRPC::InvalidOperationException);
  EXPECT_THROW(event.wait(), krpc::services::KRPC::InvalidOperationException);
  event.release();
}

TEST_F(test_event, test_remove_wakes_an_event_wait) {
  auto event = test_service.on_timer(10000);
  krpc::Event copy = event;
  std::atomic_bool waiting(false);
  std::thread waiter([&copy, &waiting] {
    copy.acquire();
    waiting.store(true);
    EXPECT_THROW(copy.wait(), krpc::StreamError);
    copy.release();
  });
  // The waiter holds the condition until it enters its wait, so the removal below cannot
  // notify before then
  ASSERT_TRUE(eventually([&waiting] { return waiting.load(); }));
  event.remove();
  waiter.join();
}

TEST_F(test_event, test_remove_while_holding_condition) {
  auto event = test_service.on_timer(10000);
  event.acquire();
  event.remove();
  EXPECT_THROW(event.wait(), krpc::StreamError);
  event.release();
}

TEST_F(test_event, test_remove_after_locking_the_condition_lock) {
  auto event = test_service.on_timer(10000);
  std::promise<void> removed;
  auto done = removed.get_future();
  std::thread remover([event, removed = std::move(removed)]() mutable {
    event.get_condition_lock().lock();
    event.remove();
    event.get_condition_lock().unlock();
    removed.set_value();
  });
  if (done.wait_for(std::chrono::seconds(10)) != std::future_status::ready) {
    remover.detach();
    FAIL() << "remove did not return";
  }
  remover.join();
  EXPECT_THROW(event.wait(), krpc::StreamError);
}

TEST_F(test_event, test_remove_through_another_handle_while_holding_condition) {
  auto event = test_service.on_timer(10000);
  event.acquire();
  event.stream().remove();
  EXPECT_THROW(event.wait(), krpc::StreamError);
  event.release();
}

TEST_F(test_event, test_connection_failure_wakes_an_event_wait) {
  std::function<void()> shutdown;
  auto client = connect_client(&shutdown);
  krpc::services::TestService service(client.get());
  auto event = service.on_timer(10000);
  std::atomic_bool waiting(false);
  std::thread waiter([&event, &waiting] {
    event.acquire();
    waiting.store(true);
    EXPECT_THROW(event.wait(), krpc::ConnectionError);
    event.release();
  });
  ASSERT_TRUE(eventually([&waiting] { return waiting.load(); }));
  // Stands in for the server closing the connection
  shutdown();
  waiter.join();
}

TEST_F(test_event, test_close_wakes_an_event_wait) {
  auto client = connect_client();
  krpc::services::TestService service(client.get());
  auto event = service.on_timer(10000);
  std::atomic_bool waiting(false);
  std::thread waiter([&event, &waiting] {
    event.acquire();
    waiting.store(true);
    EXPECT_THROW(event.wait(), krpc::ConnectionError);
    event.release();
  });
  ASSERT_TRUE(eventually([&waiting] { return waiting.load(); }));
  client.reset();
  waiter.join();
}

// The timers fire a second time, so the event has not ended when the callback is added
TEST_F(test_event, test_callback_added_after_a_wait_is_not_called) {
  auto event = test_service.on_timer(500, 2);
  ASSERT_TRUE(event.wait());
  std::atomic<int> calls(0);
  event.add_callback([&calls] { calls++; });
  std::this_thread::sleep_for(std::chrono::milliseconds(200));
  ASSERT_EQ(0, calls.load());
}

TEST_F(test_event, test_callback_added_after_an_unseen_firing_is_called) {
  auto timer = on_timer(&conn, 500, 2);
  ASSERT_TRUE(eventually([&timer] { return timer.stream->get_fired() == 1; }));
  std::atomic<int> calls(0);
  timer.event.add_callback([&calls] { calls++; });
  ASSERT_EQ(1, calls.load());
  std::this_thread::sleep_for(std::chrono::milliseconds(200));
  ASSERT_EQ(1, calls.load());
}

// A callback on the event's stream is passed every value, and gives no firing to the event
TEST_F(test_event, test_callback_added_after_a_firing_given_to_a_stream_callback_is_called) {
  auto timer = on_timer(&conn, 500, 2);
  auto stream = timer.event.stream();
  stream.add_callback([](bool) {});
  ASSERT_TRUE(eventually([&timer] { return timer.stream->get_fired() == 1; }));
  std::atomic<int> calls(0);
  timer.event.add_callback([&calls] { calls++; });
  ASSERT_EQ(1, calls.load());
}

// The firing is still given to a wait
TEST_F(test_event, test_callback_added_after_the_event_ends_is_not_called) {
  auto timer = on_timer(&conn, 100, 1);
  ASSERT_TRUE(eventually([&timer] { return timer.stream->has_ended(); }));
  ASSERT_EQ(1, timer.stream->get_fired());
  std::atomic<int> calls(0);
  timer.event.add_callback([&calls] { calls++; });
  ASSERT_EQ(0, calls.load());
  ASSERT_TRUE(timer.event.wait());
  EXPECT_THROW(timer.event.wait(), krpc::services::KRPC::StreamRemovedException);
}

// A firing given to an event callback after a later value is still given, as an event callback
// is gated on the number of firings
TEST_F(test_event, test_callback_gets_a_firing_passed_after_a_later_value) {
  krpc::StreamImpl stream(&conn, 1, std::make_shared<std::recursive_mutex>());
  std::atomic<int> calls(0);
  stream.add_event_callback(
      [&calls](const std::string& data, bool is_null) {
        if (!is_null && data == "\x01") calls++;
      },
      0);
  krpc::StreamImpl::Callbacks firing;
  krpc::StreamImpl::Callbacks later;
  uint64_t firing_version = 0;
  uint64_t firing_fired = 0;
  uint64_t later_version = 0;
  uint64_t later_fired = 0;
  {
    std::lock_guard<std::mutex> held(stream.get_condition_mutex());
    ASSERT_TRUE(stream.update_and_notify("\x01", false, &firing, &firing_version, &firing_fired));
    ASSERT_TRUE(stream.update_and_notify("\x00", false, &later, &later_version, &later_fired));
  }
  stream.call_callbacks(later, "\x00", false, later_version, later_fired);
  stream.call_callbacks(firing, "\x01", false, firing_version, firing_fired);
  ASSERT_EQ(1, calls.load());
}

// A raw call that returns an event has the event's id noted, so a firing and an ending sent
// before the event is wrapped are kept, though a later id has been noted since
TEST_F(test_event, test_raw_invoke_of_an_event_notes_its_id) {
  auto result = conn.invoke("TestService", "OnTimer",
                            {krpc::encoder::encode_arg(static_cast<uint32_t>(100)),
                             krpc::encoder::encode_arg(static_cast<uint32_t>(1))});
  krpc::schema::Event message;
  krpc::decoder::decode(message, result.value(), &conn);
  auto later = test_service.int32_to_string_stream(42);
  std::this_thread::sleep_for(std::chrono::milliseconds(500));
  krpc::Event event(&conn, message);
  ASSERT_TRUE(event.wait(0));
  EXPECT_THROW(event.wait(), krpc::services::KRPC::StreamRemovedException);
}

TEST_F(test_event, test_callback_added_after_a_firing_given_to_another_is_not_called) {
  auto event = test_service.on_timer(500, 2);
  std::atomic<int> first(0);
  event.add_callback([&first] { first++; });
  ASSERT_TRUE(eventually([&first] { return first.load() == 1; }));
  std::atomic<int> second(0);
  event.add_callback([&second] { second++; });
  std::this_thread::sleep_for(std::chrono::milliseconds(200));
  ASSERT_EQ(0, second.load());
}
