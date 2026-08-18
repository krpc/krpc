/* Benchmarks for the C-nano client, run by //tools/benchmarks:cnano.
 *
 * Measures what this client costs from inside it: the round trip for a procedure call, and what
 * a call carrying a collection of values costs. The runner starts a TestServer, passes the ports
 * in the environment and reads the JSON printed here; see tools/benchmarks/run_client.py for the
 * contract and for what happens to these numbers afterwards.
 *
 * The client is built for TCP/IP here rather than for a serial port, which is the only way the
 * figures mean anything next to the other clients': the server reads a serial port on a poll
 * whose interval is longer than everything measured here put together.
 */

/* clock_gettime is POSIX rather than C. See the same note in src/communication.c. */
#if !defined(_POSIX_C_SOURCE)
#define _POSIX_C_SOURCE 199309L
#endif

#include <krpc_cnano.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <time.h>

#include "services/test_service.h"

/* How long one timed loop should run for. Long enough that the clock and a stray scheduling
   delay do not decide the answer, short enough that a whole run stays in seconds. */
#define TARGET_SECONDS 0.2

/* How many timed loops to take. A round trip crosses a socket and a scheduler as well as the
   server, and every one of those can only make a sample slower. */
#define SAMPLES 9

/* How long one discarded chunk of calls runs for while a case is being settled, how many of
   them at a time are asked whether it has stopped getting faster, and how much better the last
   few have to be than everything before them for it to count as still improving. */
#define SETTLE_CHUNK_SECONDS 0.1
#define SETTLE_CHUNKS 3
#define SETTLE_TOLERANCE 0.02

/* How long to keep settling one case before measuring it anyway, and room for the chunks that
   takes. A chunk runs for at least SETTLE_CHUNK_SECONDS, so the timeout bounds how many there
   can be; the array is sized above that bound rather than grown. */
#define SETTLE_TIMEOUT_SECONDS 10.0
#define SETTLE_CHUNKS_MAX 128

/* How many values the collection case sends and gets back. A call carries a value at a time, so
   what one costs to encode and decode is lost in the round trip it arrives in; a list makes
   that per-value cost most of what the case measures. The same count for every client, so that
   the figures can be read against each other. */
#define LIST_VALUES 100

/* The number of cases this program can report, which is every round trip it measures. */
#define CASES 3

struct benchmark_case {
  const char *name;
  double samples[SAMPLES];
  /* Whether the case had stopped getting faster before any of it was measured. */
  int settled;
};

/* What every case is measured against, and the list the collection case sends. Held here rather
   than passed through the settling and timing code, which only ever needs to make the call. */
static krpc_connection_t connection;
static krpc_list_int32_t list_argument = KRPC_NULL_LIST;

static void check(krpc_error_t error, const char *what) {
  if (error == KRPC_OK) return;
  fprintf(stderr, "%s: %s\n", what, krpc_get_error(error));
#ifdef KRPC_ERROR_MESSAGES
  fprintf(stderr, "%s\n", krpc_get_error_message());
#endif
  exit(1);
}

static double now(void) {
  struct timespec time;
  clock_gettime(CLOCK_MONOTONIC, &time);
  return (double)time.tv_sec + (double)time.tv_nsec * 1e-9;
}

/* The calls the cases measure. A returned string is left to the client to allocate and then
   freed, rather than decoded into a buffer held across calls, because every other client
   allocates one per call and these figures are read against theirs. */

static void call_float_to_string(void) {
  char *result = NULL;
  check(krpc_TestService_FloatToString(connection, &result, 3.14159f), "FloatToString");
  krpc_free(result);
}

static void call_add_multiple_values(void) {
  char *result = NULL;
  check(krpc_TestService_AddMultipleValues(connection, &result, 3.14159f, 1, 2),
        "AddMultipleValues");
  krpc_free(result);
}

static void call_increment_list(void) {
  krpc_list_int32_t result = KRPC_NULL_LIST;
  check(krpc_TestService_IncrementList(connection, &result, &list_argument), "IncrementList");
  KRPC_FREE_LIST(result);
}

/* Call for a short while and return the milliseconds one call took. */
static double chunk(void (*call)(void)) {
  double start = now();
  long calls = 0;
  double elapsed;
  do {
    call();
    calls++;
    elapsed = now() - start;
  } while (elapsed < SETTLE_CHUNK_SECONDS);
  return elapsed * 1e3 / (double)calls;
}

static double smallest(const double *values, int count) {
  double result = values[0];
  int i;
  for (i = 1; i < count; i++)
    if (values[i] < result) result = values[i];
  return result;
}

/* Make discarded calls until they stop getting faster, and return what one costs along with
   whether it got there. A fixed warmup cannot know when it is done: both ends of a round trip
   get faster under load for a while, and a case measured before that finishes returns a curve
   rather than a cost. Every case is settled on its own, since one already warmed by the case
   before it says so within a few chunks. The cost of a call also falls out of the last chunks,
   which is what sizes the timed loops. */
static double settle(void (*call)(void), int *settled) {
  double chunks[SETTLE_CHUNKS_MAX];
  int count = 1;
  double start;
  chunks[0] = chunk(call);
  start = now();
  while (now() - start < SETTLE_TIMEOUT_SECONDS && count < SETTLE_CHUNKS_MAX) {
    chunks[count++] = chunk(call);
    if (count > SETTLE_CHUNKS) {
      int split = count - SETTLE_CHUNKS;
      double recent = smallest(chunks + split, SETTLE_CHUNKS);
      double earlier = smallest(chunks, split);
      if (recent >= earlier * (1 - SETTLE_TOLERANCE)) {
        *settled = 1;
        return recent;
      }
    }
  }
  *settled = 0;
  return count < SETTLE_CHUNKS ? smallest(chunks, count)
                               : smallest(chunks + count - SETTLE_CHUNKS, SETTLE_CHUNKS);
}

/* Time the call in a loop, several times over, filling in the milliseconds one call took. */
static struct benchmark_case round_trip(const char *name, void (*call)(void)) {
  struct benchmark_case result;
  double per_call = settle(call, &result.settled);
  long iterations = (long)(TARGET_SECONDS * 1e3 / per_call);
  int sample;
  if (iterations < 1) iterations = 1;
  result.name = name;
  for (sample = 0; sample < SAMPLES; sample++) {
    long i;
    double start = now();
    for (i = 0; i < iterations; i++) call();
    result.samples[sample] = (now() - start) * 1e3 / (double)iterations;
  }
  return result;
}

static void emit(const struct benchmark_case *cases, int count) {
  int i;
  printf("{\"results\": [");
  for (i = 0; i < count; i++) {
    int sample;
    printf("%s{\"case\": \"%s\", \"unit\": \"ms\", \"rate\": \"calls/s\", \"samples\": [",
           i == 0 ? "" : ", ", cases[i].name);
    for (sample = 0; sample < SAMPLES; sample++)
      printf("%s%.9g", sample == 0 ? "" : ", ", cases[i].samples[sample]);
    printf("]");
    if (!cases[i].settled) printf(", \"settled\": false");
    printf("}");
  }
  printf("]}\n");
}

static uint16_t port(const char *name, uint16_t fallback) {
  const char *value = getenv(name);
  return value == NULL ? fallback : (uint16_t)atoi(value);
}

int main(void) {
  krpc_connection_config_t config;
  struct benchmark_case cases[CASES];
  char list_name[64];
  int count = 0;
  int i;

  config.address = "127.0.0.1";
  config.port = port("RPC_PORT", 50000);
  check(krpc_open(&connection, &config), "open");
  check(krpc_connect(connection, "cnano_client_benchmark"), "connect");

  list_argument.size = LIST_VALUES;
  list_argument.items = (int32_t *)krpc_calloc(LIST_VALUES, sizeof(int32_t));
  for (i = 0; i < LIST_VALUES; i++) list_argument.items[i] = i;
  snprintf(list_name, sizeof(list_name), "round trip, list of %d values", LIST_VALUES);

  cases[count++] = round_trip("round trip", &call_float_to_string);
  cases[count++] = round_trip("round trip, 3 arguments", &call_add_multiple_values);
  cases[count++] = round_trip(list_name, &call_increment_list);

  KRPC_FREE_LIST(list_argument);

  emit(cases, count);
  check(krpc_close(connection), "close");
  return 0;
}
