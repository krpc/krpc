/* The sockets API is POSIX rather than C, and a compiler asked for strict C hides it unless the
 * file says which POSIX it expects. A feature test macro only has any effect before the first
 * header is included, which is why this comes ahead of them. */
#if (defined(KRPC_COMMUNICATION_TCP) || defined(KRPC_COMMUNICATION_LOCALSOCKET)) && \
    !defined(_WIN32) && !defined(_POSIX_C_SOURCE)
#define _POSIX_C_SOURCE 200112L
#endif

#include <krpc_cnano/communication.h>
#include <krpc_cnano/error.h>

#if defined(KRPC_COMMUNICATION_POSIX)

#include <fcntl.h>
#include <stdint.h>
#include <unistd.h>

krpc_error_t krpc_open(krpc_connection_t* connection, const krpc_connection_config_t* arg) {
  const char* port = arg;
  int fd = open(port, O_RDWR | O_NOCTTY);
  if (fd < 0) KRPC_RETURN_ERROR(IO, "failed to open serial port");
  *connection = fd;
  return KRPC_OK;
}

krpc_error_t krpc_close(krpc_connection_t connection) {
  close(connection);
  return KRPC_OK;
}

krpc_error_t krpc_read(krpc_connection_t connection, uint8_t* buf, size_t count) {
  size_t total = 0;
  while (total < count) {
    ssize_t result = read(connection, buf + total, count - total);
    if (result == -1) {
      KRPC_RETURN_ERROR(IO, "read failed");
    } else if (result == 0) {
      KRPC_RETURN_ERROR(EOF, "eof received");
    } else {
      total += result;
    }
  }
  return KRPC_OK;
}

krpc_error_t krpc_write(krpc_connection_t connection, const uint8_t* buf, size_t count) {
  if (count != write(connection, buf, count)) KRPC_RETURN_ERROR(IO, "write failed");
  return KRPC_OK;
}

#endif

#if defined(KRPC_COMMUNICATION_WINDOWS)

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <windows.h>

krpc_error_t krpc_open(krpc_connection_t* connection, const krpc_connection_config_t* arg) {
  const char* port = arg;
  HANDLE handle = CreateFileA(port, GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING,
                              FILE_ATTRIBUTE_NORMAL, NULL);
  if (handle == INVALID_HANDLE_VALUE) KRPC_RETURN_ERROR(IO, "failed to open serial port");
  *connection = handle;
  return KRPC_OK;
}

krpc_error_t krpc_close(krpc_connection_t connection) {
  CloseHandle((HANDLE)connection);
  return KRPC_OK;
}

krpc_error_t krpc_read(krpc_connection_t connection, uint8_t* buf, size_t count) {
  size_t total = 0;
  while (total < count) {
    DWORD bytes_read = 0;
    if (!ReadFile((HANDLE)connection, buf + total, (DWORD)(count - total), &bytes_read, NULL))
      KRPC_RETURN_ERROR(IO, "read failed");
    if (bytes_read == 0) KRPC_RETURN_ERROR(EOF, "eof received");
    total += bytes_read;
  }
  return KRPC_OK;
}

krpc_error_t krpc_write(krpc_connection_t connection, const uint8_t* buf, size_t count) {
  DWORD bytes_written = 0;
  if (!WriteFile((HANDLE)connection, buf, (DWORD)count, &bytes_written, NULL) ||
      bytes_written != (DWORD)count)
    KRPC_RETURN_ERROR(IO, "write failed");
  return KRPC_OK;
}

#endif

/* A socket carries a connection the same way whichever address family it was opened in, so a
   server reached over the network and one reached over a unix domain socket are read, written
   and closed through the same calls, and only opening the connection differs. */
#if defined(KRPC_COMMUNICATION_TCP) || defined(KRPC_COMMUNICATION_LOCALSOCKET)

#include <stdint.h>
#include <string.h>

#ifdef _WIN32
#include <winsock2.h>
#include <ws2tcpip.h>
#else
#include <errno.h>
#include <sys/socket.h>
#include <unistd.h>
/* The winsock spellings, so that the code below is the same on both platforms */
typedef int SOCKET;
#define INVALID_SOCKET (-1)
#define closesocket close
#endif

/* Sending to a socket whose peer has gone raises SIGPIPE, which by default takes the program
   down, in place of the error the caller is waiting to be told about. Where there is a send flag
   that suppresses it, it is passed on every send; where there is only a socket option, that is
   set on the socket when it is opened. Winsock raises no such signal. */
#if defined(MSG_NOSIGNAL)
#define KRPC_SEND_FLAGS MSG_NOSIGNAL
#else
#define KRPC_SEND_FLAGS 0
#endif

/* A read or write interrupted by a signal the program handles has not failed, and is resumed
   from where it stopped. Treating it as a failure would leave part of a message on the socket,
   which every call after it would then read in place of its own. Winsock does not interrupt
   calls this way, and reports through WSAGetLastError rather than errno. */
#ifdef _WIN32
#define KRPC_INTERRUPTED() 0
#else
#define KRPC_INTERRUPTED() (errno == EINTR)
#endif

#ifdef _WIN32
/* Winsock has to be started before any socket call, and counts starts against stops. It is
   started once for the process and left started, so that the count does not depend on how many
   connections have been opened and closed, or on the caller balancing its own use of winsock
   against this one. The system releases it when the process exits. */
static krpc_error_t krpc_start_winsock(void) {
  static int started = 0;
  WSADATA winsock;
  if (started) return KRPC_OK;
  if (WSAStartup(MAKEWORD(2, 2), &winsock) != 0) KRPC_RETURN_ERROR(IO, "failed to start winsock");
  started = 1;
  return KRPC_OK;
}
#endif

/* Ask the socket to suppress SIGPIPE, for a platform that offers no send flag to do it with.
   Where the flag exists there is nothing to set, as it is passed on every send instead. */
static void krpc_suppress_sigpipe(SOCKET handle) {
#if !defined(MSG_NOSIGNAL) && defined(SO_NOSIGPIPE)
  int nosigpipe = 1;
  setsockopt(handle, SOL_SOCKET, SO_NOSIGPIPE, (const char*)&nosigpipe, sizeof(nosigpipe));
#else
  (void)handle;
#endif
}

krpc_error_t krpc_close(krpc_connection_t connection) {
  closesocket((SOCKET)connection);
  return KRPC_OK;
}

krpc_error_t krpc_read(krpc_connection_t connection, uint8_t* buf, size_t count) {
  size_t total = 0;
  while (total < count) {
    int result = recv((SOCKET)connection, (char*)(buf + total), (int)(count - total), 0);
    if (result < 0) {
      if (KRPC_INTERRUPTED()) continue;
      KRPC_RETURN_ERROR(IO, "read failed");
    } else if (result == 0) {
      KRPC_RETURN_ERROR(EOF, "eof received");
    } else {
      total += (size_t)result;
    }
  }
  return KRPC_OK;
}

krpc_error_t krpc_write(krpc_connection_t connection, const uint8_t* buf, size_t count) {
  size_t total = 0;
  while (total < count) {
    int result =
        send((SOCKET)connection, (const char*)(buf + total), (int)(count - total), KRPC_SEND_FLAGS);
    if (result <= 0) {
      if (result < 0 && KRPC_INTERRUPTED()) continue;
      KRPC_RETURN_ERROR(IO, "write failed");
    }
    total += (size_t)result;
  }
  return KRPC_OK;
}

#endif

#if defined(KRPC_COMMUNICATION_TCP)

#include <stdio.h>

#ifndef _WIN32
#include <netdb.h>
#include <netinet/in.h>
#include <netinet/tcp.h>
#endif

/* The longest a port number and its terminator can be, as getaddrinfo takes the service to
 * connect to as a string */
#define KRPC_PORT_LENGTH 6

krpc_error_t krpc_open(krpc_connection_t* connection, const krpc_connection_config_t* arg) {
  struct addrinfo hints;
  struct addrinfo* addresses;
  struct addrinfo* address;
  char port[KRPC_PORT_LENGTH];
  SOCKET result = INVALID_SOCKET;
  int nodelay = 1;
  if (arg == NULL || arg->address == NULL) KRPC_RETURN_ERROR(IO, "no server address to connect to");
#ifdef _WIN32
  KRPC_RETURN_ON_ERROR(krpc_start_winsock());
#endif
  snprintf(port, sizeof(port), "%u", (unsigned int)arg->port);
  memset(&hints, 0, sizeof(hints));
  hints.ai_family = AF_UNSPEC;
  hints.ai_socktype = SOCK_STREAM;
  if (getaddrinfo(arg->address, port, &hints, &addresses) != 0)
    KRPC_RETURN_ERROR(IO, "failed to resolve server address");
  /* The address may resolve to several endpoints, IPv6 and IPv4 among them, and only one of them
     need be listening. Try each until one connects. */
  for (address = addresses; address != NULL; address = address->ai_next) {
    result = socket(address->ai_family, address->ai_socktype, address->ai_protocol);
    if (result == INVALID_SOCKET) continue;
    if (connect(result, address->ai_addr, (int)address->ai_addrlen) == 0) break;
    closesocket(result);
    result = INVALID_SOCKET;
  }
  freeaddrinfo(addresses);
  if (result == INVALID_SOCKET) KRPC_RETURN_ERROR(IO, "failed to connect to server");
  /* A request is written in full and then waited on, so holding one back in the hope of more to
     send with it only adds that wait to every call. */
  setsockopt(result, IPPROTO_TCP, TCP_NODELAY, (const char*)&nodelay, sizeof(nodelay));
  krpc_suppress_sigpipe(result);
  *connection = (krpc_connection_t)result;
  return KRPC_OK;
}

#endif

#if defined(KRPC_COMMUNICATION_LOCALSOCKET)

/* Where the address of a unix domain socket is declared. Windows names and lays it out the same
   way, in a header of its own that came with the address family in Windows 10 1803. */
#ifdef _WIN32
#include <afunix.h>
#else
#include <sys/un.h>
#endif

krpc_error_t krpc_open(krpc_connection_t* connection, const krpc_connection_config_t* arg) {
  const char* path = arg;
  struct sockaddr_un address;
  SOCKET result;
  size_t length;
  if (path == NULL) KRPC_RETURN_ERROR(IO, "no socket path to connect to");
  /* The path is copied into a field of a fixed size, so one that does not fit has to be reported
     rather than truncated to name a socket the caller did not ask for. */
  length = strlen(path);
  if (length >= sizeof(address.sun_path)) KRPC_RETURN_ERROR(IO, "socket path too long");
#ifdef _WIN32
  KRPC_RETURN_ON_ERROR(krpc_start_winsock());
#endif
  /* The address is zeroed above the copy, so the path it holds is terminated by what is
     already there rather than by a byte copied over it. */
  memset(&address, 0, sizeof(address));
  address.sun_family = AF_UNIX;
  memcpy(address.sun_path, path, length);
  result = socket(AF_UNIX, SOCK_STREAM, 0);
  if (result == INVALID_SOCKET) KRPC_RETURN_ERROR(IO, "failed to create socket");
  if (connect(result, (struct sockaddr*)&address, (int)sizeof(address)) != 0) {
    closesocket(result);
    KRPC_RETURN_ERROR(IO, "failed to connect to socket");
  }
  krpc_suppress_sigpipe(result);
  *connection = (krpc_connection_t)result;
  return KRPC_OK;
}

#endif

#if defined(KRPC_COMMUNICATION_ARDUINO)

krpc_error_t krpc_open(krpc_connection_t* connection, const krpc_connection_config_t* arg) {
  if (arg)
    (*connection)->begin(arg->speed, arg->config);
  else
    (*connection)->begin(9600, SERIAL_8N1);
  while (!*connection) {
  }
  return KRPC_OK;
}

krpc_error_t krpc_close(krpc_connection_t connection) {
  connection->end();
  return KRPC_OK;
}

krpc_error_t krpc_read(krpc_connection_t connection, uint8_t* buf, size_t count) {
  size_t total = 0;
  while (total < count) {
    // A serial link has no end-of-file, so the only sign the peer has gone is silence.
    // readBytes waits up to the serial timeout for each byte, so it returns nothing only
    // when not a single byte arrived in that time; a slow but progressing stream keeps
    // going. Use Serial.setTimeout to control how long to wait before giving up.
    size_t result = connection->readBytes(buf + total, count - total);
    if (result == 0) KRPC_RETURN_ERROR(EOF, "eof received");
    total += result;
  }
  return KRPC_OK;
}

krpc_error_t krpc_write(krpc_connection_t connection, const uint8_t* buf, size_t count) {
  if (count != connection->write(buf, count)) KRPC_RETURN_ERROR(IO, "write failed");
  return KRPC_OK;
}

#endif  // KRPC_PLATFORM_ARDUINO
