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

StreamImpl::StreamImpl(Client* client, uint64_t id,
                       std::shared_ptr<std::recursive_mutex> update_lock)
    : client(client),
      id(id),
      update_lock(std::move(update_lock)),
      updated(false),
      _is_null(false),
      version(0),
      fired(0),
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
  // Store the value before the flag that says there is one. A reader checks the flag first
  // and takes no lock, so the other order lets it see the flag set and read the value that
  // has not been stored yet.
  this->data = data;
  this->_is_null = is_null;
  updated = true;
  version++;
  // An encoded bool true
  if (!is_null && data == "\x01") fired++;
  return true;
}

// The caller holds the condition across the store and the notification, so that a
// notification cannot fire between a waiter checking the value and entering its wait. The
// callbacks are read with the value stored, so a callback added after this is called with the
// value by add_callback.
bool StreamImpl::update_and_notify(const std::string& data, bool is_null, Callbacks* callbacks,
                                   uint64_t* version) {
  {
    std::lock_guard<std::recursive_mutex> update_guard(*update_lock);
    if (!store(data, is_null)) return false;
    *callbacks = this->callbacks;
    *version = this->version;
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

bool StreamImpl::is_held() const {
  std::lock_guard<std::mutex> guard(holds_mutex);
  return holds.count(std::this_thread::get_id()) > 0;
}

void StreamImpl::add_hold() {
  std::lock_guard<std::mutex> guard(holds_mutex);
  holds[std::this_thread::get_id()]++;
}

bool StreamImpl::remove_hold() {
  std::lock_guard<std::mutex> guard(holds_mutex);
  auto hold = holds.find(std::this_thread::get_id());
  if (hold == holds.end()) return false;
  if (--hold->second > 0) return false;
  holds.erase(hold);
  return true;
}

std::condition_variable& StreamImpl::get_condition() { return condition; }

std::mutex& StreamImpl::get_condition_mutex() { return condition_mutex; }

int StreamImpl::add_callback(const Callback& callback) {
  int tag = 0;
  auto entry = std::make_shared<CallbackEntry>(CallbackEntry{callback, 0});
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
  if (has_value) call_callbacks({{tag, entry}}, value, value_is_null, value_version);
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
                                uint64_t version) {
  // An exception leaving the update thread calls std::terminate. Anything a callback throws is
  // reported, including a value that fails to decode.
  for (const auto& callback : callbacks) {
    {
      std::lock_guard<std::recursive_mutex> guard(*update_lock);
      if (callback.second->delivered >= version) continue;
      callback.second->delivered = version;
    }
    try {
      callback.second->callback(data, is_null);
    } catch (const std::exception& exn) {
      std::cerr << "kRPC: exception thrown by a stream callback: " << exn.what() << "\n";
    } catch (...) {
      std::cerr << "kRPC: unknown exception thrown by a stream callback\n";
    }
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
