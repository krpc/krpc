#include "krpc/session.hpp"

#include <google/protobuf/io/coded_stream.h>

#include <chrono>  // NOLINT(build/c++11)
#include <cstdint>
#include <exception>
#include <iostream>
#include <memory>
#include <string>
#include <utility>
#include <vector>

#include "krpc/connection.hpp"
#include "krpc/encoder.hpp"
#include "krpc/error.hpp"
#include "krpc/stream_manager.hpp"

namespace krpc {

namespace {

// The wait for the server to close its side once ours is closed
constexpr std::chrono::milliseconds close_timeout = std::chrono::seconds(1);

std::exception_ptr connection_error(const std::string& text, const std::string& reason = "") {
  return std::make_exception_ptr(ConnectionError(text, "", reason));
}

}  // namespace

constexpr std::chrono::milliseconds Session::default_stall_timeout;

Session::Session(std::shared_ptr<Connection> connection,
                 std::function<schema::Request(uint64_t)> remove_stream_request,
                 std::chrono::milliseconds stall_timeout)
    : connection(std::move(connection)),
      remove_stream_request(std::move(remove_stream_request)),
      stall_timeout(stall_timeout) {}

Session::~Session() {
  {
    std::lock_guard<std::mutex> guard(queue_lock);
    queue_stopped = true;
  }
  queue_condition.notify_all();
  {
    std::lock_guard<std::mutex> guard(state_lock);
    ended = true;
  }
  idle.notify_all();
  // Destroyed on the dispatcher thread when a callback releases the last client
  for (auto* thread : {&reader, &dispatcher}) {
    if (!thread->joinable()) continue;
    if (thread->get_id() == std::this_thread::get_id())
      thread->detach();
    else
      thread->join();
  }
}

void Session::start(StreamManager* stream_manager, std::shared_ptr<void> keep_alive) {
  manager = stream_manager;
  // A session with no connection, as the tests use, has nothing to read
  if (connection) reader = std::thread(&Session::run_reader, this);
  // Releasing keep_alive can destroy this session, so nothing is touched after it
  dispatcher = std::thread([this, keep_alive = std::move(keep_alive)]() mutable {
    run_dispatcher();
    keep_alive.reset();
  });
}

void Session::dispatch(std::function<void()> work) {
  {
    std::lock_guard<std::mutex> guard(queue_lock);
    if (queue_stopped) {
      discarded.push_back(std::move(work));
      return;
    }
    queue.push_back(std::move(work));
  }
  queue_condition.notify_one();
}

bool Session::on_dispatcher_thread() const {
  return dispatcher.get_id() == std::this_thread::get_id();
}

void Session::run_dispatcher() {
  while (true) {
    std::function<void()> work;
    {
      std::unique_lock<std::mutex> guard(queue_lock);
      queue_condition.wait(guard, [this] { return queue_stopped || !queue.empty(); });
      if (queue.empty()) return;
      work = std::move(queue.front());
      queue.pop_front();
    }
    try {
      work();
    } catch (const std::exception& exn) {
      std::cerr << "kRPC: exception on the dispatcher thread: " << exn.what() << "\n";
    } catch (...) {
      std::cerr << "kRPC: unknown exception on the dispatcher thread\n";
    }
  }
}

schema::Response Session::call(schema::Request request, std::vector<Adds> adds) {
  if (!connection) throw ConnectionError("Not connected to a server");
  auto slot = std::make_shared<Slot>();
  for (auto add : adds)
    if (add != Adds::nothing) slot->adds_streams = true;
  slot->adds = std::move(adds);
  // The slot is added before the request is written, so a fast response finds it. A caller that
  // finds no thread reading becomes the reader in the same hold of the lock.
  bool is_reader;
  {
    std::lock_guard<std::mutex> guard(state_lock);
    if (error) std::rethrow_exception(error);
    request.set_id(next_id);
    next_id++;
    pending[request.id()] = slot;
    callers++;
    is_reader = !reading;
    if (is_reader) reading = true;
  }
  bool has_returned = false;
  try {
    write_request(&request);
    if (is_reader)
      has_returned = read_until(slot.get(), true);
    else
      await(slot.get());
  } catch (...) {
    std::lock_guard<std::mutex> guard(state_lock);
    // A reader discards the response when it arrives
    slot->abandoned = true;
    if (is_reader) reading = false;
    returned();
    throw;
  }
  if (!has_returned) {
    std::lock_guard<std::mutex> guard(state_lock);
    if (is_reader) reading = false;
    returned();
  }
  if (slot->error) std::rethrow_exception(slot->error);
  return std::move(slot->response);
}

void Session::returned() {
  callers--;
  if (waiting > 0)
    changed.notify_all();
  else if (callers == 0 && streams_open())
    idle.notify_one();
}

bool Session::streams_open() const { return manager != nullptr && manager->has_streams(); }

void Session::stream_added() {
  std::lock_guard<std::mutex> guard(state_lock);
  if (callers == 0) idle.notify_all();
}

bool Session::read_until(Slot* slot, bool returning) {
  try {
    schema::Message message;
    schema::Response parsed;
    // Handle every whole message already buffered before we stop reading. The reader thread
    // waits for data on the socket, which a message left in the buffer does not wake.
    while (!slot->done || (!slot->error && connection->message_buffered())) {
      auto [data, size] = receive_data();
      // A Message holding a response is the tag of field 2, then the response's size and bytes,
      // so the response is parsed where it lies
      auto* response = &parsed;
      auto length = size >= 2 ? static_cast<uint8_t>(data[1]) : 0;
      if (size >= 2 && data[0] == 0x12 && length < 0x80 && length == size - 2) {
        parse(data + 2, size - 2, response);
      } else {
        parse(data, size, &message);
        if (message.content_case() != schema::Message::kResponse) {
          handle(&message);
          continue;
        }
        response = message.mutable_response();
      }
      {
        std::lock_guard<std::mutex> guard(state_lock);
        auto it = pending.find(response->id());
        if (it != pending.end() && it->second.get() == slot && !slot->adds_streams) {
          pending.erase(it);
          slot->response.Swap(response);
          slot->done = true;
          continue;
        }
      }
      handle_response(response);
    }
  } catch (const Ended&) {
    // The slot has failed with the connection
    return false;
  }
  if (returning) {
    std::lock_guard<std::mutex> guard(state_lock);
    reading = false;
    returned();
  }
  return returning;
}

void Session::await(Slot* slot) {
  {
    std::unique_lock<std::mutex> guard(state_lock);
    while (!slot->done && reading) {
      waiting++;
      changed.wait(guard);
      waiting--;
    }
    if (slot->done) return;
    reading = true;
  }
  try {
    read_until(slot, false);
  } catch (...) {
    stop_reading();
    throw;
  }
  stop_reading();
}

void Session::stop_reading() {
  std::lock_guard<std::mutex> guard(state_lock);
  reading = false;
  if (waiting > 0)
    changed.notify_all();
  else if (callers == 0 && streams_open())
    idle.notify_one();
}

void Session::remove_stream(uint64_t id) {
  // The reader thread makes no writes, which can block until it reads
  if (reader.get_id() == std::this_thread::get_id()) {
    dispatch([this, id] { remove_stream(id); });
    return;
  }
  auto request = remove_stream_request(id);
  auto slot = std::make_shared<Slot>();
  slot->abandoned = true;
  try {
    send(&request, slot);
  } catch (const ConnectionError&) {  // NOLINT(bugprone-empty-catch)
    // The connection closing also removes the stream
  }
}

void Session::send(schema::Request* request, const std::shared_ptr<Slot>& slot) {
  {
    std::lock_guard<std::mutex> guard(state_lock);
    if (error) std::rethrow_exception(error);
    request->set_id(next_id);
    next_id++;
    pending[request->id()] = slot;
  }
  write_request(request);
}

void Session::write_request(schema::Request* request) {
  using google::protobuf::io::CodedOutputStream;
  try {
    std::lock_guard<std::timed_mutex> guard(write_lock);
    // The Message is written around the request, which saves building one
    auto size = request->ByteSizeLong();
    auto size_length = CodedOutputStream::VarintSize64(size);
    auto message_size = 1 + size_length + size;
    send_buffer.resize(CodedOutputStream::VarintSize64(message_size) + message_size);
    auto* data = reinterpret_cast<uint8_t*>(send_buffer.data());
    data = CodedOutputStream::WriteVarint64ToArray(message_size, data);
    *data++ = 0x0a;
    data = CodedOutputStream::WriteVarint64ToArray(size, data);
    (void)request->SerializeWithCachedSizesToArray(data);
    connection->send(send_buffer);
  } catch (const std::exception& exn) {
    try {
      end(connection_error(std::string("Connection failed: ") + exn.what()));
    } catch (const Ended&) {  // NOLINT(bugprone-empty-catch)
      // The request's slot has failed with the connection
    }
  }
}

void Session::write(const schema::Message& message) {
  connection->send(encoder::encode_message_with_size(message));
}

void Session::fail(const std::exception_ptr& exn) {
  std::lock_guard<std::mutex> guard(state_lock);
  fail_locked(exn);
}

void Session::fail_locked(const std::exception_ptr& exn) {
  if (!error) error = exn;
  for (auto& entry : pending) {
    entry.second->error = error;
    entry.second->done = true;
  }
  pending.clear();
  changed.notify_all();
}

void Session::close() {
  if (!connection) {
    fail(connection_error("Connection closed"));
    return;
  }
  bool failed;
  {
    std::lock_guard<std::mutex> guard(state_lock);
    closing = true;
    failed = static_cast<bool>(error);
    failure_error = error;
    // A response that arrives after the disconnect does not reach its call
    fail_locked(connection_error("Connection closed"));
  }
  if (!failed && write_lock.try_lock_for(close_timeout)) {
    std::lock_guard<std::timed_mutex> guard(write_lock, std::adopt_lock);
    try {
      schema::Message message;
      message.mutable_disconnect()->set_reason(schema::Disconnect::CLOSED);
      write(message);
      connection->shutdown_send();
    } catch (const std::exception&) {  // NOLINT(bugprone-empty-catch)
      // The connection can already have failed
    }
  }
  auto deadline = std::chrono::steady_clock::now() + close_timeout;
  bool to_drain;
  // A thread already reading finds the server's close itself
  {
    std::unique_lock<std::mutex> guard(state_lock);
    waiting++;
    changed.wait_until(guard, deadline, [this] { return !reading || ended; });
    waiting--;
    to_drain = !reading && !ended;
    if (to_drain) reading = true;
  }
  if (to_drain) drain(deadline);
  connection->shutdown_both();
  fail(connection_error("Connection closed"));
  {
    std::lock_guard<std::mutex> guard(state_lock);
    if (to_drain) reading = false;
    ended = true;
  }
  idle.notify_all();
  changed.notify_all();
}

void Session::drain(std::chrono::steady_clock::time_point deadline) {
  while (true) {
    auto remaining = std::chrono::duration_cast<std::chrono::milliseconds>(
        deadline - std::chrono::steady_clock::now());
    if (remaining <= std::chrono::milliseconds::zero() || !connection->discard(remaining)) return;
  }
}

void Session::join(bool join_dispatcher) {
  {
    std::lock_guard<std::mutex> guard(queue_lock);
    queue_stopped = true;
  }
  queue_condition.notify_all();
  auto current = std::this_thread::get_id();
  if (reader.joinable() && reader.get_id() != current) reader.join();
  if (join_dispatcher && dispatcher.joinable() && dispatcher.get_id() != current) dispatcher.join();
}

std::exception_ptr Session::failure() {
  std::lock_guard<std::mutex> guard(state_lock);
  return failure_error;
}

void Session::end(std::exception_ptr cause) {
  if (!cause) cause = connection_error("Connection closed");
  bool was_closing;
  bool first;
  // Failed in the same hold, so a thread that finds the connection ended finds its call failed
  {
    std::lock_guard<std::mutex> guard(state_lock);
    first = !ended;
    ended = true;
    was_closing = closing;
    if (first) {
      fail_locked(cause);
      // The first failure, which the calls throw
      cause = error;
    }
  }
  if (first) {
    // The closing thread ends the streams itself. Otherwise their ending's cause is what ended
    // the connection.
    if (!was_closing && manager != nullptr) {
      auto* stream_manager = manager;
      dispatch([stream_manager, cause] { stream_manager->notify_closed(cause); });
      std::lock_guard<std::mutex> guard(queue_lock);
      queue_stopped = true;
      queue_condition.notify_all();
    }
    connection->shutdown_both();
    idle.notify_all();
  }
  throw Ended();
}

void Session::run_reader() {
  while (true) {
    {
      std::unique_lock<std::mutex> guard(state_lock);
      idle.wait(guard, [this] {
        return ended || (callers == 0 && !reading && !closing && streams_open());
      });
      if (ended) return;
      reading = true;
    }
    // Wait for data without being the reader, so a call can take over. The buffer is only
    // looked at by the reader. The wait is bounded by the stall timeout, so that part of a
    // message a call leaves in the buffer is timed.
    if (!connection->buffered()) {
      stop_reading();
      bool readable = connection->wait_readable(stall_timeout);
      {
        std::lock_guard<std::mutex> guard(state_lock);
        if (ended) return;
        if (callers > 0 || reading || closing) continue;
        reading = true;
      }
      // A call can read the data between the wait and taking the role
      if (!connection->buffered() && !(readable && connection->readable())) {
        stop_reading();
        continue;
      }
    }
    try {
      try {
        schema::Message message;
        receive(&message);
        if (message.content_case() == schema::Message::kResponse)
          handle_response(message.mutable_response());
        else
          handle(&message);
      } catch (const Ended&) {
        throw;
      } catch (const std::exception& exn) {
        end(connection_error(std::string("Connection failed: ") + exn.what()));
      }
    } catch (const Ended&) {
      stop_reading();
      return;
    }
    stop_reading();
  }
}

void Session::receive(schema::Message* message) {
  auto [data, size] = receive_data();
  parse(data, size, message);
}

void Session::parse(const char* data, size_t size, google::protobuf::MessageLite* message) {
  if (!message->ParseFromArray(data, static_cast<int>(size)))
    end(protocol_error(schema::Disconnect::MALFORMED_MESSAGE,
                       "Failed to parse a message from the server"));
}

std::pair<const char*, size_t> Session::receive_data() {
  std::exception_ptr cause;
  try {
    return connection->receive_message_data(std::chrono::milliseconds::zero(), stall_timeout);
  } catch (const TimeoutError&) {
    cause = protocol_error(schema::Disconnect::TIMEOUT,
                           "A message from the server stalled for more than " +
                               std::to_string(stall_timeout.count()) + " milliseconds");
  } catch (const EncodingError& exn) {
    cause = protocol_error(schema::Disconnect::MALFORMED_MESSAGE,
                           std::string("Failed to parse a message from the server: ") + exn.what());
  } catch (const std::exception& exn) {
    std::lock_guard<std::mutex> guard(state_lock);
    if (!closing) cause = connection_error(std::string("Connection failed: ") + exn.what());
  }
  end(cause);
}

void Session::handle(schema::Message* message) {
  switch (message->content_case()) {
    case schema::Message::kStreamUpdate:
      manager->update(message->stream_update());
      break;
    case schema::Message::kDisconnect: {
      const auto& disconnect = message->disconnect();
      auto reason = schema::Disconnect::Reason_Name(disconnect.reason());
      auto text = "The server ended the connection (" + reason + ")";
      if (!disconnect.message().empty()) text += ": " + disconnect.message();
      end(connection_error(text, reason));
      break;
    }
    case schema::Message::CONTENT_NOT_SET:
      break;
    default:
      end(protocol_error(schema::Disconnect::PROTOCOL_ERROR, "Unexpected message from the server"));
  }
}

void Session::handle_response(schema::Response* response) {
  std::shared_ptr<Slot> slot;
  bool abandoned = false;
  bool was_closing = false;
  {
    std::lock_guard<std::mutex> guard(state_lock);
    auto it = pending.find(response->id());
    if (it != pending.end()) {
      slot = it->second;
      pending.erase(it);
      abandoned = slot->abandoned;
    }
    was_closing = closing;
  }
  // The closing thread has failed the calls this answers
  if (!slot && was_closing) return;
  if (!slot)
    end(protocol_error(
        schema::Disconnect::PROTOCOL_ERROR,
        "Response with an unknown id " + std::to_string(response->id()) + " from the server"));
  if (!response->has_error()) {
    for (int i = 0; i < static_cast<int>(slot->adds.size()) && i < response->results_size(); i++) {
      const auto& result = response->results(i);
      if (slot->adds[i] == Adds::nothing || result.has_error() || result.is_null()) continue;
      uint64_t id = 0;
      if (slot->adds[i] == Adds::event) {
        schema::Event event;
        if (!event.ParseFromString(result.value())) continue;
        id = event.stream().id();
      } else {
        schema::Stream stream;
        if (!stream.ParseFromString(result.value())) continue;
        id = stream.id();
      }
      if (abandoned)
        // The server stream would otherwise never be removed
        remove_stream(id);
      else
        // Registered before the next message is read, which the stream's first update follows
        manager->register_stream(id);
    }
  }
  if (!abandoned) {
    std::lock_guard<std::mutex> guard(state_lock);
    slot->response.Swap(response);
    slot->done = true;
    if (waiting > 0) changed.notify_all();
  }
}

void Session::disconnect(schema::Disconnect::Reason reason, const std::string& text) {
  try {
    std::lock_guard<std::timed_mutex> guard(write_lock);
    schema::Message message;
    message.mutable_disconnect()->set_reason(reason);
    message.mutable_disconnect()->set_message(text);
    write(message);
    connection->shutdown_send();
  } catch (const std::exception&) {  // NOLINT(bugprone-empty-catch)
    // The connection has failed already
  }
  {
    std::lock_guard<std::mutex> guard(state_lock);
    half_closed = true;
  }
  half_closed_condition.notify_all();
}

std::exception_ptr Session::protocol_error(schema::Disconnect::Reason reason,
                                           const std::string& text) {
  auto failure = connection_error(text);
  fail(failure);
  // A callback reading the socket runs on the dispatcher thread, so makes the write itself
  if (on_dispatcher_thread()) {
    disconnect(reason, text);
  } else {
    // Held by shared pointer, so that copying the work cannot throw
    auto shared_text = std::make_shared<const std::string>(text);
    dispatch([this, reason, shared_text] { disconnect(reason, *shared_text); });
    std::unique_lock<std::mutex> guard(state_lock);
    half_closed_condition.wait_for(guard, close_timeout, [this] { return half_closed; });
  }
  drain(std::chrono::steady_clock::now() + close_timeout);
  return failure;
}

}  // namespace krpc
