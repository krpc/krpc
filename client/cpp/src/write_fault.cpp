#include "krpc/write_fault.hpp"

#ifdef KRPC_RACE_POINTS

#include <atomic>

namespace krpc {

namespace {

std::atomic<WriteFaultHook> hook{nullptr};

}  // namespace

void set_write_fault_hook(WriteFaultHook value) { hook.store(value); }

namespace detail {

void reach_write_fault() {
  auto current = hook.load();
  if (current) current();
}

}  // namespace detail

}  // namespace krpc

#endif
