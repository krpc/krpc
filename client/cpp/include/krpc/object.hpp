#pragma once

#include <google/protobuf/stubs/common.h>

#include <ostream>
#include <string>

#include "krpc/client.hpp"

namespace krpc {

template <typename T>
class Object {
 public:
  Object(Client * client, const std::string& name, google::protobuf::uint64 id = 0);
  template <typename U> friend std::ostream& operator<<(std::ostream&, const Object<U>&);
  template <typename U> friend bool operator==(const Object<U>&, const Object<U>&);
  template <typename U> friend bool operator<(const Object<U>&, const Object<U>&);
  // TODO: Ideally the following fields should be private, but they are needed by encoder and decoder.
  //       They can't be a 'friend' due to the circular dependency that would introduce.
  Client * _client;
  google::protobuf::uint64 _id;
 private:
  std::string _name;
};

template <typename T> bool operator==(const Object<T>&, const Object<T>&);
template <typename T> std::ostream& operator<<(std::ostream& stream, const Object<T>& object);

template <typename T> inline Object<T>::Object(
  Client* client, const std::string& name, google::protobuf::uint64 id):
  _client(client), _name(name), _id(id) {}

template <typename T> inline std::ostream& operator<<(std::ostream& stream, const Object<T>& object) {
  stream << object._name << "<" << object._id << ">";
  return stream;
}

template <typename T> inline bool operator==(const Object<T>& lhs, const Object<T>& rhs) {
  return lhs._id == rhs._id;
}

template <typename T> inline bool operator!=(const Object<T>& lhs, const Object<T>& rhs) {
  return !operator==(lhs, rhs);
}

template <typename T> inline bool operator<(const Object<T>& lhs, const Object<T>& rhs) {
  return lhs._id < rhs._id;
}

template <typename T> inline bool operator> (const Object<T>& lhs, const Object<T>& rhs) {
  return  operator<(rhs, lhs);
}

template <typename T> inline bool operator<=(const Object<T>& lhs, const Object<T>& rhs) {
  return !operator>(lhs, rhs);
}

template <typename T> inline bool operator>=(const Object<T>& lhs, const Object<T>& rhs) {
  return !operator<(lhs, rhs);
}

}  // namespace krpc
