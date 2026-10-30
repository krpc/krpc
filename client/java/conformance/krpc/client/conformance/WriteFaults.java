package krpc.client.conformance;

import java.lang.reflect.Field;
import java.util.concurrent.atomic.AtomicReference;
import java.util.function.Supplier;

/** The write fault armed for the client's next write, as the message of its error. */
final class WriteFaults {
  private static final AtomicReference<String> armed = new AtomicReference<>();

  private WriteFaults() {}

  /** Sets the client's write fault hook, which only the testee has. */
  static void install() throws ReflectiveOperationException {
    Field hook = Class.forName("krpc.client.WriteFault").getDeclaredField("hook");
    hook.setAccessible(true);
    Supplier<String> reach = () -> armed.getAndSet(null);
    hook.set(null, reach);
  }

  static void arm(String message) {
    armed.set(message);
  }
}
