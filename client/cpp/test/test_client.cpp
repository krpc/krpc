#include <gtest/gtest-message.h>
#include <gtest/gtest-test-part.h>

#include <atomic>
#include <chrono>
#include <exception>
#include <functional>
#include <future>
#include <memory>
#include <optional>
#include <string>
#include <thread>
#include <type_traits>
#include <utility>
#include <vector>

#include "gtest/gtest.h"
#include "krpc/client.hpp"
#include "krpc/connection.hpp"
#include "krpc/error.hpp"
#include "krpc/stream.hpp"
#include "server_test.hpp"
#include "services/test_service.hpp"

// A client can be declared before it connects, and moved into a container
static_assert(std::is_default_constructible_v<krpc::Client>);
static_assert(std::is_move_constructible_v<krpc::Client>);

// The C++ type of a parameter carries whether it can hold null, so a null at a parameter
// that is not nullable is rejected by the compiler rather than by the server
static_assert(std::is_invocable_v<decltype(&krpc::services::TestService::echo_test_object),
                                  krpc::services::TestService&, std::nullopt_t>);
static_assert(!std::is_invocable_v<decltype(&krpc::services::TestService::not_nullable_object),
                                   krpc::services::TestService&, std::nullopt_t>);

// Services, objects, streams and events hold the connection open
class test_client : public server_test {
 protected:
  // Expires once the client's state, and with it the connection, is released
  static std::weak_ptr<int> track(krpc::Client& client) {
    auto sentinel = std::make_shared<int>(0);
    client.add_exception_thrower("TestService", "Sentinel", [sentinel](const std::string&) {});
    return sentinel;
  }
};

TEST_F(test_client, test_an_object_outliving_its_client_keeps_working) {
  auto object = [this]() {
    auto client = connect_client();
    krpc::services::TestService service(*client);
    return service.create_test_object("jeb");
  }();
  ASSERT_EQ("value=jeb", object.get_value());
}

TEST_F(test_client, test_a_service_outliving_its_client_keeps_working) {
  auto service = [this]() {
    auto client = connect_client();
    return krpc::services::TestService(*client);
  }();
  ASSERT_EQ("value=jeb", service.create_test_object("jeb").get_value());
}

TEST_F(test_client, test_an_object_from_a_stream_outliving_its_client_keeps_working) {
  std::optional<krpc::services::TestService::TestClass> object;
  {
    auto client = connect_client();
    krpc::services::TestService service(*client);
    auto stream = service.create_test_object_stream("jeb");
    object = stream();
  }
  ASSERT_EQ("value=jeb", object->get_value());
}

TEST_F(test_client, test_an_object_from_a_client_in_a_vector_that_grows) {
  std::vector<krpc::Client> clients;
  clients.push_back(*connect_client());
  krpc::services::TestService service(clients.front());
  auto object = service.create_test_object("jeb");
  clients.resize(clients.capacity() + 1);
  ASSERT_EQ("value=jeb", object.get_value());
}

TEST_F(test_client, test_a_default_constructed_object_throws) {
  krpc::services::TestService::TestClass object;
  ASSERT_THROW(object.get_value(), krpc::ConnectionError);
}

TEST_F(test_client, test_a_service_constructed_from_a_temporary_client_holds_it) {
  krpc::services::TestService service(krpc::Client("", "localhost", get_port()));
  ASSERT_EQ("value=jeb", service.create_test_object("jeb").get_value());
}

TEST_F(test_client, test_an_object_constructed_from_a_client_and_an_id_uses_them) {
  auto client = connect_client();
  krpc::services::TestService service(*client);
  auto object = service.create_test_object("jeb");
  krpc::services::TestService::TestClass same(*client, krpc::get_object_id(object));
  ASSERT_EQ("value=jeb", same.get_value());
}

TEST_F(test_client, test_the_last_release_closes_the_connection) {
  auto client = connect_client();
  auto released = track(*client);
  krpc::services::TestService service(*client);
  auto object = service.create_test_object("jeb");
  auto stream = service.create_test_object_stream("jeb");
  stream();
  client.reset();
  ASSERT_FALSE(released.expired());
  stream = krpc::Stream<krpc::services::TestService::TestClass>();
  service = krpc::services::TestService(*connect_client());
  ASSERT_FALSE(released.expired());
  object = krpc::services::TestService::TestClass();
  ASSERT_TRUE(eventually([&released] { return released.expired(); }));
}

TEST_F(test_client, test_close_ends_every_handle) {
  auto client = connect_client();
  krpc::services::TestService service(*client);
  auto object = service.create_test_object("jeb");
  krpc::Client copy = *client;
  client->close();
  client->close();
  copy.close();
  ASSERT_THROW(object.get_value(), krpc::ConnectionError);
  ASSERT_THROW(service.create_test_object("jeb"), krpc::ConnectionError);
}

// A stream callback holding an object is released with the stream
TEST_F(test_client, test_a_stream_callback_holding_an_object_does_not_keep_the_connection) {
  auto client = connect_client();
  auto released = track(*client);
  krpc::services::TestService service(*client);
  auto object = service.create_test_object("jeb");
  {
    auto stream = service.counter_stream("test_client.stream_callback_object");
    stream.add_callback([object](int) { (void)object; });
    stream();
  }
  object = krpc::services::TestService::TestClass();
  service = krpc::services::TestService(*connect_client());
  client.reset();
  ASSERT_TRUE(eventually([&released] { return released.expired(); }));
}

// An update callback lives in the stream manager, so an object it holds forms a cycle
TEST_F(test_client, test_an_update_callback_holding_an_object_keeps_the_connection_until_close) {
  auto client = connect_client();
  auto released = track(*client);
  krpc::services::TestService service(*client);
  auto object = service.create_test_object("jeb");
  client->add_stream_update_callback([object]() { (void)object; });
  krpc::Client closer = *client;
  client.reset();
  service = krpc::services::TestService(*connect_client());
  object = krpc::services::TestService::TestClass();
  // Only the callback and closer hold the client
  closer.close();
  closer = krpc::Client();
  ASSERT_TRUE(eventually([&released] { return released.expired(); }));
}

// A stream callback holding its own stream is a cycle, which close breaks
TEST_F(test_client, test_a_stream_callback_holding_its_stream_keeps_the_connection_until_close) {
  auto client = connect_client();
  auto released = track(*client);
  {
    krpc::services::TestService service(*client);
    auto stream = std::make_shared<krpc::Stream<int>>(
        service.counter_stream("test_client.callback_holds_stream"));
    stream->add_callback([stream](int) { (void)stream; });
    (*stream)();
  }
  krpc::Client closer = *client;
  client.reset();
  closer.close();
  closer = krpc::Client();
  ASSERT_TRUE(eventually([&released] { return released.expired(); }));
}

// Work queued behind a slow callback holds the stream, so the callback outlives the handle by one
// run at most
TEST_F(test_client, test_a_slow_callback_does_not_keep_the_connection) {
  auto client = connect_client();
  auto released = track(*client);
  auto calls = std::make_shared<std::atomic<int>>(0);
  auto stream = std::make_unique<krpc::Stream<int>>(
      krpc::services::TestService(*client).counter_stream("test_client.slow_callback"));
  stream->add_callback([calls](int) {
    (*calls)++;
    std::this_thread::sleep_for(std::chrono::milliseconds(50));
  });
  // A backlog of updates builds up behind the callback
  std::this_thread::sleep_for(std::chrono::milliseconds(500));
  ASSERT_GT(calls->load(), 0);
  stream.reset();
  client.reset();
  int before = calls->load();
  ASSERT_TRUE(eventually([&released] { return released.expired(); }));
  ASSERT_LE(calls->load(), before + 1);
}

// A stream that has ended runs no more callbacks, so it destroys them
TEST_F(test_client, test_a_callback_that_removes_its_stream_does_not_keep_the_connection) {
  auto client = connect_client();
  auto released = track(*client);
  std::promise<void> ran;
  std::atomic_flag once = ATOMIC_FLAG_INIT;
  {
    auto stream = std::make_shared<krpc::Stream<int>>(
        krpc::services::TestService(*client).counter_stream("test_client.removes_itself"));
    stream->add_callback([stream, &ran, &once](int) {
      stream->remove();
      if (!once.test_and_set()) ran.set_value();
    });
  }
  ASSERT_EQ(std::future_status::ready, ran.get_future().wait_for(std::chrono::seconds(10)));
  client.reset();
  ASSERT_TRUE(eventually([&released] { return released.expired(); }));
}

TEST_F(test_client, test_a_callback_of_a_stream_the_server_ends_does_not_keep_the_connection) {
  auto client = connect_client();
  auto released = track(*client);
  std::promise<void> ended;
  {
    auto stream = std::make_shared<krpc::Stream<int>>(
        krpc::services::TestService(*client).throw_invalid_operation_exception_stream());
    stream->add_callback([stream](int) {});
    stream->add_end_callback([&ended](const std::exception_ptr&) { ended.set_value(); });
  }
  ASSERT_EQ(std::future_status::ready, ended.get_future().wait_for(std::chrono::seconds(10)));
  client.reset();
  ASSERT_TRUE(eventually([&released] { return released.expired(); }));
}

// A connection failure ends the streams, so it destroys their callbacks and the update callbacks
TEST_F(test_client, test_a_callback_does_not_keep_a_failed_connection) {
  auto connection = std::make_shared<krpc::Connection>("localhost", get_port());
  auto client = std::make_unique<krpc::Client>("C++ClientTest", connection);
  auto released = track(*client);
  std::promise<void> ended;
  {
    krpc::services::TestService service(*client);
    auto object = service.create_test_object("jeb");
    client->add_stream_update_callback([object] { (void)object; });
    auto stream = std::make_shared<krpc::Stream<int>>(
        service.counter_stream("test_client.failed_connection"));
    stream->add_callback([stream](int) { (void)stream; });
    stream->add_end_callback([&ended](const std::exception_ptr&) { ended.set_value(); });
  }
  connection->shutdown_both();
  ASSERT_EQ(std::future_status::ready, ended.get_future().wait_for(std::chrono::seconds(10)));
  client.reset();
  ASSERT_TRUE(eventually([&released] { return released.expired(); }));
}

// A callback that lands after the close has no later close to break its cycle
TEST_F(test_client, test_a_callback_added_after_close_does_not_keep_the_connection) {
  auto client = connect_client();
  auto released = track(*client);
  krpc::services::TestService service(*client);
  auto object = service.create_test_object("jeb");
  auto stream =
      std::make_shared<krpc::Stream<int>>(service.counter_stream("test_client.after_close"));
  client->close();
  client->add_stream_update_callback([object] { (void)object; });
  stream->add_callback([stream](int) { (void)stream; });
  client.reset();
  service = krpc::services::TestService(*connect_client());
  object = krpc::services::TestService::TestClass();
  stream.reset();
  ASSERT_TRUE(eventually([&released] { return released.expired(); }));
}

TEST_F(test_client, test_the_last_release_on_the_dispatcher_thread) {
  auto client = connect_client();
  auto released = track(*client);
  krpc::services::TestService service(*client);
  auto object =
      std::make_shared<krpc::services::TestService::TestClass>(service.create_test_object("jeb"));
  auto stream = std::make_shared<krpc::Stream<int>>(
      service.counter_stream("test_client.release_on_dispatcher"));
  std::promise<void> ran;
  std::atomic_flag once = ATOMIC_FLAG_INIT;
  auto holder = std::make_shared<std::shared_ptr<krpc::Stream<int>>>(stream);
  stream->add_callback([holder, object, &ran, &once](int) mutable {
    if (once.test_and_set()) return;
    // Releases the stream, the object and the callback's own copies from this thread
    object.reset();
    holder->reset();
    ran.set_value();
  });
  service = krpc::services::TestService(*connect_client());
  client.reset();
  object.reset();
  stream.reset();
  ASSERT_EQ(std::future_status::ready, ran.get_future().wait_for(std::chrono::seconds(10)));
  ASSERT_TRUE(eventually([&released] { return released.expired(); }));
}

// The handle holds the last copy of the client while the reader and dispatcher threads run
TEST_F(test_client, test_the_last_release_races_with_updates) {
  for (int i = 0; i < 100; i++) {
    auto client = connect_client();
    auto released = track(*client);
    krpc::services::TestService service(*client);
    auto stream = service.counter_stream("test_client.release_race");
    stream.add_callback([](int) {});
    stream();
    service = krpc::services::TestService(*connect_client());
    client.reset();
    auto other =
        std::thread([stream = std::move(stream)]() mutable { stream = krpc::Stream<int>(); });
    other.join();
    ASSERT_TRUE(eventually([&released] { return released.expired(); }));
  }
}
