#include "krpc/event.hpp"

#include <cstdint>
#include <memory>
#include <mutex>  // NOLINT(build/c++11)
#include <string>

#include "krpc/condition.hpp"
#include "krpc/krpc.pb.hpp"
#include "krpc/stream_impl.hpp"

namespace krpc {

class Client;

Event::Event() {}

Event::Event(Client* client, const krpc::schema::Event& message)
    : _stream(client, message.stream().id()) {}

Condition& Event::get_condition() const { return _stream.get_condition(); }

std::unique_lock<RecursiveMutex>& Event::get_condition_lock() const {
  return _stream.get_condition_lock();
}

void Event::acquire() { _stream.acquire(); }

void Event::release() { _stream.release(); }

bool Event::wait(double timeout) {
  _stream.check_exists();
  auto& impl = _stream.impl;
  std::lock_guard<RecursiveMutex> held(impl->get_condition_mutex());
  // A firing not yet seen returns before an ending is thrown
  auto last_seen = seen;
  if (!impl->wait(timeout, [&impl, last_seen] { return impl->get_fired() > last_seen; }))
    return false;
  seen = impl->get_fired();
  return true;
}

int Event::add_callback(const Callback& callback) {
  _stream.check_exists();
  auto& impl = _stream.impl;
  uint64_t last_seen = 0;
  {
    std::lock_guard<RecursiveMutex> held(impl->get_condition_mutex());
    last_seen = seen;
  }
  return impl->add_event_callback(
      [callback](const std::string& data, bool is_null) {
        // An encoded bool true
        if (!is_null && data == "\x01") callback();
      },
      last_seen);
}

void Event::remove_callback(int tag) { return _stream.remove_callback(tag); }

int Event::add_end_callback(const EndCallback& callback) {
  return _stream.add_end_callback(callback);
}

void Event::remove_end_callback(int tag) { _stream.remove_end_callback(tag); }

void Event::remove() { _stream.remove(); }

Stream<bool> Event::stream() const { return _stream; }

bool Event::operator==(const Event& rhs) const { return _stream == rhs._stream; }

bool Event::operator!=(const Event& rhs) const { return _stream != rhs._stream; }

Event::operator bool() const { return _stream.operator bool(); }

}  // namespace krpc
