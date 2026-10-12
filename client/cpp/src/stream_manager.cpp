#include "krpc/stream_manager.hpp"

#include <algorithm>
#include <atomic>
#include <chrono>              // NOLINT(build/c++11)
#include <condition_variable>  // NOLINT(build/c++11)
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

// Lock a mutex for the update thread. Once stop is set the returned lock does not own the
// mutex, as the closing thread can hold it while it joins the update thread.
std::unique_lock<std::mutex> lock_unless_stopped(std::mutex& mutex, const std::atomic_bool& stop) {
  std::unique_lock<std::mutex> lock(mutex, std::try_to_lock);
  auto delay = std::chrono::microseconds(10);
  while (!lock.owns_lock() && !stop.load()) {
    std::this_thread::sleep_for(delay);
    delay = std::min(delay * 2, std::chrono::microseconds(1000));
    lock.try_lock();
  }
  return lock;
}

}  // namespace

StreamManager::StreamManager(Client* client, const std::shared_ptr<Connection>& connection)
    : client(std::make_unique<Client>(*client)),
      connection(connection),
      issued(-1),
      update_lock(new std::recursive_mutex),
      stop(false),
      closed(false),
      condition_lock(condition_mutex, std::defer_lock),
      next_callback_tag(0) {
  this->client->stream_manager =
      std::shared_ptr<StreamManager>(std::shared_ptr<StreamManager>(), this);
}

std::shared_ptr<StreamManager> StreamManager::create(
    Client* client, const std::shared_ptr<Connection>& connection) {
  std::shared_ptr<StreamManager> manager(new StreamManager(client, connection));
  manager->self = manager;
  manager->update_thread = std::thread(update_thread_main, manager);
  // The clients' pointer, whose last copy closes the manager
  return std::shared_ptr<StreamManager>(manager.get(), [manager](StreamManager*) mutable {
    manager->close();
    manager.reset();
  });
}

StreamManager::~StreamManager() {
  // Destroyed on the update thread once it exits, if it holds the last reference
  if (update_thread.get_id() == std::this_thread::get_id())
    update_thread.detach();
  else if (update_thread.joinable())
    update_thread.join();
}

void StreamManager::close() {
  stop.store(true);
  try {
    end_streams(false);
  } catch (const std::exception& exn) {
    std::cerr << "kRPC: failed to end the streams: " << exn.what() << "\n";
  }
  // The update thread exits on its own once it sees stop. It cannot join itself, and a callback
  // on it can be waiting for a stream condition this thread holds.
  if (update_thread.get_id() == std::this_thread::get_id() || holds.held()) return;
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

std::shared_ptr<StreamImpl> StreamManager::get_stream(uint64_t id) {
  auto stream_impl = std::make_shared<StreamImpl>(client.get(), id, update_lock, self);
  std::exception_ptr ending;
  StreamImpl::EndCallbacks end_callbacks;
  {
    // The kept results are applied under the locks an update takes, so that no update for the
    // id lands between them. The stream is new, so it has no waiters or callbacks yet.
    std::lock_guard<std::mutex> condition_guard(stream_impl->get_condition_mutex());
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    auto it = streams.find(id);
    if (it != streams.end())
      if (auto stream = it->second.lock()) return stream;
    unregistered.erase(id);
    Pending kept;
    auto pending_it = pending.find(id);
    if (pending_it != pending.end()) {
      kept = std::move(pending_it->second);
      pending.erase(pending_it);
    }
    if (closed) {
      ending = closed_error();
    } else {
      if (kept.value) stream_impl->store(kept.value->value(), kept.value->is_null());
      // A final result ends the stream, and the server has already removed it
      if (kept.ending)
        ending = error_ptr(kept.ending->error());
      else
        streams[id] = stream_impl;
    }
    if (ending) end_callbacks = stream_impl->end_held(ending);
  }
  call_end_callbacks(end_callbacks, ending);
  return stream_impl;
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

void StreamManager::note_removed(uint64_t id) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  unregistered.erase(id);
  pending.erase(id);
  issued = std::max(issued, static_cast<int64_t>(id));
}

void StreamManager::remove_stream(StreamImpl* stream) {
  auto id = stream->get_id();
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    // The server can give a stream added since the id of this one, which stays live
    auto it = streams.find(id);
    if (it == streams.end() || it->second.lock().get() != stream) return;
    streams.erase(it);
  }
  // The stream ends before the call, so a value decoded during the call is dropped and the
  // ending holds whether or not the call succeeds
  auto ending = std::make_exception_ptr(StreamError("Stream does not exist"));
  call_end_callbacks(stream->end(ending), ending);
  services::KRPC(client.get()).remove_stream(id);
}

void StreamManager::notify_closed() { end_streams(true); }

// The conditions are taken with a try, in passes, so that a thread holding one stream's
// condition while it waits on another's wakes on the other's ending and lets the first go. A
// condition this thread holds is used as it is. A stream stays registered until it is ended,
// so that a close finds the streams the update thread gives up on.
void StreamManager::end_streams(bool give_up_on_close) {
  std::vector<std::shared_ptr<StreamImpl>> remaining;
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    closed = true;
    for (auto it = streams.begin(); it != streams.end();) {
      if (auto stream = it->second.lock()) {
        remaining.push_back(stream);
        ++it;
      } else {
        it = streams.erase(it);
      }
    }
    unregistered.clear();
    pending.clear();
  }
  auto ending = closed_error();
  auto delay = std::chrono::microseconds(10);
  while (true) {
    std::vector<std::shared_ptr<StreamImpl>> held_elsewhere;
    for (const auto& stream : remaining) {
      StreamImpl::EndCallbacks end_callbacks;
      {
        std::unique_lock<std::mutex> held(stream->get_condition_mutex(), std::defer_lock);
        if (!stream->is_held() && !held.try_lock()) {
          held_elsewhere.push_back(stream);
          continue;
        }
        unregister(stream.get());
        end_callbacks = stream->end_held(ending);
      }
      call_end_callbacks(end_callbacks, ending);
    }
    notify_update();
    if (held_elsewhere.empty() || (give_up_on_close && stop.load())) return;
    std::this_thread::sleep_for(delay);
    delay = std::min(delay * 2, std::chrono::microseconds(1000));
    remaining.swap(held_elsewhere);
  }
}

// Notified while holding the condition mutex. A notification firing between a waiter checking
// for an update and entering its wait is lost, and the waiter sleeps through the update.
void StreamManager::notify_update() {
  if (condition_owner.load() == std::this_thread::get_id()) {
    condition.notify_all();
    return;
  }
  auto held = lock_unless_stopped(condition_mutex, stop);
  condition.notify_all();
}

bool StreamManager::unregister(const StreamImpl* stream) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  auto it = streams.find(stream->get_id());
  if (it == streams.end() || it->second.lock().get() != stream) return false;
  streams.erase(it);
  return true;
}

void StreamManager::update(uint64_t id, const schema::ProcedureResult& result) {
  std::shared_ptr<StreamImpl> stream;
  // Locks are taken in the order stream condition, then update lock. The update lock is
  // held only to find the stream, and is released before the stream's condition is taken.
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    if (closed) return;
    auto it = streams.find(id);
    if (it == streams.end()) {
      // A result for a stream not registered yet is kept. One for an id below those is for
      // a stream that has ended.
      if (static_cast<int64_t>(id) > issued || unregistered.count(id) > 0) {
        auto& kept = pending[id];
        if (!kept.ending) {
          auto copy = std::make_shared<schema::ProcedureResult>(result);
          if (result.has_error())
            kept.ending = copy;
          else
            kept.value = copy;
        }
      }
      return;
    }
    stream = it->second.lock();
    if (!stream) return;
  }

  if (result.has_error()) {
    auto ending = error_ptr(result.error());
    StreamImpl::EndCallbacks end_callbacks;
    {
      auto held = lock_unless_stopped(stream->get_condition_mutex(), stop);
      if (!held.owns_lock()) return;
      // An error is the final result, and the server has already removed the stream. A
      // stream removed or closed meanwhile has its ending already.
      if (!unregister(stream.get())) return;
      end_callbacks = stream->end_held(ending);
    }
    call_end_callbacks(end_callbacks, ending);
    return;
  }

  StreamImpl::Callbacks callbacks;
  uint64_t version = 0;
  uint64_t fired = 0;
  {
    auto held = lock_unless_stopped(stream->get_condition_mutex(), stop);
    if (!held.owns_lock()) return;
    if (!stream->update_and_notify(result.value(), result.is_null(), &callbacks, &version, &fired))
      return;
  }
  stream->call_callbacks(callbacks, result.value(), result.is_null(), version, fired);
}

std::condition_variable& StreamManager::get_update_condition() { return condition; }

std::unique_lock<std::mutex>& StreamManager::get_update_condition_lock() { return condition_lock; }

void StreamManager::acquire_update() {
  condition_lock.lock();
  condition_owner.store(std::this_thread::get_id());
}

void StreamManager::release_update() {
  condition_owner.store(std::thread::id());
  condition_lock.unlock();
}

Holds& StreamManager::stream_holds() { return holds; }

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
  stream_manager->notify_closed();
}

void StreamManager::read_updates() {
  auto apply_update = [this](const std::string& data) {
    schema::StreamUpdate message;
    decoder::decode(message, data, client.get());
    for (const auto& result : message.results()) update(result.id(), result.result());
    notify_update();
    // Copy the callbacks under the update lock, as they run below without it held
    Callbacks update_callbacks;
    {
      std::lock_guard<std::recursive_mutex> guard(*update_lock);
      update_callbacks = callbacks;
    }
    for (const auto& callback : update_callbacks) {
      try {
        callback.second();
      } catch (const std::exception& exn) {
        std::cerr << "kRPC: exception thrown by a stream update callback: " << exn.what() << "\n";
      } catch (...) {
        std::cerr << "kRPC: unknown exception thrown by a stream update callback\n";
      }
    }
  };

  while (!stop.load()) {
    // Wait for next update message
    size_t size = 0;
    std::string data;
    while (!stop.load()) {
      try {
        data += connection->partial_receive(1);
        size = decoder::decode_size(data);
        break;
      } catch (EncodingError&) {  // NOLINT(bugprone-empty-catch): need more bytes
      }
    }
    if (stop.load()) break;

    // Decode and apply the update
    apply_update(connection->receive(size));
  }
}

}  // namespace krpc
