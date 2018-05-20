#pragma once

#include <condition_variable>  // NOLINT(build/c++11)
#include <map>
#include <string>

#include "krpc/client.hpp"
#include "krpc/decoder.hpp"
#include "krpc/error.hpp"
#include "krpc/krpc.pb.hpp"
#include "krpc/stream_impl.hpp"

namespace krpc {

template <typename T>
class Stream {
 public:
  Stream();
  Stream(Client* client, const schema::ProcedureCall& call);
  Stream(Client* client, google::protobuf::uint64 id);
  /** Start the stream. */
  void start(bool wait = true);
  /** The rate of the stream, in Hertz. Zero if the rate is unlimited. */
  float rate();
  /** Set the rate of the stream, in Hertz. Zero if the rate is unlimited. */
  void set_rate(float value);
  /** Get the most recent value for this stream. */
  T operator()();
  /** Condition variable that is notified when the stream updates */
  std::condition_variable& get_condition() const;
  /** Lock used with the condition variable */
  std::unique_lock<std::mutex>& get_condition_lock() const;
  /** Acquire a lock on the condition variable */
  void acquire();
  /** Release the lock on the condition variable */
  void release();
  /** Wait until the next stream update. If timeout >= 0, the
      operation times out after that many seconds. */
  void wait(double timeout = -1);
  typedef std::function<void(T)> Callback;
  /**
   * Add a callback that is invoked whenever the stream is updated.
   * Returns an integer tag for the callback which uniquely identifies it,
   * and allows it to be removed using remove_callback()
   */
  int add_callback(const Callback& callback);
  /** Remove a callback, based on its tag */
  void remove_callback(int tag);
  void remove();
  bool operator==(const Stream<T>& rhs) const;
  bool operator!=(const Stream<T>& rhs) const;
  explicit operator bool() const;

 private:
  friend class Event;
  std::shared_ptr<StreamImpl> impl;
  bool acquired;
  void check_exists() const;
};

template <typename T> inline Stream<T>::Stream() :
  impl(nullptr), acquired(false) {
}

template <typename T> inline Stream<T>::Stream(Client* client, const schema::ProcedureCall& call) :
  impl(client->add_stream(call)), acquired(false) {
}

template <typename T> inline Stream<T>::Stream(Client* client, google::protobuf::uint64 id) :
  impl(client->get_stream(id)), acquired(false) {
}

template <typename T> inline void Stream<T>::start(bool wait) {
  check_exists();
  if (impl->has_started())
    return;
  if (!wait) {
    impl->start();
  } else {
    if (!acquired)
      impl->get_condition_lock().lock();
    impl->start();
    impl->get_condition().wait(impl->get_condition_lock());
    if (!acquired)
      impl->get_condition_lock().unlock();
  }
}

template <typename T> inline float Stream<T>::rate() {
  check_exists();
  return impl->rate();
}

template <typename T> inline void Stream<T>::set_rate(float value) {
  check_exists();
  impl->set_rate(value);
}

template <typename T> inline T Stream<T>::operator()() {
  check_exists();
  if (!impl->has_started())
    start();
  std::string data = impl->get_data();
  T value;
  decoder::decode(value, data, impl->get_client());
  return value;
}

template <typename T> inline std::condition_variable& Stream<T>::get_condition() const {
  check_exists();
  return impl->get_condition();
}

template <typename T> inline std::unique_lock<std::mutex>& Stream<T>::get_condition_lock() const {
  check_exists();
  return impl->get_condition_lock();
}

template <typename T> inline void Stream<T>::acquire() {
  check_exists();
  acquired = true;
  impl->get_condition_lock().lock();
}

template <typename T> inline void Stream<T>::release() {
  check_exists();
  impl->get_condition_lock().unlock();
  acquired = false;
}

template <typename T> inline void Stream<T>::wait(double timeout) {
  check_exists();
  if (!impl->has_started())
    impl->start();
  if (timeout < 0) {
    impl->get_condition().wait(impl->get_condition_lock());
  } else {
    auto rel_time = std::chrono::milliseconds(static_cast<int>(timeout*1000));
    impl->get_condition().wait_for(impl->get_condition_lock(), rel_time);
  }
}

template <typename T> inline int Stream<T>::add_callback(const Callback& callback) {
  check_exists();
  auto callback_wrapper = [this, callback] (const std::string& data) {
    T value;
    decoder::decode(value, data, this->impl->get_client());
    callback(value);
  };
  return impl->add_callback(callback_wrapper);
}

template <typename T> inline void Stream<T>::remove_callback(int tag) {
  check_exists();
  impl->remove_callback(tag);
}

template <typename T> inline void Stream<T>::remove() {
  if (!impl) return;
  impl->remove();
  impl = nullptr;
}

template <typename T> inline bool Stream<T>::operator==(const Stream<T>& rhs) const {
  if (!impl)
    return !rhs.impl;
  if (!rhs.impl)
    return false;
  return impl->get_id() == rhs.impl->get_id();
}

template <typename T> inline bool Stream<T>::operator!=(const Stream<T>& rhs) const {
  if (!impl)
    return rhs.impl.operator bool();
  if (!rhs.impl)
    return true;
  return impl->get_id() != rhs.impl->get_id();
}

template <typename T> Stream<T>::operator bool() const {
  return impl.operator bool();
}

template <typename T> inline void Stream<T>::check_exists() const {
  if (!impl)
    throw StreamError("Stream does not exist or was removed");
}

}  // namespace krpc
