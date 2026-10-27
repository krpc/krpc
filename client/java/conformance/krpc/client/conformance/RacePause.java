package krpc.client.conformance;

import java.lang.reflect.Field;
import java.util.function.BiConsumer;
import java.util.function.IntConsumer;
import java.util.function.IntSupplier;

/** The pause armed at a race point of the client, and the thread paused there. */
final class RacePause {
  private static final RacePause INSTANCE = new RacePause();
  // Called with the tag of the callback this thread is adding, at a race point in the add, so the
  // callback can be removed while the add is paused
  private static final ThreadLocal<IntConsumer> onTag = new ThreadLocal<>();

  private String armed;
  private boolean paused = false;
  private boolean resumed = false;

  private RacePause() {}

  static RacePause instance() {
    return INSTANCE;
  }

  /** Sets the client's race point hook, which only the testee has. */
  static void install() throws ReflectiveOperationException {
    Field hook = Class.forName("krpc.client.RacePoint").getDeclaredField("hook");
    hook.setAccessible(true);
    BiConsumer<String, Integer> reach = RacePause::reach;
    hook.set(null, reach);
  }

  /** The hook the client calls at each race point. */
  private static void reach(String point, int tag) {
    IntConsumer current = onTag.get();
    if (tag >= 0 && current != null) {
      current.accept(tag);
    }
    INSTANCE.block(point);
  }

  /** Runs an add of a callback, with on tag set for this thread while it runs. */
  static int adding(IntConsumer tag, IntSupplier add) {
    onTag.set(tag);
    try {
      return add.getAsInt();
    } finally {
      onTag.remove();
    }
  }

  synchronized void arm(String point) {
    if (armed != null || paused) {
      throw new IllegalStateException("A pause is already armed");
    }
    armed = point;
  }

  /** Waits until a thread has paused, with a timeout in seconds. */
  synchronized boolean await(double timeout) throws InterruptedException {
    long deadline = System.nanoTime() + (long) (timeout * 1e9);
    while (!paused) {
      long remaining = deadline - System.nanoTime();
      if (remaining <= 0) {
        return false;
      }
      wait(remaining / 1000000, (int) (remaining % 1000000));
    }
    return true;
  }

  synchronized void resume() {
    armed = null;
    if (paused) {
      resumed = true;
    }
    notifyAll();
  }

  private synchronized void block(String point) {
    if (!point.equals(armed)) {
      return;
    }
    armed = null;
    paused = true;
    notifyAll();
    boolean interrupted = false;
    while (!resumed) {
      try {
        wait();
      } catch (InterruptedException exn) {
        interrupted = true;
      }
    }
    paused = false;
    resumed = false;
    if (interrupted) {
      Thread.currentThread().interrupt();
    }
  }
}
