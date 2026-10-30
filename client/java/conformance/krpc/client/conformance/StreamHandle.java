package krpc.client.conformance;

import java.util.function.Consumer;
import java.util.function.Function;
import krpc.client.Stream;
import krpc.conformance.Testee.Value;

/** A stream, with its values converted to conformance Values. */
final class StreamHandle {
  private final Stream<Object> stream;
  private final Function<Object, Value> convert;

  @SuppressWarnings("unchecked")
  StreamHandle(Stream<?> stream, Function<Object, Value> convert) {
    this.stream = (Stream<Object>) stream;
    this.convert = convert;
  }

  Value get() throws Exception {
    return convert.apply(stream.get());
  }

  /** Wait for an update, with no timeout if it is null. */
  boolean waitForUpdate(Double timeout) throws Exception {
    synchronized (stream.getCondition()) {
      if (timeout == null) {
        stream.waitForUpdate();
        return true;
      }
      return stream.waitForUpdateWithTimeout(timeout);
    }
  }

  Object getCondition() {
    return stream.getCondition();
  }

  /** The client's stream object. */
  Object getStream() {
    return stream;
  }

  float getRate() throws Exception {
    return stream.getRate();
  }

  void setRate(float rate) throws Exception {
    stream.setRate(rate);
  }

  void remove() throws Exception {
    stream.remove();
  }

  int addCallback(Consumer<Value> callback) {
    return stream.addCallback(x -> callback.accept(convert.apply(x)));
  }

  void removeCallback(int tag) {
    stream.removeCallback(tag);
  }

  int addEndCallback(Consumer<Exception> callback) {
    return stream.addEndCallback(callback);
  }

  void removeEndCallback(int tag) {
    stream.removeEndCallback(tag);
  }
}
