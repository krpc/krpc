#include "krpc/event.hpp"

#include <memory>
#include <string>

#include "krpc/krpc.pb.hpp"
#include "krpc/stream_impl.hpp"

namespace krpc {

class Client;

Event::Event() {
}

Event::Event(Client * client, const krpc::schema::Event& message) :
  _stream(client, message.stream().id()) {
}

void Event::start() {
  _stream.start(false);
}

std::condition_variable& Event::get_condition() const {
  return _stream.get_condition();
}

std::unique_lock<std::mutex>& Event::get_condition_lock() const {
  return _stream.get_condition_lock();
}

void Event::acquire() {
  _stream.acquire();
}

void Event::release() {
  _stream.release();
}

void Event::wait(double timeout) {
  start();
  _stream.impl->update(std::string("\x00", 1), nullptr);
  while (!_stream()) {
    bool origValue = _stream();
    _stream.wait(timeout);
    if (timeout >= 0 && _stream() == origValue)
      // Value did not change, must have timed out
      return;
  }
}

int Event::add_callback(const Callback& callback) {
  return _stream.add_callback([callback] (bool _) { callback(); });
}

void Event::remove_callback(int tag) {
  return _stream.remove_callback(tag);
}

void Event::remove() {
  _stream.remove();
}

Stream<bool> Event::stream() const {
  return _stream;
}

bool Event::operator==(const Event& rhs) const {
  return _stream == rhs._stream;
}

bool Event::operator!=(const Event& rhs) const {
  return _stream != rhs._stream;
}

Event::operator bool() const {
  return _stream.operator bool();
}

}  // namespace krpc
