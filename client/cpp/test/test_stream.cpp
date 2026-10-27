#include <gtest/gtest-message.h>
#include <gtest/gtest-test-part.h>

#include <chrono>  // NOLINT(build/c++11)
#include <functional>
#include <future>
#include <memory>
#include <string>
#include <thread>  // NOLINT(build/c++11)
#include <type_traits>

#include "gtest/gtest.h"
#include "krpc/client.hpp"
#include "krpc/error.hpp"
#include "krpc/services/krpc.hpp"
#include "krpc/stream.hpp"
#include "server_test.hpp"
#include "services/test_service.hpp"

class test_stream : public server_test {};

static_assert(std::is_default_constructible_v<krpc::Stream<int>>);
static_assert(std::is_copy_assignable_v<krpc::Stream<int>>);

TEST_F(test_stream, test_a_default_constructed_stream_is_false_until_assigned) {
  krpc::Stream<int> s;
  ASSERT_FALSE(s);
  s = test_service.counter_stream("test_stream.test_a_default_constructed_stream_is_false");
  ASSERT_TRUE(s);
  s.remove();
}

// Streams keep working through a copy of the client once the client that connected is gone
TEST_F(test_stream, test_a_copy_of_the_client_keeps_its_streams_once_the_original_is_destroyed) {
  auto client = connect_client();
  auto copy = std::make_unique<krpc::Client>(*client);
  client.reset();
  krpc::services::TestService service(copy.get());
  auto x = service.counter_stream("test_stream.test_a_copy_of_the_client_keeps_its_streams");
  auto value = x();
  ASSERT_TRUE(eventually([&x, value] { return x() > value; }));
  auto s = service.throw_invalid_operation_exception_stream();
  ASSERT_THROW(s(), krpc::services::KRPC::InvalidOperationException);
  x.remove();
}

// A lock taken through the condition lock is released by unlocking it, or by destroying the
// stream object
TEST_F(test_stream, test_the_condition_lock_is_released_by_unlocking_or_destroying_the_handle) {
  auto x = test_service.counter_stream("test_stream.test_the_condition_lock_is_released");
  ASSERT_GT(x(), 0);
  x.get_condition_lock().lock();
  ASSERT_TRUE(x.wait(5));
  x.get_condition_lock().unlock();
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

// The closing thread holds the update condition, so the update thread destroys the manager
// once it exits
TEST_F(test_stream, test_the_stream_manager_is_freed_after_a_close_under_the_update_condition) {
  auto sentinel = std::make_shared<int>(0);
  std::weak_ptr<int> manager_alive = sentinel;
  std::shared_ptr<krpc::Client> client = connect_client();
  // Kept by every copy of the client, including the stream manager's own
  client->add_exception_thrower("TestService", "Sentinel", [sentinel](const std::string&) {});
  sentinel.reset();
  ASSERT_TRUE(returns([client = std::move(client)]() mutable {
    client->get_stream_update_condition_lock().lock();
    client.reset();
  }));
  ASSERT_TRUE(eventually([&manager_alive] { return manager_alive.expired(); }));
}

TEST_F(test_stream, test_destroy_a_stream_object_while_another_thread_holds_its_lock) {
  auto x = test_service.int32_to_string_stream(57);
  ASSERT_EQ("57", x());
  auto copy = std::make_unique<krpc::Stream<std::string>>(x);
  std::promise<void> locked;
  std::promise<void> destroyed;
  std::thread holder([&copy, &locked, released = destroyed.get_future()] {
    auto& lock = copy->get_condition_lock();
    lock.lock();
    auto* mutex = lock.mutex();
    locked.set_value();
    released.wait();
    mutex->unlock();
  });
  locked.get_future().wait();
  copy.reset();
  destroyed.set_value();
  holder.join();
  x.remove();
  ASSERT_THROW(x(), krpc::StreamError);
}
