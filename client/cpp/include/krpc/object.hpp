#pragma once

#include <cstdint>
#include <ostream>
#include <stdexcept>
#include <string>

#include "krpc/client.hpp"

namespace krpc {

template <typename T>
class Object {
 public:
  Object(Client* client, const std::string& name, uint64_t id = 0);
  template <typename U>
  friend std::ostream& operator<<(std::ostream&, const Object<U>&);
  template <typename U>
  friend bool operator==(const Object<U>&, const Object<U>&);
  template <typename U>
  friend bool operator<(const Object<U>&, const Object<U>&);
  // Public because the encoder and decoder need them. Granting access with 'friend' would
  // introduce a circular dependency between this header and theirs.
  Client* _client;
  uint64_t _id;

 private:
  std::string _name;
};

template <typename T>
bool operator==(const Object<T>&, const Object<T>&);
template <typename T>
std::ostream& operator<<(std::ostream& stream, const Object<T>& object);

// The identifier of a remote object on the server, e.g. to pass to
// Expression::constant_object. Throws std::invalid_argument for a null object, whose id is 0.
template <typename T>
uint64_t get_object_id(const Object<T>& object);

template <typename T>
inline Object<T>::Object(Client* client, const std::string& name, uint64_t id)
    : _client(client), _id(id), _name(name) {}

template <typename T>
inline uint64_t get_object_id(const Object<T>& object) {
  if (object._id == 0) throw std::invalid_argument("The object is null");
  return object._id;
}

template <typename T>
inline std::ostream& operator<<(std::ostream& stream, const Object<T>& object) {
  stream << object._name << "<" << object._id << ">";
  return stream;
}

template <typename T>
inline bool operator==(const Object<T>& lhs, const Object<T>& rhs) {
  return lhs._id == rhs._id;
}

template <typename T>
inline bool operator!=(const Object<T>& lhs, const Object<T>& rhs) {
  return !operator==(lhs, rhs);
}

template <typename T>
inline bool operator<(const Object<T>& lhs, const Object<T>& rhs) {
  return lhs._id < rhs._id;
}

template <typename T>
inline bool operator>(const Object<T>& lhs, const Object<T>& rhs) {
  return operator<(rhs, lhs);
}

template <typename T>
inline bool operator<=(const Object<T>& lhs, const Object<T>& rhs) {
  return !operator>(lhs, rhs);
}

template <typename T>
inline bool operator>=(const Object<T>& lhs, const Object<T>& rhs) {
  return !operator<(lhs, rhs);
}

}  // namespace krpc
