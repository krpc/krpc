## client/lua

- **Deprecated:** `krpc.platform.sleep`, in favor of luasocket's `socket.sleep`. It now sleeps
  without holding a CPU core, and for fractions of a second (#1323)
