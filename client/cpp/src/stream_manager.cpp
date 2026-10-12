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
// mutex, as the thread destroying the manager can hold it while it joins the update thread.
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
    : client(client),
      connection(connection),
      issued(-1),
      update_lock(new std::recursive_mutex),
      stop(new std::atomic_bool(false)),
      closed(false),
      condition_lock(condition_mutex, std::defer_lock),
      next_callback_tag(0) {
  // Started once every member it uses is constructed
  update_thread = std::make_shared<std::thread>(update_thread_main, this, connection, stop);
}

StreamManager::~StreamManager() {
  stop->store(true);
  // The streams are ended on this thread, which can hold their conditions. The update thread
  // skips any stream whose condition it cannot take once stop is set.
  try {
    end_streams();
  } catch (const std::exception& exn) {
    std::cerr << "kRPC: failed to end the streams: " << exn.what() << "\n";
  }
  update_thread->join();
}

std::shared_ptr<StreamImpl> StreamManager::add_stream(const schema::ProcedureCall& call) {
  schema::Stream stream = services::KRPC(client).add_stream(call);
  return get_stream(stream.id());
}

void StreamManager::note_issued(uint64_t id) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  issued = std::max(issued, static_cast<int64_t>(id));
  if (streams.find(id) == streams.end()) unregistered.insert(id);
}

std::shared_ptr<StreamImpl> StreamManager::get_stream(uint64_t id) {
  auto stream_impl = std::make_shared<StreamImpl>(client, id, update_lock);
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
    if (it == streams.end()) return;
    stream = it->second.lock();
    // A stream whose handles are all gone is removed here
    if (!stream) streams.erase(it);
  }
  if (stream)
    remove_stream(stream.get());
  else
    services::KRPC(client).remove_stream(id);
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
  services::KRPC(client).remove_stream(id);
}

void StreamManager::notify_closed() {
  std::vector<std::shared_ptr<StreamImpl>> ended;
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    closed = true;
    for (const auto& entry : streams)
      if (auto stream = entry.second.lock()) ended.push_back(stream);
    unregistered.clear();
    pending.clear();
  }
  auto ending = closed_error();
  for (const auto& stream : ended) {
    StreamImpl::EndCallbacks end_callbacks;
    {
      auto held = lock_unless_stopped(stream->get_condition_mutex(), *stop);
      // Left registered for the thread destroying the manager to end
      if (!held.owns_lock()) continue;
      unregister(stream.get());
      end_callbacks = stream->end_held(ending);
    }
    call_end_callbacks(end_callbacks, ending);
  }
  auto held = lock_unless_stopped(condition_mutex, *stop);
  condition.notify_all();
}

void StreamManager::end_streams() {
  std::vector<std::shared_ptr<StreamImpl>> ended;
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    closed = true;
    for (const auto& entry : streams)
      if (auto stream = entry.second.lock()) ended.push_back(stream);
    streams.clear();
    unregistered.clear();
    pending.clear();
  }
  auto ending = closed_error();
  for (const auto& stream : ended) call_end_callbacks(stream->end(ending), ending);
  // This thread can hold the condition, through get_update_condition_lock()
  std::unique_lock<std::mutex> held(condition_mutex, std::try_to_lock);
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
      auto held = lock_unless_stopped(stream->get_condition_mutex(), *stop);
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
    auto held = lock_unless_stopped(stream->get_condition_mutex(), *stop);
    if (!held.owns_lock()) return;
    if (!stream->update_and_notify(result.value(), result.is_null(), &callbacks, &version, &fired))
      return;
  }
  stream->call_callbacks(callbacks, result.value(), result.is_null(), version, fired);
}

std::condition_variable& StreamManager::get_update_condition() { return condition; }

std::unique_lock<std::mutex>& StreamManager::get_update_condition_lock() { return condition_lock; }

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

void StreamManager::update_thread_main(StreamManager* stream_manager,
                                       const std::shared_ptr<Connection>& connection,
                                       const std::shared_ptr<std::atomic_bool>& stop) {
  try {
    read_updates(stream_manager, connection, stop);
  } catch (...) {
    // A connection that fails, or a message that cannot be read, leaves the connection
    // unusable
    connection->close();
  }
  // The connection is closed or failed, so no further update arrives
  stream_manager->notify_closed();
}

void StreamManager::read_updates(StreamManager* stream_manager,
                                 const std::shared_ptr<Connection>& connection,
                                 const std::shared_ptr<std::atomic_bool>& stop) {
  Client* client = stream_manager->client;

  auto apply_update = [stream_manager, client](const std::string& data) {
    schema::StreamUpdate update;
    decoder::decode(update, data, client);
    for (const auto& result : update.results())
      stream_manager->update(result.id(), result.result());
    {
      // Notify while holding the condition mutex. A notification firing between a
      // waiter checking the stream value and entering its wait is lost, and the
      // waiter sleeps through the update.
      auto held = lock_unless_stopped(stream_manager->condition_mutex, *stream_manager->stop);
      stream_manager->condition.notify_all();
    }
    // Copy the callbacks under the update lock, as they run below without it held
    Callbacks callbacks;
    {
      std::lock_guard<std::recursive_mutex> guard(*stream_manager->update_lock);
      callbacks = stream_manager->callbacks;
    }
    for (const auto& callback : callbacks) {
      try {
        callback.second();
      } catch (const std::exception& exn) {
        std::cerr << "kRPC: exception thrown by a stream update callback: " << exn.what() << "\n";
      } catch (...) {
        std::cerr << "kRPC: unknown exception thrown by a stream update callback\n";
      }
    }
  };

  while (!stop->load()) {
    // Wait for next update message
    size_t size = 0;
    std::string data;
    while (!stop->load()) {
      try {
        data += connection->partial_receive(1);
        size = decoder::decode_size(data);
        break;
      } catch (EncodingError&) {  // NOLINT(bugprone-empty-catch): need more bytes
      }
    }
    if (stop->load()) break;

    // Decode and apply the update
    apply_update(connection->receive(size));
  }
}

}  // namespace krpc
