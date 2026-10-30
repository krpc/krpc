package krpc.client;

import java.util.function.BiConsumer;

/**
 * A point in the client where a test can pause a thread, to force a race. Only the conformance
 * testee sets the hook, by reflection.
 */
enum RacePoint {
  /**
   * The dispatcher thread, with a value of a stream and a copy of its callbacks, before it calls
   * them.
   */
  STREAM_CALLBACKS,
  /**
   * The dispatcher thread, with the ending of a stream stored, before it wakes the stream's
   * waiters and calls its end callbacks.
   */
  STREAM_ENDING,
  /** The thread adding a callback, with the stream's value, before the first call. */
  FIRST_CALL,
  /**
   * The thread adding a callback, holding the callback's lock for the first call, before it marks
   * the call.
   */
  FIRST_CALL_STARTED,
  /** The thread adding an event callback, with a firing reserved for it, before the replay. */
  REPLAY,
  /**
   * The dispatcher thread, with a value of a stream stored, before it wakes the stream's waiters.
   */
  STREAM_VALUE,
  /**
   * The thread adding a callback, with the first call made and the values queued during it
   * passed, before it releases the callback's lock.
   */
  FIRST_CALL_ENDED;

  // Called at each point with its name and the tag of the callback being added, or -1 on the
  // dispatcher thread
  private static volatile BiConsumer<String, Integer> hook;

  /** Calls the hook, if one is set. */
  static void reach(RacePoint point, int tag) {
    BiConsumer<String, Integer> current = hook;
    if (current != null) {
      current.accept(point.name(), tag);
    }
  }
}
