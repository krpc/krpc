#ifndef HEADER_KRPC_OBJECT
#define HEADER_KRPC_OBJECT

#include "krpc/client.hpp"
#include <google/protobuf/stubs/common.h>
#include <iostream>
#include <string>

namespace krpc {

  class Encoder;
  class Decoder;

  template <typename T>
  class Object {
  protected:
    Client& client;
  private:
    const std::string name;
    google::protobuf::uint64 id;
  public:
    Object(Client& client, const std::string& name, google::protobuf::uint64 id = 0);
    template <typename U>
    friend std::ostream& operator<<(std::ostream&, const Object<U>&);
    template <typename U>
    friend bool operator==(const Object<U>&, const Object<U>&);
    friend Decoder;
    friend Encoder;
  };

  template <typename T>
  bool operator==(const Object<T>&, const Object<T>&);
  template <typename T>
  std::ostream& operator<<(std::ostream& stream, const Object<T>& object);

  template <typename T>
  inline Object<T>::Object(Client& client, const std::string& name, google::protobuf::uint64 id):
    client(client), name(name), id(id) {}

  template <typename T>
  inline bool operator==(const Object<T>& lhs, const Object<T>& rhs)
  {
    return lhs.id == rhs.id;
  }

  template <typename T>
  inline std::ostream& operator<<(std::ostream& stream, const Object<T>& object)
  {
    stream << object.name << "<" << object.id << ">";
    return stream;
  }

}

#endif
