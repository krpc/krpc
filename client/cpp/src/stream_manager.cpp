#include "krpc/stream_manager.hpp"

#include <algorithm>
#include <atomic>
#include <chrono>              // NOLINT(build/c++11)
#include <condition_variable>  // NOLINT(build/c++11)
#include <exception>
#include <functional>
#include <iostream>
#include <memory>
#include <string>
#include <utility>
#include <vector>

#include "krpc/client.hpp"
#include "krpc/connection.hpp"
#include "krpc/encoder.hpp"
#include "krpc/error.hpp"
#include "krpc/krpc.pb.hpp"
#include "krpc/services/krpc.hpp"
#include "krpc/session.hpp"
#include "krpc/stream_impl.hpp"

namespace krpc {

namespace {

// The error that ends a stream once the connection closes. It nests the error that closed the
// connection, if it failed, and carries that error's disconnect reason.
std::exception_ptr closed_error(const std::exception_ptr& cause) {
  if (!cause) return std::make_exception_ptr(ConnectionError("Connection closed"));
  std::string reason;
  try {
    std::rethrow_exception(cause);
  } catch (const ConnectionError& exn) {
    reason = exn.reason();
  } catch (...) {  // NOLINT(bugprone-empty-catch)
  }
  try {
    try {
      std::rethrow_exception(cause);
    } catch (...) {
      std::throw_with_nested(ConnectionError("Connection closed", "", reason));
    }
  } catch (...) {
    return std::current_exception();
  }
}

}  // namespace

StreamManager::StreamManager(Client* client, const std::shared_ptr<Connection>& connection)
    : client(std::make_unique<Client>(*client)),
      update_lock(new std::recursive_mutex),
      condition(condition_mutex),
      condition_lock(condition_mutex, std::defer_lock),
      next_callback_tag(0),
      closed(false),
      close_requested(false) {
  this->client->stream_manager =
      std::shared_ptr<StreamManager>(std::shared_ptr<StreamManager>(), this);
  auto* manager_client = this->client.get();
  session = std::make_unique<Session>(connection, [manager_client](uint64_t id) {
    return manager_client->build_request("KRPC", "RemoveStream", {encoder::encode(id)});
  });
}

std::shared_ptr<StreamManager> StreamManager::create(
    Client* client, const std::shared_ptr<Connection>& connection) {
  std::shared_ptr<StreamManager> manager(new StreamManager(client, connection));
  manager->self = manager;
  manager->session->start(manager.get(), manager);
  // The clients' pointer, whose last copy closes the manager
  return std::shared_ptr<StreamManager>(manager.get(), [manager](StreamManager*) mutable {
    manager->close();
    manager.reset();
  });
}

StreamManager::~StreamManager() { detail::release_if_held_elsewhere(&condition_lock); }

// Streams end before the join, so a thread waiting on one is woken
void StreamManager::close() {
  request_close();
  session->close();
  try {
    notify_closed(session->failure());
  } catch (const std::exception& exn) {
    std::cerr << "kRPC: failed to end the streams: " << exn.what() << "\n";
  }
  // Callbacks run on the dispatcher thread, so a client closed from one does not join it. A
  // callback can also wait for a condition the caller holds, so the dispatcher thread then ends
  // once the caller releases it.
  session->join(!holds_a_condition());
}

std::shared_ptr<StreamImpl> StreamManager::add_stream(const schema::ProcedureCall& call) {
  schema::Stream stream = services::KRPC(client.get()).add_stream(call);
  return get_stream(stream.id());
}

void StreamManager::register_stream(uint64_t id) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  if (closed) return;
  auto it = streams.find(id);
  if (it != streams.end() && !it->second.expired()) return;
  auto stream = std::make_shared<StreamImpl>(client.get(), id, update_lock, self);
  streams[id] = stream;
  unclaimed[id] = stream;
  open_streams = true;
  registered.erase(std::remove_if(registered.begin(), registered.end(),
                                  [](const std::weak_ptr<StreamImpl>& x) { return x.expired(); }),
                   registered.end());
  registered.push_back(stream);
}

bool StreamManager::is_registered(uint64_t id) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  auto it = streams.find(id);
  return it != streams.end() && !it->second.expired();
}

std::shared_ptr<StreamImpl> StreamManager::get_stream(uint64_t id) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  auto it = streams.find(id);
  if (it != streams.end()) {
    if (auto existing = it->second.lock()) {
      // The first handle claims a stream the reader registered. One the server has already
      // ended leaves the registry with it.
      if (unclaimed.erase(id) > 0 && existing->has_ended()) streams.erase(it);
      open_streams = !streams.empty();
      return existing;
    }
  }
  auto stream = std::make_shared<StreamImpl>(client.get(), id, update_lock, self);
  if (closed) {
    stream->set_ending(closed_error(nullptr));
    return stream;
  }
  streams[id] = stream;
  open_streams = true;
  registered.erase(std::remove_if(registered.begin(), registered.end(),
                                  [](const std::weak_ptr<StreamImpl>& x) { return x.expired(); }),
                   registered.end());
  registered.push_back(stream);
  session->stream_added();
  return stream;
}

std::exception_ptr StreamManager::error_ptr(const schema::Error& error) const {
  try {
    client->throw_exception(error);
  } catch (...) {
    return std::current_exception();
  }
  return nullptr;
}

void StreamManager::drop_stream(uint64_t id) {
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    auto it = streams.find(id);
    // A live entry is another stream, given the id once the server removed this one
    if (it == streams.end() || !it->second.expired()) return;
    streams.erase(it);
    open_streams = !streams.empty();
    if (closed || close_requested) return;
  }
  session->remove_stream(id);
}

void StreamManager::remove_stream(StreamImpl* stream) {
  auto id = stream->get_id();
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    auto it = streams.find(id);
    // A stream the server or the connection has already ended, or another handle has removed
    if (it == streams.end() || it->second.lock().get() != stream) return;
    streams.erase(it);
    unclaimed.erase(id);
    open_streams = !streams.empty();
  }
  // The stream ends before the call, so a value that arrives during the call is dropped and the
  // ending holds whether or not the call succeeds
  auto ending = std::make_exception_ptr(StreamError("Stream does not exist"));
  call_end_callbacks(stream->end(ending), ending);
  services::KRPC(client.get()).remove_stream(id);
}

Condition& StreamManager::get_update_condition() { return condition; }

std::unique_lock<RecursiveMutex>& StreamManager::get_update_condition_lock() {
  return condition_lock;
}

void StreamManager::acquire_update() { condition_mutex.lock(); }

void StreamManager::release_update() { condition_mutex.unlock(); }

int StreamManager::add_update_callback(const Callback& callback) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  auto tag = next_callback_tag;
  next_callback_tag++;
  callbacks[tag] = callback;
  return tag;
}

void StreamManager::remove_update_callback(int tag) {
  Callback removed;
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  auto it = callbacks.find(tag);
  if (it == callbacks.end()) return;
  removed = std::move(it->second);
  callbacks.erase(it);
}

void StreamManager::request_close() { close_requested.store(true); }

bool StreamManager::is_close_requested() const { return close_requested.load(); }

bool StreamManager::is_closed() const { return closed.load(); }

bool StreamManager::holds_a_condition() {
  if (condition_mutex.held_by_this_thread()) return true;
  std::vector<std::shared_ptr<StreamImpl>> held;
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    for (const auto& stream : registered)
      if (auto live = stream.lock()) held.push_back(live);
  }
  return std::any_of(held.begin(), held.end(), [](const std::shared_ptr<StreamImpl>& stream) {
    return stream->get_condition_mutex().held_by_this_thread();
  });
}

void StreamManager::notify_closed(const std::exception_ptr& cause) {
  std::vector<std::pair<StreamImpl::EndCallbacks, std::exception_ptr>> endings;
  std::vector<std::shared_ptr<StreamImpl>> to_wake;
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    closed = true;
    auto ending = closed_error(cause);
    for (const auto& entry : streams)
      if (auto stream = entry.second.lock())
        endings.emplace_back(stream->set_ending(ending, true, true), ending);
    streams.clear();
    unclaimed.clear();
    open_streams = false;
    for (const auto& stream : registered)
      if (auto live = stream.lock()) to_wake.push_back(live);
  }
  // A waiter on a condition another thread holds wakes once that thread lets go of it, such as
  // a thread holding one stream's condition while it waits on another
  for (const auto& stream : to_wake) stream->get_condition().notify_all_when_free();
  // A thread holding a stream condition can be waiting for a stream update
  condition.notify_all_when_free();
  for (const auto& entry : endings) call_end_callbacks(entry.first, entry.second);
}

void StreamManager::notify_update(bool on_dispatcher) {
  if (on_dispatcher) {
    if (!lock(&condition_mutex)) return;
  } else {
    condition_mutex.lock();
  }
  std::lock_guard<RecursiveMutex> held(condition_mutex, std::adopt_lock);
  condition.notify_all();
}

bool StreamManager::lock(RecursiveMutex* mutex) {
  while (!mutex->try_lock_for(std::chrono::milliseconds(50))) {
    if (close_requested) return false;
  }
  return true;
}

void StreamManager::apply(const std::vector<Change>& changes, const Callbacks& update_callbacks) {
  for (const auto& change : changes) {
    const auto& stream = change.stream;
    bool locked = lock(&stream->get_condition_mutex());
    if (locked) {
      std::lock_guard<RecursiveMutex> held(stream->get_condition_mutex(), std::adopt_lock);
      if (change.ending)
        stream->announce_ending();
      else
        stream->announce(change.data, change.is_null, change.version);
    }
    // The ending is already stored, so the end callbacks run even once closing
    if (change.ending) {
      if (!stream->defer_ending(change.end_callbacks, change.ending))
        call_end_callbacks(change.end_callbacks, change.ending);
      continue;
    }
    if (!locked) continue;
    for (const auto& callback : change.callbacks)
      stream->call(callback, callback->event ? change.fired : change.version, change.data,
                   change.is_null);
  }

  notify_update(true);
  for (const auto& callback : update_callbacks) {
    // Update callbacks have no stream ending to stop them once the client closes
    if (closed) break;
    try {
      callback.second();
    } catch (const std::exception& exn) {
      std::cerr << "kRPC: exception thrown by a stream update callback: " << exn.what() << "\n";
    } catch (...) {
      std::cerr << "kRPC: unknown exception thrown by a stream update callback\n";
    }
  }
}

void StreamManager::update(const schema::StreamUpdate& message) {
  std::vector<Change> changes;
  Callbacks update_callbacks;
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    for (const auto& result : message.results()) {
      auto id = result.id();
      auto it = streams.find(id);
      // A result for a stream not in the registry is dropped, as the client has removed it
      if (it == streams.end()) continue;
      auto stream = it->second.lock();
      // The stream's handles are all gone
      if (!stream) {
        streams.erase(it);
        continue;
      }
      Change change;
      change.stream = stream;
      if (result.result().has_error()) {
        // The server has removed the stream. One no handle has claimed stays in the registry,
        // so its first handle finds it ended.
        change.ending = error_ptr(result.result().error());
        change.end_callbacks = stream->set_ending(change.ending, false);
        if (unclaimed.count(id) == 0) streams.erase(it);
      } else {
        stream->set_value(result.result().value(), result.result().is_null());
        // Read with the value stored, so a callback added after this reads the value itself
        change.callbacks = stream->get_callbacks();
        change.version = stream->get_version();
        change.fired = stream->get_fired();
        change.data = result.result().value();
        change.is_null = result.result().is_null();
      }
      changes.push_back(std::move(change));
    }
    open_streams = !streams.empty();
    update_callbacks = callbacks;
  }
  // Moved, so the reader thread keeps no copy of a callback, which can hold the last copy of the
  // client
  session->dispatch(
      [this, changes = std::move(changes), update_callbacks = std::move(update_callbacks)]() {
        apply(changes, update_callbacks);
      });
}

void StreamManager::update_and_wait(const schema::StreamUpdate& message) {
  update(message);
  struct Applied {
    std::mutex mutex;
    std::condition_variable condition;
    bool done = false;
  };
  auto applied = std::make_shared<Applied>();
  session->dispatch([applied] {
    std::lock_guard<std::mutex> guard(applied->mutex);
    applied->done = true;
    applied->condition.notify_all();
  });
  std::unique_lock<std::mutex> guard(applied->mutex);
  applied->condition.wait_for(guard, std::chrono::seconds(10),
                              [&applied] { return applied->done; });
}

}  // namespace krpc
