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

Client connect(const std::string& name, const std::string& address, unsigned int port,
               std::chrono::milliseconds timeout, std::chrono::milliseconds handshake_timeout,
               std::chrono::milliseconds stall_timeout) {
  return Client(name, address, port, timeout, handshake_timeout, stall_timeout);
}

namespace {

/** The name of the user running this process. The fallback path below is named after it so
    that accounts do not share a socket. USER is only a hint, and is unset in a process
    started without a login shell, so the account database is asked first. */
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
        "connect to; pass the path the server window shows instead");
  return user;
}

/** The default socket path, matching the one the server uses unless it was configured with
    another. Windows has no runtime directory for this, so its per-user application data
    directory stands in.

    The server works the same path out for itself, so the two only meet if they agree on
    every step. The fallback therefore names a fixed directory rather than the temporary
    one, which TMPDIR moves for the client and not the server. Windows always names a
    directory in LOCALAPPDATA and so never reaches the fallback. */
std::string default_path() {
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
    return std::string(directory) + separator + "krpc.sock";
  return std::string(temporary) + separator + "krpc-" + user_name() + ".sock";
}

}  // namespace

Client connect_local(const std::string& name, const std::string& path,
                     std::chrono::milliseconds handshake_timeout,
                     std::chrono::milliseconds stall_timeout) {
  return Client(name, std::make_shared<LocalConnection>(path.empty() ? default_path() : path),
                handshake_timeout, stall_timeout);
}

}  // namespace krpc
