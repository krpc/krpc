#include <gtest/gtest-message.h>
#include <gtest/gtest-test-part.h>

#include <atomic>
#include <chrono>  // NOLINT(build/c++11)
// IWYU pragma: no_include <ext/alloc_traits.h>

#include "gtest/gtest.h"

#include "krpc/event.hpp"
#include "krpc/services/krpc.hpp"
#include "krpc/stream.hpp"

#include "server_test.hpp"
#include "services/test_service.hpp"

class test_event: public server_test {
};

TEST_F(test_event, test_event) {
  auto event = test_service.on_timer(200);
  event.acquire();
  auto start_time = std::chrono::high_resolution_clock::now();
  event.wait();
  auto end_time = std::chrono::high_resolution_clock::now();
  std::chrono::duration<double> duration = end_time - start_time;
  ASSERT_GT(duration.count(), 0.2);
  ASSERT_TRUE(event.stream()());
  event.release();
}

TEST_F(test_event, test_event_timeout_short) {
  auto event = test_service.on_timer(200);
  event.acquire();
  auto start_time = std::chrono::high_resolution_clock::now();
  event.wait(0.1);
  auto end_time = std::chrono::high_resolution_clock::now();
  std::chrono::duration<double> duration = end_time - start_time;
  ASSERT_LT(duration.count(), 0.2);
  ASSERT_GT(duration.count(), 0.1);
  ASSERT_FALSE(event.stream()());
  event.wait();
  ASSERT_TRUE(event.stream()());
  event.release();
}

TEST_F(test_event, test_event_timeout_long) {
  auto event = test_service.on_timer(200);
  event.acquire();
  auto start_time = std::chrono::high_resolution_clock::now();
  event.wait(1);
  auto end_time = std::chrono::high_resolution_clock::now();
  std::chrono::duration<double> duration = end_time - start_time;
  ASSERT_GT(duration.count(), 0.2);
  ASSERT_TRUE(event.stream()());
  event.release();
}

TEST_F(test_event, test_event_loop) {
  auto start_time = std::chrono::high_resolution_clock::now();
  auto event = test_service.on_timer(200, 5);
  event.acquire();
  auto repeat = 0;
  while (true) {
    event.wait();
    auto end_time = std::chrono::high_resolution_clock::now();
    std::chrono::duration<double> duration = end_time - start_time;
    ASSERT_TRUE(event.stream()());
    repeat++;
    ASSERT_GT(duration.count(), 0.2*repeat);
    if (repeat == 5)
      break;
  }
  event.release();
}

TEST_F(test_event, test_event_callback) {
  auto event = test_service.on_timer(200);
  std::atomic_flag called;
  called.test_and_set();
  event.add_callback([&called] () { called.clear(); });
  auto start_time = std::chrono::high_resolution_clock::now();
  event.start();
  while (called.test_and_set()) {
  }
  auto end_time = std::chrono::high_resolution_clock::now();
  std::chrono::duration<double> duration = end_time - start_time;
  ASSERT_GT(duration.count(), 0.2);
}

TEST_F(test_event, test_event_callback_timeout) {
  auto event = test_service.on_timer(1000);
  std::atomic_flag called;
  called.test_and_set();
  event.add_callback([&called] () { called.clear(); });
  auto start_time = std::chrono::high_resolution_clock::now();
  event.start();
  while (called.test_and_set()) {
    auto time = std::chrono::high_resolution_clock::now();
    std::chrono::duration<double> duration = time - start_time;
    if (duration.count() >= 0.1)
      break;
  }
  auto end_time = std::chrono::high_resolution_clock::now();
  std::chrono::duration<double> duration = end_time - start_time;
  ASSERT_GT(duration.count(), 0.1);
  ASSERT_TRUE(called.test_and_set());
}

TEST_F(test_event, test_event_callback_loop) {
  auto event = test_service.on_timer(200, 5);
  std::atomic<int> count(0);
  event.add_callback([&count] () { count++; });
  auto start_time = std::chrono::high_resolution_clock::now();
  event.start();
  while (count < 5) {
  }
  auto end_time = std::chrono::high_resolution_clock::now();
  std::chrono::duration<double> duration = end_time - start_time;
  ASSERT_GT(duration.count(), 1);
  ASSERT_EQ(count, 5);
}

TEST_F(test_event, test_custom_event) {
  typedef krpc::services::KRPC::Expression Expr;
  auto counter = Expr::call(conn, test_service.counter_call("test_event.test_custom_event"));
  auto expr = Expr::equal(conn,
    Expr::multiply(conn,
      Expr::constant_int(conn, 2),
      Expr::constant_int(conn, 10)),
    counter);
  auto event = krpc.add_event(expr);
  event.acquire();
  event.wait();
  ASSERT_EQ(test_service.counter("test_event.test_custom_event"), 21);
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
  e.wait();
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
