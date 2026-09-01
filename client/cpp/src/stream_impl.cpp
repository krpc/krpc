#include "krpc/stream_impl.hpp"

#include <string>

#include "krpc/client.hpp"
#include "krpc/error.hpp"
#include "krpc/services/krpc.hpp"

namespace krpc {

StreamImpl::StreamImpl(Client* client, uint64_t id, std::recursive_mutex* update_lock)
    : client(client),
      id(id),
      update_lock(update_lock),
      started(false),
      updated(false),
      _is_null(false),
      condition_lock(condition_mutex, std::defer_lock),
      next_callback_tag(0),
      _rate(0) {}

Client* StreamImpl::get_client() const { return client; }

uint64_t StreamImpl::get_id() const { return id; }

void StreamImpl::start() {
  if (!started) {
    services::KRPC(client).start_stream(id);
    started = true;
  }
}

float StreamImpl::rate() const { return _rate; }

void StreamImpl::set_rate(float value) {
  _rate = value;
  services::KRPC(client).set_stream_rate(id, value);
}

bool StreamImpl::has_started() const { return started; }

const std::string& StreamImpl::get_data() {
  if (!updated) throw StreamError("Stream has no value");
  if (exception) std::rethrow_exception(exception);
  return data;
}

bool StreamImpl::is_null() const { return _is_null; }

void StreamImpl::update(const std::string& data, bool is_null,
                        const std::exception_ptr& exception) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  // Store the value before the flag that says there is one. A reader checks the flag first
  // and takes no lock, so the other order lets it see the flag set and read the value that
  // has not been stored yet.
  this->data = data;
  this->_is_null = is_null;
  this->exception = exception;
  updated = true;
}

// Store an update and wake the threads waiting for one. We hold the condition across both,
// so that a notification cannot fire between a waiter checking the value and entering its
// wait.
void StreamImpl::update_and_notify(const std::string& data, bool is_null,
                                   const std::exception_ptr& exception) {
  std::lock_guard<std::mutex> guard(condition_mutex);
  update(data, is_null, exception);
  condition.notify_all();
}

bool StreamImpl::has_updated() const { return updated; }

std::condition_variable& StreamImpl::get_condition() { return condition; }

std::unique_lock<std::mutex>& StreamImpl::get_condition_lock() { return condition_lock; }

const StreamImpl::Callbacks& StreamImpl::get_callbacks() const { return callbacks; }

int StreamImpl::add_callback(const Callback& callback) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  auto tag = next_callback_tag;
  next_callback_tag++;
  callbacks[tag] = callback;
  return tag;
}

void StreamImpl::remove_callback(int tag) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  callbacks.erase(tag);
}

void StreamImpl::remove() { client->remove_stream(id); }

}  // namespace krpc
