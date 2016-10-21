#pragma once

#include <string>

#include "krpc/client.hpp"
#include "krpc/krpc.pb.hpp"

namespace krpc {

template <typename T>
class Stream {
 public:
  Stream() = default;
  Stream(Client* client, const schema::Request& request);
  T operator()();
  void remove();
  bool operator==(const Stream<T>& rhs) const;
  bool operator!=(const Stream<T>& rhs) const;
  explicit operator bool() const;
 private:
  struct Ptr {
    Ptr(Client* client, google::protobuf::uint64 id);
    ~Ptr();
    Client* client;
    google::protobuf::uint64 id;
  };
  std::shared_ptr<Ptr> ptr;
};

template <typename T> inline Stream<T>::Stream(Client* client, const schema::Request& request) {
  ptr = std::make_shared<Ptr>(client, client->add_stream(request));
}

template <typename T> inline T Stream<T>::operator()() {
  std::string data = ptr->client->get_stream(ptr->id);
  T value;
  decoder::decode(value, data, ptr->client);
  return value;
}

template <typename T> inline void Stream<T>::remove() {
  ptr->client->remove_stream(ptr->id);
}

template <typename T> inline bool Stream<T>::operator==(const Stream<T>& rhs) const {
  return this->ptr->id == rhs.ptr->id;
}

template <typename T> inline bool Stream<T>::operator!=(const Stream<T>& rhs) const {
  return this->ptr->id != rhs.ptr->id;
}

template <typename T> Stream<T>::operator bool() const {
  return ptr.operator bool();
}

template <typename T> inline Stream<T>::Ptr::Ptr(Client* client, google::protobuf::uint64 id)
  : client(client), id(id) {
}

template <typename T> inline Stream<T>::Ptr::~Ptr() {
  client->remove_stream(id);
}

}  // namespace krpc
