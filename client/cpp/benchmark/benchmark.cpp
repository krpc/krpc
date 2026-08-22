// Benchmarks for the C++ client, run by //tools/benchmarks:cpp.
//
// Measures what this client costs from inside it: the round trip for a procedure call, and what
// a call carrying a collection of values costs. The runner starts a TestServer, says in the
// environment where it is listening and which transport that is, and reads the JSON printed
// here; see tools/benchmarks/run_client.py for the contract and for what happens to these
// numbers afterwards.

#include <krpc.hpp>

#include <algorithm>
#include <chrono>
#include <cstdlib>
#include <functional>
#include <iostream>
#include <sstream>
#include <string>
#include <utility>
#include <vector>

// The generated TestService header names KRPC::Expression, whose type is declared here.
#include <krpc/services/krpc.hpp>

#include "services/test_service.hpp"

namespace {

using clock_type = std::chrono::steady_clock;

// How long one timed loop should run for. Long enough that the clock and a stray scheduling
// delay do not decide the answer, short enough that a whole run stays in seconds.
const double kTargetSeconds = 0.2;

// How many timed loops to take. A round trip crosses a socket and a scheduler as well as the
// server, and every one of those can only make a sample slower.
const int kSamples = 9;

// How long one discarded chunk of calls runs for while a case is being settled, how many of
// them at a time are asked whether it has stopped getting faster, and how much better the
// last few have to be than everything before them for it to count as still improving.
const double kSettleChunkSeconds = 0.1;
const int kSettleChunks = 3;
const double kSettleTolerance = 0.02;

// How long to keep settling one case before measuring it anyway.
const double kSettleTimeoutSeconds = 10.0;

// How many values the collection case sends and gets back. A call carries a value at a time,
// so what one costs to encode and decode is lost in the round trip it arrives in; a list makes
// that per-value cost most of what the case measures. The same count for every client, so that
// the figures can be read against each other.
const int kListValues = 100;

struct Case {
  std::string name;
  std::string unit;
  std::vector<double> samples;
  std::string rate;
  std::string note;
  // Whether the case had settled before it was measured. See tools/benchmarks/run_client.py
  // for what the runner does with it.
  bool settled = true;
};

double seconds_since(clock_type::time_point start) {
  return std::chrono::duration<double>(clock_type::now() - start).count();
}

int port(const char* name, int fallback) {
  const char* value = std::getenv(name);
  return value == nullptr ? fallback : std::stoi(value);
}

// Connect over whichever transport the runner started the server with, which it names by socket
// path or by port. Both are measured, since which one carries a call is part of what it costs.
krpc::Client connect_to_server() {
  const char* rpc_path = std::getenv("RPC_PATH");
  if (rpc_path != nullptr) {
    const char* stream_path = std::getenv("STREAM_PATH");
    return krpc::connect_local("cpp_client_benchmark", rpc_path,
                               stream_path == nullptr ? "" : stream_path);
  }
  return krpc::connect("cpp_client_benchmark", "localhost", port("RPC_PORT", 50000),
                       port("STREAM_PORT", 50001));
}

// Call for a short while and return the milliseconds one call took.
double chunk(const std::function<void()>& call) {
  auto start = clock_type::now();
  long calls = 0;
  while (seconds_since(start) < kSettleChunkSeconds) {
    call();
    calls++;
  }
  return seconds_since(start) * 1e3 / std::max(calls, 1L);
}

struct Settled {
  double per_call;
  bool settled;
};

// Make discarded calls until they stop getting faster, and return what one costs along with
// whether it got there. A fixed warmup cannot know when it is done: both ends of a round trip
// get faster under load for a while, and a case measured before that finishes returns a curve
// rather than a cost. Every case is settled on its own, since one already warmed by the case
// before it says so within a few chunks. The cost of a call also falls out of the last chunk,
// which is what sizes the timed loops.
Settled settle(const std::function<void()>& call) {
  std::vector<double> chunks{chunk(call)};
  auto start = clock_type::now();
  while (seconds_since(start) < kSettleTimeoutSeconds) {
    chunks.push_back(chunk(call));
    if (static_cast<int>(chunks.size()) > kSettleChunks) {
      auto split = chunks.end() - kSettleChunks;
      double recent = *std::min_element(split, chunks.end());
      double earlier = *std::min_element(chunks.begin(), split);
      if (recent >= earlier * (1 - kSettleTolerance))
        return {recent, true};
    }
  }
  // However many chunks it got through, which is fewer than a settle compares where a single
  // one of them ran longer than the whole timeout.
  auto taken = static_cast<long>(chunks.size());
  auto recent = chunks.begin() + std::max(taken - kSettleChunks, 0L);
  return {*std::min_element(recent, chunks.end()), false};
}

// Time the call in a loop, several times over, and return milliseconds per call along with
// whether the case had settled before any of it was measured.
std::pair<std::vector<double>, bool> timed_loop(const std::function<void()>& call) {
  Settled warm = settle(call);
  long iterations = std::max(static_cast<long>(kTargetSeconds * 1e3 / warm.per_call), 1L);

  std::vector<double> samples;
  for (int sample = 0; sample < kSamples; sample++) {
    auto loop = clock_type::now();
    for (long i = 0; i < iterations; i++)
      call();
    samples.push_back(seconds_since(loop) * 1e3 / iterations);
  }
  return {samples, warm.settled};
}

Case round_trip(const std::string& name, const std::function<void()>& call) {
  auto [samples, settled] = timed_loop(call);
  return {name, "ms", samples, "calls/s", "", settled};
}

std::string quote(const std::string& value) { return "\"" + value + "\""; }

void emit(const std::vector<Case>& cases) {
  std::ostringstream out;
  out << "{\"results\": [";
  for (size_t i = 0; i < cases.size(); i++) {
    const Case& item = cases[i];
    out << (i == 0 ? "" : ", ") << "{\"case\": " << quote(item.name)
        << ", \"unit\": " << quote(item.unit) << ", \"rate\": " << quote(item.rate)
        << ", \"note\": " << quote(item.note) << ", \"samples\": [";
    for (size_t j = 0; j < item.samples.size(); j++)
      out << (j == 0 ? "" : ", ") << item.samples[j];
    out << "]";
    if (!item.settled)
      out << ", \"settled\": false";
    out << "}";
  }
  out << "]}";
  std::cout << out.str() << std::endl;
}

}  // namespace

int main() {
  krpc::Client conn = connect_to_server();
  krpc::services::TestService test_service(&conn);

  std::vector<int32_t> values;
  values.reserve(kListValues);
  for (int i = 0; i < kListValues; i++)
    values.push_back(i);
  std::ostringstream list_name;
  list_name << "round trip, list of " << kListValues << " values";

  std::vector<Case> cases{
    round_trip("round trip", [&] { test_service.float_to_string(3.14159f); }),
    round_trip("round trip, 3 arguments",
               [&] { test_service.add_multiple_values(3.14159f, 1, 2); }),
    round_trip(list_name.str(), [&] { test_service.increment_list(values); }),
  };

  emit(cases);
  return 0;
}
