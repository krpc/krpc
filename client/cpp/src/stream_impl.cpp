#include "krpc/stream_impl.hpp"

#include <algorithm>
#include <atomic>
#include <chrono>  // NOLINT(build/c++11)
#include <exception>
#include <iostream>
#include <memory>
#include <string>
#include <thread>  // NOLINT(build/c++11)
#include <utility>

#include "krpc/client.hpp"
#include "krpc/error.hpp"
#include "krpc/services/krpc.hpp"
#include "krpc/stream_manager.hpp"

namespace krpc {

namespace {

// An encoded bool true, which for an event is a firing
bool is_firing(const std::string& data, bool is_null) { return !is_null && data == "\x01"; }

}  // namespace

bool Holds::held() const {
  std::lock_guard<std::mutex> guard(mutex);
  return counts.count(std::this_thread::get_id()) > 0;
}

void Holds::add() {
  std::lock_guard<std::mutex> guard(mutex);
  counts[std::this_thread::get_id()]++;
}

bool Holds::remove() {
  std::lock_guard<std::mutex> guard(mutex);
  auto count = counts.find(std::this_thread::get_id());
  if (count == counts.end()) return false;
  if (--count->second > 0) return false;
  counts.erase(count);
  return true;
}

StreamImpl::StreamImpl(Client* client, uint64_t id,
                       std::shared_ptr<std::recursive_mutex> update_lock)
    : client(client),
      id(id),
      update_lock(std::move(update_lock)),
      updated(false),
      _is_null(false),
      version(0),
      fired(0),
      callbacks_fired(0),
      decode_failure_reported(false),
      next_callback_tag(0),
      next_end_callback_tag(0),
      _rate(0) {}

Client* StreamImpl::get_client() const { return client; }

uint64_t StreamImpl::get_id() const { return id; }

float StreamImpl::rate() const { return _rate; }

void StreamImpl::set_rate(float value) {
  _rate = value;
  // The client of an ended stream can be gone
  if (has_ended()) return;
  services::KRPC(client).set_stream_rate(id, value);
}

std::string StreamImpl::get_data(bool* is_null) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  if (ending) std::rethrow_exception(ending);
  if (!updated) throw StreamError("Stream has no value");
  *is_null = _is_null;
  return data;
}

bool StreamImpl::store(const std::string& data, bool is_null) {
  // The stream can be removed while its value is in flight. The ending it stored is kept.
  if (ending) return false;
  this->data = data;
  this->_is_null = is_null;
  updated.store(true);
  version++;
  if (is_firing(data, is_null)) fired++;
  return true;
}

// The caller holds the condition across the store and the notification, so that a
// notification cannot fire between a waiter checking the value and entering its wait. The
// callbacks are read with the value stored, so a callback added after this is called with the
// value by add_callback.
bool StreamImpl::update_and_notify(const std::string& data, bool is_null, Callbacks* callbacks,
                                   uint64_t* version, uint64_t* fired) {
  {
    std::lock_guard<std::recursive_mutex> update_guard(*update_lock);
    if (!store(data, is_null)) return false;
    *callbacks = this->callbacks;
    *version = this->version;
    *fired = this->fired;
    if (is_firing(data, is_null) &&
        std::any_of(callbacks->begin(), callbacks->end(),
                    [](const Callbacks::value_type& entry) { return entry.second->event; }))
      callbacks_fired = this->fired;
  }
  condition.notify_all();
  return true;
}

bool StreamImpl::has_updated() const { return updated; }

uint64_t StreamImpl::get_version() const {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  return version;
}

uint64_t StreamImpl::get_fired() const {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  return fired;
}

bool StreamImpl::has_ended() const {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  return ending != nullptr;
}

void StreamImpl::throw_if_ended() const {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  if (ending) std::rethrow_exception(ending);
}

StreamImpl::EndCallbacks StreamImpl::end(const std::exception_ptr& ending) {
  std::unique_lock<std::mutex> guard(condition_mutex, std::defer_lock);
  if (!is_held()) guard.lock();
  return end_held(ending);
}

StreamImpl::EndCallbacks StreamImpl::end_held(const std::exception_ptr& ending) {
  EndCallbacks result;
  {
    std::lock_guard<std::recursive_mutex> update_guard(*update_lock);
    if (this->ending) return result;
    this->ending = ending;
    result.swap(end_callbacks);
  }
  condition.notify_all();
  return result;
}

bool StreamImpl::wait(std::unique_lock<std::mutex>* lock, double timeout,
                      const std::function<bool()>& done) {
  // Clamped so that the deadline does not overflow the clock
  auto duration = std::chrono::duration<double>(std::min(timeout, 1e9));
  auto deadline = std::chrono::steady_clock::now() +
                  std::chrono::duration_cast<std::chrono::steady_clock::duration>(duration);
  while (!done()) {
    throw_if_ended();
    if (timeout < 0) {
      condition.wait(*lock);
      continue;
    }
    if (std::chrono::steady_clock::now() >= deadline) return false;
    condition.wait_until(*lock, deadline);
  }
  return true;
}

void StreamImpl::with_condition(const std::function<void(std::unique_lock<std::mutex>&)>& fn) {
  if (!is_held()) {
    std::unique_lock<std::mutex> lock(condition_mutex);
    fn(lock);
    return;
  }
  // The thread's own hold, which a wait in fn releases and takes again
  std::unique_lock<std::mutex> lock(condition_mutex, std::adopt_lock);
  try {
    fn(lock);
  } catch (...) {
    lock.release();
    throw;
  }
  lock.release();
}

bool StreamImpl::is_held() const { return holds.held(); }

void StreamImpl::add_hold() { holds.add(); }

bool StreamImpl::remove_hold() { return holds.remove(); }

std::condition_variable& StreamImpl::get_condition() { return condition; }

std::mutex& StreamImpl::get_condition_mutex() { return condition_mutex; }

int StreamImpl::add_callback(const Callback& callback) {
  int tag = 0;
  auto entry = std::make_shared<CallbackEntry>(callback, false);
  bool has_value = false;
  std::string value;
  bool value_is_null = false;
  uint64_t value_version = 0;
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    tag = next_callback_tag;
    next_callback_tag++;
    callbacks[tag] = entry;
    has_value = updated && !ending;
    value = data;
    value_is_null = _is_null;
    value_version = version;
  }
  // Called with the value the stream already has, unless an update has passed it a later one
  if (has_value) deliver(entry.get(), value, value_is_null, value_version);
  return tag;
}

int StreamImpl::add_event_callback(const Callback& callback, uint64_t seen) {
  int tag = 0;
  auto entry = std::make_shared<CallbackEntry>(callback, true);
  bool replay = false;
  uint64_t replay_fired = 0;
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    tag = next_callback_tag;
    next_callback_tag++;
    callbacks[tag] = entry;
    if (!ending && fired > std::max(seen, callbacks_fired)) {
      callbacks_fired = fired;
      replay = true;
      replay_fired = fired;
    }
  }
  if (replay) deliver(entry.get(), "\x01", false, replay_fired);
  return tag;
}

void StreamImpl::remove_callback(int tag) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  callbacks.erase(tag);
}

int StreamImpl::add_end_callback(const EndCallback& callback) {
  std::exception_ptr ended;
  int tag = 0;
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    tag = next_end_callback_tag;
    next_end_callback_tag++;
    ended = ending;
    if (!ended) end_callbacks[tag] = callback;
  }
  // A stream that has already ended calls the callback here, as no ending is to come
  if (ended) call_end_callbacks({{tag, callback}}, ended);
  return tag;
}

void StreamImpl::remove_end_callback(int tag) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  end_callbacks.erase(tag);
}

void StreamImpl::remove() {
  // The client of an ended stream can be gone
  if (has_ended()) return;
  client->streams().remove_stream(this);
}

void StreamImpl::call_callbacks(const Callbacks& callbacks, const std::string& data, bool is_null,
                                uint64_t version, uint64_t fired) {
  for (const auto& callback : callbacks) {
    auto* entry = callback.second.get();
    deliver(entry, data, is_null, entry->event ? fired : version);
  }
}

void StreamImpl::deliver(CallbackEntry* entry, const std::string& data, bool is_null,
                         uint64_t count) {
  if (entry->event && !is_firing(data, is_null)) return;
  std::lock_guard<std::recursive_mutex> running(entry->mutex);
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    if (ending || entry->delivered >= count) return;
    entry->delivered = count;
  }
  // An exception leaving the update thread calls std::terminate, so anything a callback throws
  // is reported
  try {
    entry->callback(data, is_null);
  } catch (const CallbackDecodeError& exn) {
    // Reported once, as every later value can fail the same way
    if (!decode_failure_reported.exchange(true))
      std::cerr << "kRPC: failed to decode a value of stream " << id
                << " for a callback: " << exn.what() << "\n";
  } catch (const std::exception& exn) {
    std::cerr << "kRPC: exception thrown by a stream callback: " << exn.what() << "\n";
  } catch (...) {
    std::cerr << "kRPC: unknown exception thrown by a stream callback\n";
  }
}

void call_end_callbacks(const StreamImpl::EndCallbacks& callbacks,
                        const std::exception_ptr& ending) {
  for (const auto& callback : callbacks) {
    try {
      callback.second(ending);
    } catch (const std::exception& exn) {
      std::cerr << "kRPC: exception thrown by a stream end callback: " << exn.what() << "\n";
    } catch (...) {
      std::cerr << "kRPC: unknown exception thrown by a stream end callback\n";
    }
  }
}

}  // namespace krpc
