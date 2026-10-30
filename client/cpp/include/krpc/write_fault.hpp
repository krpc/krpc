#pragma once

namespace krpc {

#ifdef KRPC_RACE_POINTS

/** Called before each write to the connection. A hook that throws fails the write with what it
    throws, as a failed socket write does. Compiled in only with KRPC_RACE_POINTS defined, for the
    conformance testee. */
typedef void (*WriteFaultHook)();

/** Set the hook called before each write. None is set at first. */
void set_write_fault_hook(WriteFaultHook hook);

#endif

namespace detail {

#ifdef KRPC_RACE_POINTS
void reach_write_fault();
#else
inline void reach_write_fault() {}
#endif

}  // namespace detail

}  // namespace krpc
