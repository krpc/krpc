#include "krpc.hpp"

#include <chrono>  // NOLINT(build/c++11)
#include <cstdlib>
#include <memory>
#include <string>

#ifndef _WIN32
#include <pwd.h>
#include <unistd.h>
#endif

#include "krpc/connection.hpp"
#include "krpc/error.hpp"

namespace krpc {

Client connect(const std::string& name, const std::string& address, unsigned int rpc_port,
               unsigned int stream_port, std::chrono::milliseconds timeout) {
  return Client(name, address, rpc_port, stream_port, timeout);
}

namespace {

/** The name of the user running this process, which the fallback path below is named after
    so that sockets are neither shared between accounts nor left in a directory anyone can
    write to. USER is only a hint, and is unset in a process started without a login shell,
    so the account database is asked first. */
std::string user_name() {
#ifdef _WIN32
  const char* user = std::getenv("USERNAME");
#else
  const passwd* entry = getpwuid(getuid());
  const char* user = entry == nullptr ? nullptr : entry->pw_name;
  if (user == nullptr || user[0] == '\0') user = std::getenv("USER");
#endif
  if (user == nullptr || user[0] == '\0')
    throw ConnectionError(
        "could not work out which user this is, so there is no default socket path to "
        "connect to; pass the paths the server window shows instead");
  return user;
}

/** A default path for a socket of the given name, matching the one the server uses unless
    it was configured with another. Windows has no runtime directory for this, so its
    per-user application data directory stands in.

    The server works the same path out for itself, so the two only meet if they agree on
    every step. The fallback therefore names a fixed directory rather than the temporary
    one, which TMPDIR moves for the client and not the server. Windows always names a
    directory in LOCALAPPDATA and so never reaches the fallback. */
std::string default_path(const std::string& name) {
#ifdef _WIN32
  const char* separator = "\\";
  const char* directory = std::getenv("LOCALAPPDATA");
  /* The first two of the variables GetTempPath reads. It goes no further because
     LOCALAPPDATA is always set on Windows, leaving this unreachable there. */
  const char* temporary = std::getenv("TMP");
  if (temporary == nullptr) temporary = std::getenv("TEMP");
  if (temporary == nullptr) temporary = ".";
#else
  const char* separator = "/";
  const char* directory = std::getenv("XDG_RUNTIME_DIR");
  const char* temporary = "/tmp";
#endif
  if (directory != nullptr && directory[0] != '\0')
    return std::string(directory) + separator + "krpc" + separator + name;
  return std::string(temporary) + separator + "krpc-" + user_name() + separator + name;
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
