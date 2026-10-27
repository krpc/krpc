package krpc.client;

import java.io.IOException;
import java.util.function.Supplier;

/**
 * A fault that fails a write to the connection, as a failed socket write does. Only the
 * conformance testee sets the hook, by reflection.
 */
final class WriteFault {
  private WriteFault() {}

  // Called before each write, returning the message of the error to fail it with, or null
  private static volatile Supplier<String> hook;

  /** Throws the error of an armed fault, if the hook arms one. */
  static void reach() throws IOException {
    Supplier<String> current = hook;
    if (current == null) {
      return;
    }
    String message = current.get();
    if (message != null) {
      throw new IOException(message);
    }
  }
}
