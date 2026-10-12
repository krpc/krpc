#pragma once

#include <exception>
#include <functional>
#include <map>
#include <memory>
#include <mutex>  // NOLINT(build/c++11)
#include <string>
#include <utility>

#include "krpc/client.hpp"
#include "krpc/condition.hpp"
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
  Stream(Client* client, uint64_t id);
  Stream(const Stream<T>& other);
  Stream(Stream<T>&& other) noexcept;
  Stream<T>& operator=(const Stream<T>& other);
  Stream<T>& operator=(Stream<T>&& other) noexcept;
  ~Stream();
  /** The rate of the stream, in Hertz. Zero if the rate is unlimited. */
  float rate();
  /** Set the rate of the stream, in Hertz. Zero if the rate is unlimited. */
  void set_rate(float value);
  /** Get the most recent value for this stream. Blocks until the first value arrives. Throws
      the exception that ended the stream, if it has ended. */
  T operator()();
  /** Condition variable that is notified when the stream updates. Every copy of the stream
      object shares it. */
  Condition& get_condition() const;
  /** Lock on the condition's mutex. Each stream object has its own lock, used by one thread,
      and the mutex is re-entrant. */
  std::unique_lock<RecursiveMutex>& get_condition_lock() const;
  /** Lock the condition's mutex, which every copy of the stream object shares */
  void acquire();
  /** Unlock the condition's mutex, which every copy of the stream object shares */
  void release();
  /** Wait until the next stream update. If timeout >= 0, the operation times out after that
      many seconds. Returns true if the stream updated, and false on a timeout. Throws the
      exception that ended the stream, if it ends before or during the wait. */
  bool wait(double timeout = -1);
  typedef std::function<void(T)> Callback;
  /**
   * Add a callback that is invoked whenever the stream is updated. It is invoked straight
   * away with the value the stream already has, if any. An error from the stream does not
   * invoke the callback. Calling the stream throws it. Returns an integer tag for the
   * callback which uniquely identifies it, and allows it to be removed using remove_callback()
   */
  int add_callback(const Callback& callback);
  /** Remove a callback, based on its tag */
  void remove_callback(int tag);
  typedef std::function<void(std::exception_ptr)> EndCallback;
  /**
   * Add a callback that is invoked once when the stream ends, with the exception that ended
   * it. It is invoked straight away if the stream has already ended. Returns an integer tag
   * for the callback, which allows it to be removed using remove_end_callback()
   */
  int add_end_callback(const EndCallback& callback);
  /** Remove an end callback, based on its tag */
  void remove_end_callback(int tag);
  /** Remove the stream from the server, for every copy of the stream object. Another stream
      added for the same call stays live. */
  void remove();
  bool operator==(const Stream<T>& rhs) const;
  bool operator!=(const Stream<T>& rhs) const;
  explicit operator bool() const;

 private:
  friend class Event;
  std::shared_ptr<StreamImpl> impl;
  // Declared after impl, so that it releases the mutex before the stream can be destroyed
  mutable std::unique_lock<RecursiveMutex> lock;
  /** Release the lock, unlocking the mutex only if this thread holds it. */
  void drop_lock();
  std::unique_lock<RecursiveMutex> make_lock() const;
  void check_exists() const;
  static T decode_value(const std::string& data, bool is_null, Client* client);
};

template <typename T>
inline Stream<T>::Stream() : impl(nullptr) {}

template <typename T>
inline Stream<T>::Stream(Client* client, const schema::ProcedureCall& call)
    : impl(client->add_stream(call)), lock(make_lock()) {}

template <typename T>
inline Stream<T>::Stream(Client* client, uint64_t id)
    : impl(client->get_stream(id)), lock(make_lock()) {}

template <typename T>
inline Stream<T>::Stream(const Stream<T>& other) : impl(other.impl), lock(make_lock()) {}

template <typename T>
inline Stream<T>::Stream(Stream<T>&& other) noexcept
    : impl(std::move(other.impl)), lock(std::move(other.lock)) {}

template <typename T>
inline Stream<T>::~Stream() {
  drop_lock();
}

template <typename T>
inline Stream<T>& Stream<T>::operator=(const Stream<T>& other) {
  if (this == &other) return *this;
  // Released before the stream it locks can be destroyed
  drop_lock();
  impl = other.impl;
  lock = make_lock();
  return *this;
}

template <typename T>
inline Stream<T>& Stream<T>::operator=(Stream<T>&& other) noexcept {
  if (this == &other) return *this;
  // Released before the stream it locks can be destroyed
  drop_lock();
  lock = std::move(other.lock);
  impl = std::move(other.impl);
  return *this;
}

template <typename T>
inline void Stream<T>::drop_lock() {
  detail::release_if_held_elsewhere(&lock);
  lock = std::unique_lock<RecursiveMutex>();
}

template <typename T>
inline std::unique_lock<RecursiveMutex> Stream<T>::make_lock() const {
  if (!impl) return std::unique_lock<RecursiveMutex>();
  return std::unique_lock<RecursiveMutex>(impl->get_condition_mutex(), std::defer_lock);
}

template <typename T>
inline float Stream<T>::rate() {
  check_exists();
  return impl->rate();
}

template <typename T>
inline void Stream<T>::set_rate(float value) {
  check_exists();
  impl->set_rate(value);
}

template <typename T>
inline T Stream<T>::operator()() {
  check_exists();
  if (!impl->has_updated()) {
    std::lock_guard<RecursiveMutex> held(impl->get_condition_mutex());
    while (!impl->has_updated() && !impl->has_ended()) impl->get_condition().wait();
  }
  bool is_null = false;
  auto data = impl->get_data(&is_null);
  return decode_value(data, is_null, impl->get_client());
}

template <typename T>
inline T Stream<T>::decode_value(const std::string& data, bool is_null, Client* client) {
  // A null value is signaled out-of-band by is_null, and leaves the value default
  // constructed, which for a nullable stream is an empty optional.
  T value{};
  // Unqualified, so that argument dependent lookup finds the overload for a type a
  // service defines at the point this template is used
  using decoder::decode;
  if (!is_null) decode(value, data, client);
  return value;
}

template <typename T>
inline Condition& Stream<T>::get_condition() const {
  check_exists();
  return impl->get_condition();
}

template <typename T>
inline std::unique_lock<RecursiveMutex>& Stream<T>::get_condition_lock() const {
  check_exists();
  return lock;
}

template <typename T>
inline void Stream<T>::acquire() {
  check_exists();
  impl->get_condition_mutex().lock();
}

template <typename T>
inline void Stream<T>::release() {
  check_exists();
  impl->get_condition_mutex().unlock();
}

template <typename T>
inline bool Stream<T>::wait(double timeout) {
  check_exists();
  std::lock_guard<RecursiveMutex> held(impl->get_condition_mutex());
  impl->throw_if_ended();
  auto version = impl->get_version();
  return impl->wait(timeout, [this, version] { return impl->get_version() != version; });
}

template <typename T>
inline int Stream<T>::add_callback(const Callback& callback) {
  check_exists();
  // Captures no handle, as the handle can be moved or destroyed while the callback is added
  auto callback_wrapper = [client = impl->get_client(), callback](const std::string& data,
                                                                  bool is_null) {
    T value{};
    try {
      value = decode_value(data, is_null, client);
    } catch (const std::exception& exn) {
      throw CallbackDecodeError(exn.what());
    }
    callback(std::move(value));
  };
  return impl->add_callback(callback_wrapper);
}

template <typename T>
inline void Stream<T>::remove_callback(int tag) {
  check_exists();
  impl->remove_callback(tag);
}

template <typename T>
inline int Stream<T>::add_end_callback(const EndCallback& callback) {
  check_exists();
  return impl->add_end_callback(callback);
}

template <typename T>
inline void Stream<T>::remove_end_callback(int tag) {
  check_exists();
  impl->remove_end_callback(tag);
}

template <typename T>
inline void Stream<T>::remove() {
  if (!impl) return;
  impl->remove();
}

template <typename T>
inline bool Stream<T>::operator==(const Stream<T>& rhs) const {
  if (!impl) return !rhs.impl;
  if (!rhs.impl) return false;
  return impl->get_id() == rhs.impl->get_id();
}

template <typename T>
inline bool Stream<T>::operator!=(const Stream<T>& rhs) const {
  if (!impl) return rhs.impl.operator bool();
  if (!rhs.impl) return true;
  return impl->get_id() != rhs.impl->get_id();
}

template <typename T>
Stream<T>::operator bool() const {
  return impl.operator bool();
}

template <typename T>
inline void Stream<T>::check_exists() const {
  if (!impl) throw StreamError("Stream does not exist or was removed");
}

}  // namespace krpc
