#pragma once

#include <string>

#include "krpc/client.hpp"
#include "krpc/krpc.pb.hpp"

namespace krpc {

template <typename T>
class Stream {
 public:
  Stream(Client* client, const schema::Request& request);
  ~Stream();
  T operator()();
  void remove();
  bool operator==(const Stream<T>& rhs) const;
  bool operator!=(const Stream<T>& rhs) const;
 private:
  Client* client;
  google::protobuf::uint32 id;
};

template <typename T> inline Stream<T>::Stream(Client* client, const schema::Request& request)
  : client(client) {
  id = client->add_stream(request);
}

template <typename T> inline Stream<T>::~Stream() {
  client->remove_stream(id);
}

template <typename T> inline T Stream<T>::operator()() {
  std::string data = client->get_stream(id);
  T value;
  decoder::decode(value, data, client);
  return value;
}

template <typename T> inline void Stream<T>::remove() {
  client->remove_stream(id);
}

template <typename T> inline bool Stream<T>::operator==(const Stream<T>& rhs) const {
  return this->id == rhs.id;
}

template <typename T> inline bool Stream<T>::operator!=(const Stream<T>& rhs) const {
  return this->id != rhs.id;
}

}  // namespace krpc
