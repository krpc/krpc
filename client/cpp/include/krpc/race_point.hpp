#pragma once

#include <cstdint>

namespace krpc {

/** A point in the client where a test can pause a thread, to force a race. The points are
    compiled in only with KRPC_RACE_POINTS defined, for the conformance testee. */
enum class RacePoint : uint8_t {
  /** The update thread, with a value of a stream and a copy of its callbacks, before it calls
      them. */
  stream_callbacks,
  /** The update thread, before it calls the update callbacks. */
  update_callbacks,
  /** The thread adding a callback, with the stream's value, before the first call. */
  first_call,
  /** The thread adding a callback, holding the callback's lock for the first call, before it
      marks the call. */
  first_call_started,
  /** The thread adding an event callback, with a firing reserved for it, before the replay. */
  replay,
  /** The update thread, with the ending of a stream stored, before it wakes the stream's waiters
      and calls its end callbacks. */
  stream_ending,
  /** The update thread, with a value of a stream stored, before it wakes the stream's waiters. */
  stream_value,
  /** The thread adding a callback, with the first call made and the values queued during it
      passed, before it releases the callback's lock. */
  first_call_ended
};

#ifdef KRPC_RACE_POINTS

/** Called at each race point, with the tag of the callback being added, or -1 on the update
    thread. */
typedef void (*RacePointHook)(RacePoint point, int tag);

/** Set the hook called at each race point. None is set at first. */
void set_race_point_hook(RacePointHook hook);

#endif

namespace detail {

#ifdef KRPC_RACE_POINTS
void reach_race_point(RacePoint point, int tag);
#else
inline void reach_race_point(RacePoint, int) {}
#endif

}  // namespace detail

}  // namespace krpc
