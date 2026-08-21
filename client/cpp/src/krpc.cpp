#include "krpc.hpp"

#include <chrono>  // NOLINT(build/c++11)
#include <cstdlib>
#include <memory>
#include <string>

#include "krpc/connection.hpp"

namespace krpc {

Client connect(const std::string& name, const std::string& address, unsigned int rpc_port,
               unsigned int stream_port, std::chrono::milliseconds timeout) {
  return Client(name, address, rpc_port, stream_port, timeout);
}

namespace {

/** A default path for a socket of the given name, matching the one the server uses unless
    it was configured with another. Windows has no runtime directory for this, so its
    per-user application data directory stands in. */
std::string default_path(const std::string& name) {
#ifdef _WIN32
  const char* separator = "\\";
  const char* directory = std::getenv("LOCALAPPDATA");
  const char* user = std::getenv("USERNAME");
  /* The order the system's own GetTempPath takes them in, so that the directory this falls
     back to is the one the server falls back to. */
  const char* temporary = std::getenv("TMP");
  if (temporary == nullptr) temporary = std::getenv("TEMP");
#else
  const char* separator = "/";
  const char* directory = std::getenv("XDG_RUNTIME_DIR");
  const char* user = std::getenv("USER");
  const char* temporary = "/tmp";
#endif
  if (directory != nullptr && directory[0] != '\0')
    return std::string(directory) + separator + "krpc" + separator + name;
  return std::string(temporary == nullptr ? "." : temporary) + separator + "krpc-" +
         (user == nullptr ? "" : user) + separator + name;
}

}  // namespace

Client connect_local(const std::string& name, const std::string& rpc_path,
                     const std::string& stream_path) {
  return Client(
      name, std::make_shared<LocalConnection>(rpc_path.empty() ? default_path("rpc") : rpc_path),
      std::make_shared<LocalConnection>(stream_path.empty() ? default_path("stream")
                                                            : stream_path));
}

}  // namespace krpc
