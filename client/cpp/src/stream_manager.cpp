#include "krpc/stream_manager.hpp"

#include <algorithm>
#include <atomic>
#include <chrono>  // NOLINT(build/c++11)
#include <cstddef>
#include <exception>
#include <functional>
#include <iostream>
#include <memory>
#include <string>
#include <thread>  // NOLINT(build/c++11)
#include <utility>
#include <vector>

#include "krpc/client.hpp"
#include "krpc/connection.hpp"
#include "krpc/decoder.hpp"
#include "krpc/error.hpp"
#include "krpc/krpc.pb.hpp"
#include "krpc/services/krpc.hpp"
#include "krpc/stream_impl.hpp"

namespace krpc {

namespace {

std::exception_ptr closed_error() {
  return std::make_exception_ptr(ConnectionError("Connection closed"));
}

}  // namespace

StreamManager::StreamManager(Client* client, const std::shared_ptr<Connection>& connection)
    : client(std::make_unique<Client>(*client)),
      connection(connection),
      update_lock(new std::recursive_mutex),
      condition(condition_mutex),
      condition_lock(condition_mutex, std::defer_lock),
      next_callback_tag(0),
      closed(false),
      close_requested(false),
      issued(-1) {
  this->client->stream_manager =
      std::shared_ptr<StreamManager>(std::shared_ptr<StreamManager>(), this);
}

std::shared_ptr<StreamManager> StreamManager::create(
    Client* client, const std::shared_ptr<Connection>& connection) {
  std::shared_ptr<StreamManager> manager(new StreamManager(client, connection));
  manager->self = manager;
  if (connection) manager->update_thread = std::thread(update_thread_main, manager);
  // The clients' pointer, whose last copy closes the manager
  return std::shared_ptr<StreamManager>(manager.get(), [manager](StreamManager*) mutable {
    manager->close();
    manager.reset();
  });
}

StreamManager::~StreamManager() {
  detail::release_if_held_elsewhere(&condition_lock);
  // Destroyed on the update thread once it exits, if it holds the last reference
  if (update_thread.get_id() == std::this_thread::get_id())
    update_thread.detach();
  else if (update_thread.joinable())
    update_thread.join();
}

// Streams end before the join, as the caller can hold a stream's condition, which the update
// thread then stops trying to take
void StreamManager::close() {
  request_close();
  try {
    notify_closed();
  } catch (const std::exception& exn) {
    std::cerr << "kRPC: failed to end the streams: " << exn.what() << "\n";
  }
  // Callbacks run on the update thread, which cannot join itself. A callback can also wait for
  // a condition the caller holds, so the update thread then ends once the caller releases it.
  if (!update_thread.joinable() || update_thread.get_id() == std::this_thread::get_id() ||
      holds_a_condition())
    return;
  update_thread.join();
}

std::shared_ptr<StreamImpl> StreamManager::add_stream(const schema::ProcedureCall& call) {
  schema::Stream stream = services::KRPC(client.get()).add_stream(call);
  return get_stream(stream.id());
}

void StreamManager::note_issued(uint64_t id) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  issued = std::max(issued, static_cast<int64_t>(id));
  if (streams.find(id) == streams.end()) unregistered.insert(id);
}

void StreamManager::note_removed(uint64_t id) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  issued = std::max(issued, static_cast<int64_t>(id));
  unregistered.erase(id);
  pending.erase(id);
}

std::shared_ptr<StreamImpl> StreamManager::get_stream(uint64_t id) {
  auto stream = std::make_shared<StreamImpl>(client.get(), id, update_lock, self);
  // The kept results are stored under the new stream's condition and the update lock, so an
  // update for the stream lands after them. The stream is new, so it has no end callbacks.
  std::lock_guard<RecursiveMutex> held(stream->get_condition_mutex());
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  auto it = streams.find(id);
  if (it != streams.end())
    if (auto existing = it->second.lock()) return existing;
  unregistered.erase(id);
  Pending kept;
  auto pending_it = pending.find(id);
  if (pending_it != pending.end()) {
    kept = std::move(pending_it->second);
    pending.erase(pending_it);
  }
  if (closed) {
    stream->end(closed_error());
    return stream;
  }
  if (kept.value) stream->set_value(kept.value->value(), kept.value->is_null());
  // A final result ends the stream, and the server has already removed it
  if (kept.ending) {
    stream->end(error_ptr(kept.ending->error()));
    return stream;
  }
  streams[id] = stream;
  registered.erase(std::remove_if(registered.begin(), registered.end(),
                                  [](const std::weak_ptr<StreamImpl>& x) { return x.expired(); }),
                   registered.end());
  registered.push_back(stream);
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

void StreamManager::remove_stream(uint64_t id) {
  std::shared_ptr<StreamImpl> stream;
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    auto it = streams.find(id);
    if (it != streams.end()) {
      stream = it->second.lock();
      // A stream whose handles are all gone is removed here
      if (!stream) streams.erase(it);
    }
  }
  if (stream)
    remove_stream(stream.get());
  else
    services::KRPC(client.get()).remove_stream(id);
}

void StreamManager::remove_stream(StreamImpl* stream) {
  auto id = stream->get_id();
  std::shared_ptr<StreamImpl> decoded_end;
  std::exception_ptr ending;
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    auto it = streams.find(id);
    if (it == streams.end() || it->second.lock().get() != stream) {
      // A stream with a decoded final result, or one the client is closing, is ended with that
      // ending. The server has no stream for it to remove. Another handle can have removed the
      // stream already.
      auto ended = std::find_if(to_end.begin(), to_end.end(), [stream](const auto& entry) {
        return entry.first.get() == stream;
      });
      if (ended == to_end.end()) return;
      decoded_end = ended->first;
      ending = ended->second;
    } else {
      streams.erase(it);
    }
  }
  if (decoded_end) {
    decoded_end->get_condition_mutex().lock();
    end(decoded_end, ending);
    return;
  }
  // The stream ends before the call, so a value decoded during the call is dropped and the
  // ending holds whether or not the call succeeds
  ending = std::make_exception_ptr(StreamError("Stream does not exist"));
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
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  callbacks.erase(tag);
}

void StreamManager::request_close() { close_requested.store(true); }

bool StreamManager::is_close_requested() const { return close_requested.load(); }

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

void StreamManager::notify_closed(bool on_update_thread) {
  auto ending = closed_error();
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    closed = true;
    for (const auto& entry : streams)
      if (auto stream = entry.second.lock()) to_end[stream] = ending;
    streams.clear();
    pending.clear();
    unregistered.clear();
  }
  while (true) {
    std::vector<std::pair<std::shared_ptr<StreamImpl>, std::exception_ptr>> remaining;
    {
      std::lock_guard<std::recursive_mutex> guard(*update_lock);
      for (auto it = to_end.begin(); it != to_end.end();) {
        if (it->first->has_ended())
          it = to_end.erase(it);
        else
          ++it;
      }
      if (to_end.empty()) break;
      if (on_update_thread && close_requested) break;
      remaining.assign(to_end.begin(), to_end.end());
    }
    for (const auto& entry : remaining) try_end(entry.first, entry.second);
    // A thread holding a stream condition can be waiting for a stream update
    if (condition_mutex.try_lock()) {
      condition.notify_all();
      condition_mutex.unlock();
    }
  }
  notify_update(on_update_thread);
}

void StreamManager::notify_update(bool on_update_thread) {
  if (on_update_thread) {
    if (!lock(&condition_mutex)) return;
  } else {
    condition_mutex.lock();
  }
  std::lock_guard<RecursiveMutex> held(condition_mutex, std::adopt_lock);
  condition.notify_all();
}

void StreamManager::try_end(const std::shared_ptr<StreamImpl>& stream,
                            const std::exception_ptr& ending) {
  if (stream->get_condition_mutex().try_lock_for(std::chrono::milliseconds(10)))
    end(stream, ending);
}

void StreamManager::end(const std::shared_ptr<StreamImpl>& stream,
                        const std::exception_ptr& ending) {
  StreamImpl::EndCallbacks end_callbacks;
  {
    std::lock_guard<RecursiveMutex> held(stream->get_condition_mutex(), std::adopt_lock);
    end_callbacks = stream->end(ending);
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    to_end.erase(stream);
  }
  call_end_callbacks(end_callbacks, ending);
}

bool StreamManager::lock(RecursiveMutex* mutex) {
  while (!mutex->try_lock_for(std::chrono::milliseconds(50))) {
    if (close_requested) return false;
  }
  return true;
}

void StreamManager::end_on_update_thread(const std::shared_ptr<StreamImpl>& stream,
                                         const std::exception_ptr& ending) {
  if (lock(&stream->get_condition_mutex())) end(stream, ending);
}

StreamManager::Decoded StreamManager::decode(const std::shared_ptr<StreamImpl>& stream,
                                             const schema::ProcedureResult& result) {
  if (!result.has_error()) return {stream, result.value(), result.is_null(), nullptr};
  auto ending = error_ptr(result.error());
  streams.erase(stream->get_id());
  to_end[stream] = ending;
  return {stream, std::string(), false, ending};
}

void StreamManager::apply(const std::vector<Decoded>& decoded) {
  for (const auto& entry : decoded) {
    const auto& stream = entry.stream;
    if (entry.ending) {
      end_on_update_thread(stream, entry.ending);
      continue;
    }
    if (!lock(&stream->get_condition_mutex())) continue;
    StreamImpl::Callbacks callbacks;
    uint64_t version = 0;
    uint64_t fired = 0;
    {
      std::lock_guard<RecursiveMutex> held(stream->get_condition_mutex(), std::adopt_lock);
      {
        std::lock_guard<std::recursive_mutex> guard(*update_lock);
        // The stream can be removed while its new value is in flight. The ending it stored is
        // kept.
        if (stream->has_ended()) continue;
        stream->set_value(entry.data, entry.is_null);
        // Read with the value stored, so a callback added after this reads the value itself
        callbacks = stream->get_callbacks();
        version = stream->get_version();
        fired = stream->get_fired();
      }
      stream->get_condition().notify_all();
    }
    for (const auto& callback : callbacks)
      stream->call(callback, callback->event ? fired : version, entry.data, entry.is_null);
  }
}

// The update lock is held only to find the streams and decode their results, and is released
// before any stream condition is taken. A thread waiting for an update holds a condition and
// then needs the update lock, so taking the two in the opposite order deadlocks.
void StreamManager::update(const schema::StreamUpdate& message) {
  std::vector<Decoded> decoded;
  Callbacks update_callbacks;
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    for (const auto& result : message.results()) {
      auto id = result.id();
      auto it = streams.find(id);
      if (it == streams.end()) {
        // A result for a stream not registered yet is kept. One for an id below those is for
        // a stream that has ended.
        if (closed) continue;
        if (static_cast<int64_t>(id) > issued || unregistered.count(id) > 0)
          keep(id, result.result());
        continue;
      }
      auto stream = it->second.lock();
      // The stream's handles are all gone
      if (!stream) {
        streams.erase(it);
        continue;
      }
      decoded.push_back(decode(stream, result.result()));
    }
    update_callbacks = callbacks;
  }
  apply(decoded);

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

void StreamManager::keep(uint64_t id, const schema::ProcedureResult& result) {
  auto& kept = pending[id];
  // An ending is the last result kept
  if (kept.ending) return;
  auto copy = std::make_shared<schema::ProcedureResult>(result);
  if (result.has_error())
    kept.ending = std::move(copy);
  else
    kept.value = std::move(copy);
}

void StreamManager::update_thread_main(const std::shared_ptr<StreamManager>& stream_manager) {
  try {
    stream_manager->read_updates();
  } catch (...) {
    // A connection that fails, or a message that cannot be read, leaves the connection
    // unusable. Closing it can fail as well, which leaves it as unusable.
    try {
      stream_manager->connection->close();
    } catch (const std::exception&) {  // NOLINT(bugprone-empty-catch)
    }
  }
  // The connection is closed or failed, so no further update arrives
  stream_manager->notify_closed(true);
}

void StreamManager::read_updates() {
  while (!close_requested) {
    // Wait for next update message
    size_t size = 0;
    std::string data;
    while (!close_requested) {
      try {
        data += connection->partial_receive(1);
        size = decoder::decode_size(data);
        break;
      } catch (EncodingError&) {  // NOLINT(bugprone-empty-catch): need more bytes
      }
    }
    if (close_requested) break;

    // Decode and apply the update
    schema::StreamUpdate message;
    decoder::decode(message, connection->receive(size), client.get());
    update(message);
  }
}

}  // namespace krpc
