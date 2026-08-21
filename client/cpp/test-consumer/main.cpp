#include <iostream>

#include <krpc.hpp>
#include <krpc/services/krpc.hpp>

// Opening a connection is what pulls in the code of a transport, and with it the libraries that
// transport needs, so doing it for each of them is what proves both reach a program through the
// installed package. Nothing is listening at either endpoint, so both are expected to fail.
int main() {
  try {
    krpc::connect("consumer test", "127.0.0.1", 1, 0);
    std::cout << "opened a connection to a port nothing should be listening on" << std::endl;
    return 1;
  } catch (const std::exception&) {
  }
  try {
    krpc::connect_local("consumer test", "/nonexistent/krpc-consumer-test.sock");
    std::cout << "opened a connection to a socket nothing should be listening on" << std::endl;
    return 1;
  } catch (const std::exception&) {
  }
  std::cout << "krpc library linked OK" << std::endl;
  return 0;
}
