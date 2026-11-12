package krpc.client;

import com.google.protobuf.CodedOutputStream;
import com.google.protobuf.InvalidProtocolBufferException;
import com.google.protobuf.WireFormat;
import java.io.EOFException;
import java.io.IOException;
import java.io.InterruptedIOException;
import java.net.SocketTimeoutException;
import java.nio.ByteBuffer;
import java.nio.channels.ClosedChannelException;
import java.nio.channels.ClosedSelectorException;
import java.nio.channels.SelectionKey;
import java.nio.channels.Selector;
import java.nio.channels.SocketChannel;
import java.time.Duration;
import java.util.Arrays;
import java.util.function.Consumer;
import krpc.schema.KRPC;

/**
 * The socket channel to the server, and the length prefixed messages sent and received over it.
 * Each subclass opens the channel over one protocol.
 *
 * <p>The channel blocks until {@link #startNonBlocking} makes it non-blocking. It then waits for
 * data to read with one selector, the reader thread with another, and a write that fills the send
 * buffer with a third.
 */
abstract class Transport {
  // A length prefix is a varint, and one that does not end within five bytes cannot be the
  // uint32 it is meant to be
  private static final int MAXIMUM_PREFIX_LENGTH = 5;
  // The longest wait for data before checking the channel is still open, as a channel closed
  // without closing its selector does not wake the selector
  private static final long MAXIMUM_WAIT_MILLISECONDS = 1000;
  // A selector with one channel needs nothing from the key that is ready
  private static final Consumer<SelectionKey> IGNORE_KEY = key -> { };

  private final SocketChannel channel;
  private Selector readSelector;
  private Selector idleSelector;
  private Selector writeSelector;
  // The bytes read and not yet handed out are those from start to the buffer's position
  private ByteBuffer buffer = ByteBuffer.allocate(64 * 1024);
  private int start = 0;
  // When a byte last arrived, and whether part of a message is waiting for the rest
  private long lastProgress = System.nanoTime();
  private boolean partial = false;

  protected Transport(SocketChannel channel) {
    this.channel = channel;
  }

  /** Make the channel non-blocking, and open the selectors that wait on it. */
  void startNonBlocking() throws IOException {
    channel.configureBlocking(false);
    readSelector = Selector.open();
    idleSelector = Selector.open();
    writeSelector = Selector.open();
    channel.register(readSelector, SelectionKey.OP_READ);
    channel.register(idleSelector, SelectionKey.OP_READ);
    channel.register(writeSelector, SelectionKey.OP_WRITE);
  }

  /** Write a message. */
  void send(KRPC.Message message) throws IOException {
    int size = message.getSerializedSize();
    byte[] data = new byte[CodedOutputStream.computeUInt32SizeNoTag(size) + size];
    CodedOutputStream output = CodedOutputStream.newInstance(data);
    output.writeUInt32NoTag(size);
    message.writeTo(output);
    write(ByteBuffer.wrap(data));
  }

  /** Write a message holding the request. */
  void send(KRPC.Request request) throws IOException {
    // The Message is written around the request, which saves building one
    int size = request.getSerializedSize();
    int messageSize = 1 + CodedOutputStream.computeUInt32SizeNoTag(size) + size;
    byte[] data = new byte[CodedOutputStream.computeUInt32SizeNoTag(messageSize) + messageSize];
    CodedOutputStream output = CodedOutputStream.newInstance(data);
    output.writeUInt32NoTag(messageSize);
    output.writeTag(KRPC.Message.REQUEST_FIELD_NUMBER, WireFormat.WIRETYPE_LENGTH_DELIMITED);
    output.writeUInt32NoTag(size);
    request.writeTo(output);
    write(ByteBuffer.wrap(data));
  }

  /**
   * Write all of a buffer, waiting for room in the send buffer. An interrupt is held back until
   * the write ends, as a message cut short breaks the framing.
   */
  private void write(ByteBuffer data) throws IOException {
    WriteFault.reach();
    boolean interrupted = false;
    try {
      while (true) {
        channel.write(data);
        if (!data.hasRemaining()) {
          return;
        }
        writeSelector.select(IGNORE_KEY, MAXIMUM_WAIT_MILLISECONDS);
        // Cleared, as the selector returns at once while the thread is interrupted
        if (Thread.interrupted()) {
          interrupted = true;
        }
      }
    } catch (ClosedSelectorException exn) {
      throw new ClosedChannelException();
    } finally {
      if (interrupted) {
        Thread.currentThread().interrupt();
      }
    }
  }

  /** Close the write side, leaving the read side open. */
  void shutdownOutput() throws IOException {
    channel.shutdownOutput();
  }

  /** Close the channel and its selectors, which wakes a thread waiting on them. */
  void close() {
    try {
      channel.close();
    } catch (IOException exn) {
      // The channel is closed or failed already
    }
    for (Selector selector : new Selector[] {readSelector, idleSelector, writeSelector}) {
      if (selector == null) {
        continue;
      }
      try {
        selector.wakeup();
        selector.close();
      } catch (IOException exn) {
        // The selector is closed already
      }
    }
  }

  /**
   * Wait a while for data, without reading it. Returns whether data has arrived or the channel
   * has closed.
   */
  boolean waitReadable() throws IOException {
    return idleSelector.select(IGNORE_KEY, MAXIMUM_WAIT_MILLISECONDS) > 0 || !channel.isOpen();
  }

  /** Discard what arrives until the server closes the channel, or until the deadline. */
  void discard(long deadline) {
    ByteBuffer discard = ByteBuffer.allocate(4096);
    try {
      while (true) {
        long remaining = deadline - System.currentTimeMillis();
        if (remaining <= 0) {
          return;
        }
        readSelector.select(IGNORE_KEY, remaining);
        discard.clear();
        if (channel.read(discard) < 0) {
          return;
        }
      }
    } catch (IOException | ClosedSelectorException exn) {
      // The connection has failed already
    }
  }

  /** Read the next message, blocking until it has arrived in full. */
  byte[] read() throws IOException {
    while (true) {
      byte[] message = take();
      if (message != null) {
        return message;
      }
      fill();
    }
  }

  /**
   * Read the next message, waiting no longer than the timeout. Zero waits indefinitely. Throws a
   * SocketTimeoutException if no whole message arrives in time. The channel is left blocking.
   */
  byte[] read(Duration timeout) throws IOException {
    if (timeout.isZero()) {
      return read();
    }
    long deadline = System.nanoTime() + timeout.toNanos();
    channel.configureBlocking(false);
    try (Selector selector = Selector.open()) {
      channel.register(selector, SelectionKey.OP_READ);
      while (true) {
        byte[] message = take();
        if (message != null) {
          return message;
        }
        long remaining = (deadline - System.nanoTime()) / 1000000;
        if (remaining <= 0 || selector.select(remaining) == 0) {
          throw new SocketTimeoutException("Timed out waiting for data from the server");
        }
        selector.selectedKeys().clear();
        fill();
      }
    } finally {
      channel.configureBlocking(true);
    }
  }

  /**
   * Read the next message once the channel is non-blocking. Throws a SocketTimeoutException if a
   * message that has started to arrive receives no new byte within the stall timeout, unless it is
   * zero, and an InterruptedIOException if the thread is interrupted while waiting.
   */
  byte[] readNonBlocking(Duration stallTimeout) throws IOException {
    long stall = stallTimeout.toNanos();
    while (true) {
      byte[] message = take();
      if (message != null) {
        return message;
      }
      // Waiting first, as a read straight after a request finds nothing
      long timeout = MAXIMUM_WAIT_MILLISECONDS;
      // A stall timeout of zero waits indefinitely
      if (partial && stall > 0) {
        long remaining = stall - (System.nanoTime() - lastProgress);
        // Bytes already waiting are no stall, such as after a long pause of this thread
        if (remaining <= 0 && fill() == 0) {
          throw new SocketTimeoutException("A message from the server stalled");
        }
        if (remaining <= 0) {
          continue;
        }
        timeout = Math.min(timeout, Math.max(1, remaining / 1000000));
      }
      readSelector.select(IGNORE_KEY, timeout);
      if (Thread.currentThread().isInterrupted()) {
        throw new InterruptedIOException("Interrupted while waiting for data from the server");
      }
      fill();
    }
  }

  /**
   * Whether a whole message has been read and not handed out yet. True for a malformed length
   * prefix, so the next read throws.
   */
  boolean hasMessage() {
    byte[] array = buffer.array();
    int end = buffer.position();
    long size = 0;
    int prefix = 0;
    while (true) {
      if (start + prefix >= end) {
        return false;
      }
      byte b = array[start + prefix];
      size |= (long) (b & 0x7f) << (7 * prefix);
      prefix++;
      if ((b & 0x80) == 0) {
        return end - start - prefix >= size;
      }
      if (prefix == MAXIMUM_PREFIX_LENGTH) {
        return true;
      }
    }
  }

  /** Whether part of a message has been read. */
  boolean hasBuffered() {
    return buffer.position() > start;
  }

  /** Read what has arrived into the buffer, and return the number of bytes read. */
  private int fill() throws IOException {
    if (!buffer.hasRemaining()) {
      grow();
    }
    int read = channel.read(buffer);
    if (read < 0) {
      throw new EOFException("Connection closed by the server");
    }
    if (read > 0) {
      lastProgress = System.nanoTime();
    }
    return read;
  }

  /** Make room at the end of the buffer, by moving the unread bytes to its start or growing it. */
  private void grow() {
    int unread = buffer.position() - start;
    byte[] array = buffer.array();
    if (start > 0) {
      System.arraycopy(array, start, array, 0, unread);
      start = 0;
      buffer.position(unread);
      if (buffer.hasRemaining()) {
        return;
      }
    }
    ByteBuffer grown = ByteBuffer.allocate(array.length * 2);
    grown.put(array, 0, unread);
    buffer = grown;
  }

  /** The next message if it has arrived in full, or null. */
  private byte[] take() throws InvalidProtocolBufferException {
    byte[] array = buffer.array();
    int end = buffer.position();
    long size = 0;
    int prefix = 0;
    while (true) {
      if (start + prefix >= end) {
        partial = end > start;
        return null;
      }
      byte b = array[start + prefix];
      size |= (long) (b & 0x7f) << (7 * prefix);
      prefix++;
      if ((b & 0x80) == 0) {
        break;
      }
      if (prefix == MAXIMUM_PREFIX_LENGTH) {
        throw new InvalidProtocolBufferException("Message length prefix is malformed");
      }
    }
    if (size > Integer.MAX_VALUE - MAXIMUM_PREFIX_LENGTH) {
      throw new InvalidProtocolBufferException("Message is longer than can be read");
    }
    int total = prefix + (int) size;
    if (end - start < total) {
      partial = true;
      // Room for the whole message, so that the reads that fill it make progress
      if (buffer.capacity() - start < total) {
        ByteBuffer grown = ByteBuffer.allocate(Math.max(total, buffer.capacity() * 2));
        grown.put(array, start, end - start);
        buffer = grown;
        start = 0;
      }
      return null;
    }
    final byte[] message = Arrays.copyOfRange(array, start + prefix, start + total);
    start += total;
    if (start == end) {
      start = 0;
      buffer.clear();
    }
    partial = start != buffer.position();
    return message;
  }
}
