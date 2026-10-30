#include "krpc/stream_impl.hpp"

#include <algorithm>
#include <atomic>
#include <chrono>  // NOLINT(build/c++11)
#include <cmath>
#include <exception>
#include <iostream>
#include <memory>
#include <string>
#include <thread>  // NOLINT(build/c++11)
#include <utility>

#include "krpc/client.hpp"
#include "krpc/error.hpp"
#include "krpc/race_point.hpp"
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

StreamImpl::~StreamImpl() {
  if (ending) return;
  try {
    if (auto held_manager = manager.lock()) held_manager->drop_stream(id);
  } catch (...) {  // NOLINT(bugprone-empty-catch)
    // The connection closing also removes the stream
  }
}

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
  bool held = condition_mutex.held_by_this_thread();
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  if (ending_visible()) std::rethrow_exception(ending);
  if (!(held ? announced : updated)) throw StreamError("Stream has no value");
  seen[std::this_thread::get_id()] = held ? announced_version : version;
  *is_null = held ? announced_is_null : _is_null;
  return held ? announced_data : data;
}

uint64_t StreamImpl::get_seen_version() const {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  auto it = seen.find(std::this_thread::get_id());
  return it == seen.end() ? 0 : it->second;
}

void StreamImpl::announce(const std::string& data, bool is_null, uint64_t version) {
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    announced_data = data;
    announced_is_null = is_null;
    announced_version = version;
    announced.store(true);
  }
  condition.notify_all();
}

void StreamImpl::set_value(const std::string& data, bool is_null) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  this->data = data;
  this->_is_null = is_null;
  updated.store(true);
  version++;
  if (is_firing(data, is_null)) fired++;
}

bool StreamImpl::has_updated() const {
  return condition_mutex.held_by_this_thread() ? announced : updated;
}

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

bool StreamImpl::is_ended_by_client() const {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  return ended_by_client;
}

bool StreamImpl::is_ending_announced() const {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  return announced_ending != nullptr;
}

bool StreamImpl::ending_visible() const {
  if (!ending) return false;
  if (ending == announced_ending || !condition_mutex.held_by_this_thread()) return true;
  auto held_manager = manager.lock();
  return !held_manager || held_manager->is_closed();
}

void StreamImpl::throw_if_ended() const {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  if (ending_visible()) std::rethrow_exception(ending);
}

StreamImpl::EndCallbacks StreamImpl::set_ending(const std::exception_ptr& ending, bool announce,
                                                bool by_client) {
  std::lock_guard<std::recursive_mutex> update_guard(*update_lock);
  EndCallbacks result;
  if (this->ending) return result;
  this->ending = ending;
  ended_by_client = by_client;
  if (announce) announced_ending = ending;
  result.swap(end_callbacks);
  return result;
}

void StreamImpl::announce_ending() {
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    announced_ending = ending;
  }
  condition.notify_all();
}

StreamImpl::EndCallbacks StreamImpl::end(const std::exception_ptr& ending) {
  std::lock_guard<RecursiveMutex> held(condition_mutex);
  auto result = set_ending(ending, true, true);
  condition.notify_all();
  return result;
}

bool StreamImpl::wait(double timeout, const std::function<bool()>& done) {
  if (std::isnan(timeout) || timeout < 0)
    throw std::invalid_argument("The timeout must not be negative or NaN");
  bool infinite = std::isinf(timeout);
  // Clamped so that the deadline does not overflow the clock
  auto duration = std::chrono::duration<double>(std::min(timeout, 1e9));
  auto deadline = std::chrono::steady_clock::now() +
                  std::chrono::duration_cast<std::chrono::steady_clock::duration>(duration);
  while (!done()) {
    throw_if_ended();
    if (infinite) {
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
    detail::reach_race_point(RacePoint::first_call_started, callback->tag);
  } else if (!enter_on_update_thread(callback.get(), version, data, is_null)) {
    return;
  }
  std::lock_guard<RecursiveMutex> running(callback->lock, std::adopt_lock);
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    // A late event callback is passed an unseen firing after a server ending
    bool ended = ending && (!added || ended_by_client);
    if (ended || !note_passed(callback.get(), version)) return;
    callback->adding = added;
  }
  // invoke reports what a callback throws, so the adding thread always passes on the queue
  invoke(*callback, data, is_null);
  if (added) {
    call_missed(callback.get());
    detail::reach_race_point(RacePoint::first_call_ended, callback->tag);
  }
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
  std::deque<CallbackEntry::Value> missed;
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    missed.swap(callback->missed);
    callback->adding = false;
    callback->draining = true;
  }
  for (const auto& value : missed) {
    {
      std::lock_guard<std::recursive_mutex> guard(*update_lock);
      if (ended_by_client) break;
      if (!note_passed(callback, value.version)) continue;
    }
    invoke(*callback, value.data, value.is_null);
  }
  EndCallbacks end_callbacks;
  std::exception_ptr deferred;
  {
    std::lock_guard<std::recursive_mutex> guard(*update_lock);
    callback->draining = false;
    end_callbacks.swap(callback->end_callbacks);
    deferred = callback->ending;
    callback->ending = nullptr;
  }
  if (deferred) call_end_callbacks(end_callbacks, deferred);
}

bool StreamImpl::defer_ending(const EndCallbacks& callbacks, const std::exception_ptr& ending) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  for (const auto& entry : this->callbacks) {
    auto& callback = entry.second;
    if (callback->adding || callback->draining) {
      callback->end_callbacks = callbacks;
      callback->ending = ending;
      return true;
    }
  }
  return false;
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

// Anything a callback throws is reported, and the dispatcher thread runs on
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
  if (has_value) {
    detail::reach_race_point(RacePoint::first_call, entry->tag);
    call(entry, value_version, value, value_is_null, true);
  }
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
  if (replay_fired > 0) {
    detail::reach_race_point(RacePoint::replay, entry->tag);
    replay(entry, replay_fired);
  }
  return entry->tag;
}

uint64_t StreamImpl::reserve_replay(uint64_t seen) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  if (ended_by_client || fired <= std::max({seen, callbacks_fired, replaying})) return 0;
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

// The callback is destroyed once the lock is released, as it can hold the last copy of the client
void StreamImpl::remove_callback(int tag) {
  std::shared_ptr<CallbackEntry> removed;
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  auto it = callbacks.find(tag);
  if (it == callbacks.end()) return;
  removed = std::move(it->second);
  callbacks.erase(it);
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
  EndCallback removed;
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  auto it = end_callbacks.find(tag);
  if (it == end_callbacks.end()) return;
  removed = std::move(it->second);
  end_callbacks.erase(it);
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
