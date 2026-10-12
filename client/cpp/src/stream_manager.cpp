#include "krpc/stream_manager.hpp"

#include <algorithm>
#include <condition_variable>  // NOLINT(build/c++11)
#include <cstddef>
#include <exception>
#include <functional>
#include <iostream>
#include <memory>
#include <string>
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
  std::shared_ptr<StreamImpl> stream_impl;
  // The result that ends the stream, if one is pending
  const schema::ProcedureResult* final_result = nullptr;
  std::vector<schema::ProcedureResult> results;
  bool was_closed = false;
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    auto it = streams.find(id);
    if (it != streams.end())
      if (auto stream = it->second.lock()) return stream;
    stream_impl = std::make_shared<StreamImpl>(client, id, update_lock);
    was_closed = closed;
    unregistered.erase(id);
    auto pending_it = pending.find(id);
    if (pending_it != pending.end()) {
      results.swap(pending_it->second);
      pending.erase(pending_it);
    }
    if (!was_closed) {
      // The pending results are stored as an update would store them. The stream is new, so
      // it has no waiters or callbacks yet.
      for (const auto& result : results) {
        if (result.has_error()) {
          final_result = &result;
          break;
        }
        stream_impl->store(result.value(), result.is_null());
      }
      // A final result ends the stream, and the server has already removed it
      if (!final_result) streams[id] = stream_impl;
    }
  }
  std::exception_ptr ending;
  if (was_closed) {
    ending = closed_error();
  } else if (final_result) {
    try {
      client->throw_exception(final_result->error());
    } catch (...) {
      ending = std::current_exception();
    }
  }
  if (ending) call_end_callbacks(stream_impl->end(ending), ending);
  return stream_impl;
}

void StreamManager::remove_stream(uint64_t id) {
  std::shared_ptr<StreamImpl> stream;
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    auto it = streams.find(id);
    if (it == streams.end()) return;
    stream = it->second.lock();
  }
  if (stream) remove_stream(stream.get());
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
    streams.clear();
    unregistered.clear();
    pending.clear();
  }
  auto ending = closed_error();
  for (const auto& stream : ended) call_end_callbacks(stream->end(ending), ending);
  std::lock_guard<std::mutex> guard(condition_mutex);
  condition.notify_all();
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
      if (static_cast<int64_t>(id) > issued || unregistered.count(id) > 0)
        pending[id].push_back(result);
      return;
    }
    stream = it->second.lock();
    if (!stream) return;
    // An error is the final result, and the server has already removed the stream
    if (result.has_error()) streams.erase(it);
  }

  if (result.has_error()) {
    std::exception_ptr ending;
    try {
      client->throw_exception(result.error());
    } catch (...) {
      ending = std::current_exception();
    }
    call_end_callbacks(stream->end(ending), ending);
    return;
  }

  StreamImpl::Callbacks callbacks;
  if (!stream->update_and_notify(result.value(), result.is_null(), &callbacks)) return;
  call_callbacks(callbacks, result.value(), result.is_null());
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
      std::lock_guard<std::mutex> guard(stream_manager->condition_mutex);
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
