#include "krpc/race_point.hpp"

#ifdef KRPC_RACE_POINTS

#include <atomic>

namespace krpc {

namespace {

std::atomic<RacePointHook> hook{nullptr};

}  // namespace

void set_race_point_hook(RacePointHook value) { hook.store(value); }

namespace detail {

void reach_race_point(RacePoint point, int tag) {
  auto current = hook.load();
  if (current) current(point, tag);
}

}  // namespace detail

}  // namespace krpc

#endif
