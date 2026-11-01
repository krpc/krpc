// Conversions between conformance Values and the client's values, without the client's
// encoder, for the generated dispatch.
#pragma once

#include <krpc_cnano.h>
#include <krpc_cnano/pb_common.h>
#include <krpc_cnano/pb_encode.h>

#include <cstdint>
#include <cstring>
#include <map>
#include <mutex>
#include <optional>
#include <stdexcept>
#include <string>
#include <type_traits>
#include <utility>

#include "testee.pb.h"

namespace krpc_conformance {

using Value = krpc::conformance::Value;
using Arguments = google::protobuf::RepeatedPtrField<Value>;

/** An error a client function returned, with the message the client recorded for it. */
class ClientError : public std::runtime_error {
 public:
  ClientError(krpc_error_t code, const std::string& message)
      : std::runtime_error(message), code(code) {}
  krpc_error_t code;
};

/** The client offers no way to do what a command asks. */
class Unsupported : public std::runtime_error {
 public:
  using std::runtime_error::runtime_error;
};

inline void check(krpc_error_t error) {
  if (error != KRPC_OK) throw ClientError(error, krpc_get_error_message());
}

/** The objects the testee holds, by handle. An object is its id, and each object a call returns
    gets a new handle, so two handles can refer to one server object. */
class Handles {
 public:
  static Handles& instance() {
    static Handles handles;
    return handles;
  }

  uint64_t add(krpc_object_t id) {
    std::lock_guard<std::mutex> lock(mutex);
    objects[next] = id;
    return next++;
  }

  krpc_object_t get(uint64_t handle) {
    std::lock_guard<std::mutex> lock(mutex);
    return objects.at(handle);
  }

  void clear() {
    std::lock_guard<std::mutex> lock(mutex);
    objects.clear();
  }

 private:
  std::mutex mutex;
  uint64_t next = 1;
  std::map<uint64_t, krpc_object_t> objects;
};

inline krpc_object_t arg_object(const Value& value) {
  if (value.value_case() != Value::kObject) return 0;
  // A handle of 0 asks for an object with the given id
  if (value.object().handle() == 0) return value.object().id();
  return Handles::instance().get(value.object().handle());
}

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

inline bool is_null(const Value& value) {
  return value.value_case() == Value::VALUE_NOT_SET || value.value_case() == Value::kNull;
}

/** Throws Unsupported for a null value, which the C type for a value cannot hold. */
inline const Value& not_null(const Value& value) {
  if (is_null(value)) throw Unsupported("The C type cannot hold null");
  return value;
}

// The conversion of each kind of value, as a tag type: the C type of the value, and an arg and
// a result function. The generated dispatch composes them for a collection, and generates a
// tag for each struct and tuple. The testee leaks what it allocates for an argument, and what
// the client allocates for a result.

#define KRPC_CONFORMANCE_SCALAR(Tag, Type, field)                           \
  struct Tag {                                                              \
    using type = Type;                                                      \
    static type arg(const Value& value) { return not_null(value).field(); } \
    static Value result(const type& x) {                                    \
      Value value;                                                          \
      value.set_##field(x);                                                 \
      return value;                                                         \
    }                                                                       \
  };

KRPC_CONFORMANCE_SCALAR(Double, double, double_)
KRPC_CONFORMANCE_SCALAR(Float, float, float_)
KRPC_CONFORMANCE_SCALAR(Sint32, int32_t, sint32)
KRPC_CONFORMANCE_SCALAR(Sint64, int64_t, sint64)
KRPC_CONFORMANCE_SCALAR(Uint32, uint32_t, uint32)
KRPC_CONFORMANCE_SCALAR(Uint64, uint64_t, uint64)
KRPC_CONFORMANCE_SCALAR(Bool, bool, bool_)

#undef KRPC_CONFORMANCE_SCALAR

/** A string, which is NULL for null */
struct String {
  using type = char*;
  static type arg(const Value& value) {
    if (is_null(value)) return nullptr;
    const std::string& x = value.string();
    auto* result = static_cast<char*>(krpc_malloc(x.size() + 1));
    std::memcpy(result, x.c_str(), x.size() + 1);
    return result;
  }
  static Value result(const type& x) {
    if (x == nullptr) return result_null();
    Value value;
    value.set_string(x);
    return value;
  }
};

struct Bytes {
  using type = krpc_bytes_t;
  static type arg(const Value& value) {
    const std::string& x = not_null(value).bytes();
    type result{x.size(), static_cast<uint8_t*>(krpc_malloc(x.size() + 1))};
    std::memcpy(result.data, x.data(), x.size());
    return result;
  }
  static Value result(const type& x) {
    Value value;
    value.set_bytes(std::string(reinterpret_cast<const char*>(x.data), x.size));
    return value;
  }
};

/** An object, which is KRPC_NULL for null */
struct Object {
  using type = krpc_object_t;
  static type arg(const Value& value) { return arg_object(value); }
  static Value result(const type& id) {
    if (id == 0) return result_null();
    Value value;
    value.mutable_object()->set_handle(Handles::instance().add(id));
    value.mutable_object()->set_id(id);
    return value;
  }
};

/** A protobuf message the client decoded, encoded again as the driver reads it. */
template <typename T, const pb_msgdesc_t* Fields>
struct Message {
  using type = T;
  static Value result(const type& x) {
    // A repeated field with no max_count decodes only through a callback the caller sets,
    // which the testee has no way to set
    pb_field_iter_t field;
    if (pb_field_iter_begin_const(&field, Fields, &x)) {
      do {
        if (PB_ATYPE(field.type) == PB_ATYPE_CALLBACK)
          throw Unsupported("The message has a field that decodes only through a callback");
      } while (pb_field_iter_next(&field));
    }
    size_t size = 0;
    if (!pb_get_encoded_size(&size, Fields, &x))
      throw std::runtime_error("The message cannot be encoded");
    std::string data(size, '\0');
    pb_ostream_t stream = pb_ostream_from_buffer(reinterpret_cast<pb_byte_t*>(data.data()), size);
    if (!pb_encode(&stream, Fields, &x)) throw std::runtime_error("The message cannot be encoded");
    Value value;
    value.set_message(data);
    return value;
  }
};

template <typename T>
struct Enum {
  using type = T;
  static type arg(const Value& value) { return static_cast<T>(not_null(value).enum_().value()); }
  static Value result(const type& x) {
    Value value;
    value.mutable_enum_()->set_value(static_cast<int32_t>(x));
    return value;
  }
};

/** A value at a position that can hold null, beside a flag saying whether it is there */
template <typename CType, typename Tag>
struct Nullable {
  using type = CType;
  static type arg(const Value& value) {
    type result{};
    if (!is_null(value)) {
      result.value = Tag::arg(value);
      result.has_value = true;
    }
    return result;
  }
  static Value result(const type& x) { return x.has_value ? Tag::result(x.value) : result_null(); }
};

template <typename CType, typename Tag>
struct List {
  using type = CType;
  using item_type = std::remove_pointer_t<decltype(std::declval<CType>().items)>;
  static type arg(const Value& value) {
    if (value.value_case() == Value::kNull) throw Unsupported("A collection cannot be null");
    const auto& items = value.has_list() ? value.list().items() : value.set().items();
    type result{};
    result.size = static_cast<size_t>(items.size());
    result.items = static_cast<item_type*>(krpc_calloc(result.size + 1, sizeof(item_type)));
    for (size_t i = 0; i < result.size; i++)
      result.items[i] = Tag::arg(items.Get(static_cast<int>(i)));
    return result;
  }
  static Value result(const type& x) {
    Value value;
    auto* items = value.mutable_list();
    for (size_t i = 0; i < x.size; i++) *items->add_items() = Tag::result(x.items[i]);
    return value;
  }
};

/** A set, which has the C type of a list */
template <typename CType, typename Tag>
struct Set {
  using type = CType;
  static type arg(const Value& value) { return List<CType, Tag>::arg(value); }
  static Value result(const type& x) {
    Value value;
    auto* items = value.mutable_set();
    for (size_t i = 0; i < x.size; i++) *items->add_items() = Tag::result(x.items[i]);
    return value;
  }
};

template <typename CType, typename Key, typename Item>
struct Dictionary {
  using type = CType;
  using entry_type = std::remove_pointer_t<decltype(std::declval<CType>().entries)>;
  static type arg(const Value& value) {
    if (value.value_case() == Value::kNull) throw Unsupported("A collection cannot be null");
    const auto& entries = value.dictionary().entries();
    type result{};
    result.size = static_cast<size_t>(entries.size());
    result.entries = static_cast<entry_type*>(krpc_calloc(result.size + 1, sizeof(entry_type)));
    for (size_t i = 0; i < result.size; i++) {
      const auto& entry = entries.Get(static_cast<int>(i));
      result.entries[i].key = Key::arg(entry.key());
      result.entries[i].value = Item::arg(entry.value());
    }
    return result;
  }
  static Value result(const type& x) {
    Value value;
    auto* entries = value.mutable_dictionary();
    for (size_t i = 0; i < x.size; i++) {
      auto* entry = entries->add_entries();
      *entry->mutable_key() = Key::result(x.entries[i].key);
      *entry->mutable_value() = Item::result(x.entries[i].value);
    }
    return value;
  }
};

/** A value at a position that cannot hold null, which the C type of the value cannot refuse */
template <typename Tag>
struct NotNull {
  using type = typename Tag::type;
  static type arg(const Value& value) {
    if (value.value_case() == Value::kNull) throw Unsupported("The C type cannot refuse null");
    return Tag::arg(value);
  }
  static Value result(const type& x) { return Tag::result(x); }
};

/** An argument that can be null, which the client function takes by pointer */
template <typename Tag>
std::optional<typename Tag::type> optional_arg(const Value& value) {
  if (is_null(value)) return std::nullopt;
  return Tag::arg(value);
}

}  // namespace krpc_conformance
