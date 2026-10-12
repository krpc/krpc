#include "krpc/stream_impl.hpp"

#include <algorithm>
#include <atomic>
#include <chrono>  // NOLINT(build/c++11)
#include <exception>
#include <iostream>
#include <memory>
#include <string>
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

StreamImpl::StreamImpl(Client* client, uint64_t id,
                       std::shared_ptr<std::recursive_mutex> update_lock,
                       std::weak_ptr<StreamManager> manager)
    : client(client),
      id(id),
      update_lock(std::move(update_lock)),
      manager(std::move(manager)),
      updated(false),
      _is_null(false),
      version(0),
      fired(0),
      callbacks_fired(0),
      replaying(0),
      decode_failure_reported(false),
      condition(condition_mutex),
      next_callback_tag(0),
      next_end_callback_tag(0),
      _rate(0) {}

Client* StreamImpl::get_client() const { return client; }

uint64_t StreamImpl::get_id() const { return id; }

float StreamImpl::rate() const { return _rate; }

void StreamImpl::set_rate(float value) {
  _rate = value;
  // Holding the manager keeps its client alive for the call
  auto held_manager = manager.lock();
  if (!held_manager || has_ended()) return;
  services::KRPC(client).set_stream_rate(id, value);
}

std::string StreamImpl::get_data(bool* is_null) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  if (ending) std::rethrow_exception(ending);
  if (!updated) throw StreamError("Stream has no value");
  *is_null = _is_null;
  return data;
}

void StreamImpl::set_value(const std::string& data, bool is_null) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  // Stored before the flag, as a reader checks the flag with no lock held
  this->data = data;
  this->_is_null = is_null;
  updated.store(true);
  version++;
  if (is_firing(data, is_null)) fired++;
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
  std::lock_guard<RecursiveMutex> held(condition_mutex);
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

bool StreamImpl::wait(double timeout, const std::function<bool()>& done) {
  // Clamped so that the deadline does not overflow the clock
  auto duration = std::chrono::duration<double>(std::min(timeout, 1e9));
  auto deadline = std::chrono::steady_clock::now() +
                  std::chrono::duration_cast<std::chrono::steady_clock::duration>(duration);
  while (!done()) {
    throw_if_ended();
    if (timeout < 0) {
      condition.wait();
      continue;
    }
    if (std::chrono::steady_clock::now() >= deadline) return false;
    condition.wait_until(deadline);
  }
  return true;
}

Condition& StreamImpl::get_condition() { return condition; }

RecursiveMutex& StreamImpl::get_condition_mutex() { return condition_mutex; }

StreamImpl::Callbacks StreamImpl::get_callbacks() const {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  Callbacks result;
  result.reserve(callbacks.size());
  for (const auto& callback : callbacks) result.push_back(callback.second);
  return result;
}

void StreamImpl::call(const std::shared_ptr<CallbackEntry>& callback, uint64_t version,
                      const std::string& data, bool is_null, bool added) {
  if (callback->event && !is_firing(data, is_null)) return;
  if (added) {
    if (!callback->lock.try_lock()) return;
  } else if (!enter_on_update_thread(callback.get(), version, data, is_null)) {
    return;
  }
  std::lock_guard<RecursiveMutex> running(callback->lock, std::adopt_lock);
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    if (ending || !note_passed(callback.get(), version)) return;
    callback->adding = added;
  }
  // invoke reports what a callback throws, so the adding thread always passes on the queue
  invoke(*callback, data, is_null);
  if (added) call_missed(callback.get());
}

bool StreamImpl::enter_on_update_thread(CallbackEntry* callback, uint64_t version,
                                        const std::string& data, bool is_null) {
  while (!callback->lock.try_lock_for(std::chrono::milliseconds(10))) {
    auto held_manager = manager.lock();
    if (!held_manager || held_manager->is_close_requested()) return false;
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    if (callback->adding) {
      callback->missed.push_back({version, data, is_null});
      return false;
    }
  }
  return true;
}

void StreamImpl::call_missed(CallbackEntry* callback) {
  while (true) {
    CallbackEntry::Value value;
    {
      std::lock_guard<std::recursive_mutex> guard(*update_lock);
      if (callback->missed.empty()) {
        callback->adding = false;
        return;
      }
      value = std::move(callback->missed.front());
      callback->missed.pop_front();
      if (!note_passed(callback, value.version)) continue;
    }
    invoke(*callback, value.data, value.is_null);
  }
}

bool StreamImpl::note_passed(CallbackEntry* callback, uint64_t version) {
  auto registered = callbacks.find(callback->tag);
  if (version <= callback->version || registered == callbacks.end() ||
      registered->second.get() != callback)
    return false;
  callback->version = version;
  if (callback->event) callbacks_fired = std::max(callbacks_fired, version);
  return true;
}

// An exception leaving the update thread calls std::terminate, so anything a callback throws is
// reported
void StreamImpl::invoke(const CallbackEntry& callback, const std::string& data, bool is_null) {
  try {
    callback.callback(data, is_null);
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

int StreamImpl::add_callback(const Callback& callback) {
  std::shared_ptr<CallbackEntry> entry;
  bool has_value = false;
  std::string value;
  bool value_is_null = false;
  uint64_t value_version = 0;
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    entry = std::make_shared<CallbackEntry>(next_callback_tag++, callback, false);
    callbacks[entry->tag] = entry;
    has_value = updated && !ending;
    value = data;
    value_is_null = _is_null;
    value_version = version;
  }
  // Called with the value the stream already has, which no update will pass to it
  if (has_value) call(entry, value_version, value, value_is_null, true);
  return entry->tag;
}

int StreamImpl::add_event_callback(const Callback& callback, uint64_t seen) {
  std::shared_ptr<CallbackEntry> entry;
  uint64_t replay_fired = 0;
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    entry = std::make_shared<CallbackEntry>(next_callback_tag++, callback, true);
    callbacks[entry->tag] = entry;
    replay_fired = reserve_replay(seen);
  }
  if (replay_fired > 0) replay(entry, replay_fired);
  return entry->tag;
}

uint64_t StreamImpl::reserve_replay(uint64_t seen) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  if (ending || fired <= std::max({seen, callbacks_fired, replaying})) return 0;
  replaying = fired;
  return fired;
}

void StreamImpl::replay(const std::shared_ptr<CallbackEntry>& callback, uint64_t fired) {
  call(callback, fired, "\x01", false, true);
  // A replay that passes the firing has raised callbacks_fired. One that does not, as the
  // callback was removed or the stream ended, leaves the firing.
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  if (replaying == fired) replaying = 0;
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
  auto held_manager = manager.lock();
  if (!held_manager || has_ended()) return;
  held_manager->remove_stream(this);
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
