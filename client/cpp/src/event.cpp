#include "krpc/event.hpp"

#include <memory>
#include <string>

#include "krpc/krpc.pb.hpp"
#include "krpc/stream_impl.hpp"

namespace krpc {

class Client;

Event::Event() {}

Event::Event(Client* client, const krpc::schema::Event& message)
    : _stream(client, message.stream().id()) {}

void Event::start() { _stream.start(false); }

std::condition_variable& Event::get_condition() const { return _stream.get_condition(); }

std::unique_lock<std::mutex>& Event::get_condition_lock() const {
  return _stream.get_condition_lock();
}

void Event::acquire() { _stream.acquire(); }

void Event::release() { _stream.release(); }

bool Event::wait(double timeout) {
  _stream.check_exists();
  auto& impl = _stream.impl;
  bool fired = false;
  impl->with_condition([this, &impl, timeout, &fired](std::unique_lock<std::mutex>& held) {
    if (!impl->has_started()) {
      impl->throw_if_ended();
      impl->start();
    }
    // A firing not yet seen returns before an ending is thrown
    auto last_seen = seen;
    fired =
        impl->wait(&held, timeout, [&impl, last_seen] { return impl->get_fired() > last_seen; });
    if (fired) seen = impl->get_fired();
  });
  return fired;
}

int Event::add_callback(const Callback& callback) {
  return _stream.add_callback([callback](bool _) { callback(); });
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
