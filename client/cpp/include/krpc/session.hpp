#pragma once

#include <atomic>
#include <chrono>              // NOLINT(build/c++11)
#include <condition_variable>  // NOLINT(build/c++11)
#include <cstdint>
#include <deque>
#include <exception>
#include <functional>
#include <map>
#include <memory>
#include <mutex>  // NOLINT(build/c++11)
#include <string>
#include <thread>  // NOLINT(build/c++11)
#include <utility>
#include <vector>

#include "krpc/krpc.pb.hpp"

namespace krpc {

class Connection;
class StreamManager;

/** The messages of one connection.

    One thread at a time reads the socket, the reader. A caller with no reader takes the
    role and reads until its response arrives, so a call costs no hand-off between threads.
    While streams are open, the reader thread reads when no call is waiting for a response, so
    their updates arrive between calls. It waits for data without being the reader, so a caller
    can take over at once. The reader never runs user code, and takes no lock user code can
    hold.

    The dispatcher thread runs callbacks, wakes stream waiters, and makes the writes the reader
    needs, in the order the reader queues them. */
class Session {
 public:
  /** A message that has started to arrive may go this long without a new byte. */
  static constexpr std::chrono::milliseconds stall_timeout = std::chrono::seconds(30);

  /** What each call of a request returns: a new stream, a new event, or another value. The
      reader registers the stream a call adds. */
  enum class Adds : uint8_t { nothing, stream, event };

  Session(std::shared_ptr<Connection> connection,
          std::function<schema::Request(uint64_t)> remove_stream_request);
  ~Session();
  Session(const Session&) = delete;
  Session& operator=(const Session&) = delete;

  /** Start the threads. The dispatcher thread holds keep_alive until it exits, as a callback
      can still be running once the last client is released. */
  void start(StreamManager* manager, std::shared_ptr<void> keep_alive);
  /** Queue work for the dispatcher thread. */
  void dispatch(std::function<void()> work);
  /** Whether this thread is the dispatcher thread, on which callbacks run. */
  bool on_dispatcher_thread() const;
  /** Send a request and wait for its response. Throws the connection error if the connection
      has failed or fails before the response arrives. */
  schema::Response call(schema::Request request, std::vector<Adds> adds);
  /** Send a request to remove a stream on the server, whose response is discarded. On the reader
      thread, the dispatcher thread sends it. */
  void remove_stream(uint64_t id);
  /** Wake the reader thread for a stream added outside a call. */
  void stream_added();
  /** Tell the server the connection is closing, then close it. Waits for the server to close
      its side, for up to a second, discarding what arrives meanwhile. Skips telling the server
      if the write lock is held for longer than that, such as by a caller blocked on a full send
      buffer. */
  void close();
  /** The error that failed the connection before it closed, or null if it had not failed. The
      streams end with this as their ending's cause. */
  std::exception_ptr failure();
  /** Stop the threads, and wait for them except the one this runs on. */
  void join(bool join_dispatcher);

 private:
  /** A call waiting for its response. Changed under the state lock. */
  struct Slot {
    std::vector<Adds> adds;
    // Whether a call adds a stream or an event, which the reader registers
    bool adds_streams = false;
    bool abandoned = false;
    std::atomic_bool done{false};
    schema::Response response;
    std::exception_ptr error;
  };

  /** Thrown once the connection has ended, to stop the thread reading it. */
  struct Ended : std::exception {};

  void send(schema::Request* request, const std::shared_ptr<Slot>& slot);
  void write(const schema::Message& message);
  void write_request(schema::Request* request);
  void fail(const std::exception_ptr& error);
  void fail_locked(const std::exception_ptr& error);
  void returned();
  bool read_until(Slot* slot, bool returning);
  void await(Slot* slot);
  void stop_reading();
  bool streams_open() const;
  void run_reader();
  void run_dispatcher();
  void receive(schema::Message* message);
  std::pair<const char*, size_t> receive_data();
  void parse(const char* data, size_t size, google::protobuf::MessageLite* message);
  void handle(schema::Message* message);
  void handle_response(schema::Response* response);
  [[noreturn]] void end(std::exception_ptr cause);
  void drain(std::chrono::steady_clock::time_point deadline);
  std::exception_ptr protocol_error(schema::Disconnect::Reason reason, const std::string& text);
  void disconnect(schema::Disconnect::Reason reason, const std::string& text);

  std::shared_ptr<Connection> connection;
  std::function<schema::Request(uint64_t)> remove_stream_request;
  StreamManager* manager = nullptr;
  // Held for the whole of a write
  std::timed_mutex write_lock;
  // The bytes of the request being written, kept to save allocating them for every call. Guarded
  // by the write lock.
  std::string send_buffer;
  // Guards the ids, the pending calls, the error, and which thread is reading
  std::mutex state_lock;
  // Notified when a call completes or the socket is free to read, for a caller waiting for one
  // of those
  std::condition_variable changed;
  // Notified when the last waiting call returns while streams are open, for the reader thread
  std::condition_variable idle;
  uint64_t next_id = 1;
  std::map<uint64_t, std::shared_ptr<Slot>> pending;
  std::exception_ptr error;
  // The error that failed the connection before it closed
  std::exception_ptr failure_error;
  bool closing = false;
  bool ended = false;
  // Whether a thread is reading the socket, the number of calls waiting for their response, and
  // the number of those waiting for another thread to read it
  bool reading = false;
  int callers = 0;
  int waiting = 0;
  bool half_closed = false;
  std::condition_variable half_closed_condition;
  // The dispatcher's work, and whether it has been told to stop
  std::mutex queue_lock;
  std::condition_variable queue_condition;
  std::deque<std::function<void()>> queue;
  bool queue_stopped = false;
  // Work dispatched once the queue has stopped, destroyed with the session. It can hold the last
  // copy of a callback, and so of the client.
  std::vector<std::function<void()>> discarded;
  std::thread reader;
  std::thread dispatcher;
};

}  // namespace krpc
