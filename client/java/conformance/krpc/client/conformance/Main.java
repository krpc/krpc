package krpc.client.conformance;

import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import com.google.protobuf.Descriptors.FieldDescriptor;
import com.google.protobuf.Message;
import com.google.protobuf.util.JsonFormat;
import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.io.PrintStream;
import java.nio.charset.StandardCharsets;
import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.LinkedBlockingQueue;
import krpc.conformance.Testee.Command;
import krpc.conformance.Testee.Hello;
import krpc.conformance.Testee.Output;
import krpc.conformance.Testee.Reply;
import krpc.conformance.Testee.Value;

/**
 * The conformance testee for the Java client. Reads commands from the conformance driver on
 * standard input, runs them against the client, and writes the replies to standard output. See
 * conformance/testee.proto for the protocol.
 */
public final class Main {
  private static final String[] PROFILES = {
    "core", "error-types", "streams", "events", "functions", "connections", "threads",
    "callbacks", "conditions", "ordering", "races"
  };
  private static final String[] PROTOCOLS = {"tcpip", "localsocket"};

  private Main() {}

  /**
   * Sets the negative zeros in a message from its JSON form. JsonFormat parses a floating point
   * value through BigDecimal, which drops the sign of zero.
   */
  static void restoreNegativeZeros(Message.Builder builder, JsonObject json) {
    for (FieldDescriptor field : builder.getDescriptorForType().getFields()) {
      JsonElement element = json.get(field.getJsonName());
      if (element == null) {
        element = json.get(field.getName());
      }
      if (element == null || element.isJsonNull() || field.isMapField()) {
        continue;
      }
      if (field.getJavaType() == FieldDescriptor.JavaType.MESSAGE) {
        if (field.isRepeated()) {
          JsonArray items = element.getAsJsonArray();
          for (int i = 0; i < items.size(); i++) {
            restoreNegativeZeros(
                builder.getRepeatedFieldBuilder(field, i), items.get(i).getAsJsonObject());
          }
        } else {
          restoreNegativeZeros(builder.getFieldBuilder(field), element.getAsJsonObject());
        }
      } else if (!field.isRepeated() && isNegativeZero(element)) {
        if (field.getJavaType() == FieldDescriptor.JavaType.DOUBLE) {
          builder.setField(field, -0.0);
        } else if (field.getJavaType() == FieldDescriptor.JavaType.FLOAT) {
          builder.setField(field, -0.0f);
        }
      }
    }
  }

  private static boolean isNegativeZero(JsonElement element) {
    if (!element.isJsonPrimitive()) {
      return false;
    }
    String text = element.getAsString();
    try {
      return text.startsWith("-") && Double.parseDouble(text) == 0.0;
    } catch (NumberFormatException e) {
      return false;
    }
  }

  /** Writes messages to the driver, each whole under a lock. */
  static final class Writer {
    private static final JsonFormat.Printer PRINTER =
        JsonFormat.printer().omittingInsignificantWhitespace();
    private final OutputStream stream;

    Writer(OutputStream stream) {
      this.stream = stream;
    }

    synchronized void write(Output message) {
      try {
        byte[] line = (PRINTER.print(message) + "\n").getBytes(StandardCharsets.UTF_8);
        stream.write(line);
        stream.flush();
      } catch (IOException exn) {
        throw new IllegalStateException(exn);
      }
    }
  }

  /** A named thread that runs the commands sent to it in order. */
  static final class Worker {
    private static final Command STOP = Command.getDefaultInstance();
    private final LinkedBlockingQueue<Command> queue = new LinkedBlockingQueue<>();
    private final Thread thread;
    private boolean stopped;

    Worker(Testee testee) {
      thread = new Thread(() -> {
        Command command;
        while ((command = next()) != null) {
          testee.run(command, this::next);
        }
      });
      thread.setDaemon(true);
      thread.start();
    }

    /** The next command, or null once the worker is stopped. */
    private Command next() {
      while (!stopped) {
        Command command;
        try {
          command = queue.take();
        } catch (InterruptedException exn) {
          // An interrupt that arrived once the command it was for had finished
          continue;
        }
        if (command == STOP) {
          stopped = true;
          break;
        }
        return command;
      }
      return null;
    }

    void submit(Command command) {
      queue.add(command);
    }

    void interrupt() {
      thread.interrupt();
    }

    void stop() throws InterruptedException {
      queue.add(STOP);
      thread.join();
    }
  }

  /** Runs the testee until the driver closes standard input. */
  public static void main(String[] args) throws Exception {
    OutputStream stdout = System.out;
    // The protocol owns standard output
    System.setOut(new PrintStream(System.err, true, "UTF-8"));
    Writer output = new Writer(stdout);
    Hello.Builder hello = Hello.newBuilder().setClient("java");
    for (String profile : PROFILES) {
      hello.addProfiles(profile);
    }
    for (String protocol : PROTOCOLS) {
      hello.addProtocols(protocol);
    }
    output.write(Output.newBuilder().setHello(hello).build());

    RacePause.install();
    WriteFaults.install();
    Testee testee = new Testee(output);
    Map<String, Worker> workers = new HashMap<>();
    BufferedReader input =
        new BufferedReader(new InputStreamReader(System.in, StandardCharsets.UTF_8));
    while (true) {
      // Each command is its JSON form on a line of its own
      String line = input.readLine();
      if (line == null) {
        break;
      }
      Command.Builder builder = Command.newBuilder();
      JsonFormat.parser().merge(line, builder);
      restoreNegativeZeros(builder, JsonParser.parseString(line).getAsJsonObject());
      Command command = builder.build();
      if (command.hasInterrupt()) {
        // Run here, as the worker to interrupt is busy
        Worker worker = workers.get(command.getInterrupt().getThread());
        if (worker != null) {
          worker.interrupt();
        }
        output.write(Output.newBuilder()
            .setReply(Reply.newBuilder().setId(command.getId()).setValue(Value.newBuilder()))
            .build());
        continue;
      }
      workers.computeIfAbsent(command.getThread(), x -> new Worker(testee)).submit(command);
    }
    for (Worker worker : workers.values()) {
      worker.stop();
    }
    testee.close();
    System.exit(0);
  }
}
