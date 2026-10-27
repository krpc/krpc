// Conversions between conformance Values and the client's values, without the client's
// encoder, for the generated dispatch.
#pragma once

#include <any>
#include <chrono>
#include <condition_variable>
#include <cstdint>
#include <exception>
#include <functional>
#include <krpc.hpp>
#include <map>
#include <memory>
#include <mutex>
#include <optional>
#include <set>
#include <stdexcept>
#include <string>
#include <tuple>
#include <utility>
#include <vector>

#include "testee.pb.h"

namespace krpc_conformance {

using Value = krpc::conformance::Value;
using Arguments = google::protobuf::RepeatedPtrField<Value>;

/** A stream, with its values converted to conformance Values. */
struct StreamHandle {
  std::function<Value()> get;
  std::function<bool(double)> wait;
  // Lock and unlock the condition's mutex, as a program holding the condition does
  std::function<void()> acquire;
  std::function<void()> release;
  // Wait on the condition itself, holding it, and return whether the wait was woken in time
  std::function<bool(double)> wait_on_condition;
  // Notify the threads waiting on the condition, holding it, with no update
  std::function<void()> notify_condition;
  std::function<float()> rate;
  std::function<void(float)> set_rate;
  std::function<void()> remove;
  // A copy of the stream object, as the language copies one
  std::function<StreamHandle()> copy;
  // The stream object, and its equality with another handle's
  std::any value;
  std::function<bool(const std::any&)> equals;
  std::function<int(std::function<void(const Value&)>)> add_callback;
  std::function<void(int)> remove_callback;
  std::function<int(std::function<void(std::exception_ptr)>)> add_end_callback;
  std::function<void(int)> remove_end_callback;
};

/** An object the testee holds, how to compare it with another, and its hash. */
struct Held {
  std::any value;
  std::function<bool(const std::any&)> equals;
  std::function<std::size_t()> hash;
};

/** The objects and streams the testee holds, by handle. Each object a call returns gets a new
    handle, so two handles can refer to one server object. */
class Handles {
 public:
  static Handles& instance() {
    static Handles handles;
    return handles;
  }

  uint64_t add_object(Held held) {
    std::lock_guard<std::mutex> lock(mutex);
    objects[next] = std::move(held);
    return next++;
  }

  Held object(uint64_t handle) {
    std::lock_guard<std::mutex> lock(mutex);
    return objects.at(handle);
  }

  uint64_t add_stream(StreamHandle stream) {
    std::lock_guard<std::mutex> lock(mutex);
    streams[next] = std::make_shared<StreamHandle>(std::move(stream));
    return next++;
  }

  std::shared_ptr<StreamHandle> stream(uint64_t handle) {
    std::lock_guard<std::mutex> lock(mutex);
    return streams.at(handle);
  }

  uint64_t add_event(krpc::Event event) {
    std::lock_guard<std::mutex> lock(mutex);
    events[next] = std::make_shared<krpc::Event>(std::move(event));
    return next++;
  }

  /** The event with a handle, or null for a stream's handle. */
  std::shared_ptr<krpc::Event> event(uint64_t handle) {
    std::lock_guard<std::mutex> lock(mutex);
    auto event = events.find(handle);
    return event == events.end() ? nullptr : event->second;
  }

  /** Forget a stream or event handle. The handle is destroyed once the lock is released. */
  void drop(uint64_t handle) {
    std::shared_ptr<StreamHandle> stream;
    std::shared_ptr<krpc::Event> event;
    std::lock_guard<std::mutex> lock(mutex);
    auto it = streams.find(handle);
    if (it != streams.end()) {
      stream = std::move(it->second);
      streams.erase(it);
    }
    auto found = events.find(handle);
    if (found != events.end()) {
      event = std::move(found->second);
      events.erase(found);
    }
  }

  void clear() {
    std::lock_guard<std::mutex> lock(mutex);
    objects.clear();
    streams.clear();
    events.clear();
  }

 private:
  std::mutex mutex;
  uint64_t next = 1;
  std::map<uint64_t, Held> objects;
  std::map<uint64_t, std::shared_ptr<StreamHandle>> streams;
  std::map<uint64_t, std::shared_ptr<krpc::Event>> events;
};

inline Value result_none() { return Value(); }

inline Value result_null() {
  Value value;
  value.set_null(true);
  return value;
}

inline Value result_bool(bool x) {
  Value value;
  value.set_bool_(x);
  return value;
}

template <typename T>
Value result_object(const T& obj) {
  Held held;
  held.value = obj;
  held.equals = [obj](const std::any& other) {
    const T* other_obj = std::any_cast<T>(&other);
    return other_obj != nullptr && obj == *other_obj;
  };
  held.hash = [obj]() { return krpc::hash_value(obj); };
  Value value;
  value.mutable_object()->set_handle(Handles::instance().add_object(std::move(held)));
  value.mutable_object()->set_id(krpc::get_object_id(obj));
  return value;
}

/** The client offers no way to do what a command asks. */
class Unsupported : public std::runtime_error {
 public:
  using std::runtime_error::runtime_error;
};

/** Throws Unsupported for a null value, which the client's type for a value cannot hold. */
inline const Value& not_null(const Value& value) {
  if (value.value_case() == Value::kNull) throw Unsupported("The client's type cannot hold null");
  return value;
}

// The conversion of each kind of value, as a tag type: the client's type for the value, and
// an arg and a result function. The generated dispatch composes them for a collection, and
// generates a tag for each struct.

#define KRPC_CONFORMANCE_SCALAR(Tag, Type, field)                                          \
  struct Tag {                                                                             \
    using type = Type;                                                                     \
    static type arg(krpc::Client&, const Value& value) { return not_null(value).field(); } \
    static Value result(const type& x) {                                                   \
      Value value;                                                                         \
      value.set_##field(x);                                                                \
      return value;                                                                        \
    }                                                                                      \
  };

KRPC_CONFORMANCE_SCALAR(Double, double, double_)
KRPC_CONFORMANCE_SCALAR(Float, float, float_)
KRPC_CONFORMANCE_SCALAR(Sint32, int32_t, sint32)
KRPC_CONFORMANCE_SCALAR(Sint64, int64_t, sint64)
KRPC_CONFORMANCE_SCALAR(Uint32, uint32_t, uint32)
KRPC_CONFORMANCE_SCALAR(Uint64, uint64_t, uint64)
KRPC_CONFORMANCE_SCALAR(Bool, bool, bool_)
KRPC_CONFORMANCE_SCALAR(String, std::string, string)
KRPC_CONFORMANCE_SCALAR(Bytes, std::string, bytes)

#undef KRPC_CONFORMANCE_SCALAR

template <typename T>
struct Object {
  using type = T;
  static type arg(krpc::Client& client, const Value& value) {
    if (value.value_case() != Value::kObject) return T();
    // A handle of 0 asks for an object of the class with the given id
    if (value.object().handle() == 0) return T(&client, value.object().id());
    return std::any_cast<T>(Handles::instance().object(value.object().handle()).value);
  }
  static Value result(const type& x) { return result_object(x); }
};

/** An event, which is only ever returned. */
struct EventHandle {
  using type = krpc::Event;
  static Value result(const type& x) {
    Value value;
    value.set_event(Handles::instance().add_event(x));
    return value;
  }
};

/** A protobuf message, which is only ever returned. */
template <typename T>
struct Message {
  using type = T;
  static Value result(const type& x) {
    Value value;
    value.set_message(x.SerializeAsString());
    return value;
  }
};

template <typename T>
struct Enum {
  using type = T;
  static type arg(krpc::Client&, const Value& value) {
    return static_cast<T>(not_null(value).enum_().value());
  }
  static Value result(const type& x) {
    Value value;
    value.mutable_enum_()->set_value(static_cast<int32_t>(x));
    return value;
  }
};

template <typename Tag>
struct Optional {
  using type = std::optional<typename Tag::type>;
  static type arg(krpc::Client& client, const Value& value) {
    if (value.value_case() == Value::VALUE_NOT_SET || value.value_case() == Value::kNull)
      return std::nullopt;
    return Tag::arg(client, value);
  }
  static Value result(const type& x) { return x ? Tag::result(*x) : result_null(); }
};

template <typename Tag>
struct List {
  using type = std::vector<typename Tag::type>;
  static type arg(krpc::Client& client, const Value& value) {
    type result;
    for (const auto& x : not_null(value).list().items()) result.push_back(Tag::arg(client, x));
    return result;
  }
  static Value result(const type& x) {
    Value value;
    auto* items = value.mutable_list();
    for (const auto& item : x) *items->add_items() = Tag::result(item);
    return value;
  }
};

template <typename Tag>
struct Set {
  using type = std::set<typename Tag::type>;
  static type arg(krpc::Client& client, const Value& value) {
    type result;
    for (const auto& x : not_null(value).set().items()) result.insert(Tag::arg(client, x));
    return result;
  }
  static Value result(const type& x) {
    Value value;
    auto* items = value.mutable_set();
    for (const auto& item : x) *items->add_items() = Tag::result(item);
    return value;
  }
};

template <typename Key, typename Item>
struct Dictionary {
  using type = std::map<typename Key::type, typename Item::type>;
  static type arg(krpc::Client& client, const Value& value) {
    type result;
    for (const auto& x : not_null(value).dictionary().entries())
      result[Key::arg(client, x.key())] = Item::arg(client, x.value());
    return result;
  }
  static Value result(const type& x) {
    Value value;
    auto* entries = value.mutable_dictionary();
    for (const auto& [key, item] : x) {
      auto* entry = entries->add_entries();
      *entry->mutable_key() = Key::result(key);
      *entry->mutable_value() = Item::result(item);
    }
    return value;
  }
};

template <typename... Items>
struct Tuple {
  using type = std::tuple<typename Items::type...>;
  static type arg(krpc::Client& client, const Value& value) {
    return arg(client, value, std::index_sequence_for<Items...>());
  }
  static Value result(const type& x) {
    Value value;
    auto* items = value.mutable_tuple();
    std::apply([items](const auto&... item) { ((*items->add_items() = Items::result(item)), ...); },
               x);
    return value;
  }

 private:
  template <std::size_t... I>
  static type arg(krpc::Client& client, const Value& value, std::index_sequence<I...>) {
    return type(Items::arg(client, not_null(value).tuple().items(static_cast<int>(I)))...);
  }
};

template <typename T, typename F>
StreamHandle result_stream(krpc::Stream<T> stream, F convert) {
  auto shared = std::make_shared<krpc::Stream<T>>(std::move(stream));
  StreamHandle handle;
  handle.get = [shared, convert]() { return convert((*shared)()); };
  handle.wait = [shared](double timeout) { return shared->wait(timeout); };
  handle.acquire = [shared]() { shared->acquire(); };
  handle.release = [shared]() { shared->release(); };
  handle.wait_on_condition = [shared](double timeout) {
    auto duration = std::chrono::duration<double>(timeout);
    return shared->get_condition().wait_for(shared->get_condition_lock(), duration) ==
           std::cv_status::no_timeout;
  };
  handle.notify_condition = [shared]() {
    shared->acquire();
    shared->get_condition().notify_all();
    shared->release();
  };
  handle.rate = [shared]() { return shared->rate(); };
  handle.set_rate = [shared](float rate) { shared->set_rate(rate); };
  handle.remove = [shared]() { shared->remove(); };
  handle.copy = [shared, convert]() { return result_stream(krpc::Stream<T>(*shared), convert); };
  handle.value = shared;
  handle.equals = [shared](const std::any& other) {
    const auto* other_stream = std::any_cast<std::shared_ptr<krpc::Stream<T>>>(&other);
    return other_stream != nullptr && *shared == **other_stream;
  };
  handle.add_callback = [shared, convert](std::function<void(const Value&)> callback) {
    return shared->add_callback([convert, callback](T x) { callback(convert(x)); });
  };
  handle.remove_callback = [shared](int tag) { shared->remove_callback(tag); };
  handle.add_end_callback = [shared](std::function<void(std::exception_ptr)> callback) {
    return shared->add_end_callback(callback);
  };
  handle.remove_end_callback = [shared](int tag) { shared->remove_end_callback(tag); };
  return handle;
}

}  // namespace krpc_conformance
