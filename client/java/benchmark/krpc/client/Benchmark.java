package krpc.client;

import java.io.IOException;
import java.util.ArrayList;
import java.util.List;
import krpc.client.services.TestService;

/**
 * Benchmarks for the java client, run by {@code //tools/benchmarks:java}.
 *
 * <p>Measures what this client costs from inside it: the round trip for a procedure call, and
 * what a call carrying a collection of values costs. The runner starts a TestServer, says in
 * the environment where it is listening and which transport that is, and reads the JSON printed
 * here; see tools/benchmarks/run_client.py for the contract and for what happens to these
 * numbers afterwards.
 */
public final class Benchmark {

  /**
   * How long one timed loop should run for. Long enough that the clock and a stray scheduling
   * delay do not decide the answer, short enough that a whole run stays in seconds.
   */
  private static final double TARGET_SECONDS = 0.2;

  /** How many timed loops to take. */
  private static final int SAMPLES = 9;

  /**
   * How long one discarded chunk of calls runs for while a case is being settled, how many of
   * them at a time are asked whether it has stopped getting faster, and how much better the
   * last few have to be than everything before them for it to count as still improving.
   */
  private static final double SETTLE_CHUNK_SECONDS = 0.1;

  private static final int SETTLE_CHUNKS = 3;

  private static final double SETTLE_TOLERANCE = 0.02;

  /** How long to keep settling one case before measuring it anyway. */
  private static final double SETTLE_TIMEOUT_SECONDS = 10.0;

  /**
   * How many values the collection case sends and gets back. A call carries a value at a time,
   * so what one costs to encode and decode is lost in the round trip it arrives in; a list makes
   * that per-value cost most of what the case measures. The same count for every client, so
   * that the figures can be read against each other.
   */
  private static final int LIST_VALUES = 100;

  private Benchmark() {
  }

  /** A measured case, as the runner expects to read it. */
  private static final class Case {
    private final String name;
    private final String unit;
    private final List<Double> samples;
    private final String rate;
    private final String note;
    /**
     * Whether the case had settled before it was measured. See tools/benchmarks/run_client.py
     * for what the runner does with it.
     */
    private final boolean settled;

    Case(String name, String unit, List<Double> samples, String rate, String note,
         boolean settled) {
      this.name = name;
      this.unit = unit;
      this.samples = samples;
      this.rate = rate;
      this.note = note;
      this.settled = settled;
    }
  }

  /** Samples for a case, and whether it had settled before they were taken. */
  private static final class Timing {
    private final List<Double> samples;
    private final boolean settled;

    Timing(List<Double> samples, boolean settled) {
      this.samples = samples;
      this.settled = settled;
    }
  }

  /** What one call costs once a case has stopped getting faster, and whether it got there. */
  private static final class Settled {
    private final double perCall;
    private final boolean reached;

    Settled(double perCall, boolean reached) {
      this.perCall = perCall;
      this.reached = reached;
    }
  }

  /** Something to time, which may throw the way a remote call does. */
  private interface Call {
    void run() throws RPCException;
  }

  private static int port(String name, int fallback) {
    String value = System.getenv(name);
    return value == null ? fallback : Integer.parseInt(value);
  }

  /**
   * Connects over whichever transport the runner started the server with, which it names by
   * socket path or by port. Both are measured, since which one carries a call is part of what it
   * costs.
   */
  private static Connection connect() throws IOException {
    String rpcPath = System.getenv("RPC_PATH");
    if (rpcPath != null) {
      return Connection.newLocalInstance(
          "java_client_benchmark", rpcPath, System.getenv("STREAM_PATH"));
    }
    return Connection.newInstance(
        "java_client_benchmark", "localhost", port("RPC_PORT", 50000),
        port("STREAM_PORT", 50001));
  }

  private static double secondsSince(long start) {
    return (System.nanoTime() - start) / 1e9;
  }

  /** Call for a short while and return the milliseconds one call took. */
  private static double chunk(Call call) throws RPCException {
    long start = System.nanoTime();
    long calls = 0;
    while (secondsSince(start) < SETTLE_CHUNK_SECONDS) {
      call.run();
      calls++;
    }
    return secondsSince(start) * 1e3 / Math.max(calls, 1L);
  }

  /**
   * Make discarded calls until they stop getting faster, and return what one costs along with
   * whether it got there.
   *
   * <p>A fixed warmup cannot know when it is done. Both ends of a round trip get faster under
   * load for a while - the server's rate control adapts to what it is being asked for, and the
   * JVM compiles a method once it has been called enough - and a case measured before that
   * finishes returns a curve rather than a cost. Every case is settled on its own, since one
   * already warmed by the case before it says so within a few chunks. The cost of a call also
   * falls out of the last chunk, which is what sizes the timed loops.
   */
  private static Settled settle(Call call) throws RPCException {
    List<Double> chunks = new ArrayList<>();
    chunks.add(chunk(call));
    long start = System.nanoTime();
    while (secondsSince(start) < SETTLE_TIMEOUT_SECONDS) {
      chunks.add(chunk(call));
      if (chunks.size() > SETTLE_CHUNKS) {
        int split = chunks.size() - SETTLE_CHUNKS;
        double recent = smallest(chunks.subList(split, chunks.size()));
        double earlier = smallest(chunks.subList(0, split));
        if (recent >= earlier * (1 - SETTLE_TOLERANCE)) {
          return new Settled(recent, true);
        }
      }
    }
    // The chunks it got through, which is fewer than a settle compares when a single chunk ran
    // longer than the whole timeout.
    int taken = Math.max(chunks.size() - SETTLE_CHUNKS, 0);
    return new Settled(smallest(chunks.subList(taken, chunks.size())), false);
  }

  private static double smallest(List<Double> values) {
    double best = values.get(0);
    for (double value : values) {
      best = Math.min(best, value);
    }
    return best;
  }

  /**
   * Time the call in a loop, several times over, and return milliseconds per call along with
   * whether the case had settled before any of it was measured.
   */
  private static Timing timedLoop(Call call) throws RPCException {
    Settled warm = settle(call);
    long iterations = Math.max((long) (TARGET_SECONDS * 1e3 / warm.perCall), 1L);

    List<Double> samples = new ArrayList<>();
    for (int sample = 0; sample < SAMPLES; sample++) {
      long loop = System.nanoTime();
      for (long i = 0; i < iterations; i++) {
        call.run();
      }
      samples.add(secondsSince(loop) * 1e3 / iterations);
    }
    return new Timing(samples, warm.reached);
  }

  private static Case roundTrip(String name, Call call) throws RPCException {
    Timing timing = timedLoop(call);
    return new Case(name, "ms", timing.samples, "calls/s", "", timing.settled);
  }

  private static void emit(List<Case> cases) {
    StringBuilder out = new StringBuilder("{\"results\": [");
    for (int i = 0; i < cases.size(); i++) {
      Case item = cases.get(i);
      out.append(i == 0 ? "" : ", ")
          .append("{\"case\": \"").append(item.name)
          .append("\", \"unit\": \"").append(item.unit)
          .append("\", \"rate\": \"").append(item.rate)
          .append("\", \"note\": \"").append(item.note)
          .append("\", \"samples\": [");
      for (int j = 0; j < item.samples.size(); j++) {
        out.append(j == 0 ? "" : ", ").append(item.samples.get(j));
      }
      out.append("]");
      if (!item.settled) {
        out.append(", \"settled\": false");
      }
      out.append("}");
    }
    out.append("]}");
    System.out.println(out);
  }

  /** Run the benchmarks and print them for the runner to read. */
  public static void main(String[] args) throws Exception {
    Connection connection = connect();
    TestService testService = TestService.newInstance(connection);

    List<Integer> values = new ArrayList<>(LIST_VALUES);
    for (int i = 0; i < LIST_VALUES; i++) {
      values.add(i);
    }

    List<Case> cases = new ArrayList<>();
    cases.add(roundTrip("round trip", () -> testService.floatToString(3.14159f)));
    cases.add(roundTrip("round trip, 3 arguments",
                        () -> testService.addMultipleValues(3.14159f, 1, 2)));
    cases.add(roundTrip("round trip, list of " + LIST_VALUES + " values",
                        () -> testService.incrementList(values)));

    emit(cases);
    connection.close();
  }
}
