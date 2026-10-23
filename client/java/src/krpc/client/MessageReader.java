package krpc.client;

import com.google.protobuf.InvalidProtocolBufferException;
import java.io.EOFException;
import java.io.IOException;
import java.io.InterruptedIOException;
import java.net.SocketTimeoutException;
import java.nio.ByteBuffer;
import java.nio.channels.SelectionKey;
import java.nio.channels.Selector;
import java.nio.channels.SocketChannel;
import java.time.Duration;
import java.util.Arrays;
import java.util.function.Consumer;

/** Reads length prefixed messages from a socket channel. */
class MessageReader {
  // A length prefix is a varint, and one that does not end within five bytes cannot be the
  // uint32 it is meant to be
  private static final int MAXIMUM_PREFIX_LENGTH = 5;
  // The longest wait for data before checking the channel is still open, as a channel closed
  // without closing its selector does not wake the selector
  static final long MAXIMUM_WAIT_MILLISECONDS = 1000;
  // A selector with one channel needs nothing from the key that is ready
  static final Consumer<SelectionKey> IGNORE_KEY = key -> { };

  private final SocketChannel channel;
  // The bytes read and not yet handed out are those from start to the buffer's position
  private ByteBuffer buffer = ByteBuffer.allocate(64 * 1024);
  private int start = 0;
  // When a byte last arrived, and whether part of a message is waiting for the rest
  private long lastProgress = System.nanoTime();
  private boolean partial = false;

  MessageReader(SocketChannel channel) {
    this.channel = channel;
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
   * Read the next message from a channel in non-blocking mode, waiting for data with a selector
   * the channel is registered with for reading. Throws a SocketTimeoutException if a message that
   * has started to arrive receives no new byte within the stall timeout, and an
   * InterruptedIOException if the thread is interrupted while waiting.
   */
  byte[] read(Selector selector, Duration stallTimeout) throws IOException {
    long stall = stallTimeout.toNanos();
    while (true) {
      byte[] message = take();
      if (message != null) {
        return message;
      }
      // Waiting first, as a read straight after a request finds nothing
      long timeout = MAXIMUM_WAIT_MILLISECONDS;
      if (partial) {
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
      selector.select(IGNORE_KEY, timeout);
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
