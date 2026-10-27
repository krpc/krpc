// The conformance testee for the C++ client. Reads commands from the conformance driver on
// standard input, runs them against the client, and writes the replies to standard output.
// See conformance/testee.proto for the protocol.

#include <chrono>
#include <cmath>
#include <condition_variable>
#include <cstdint>
#include <cstdio>
#include <deque>
#include <exception>
#include <functional>
#include <iostream>
#include <krpc.hpp>
#include <krpc/race_point.hpp>
#include <krpc/services/krpc.hpp>
#include <krpc/write_fault.hpp>
#include <limits>
#include <map>
#include <memory>
#include <mutex>
#include <optional>
#include <stdexcept>
#include <string>
#include <system_error>
#include <thread>
#include <typeinfo>
#include <utility>
#include <vector>

#include "dispatch_values.hpp"
#include "framing.hpp"
#include "services/conformance_service.hpp"
#include "services/test_service.hpp"
// The generated dispatch, which uses the stubs included above
#include "conformance_service_dispatch.hpp"
#include "krpc_dispatch.hpp"
#include "test_service_dispatch.hpp"
// The codec, which uses the dispatch's struct conversions
#include "codec.hpp"
#include "functions.hpp"

namespace krpc_conformance {

using krpc::conformance::Command;
using krpc::conformance::Error;
using krpc::conformance::Output;
using krpc::conformance::Reply;

/** Writes messages to the driver, each whole under a lock. */
class Writer {
 public:
  void write(const Output& message) {
    std::lock_guard<std::mutex> lock(mutex);
    write_line(message);
  }

 private:
  std::mutex mutex;
};

/** A named thread that runs the commands sent to it in order. */
class Worker {
 public:
  explicit Worker(std::function<void(const Command&)> run)
      : thread([this, run]() {
          while (true) {
            std::unique_lock<std::mutex> lock(mutex);
            ready.wait(lock, [this]() { return stopping || !queue.empty(); });
            if (queue.empty()) return;
            Command command = std::move(queue.front());
            queue.pop_front();
            lock.unlock();
            run(command);
          }
        }) {}

  void submit(Command command) {
    std::lock_guard<std::mutex> lock(mutex);
    queue.push_back(std::move(command));
    ready.notify_one();
  }

  void stop() {
    {
      std::lock_guard<std::mutex> lock(mutex);
      stopping = true;
      ready.notify_one();
    }
    thread.join();
  }

 private:
  std::mutex mutex;
  std::condition_variable ready;
  std::deque<Command> queue;
  bool stopping = false;
  std::thread thread;
};

/** The write fault armed for the client's next write, as the message of its error. */
class WriteFault {
 public:
  static void arm(const std::string& message) {
    std::lock_guard<std::mutex> guard(lock());
    armed() = message;
  }

  /** The hook the client calls before each write. */
  static void reach() {
    std::optional<std::string> message;
    {
      std::lock_guard<std::mutex> guard(lock());
      message.swap(armed());
    }
    if (message) throw std::system_error(std::make_error_code(std::errc::broken_pipe), *message);
  }

 private:
  static std::mutex& lock() {
    static std::mutex value;
    return value;
  }

  static std::optional<std::string>& armed() {
    static std::optional<std::string> value;
    return value;
  }
};

/** The pause armed at a race point of the client, and the thread paused there. */
class RacePause {
 public:
  static RacePause& instance() {
    static RacePause pause;
    return pause;
  }

  /** The hook the client calls at each race point. */
  static void reach(krpc::RacePoint point, int tag) {
    if (tag >= 0 && on_tag()) on_tag()(tag);
    instance().block(point);
  }

  /** Called with the tag of the callback this thread is adding, at a race point in the add, so
      the callback can be removed while the add is paused. */
  static std::function<void(int)>& on_tag() {
    thread_local std::function<void(int)> on_tag;
    return on_tag;
  }

  /** Sets on_tag for this thread while in scope. */
  class Adding {
   public:
    explicit Adding(std::function<void(int)> on_tag) { RacePause::on_tag() = std::move(on_tag); }
    ~Adding() { RacePause::on_tag() = nullptr; }
    Adding(const Adding&) = delete;
    Adding& operator=(const Adding&) = delete;
  };

  void arm(krpc::RacePoint point) {
    std::lock_guard<std::mutex> lock(mutex);
    if (armed || paused) throw std::logic_error("A pause is already armed");
    armed = point;
  }

  bool await(double timeout) {
    std::unique_lock<std::mutex> lock(mutex);
    return changed.wait_for(lock, std::chrono::duration<double>(timeout),
                            [this]() { return paused; });
  }

  void resume() {
    std::lock_guard<std::mutex> lock(mutex);
    armed.reset();
    if (paused) resumed = true;
    changed.notify_all();
  }

 private:
  void block(krpc::RacePoint point) {
    std::unique_lock<std::mutex> lock(mutex);
    if (armed != point) return;
    armed.reset();
    paused = true;
    changed.notify_all();
    changed.wait(lock, [this]() { return resumed; });
    paused = false;
    resumed = false;
  }

  std::mutex mutex;
  std::condition_variable changed;
  std::optional<krpc::RacePoint> armed;
  bool paused = false;
  bool resumed = false;
};

/** Throws Unsupported for a behavior the language has no way to carry out. */
void check_behavior(const krpc::conformance::Behavior& behavior) {
  if (behavior.has_fail_fatally())
    throw Unsupported("C++ has no kind of error apart from its ordinary exceptions");
}

krpc::RacePoint race_point(krpc::conformance::RacePoint point) {
  switch (point) {
    case krpc::conformance::STREAM_CALLBACKS:
      return krpc::RacePoint::stream_callbacks;
    case krpc::conformance::UPDATE_CALLBACKS:
      return krpc::RacePoint::update_callbacks;
    case krpc::conformance::FIRST_CALL:
      return krpc::RacePoint::first_call;
    case krpc::conformance::FIRST_CALL_STARTED:
      return krpc::RacePoint::first_call_started;
    case krpc::conformance::REPLAY:
      return krpc::RacePoint::replay;
    case krpc::conformance::STREAM_ENDING:
      return krpc::RacePoint::stream_ending;
    case krpc::conformance::STREAM_VALUE:
      return krpc::RacePoint::stream_value;
    case krpc::conformance::FIRST_CALL_ENDED:
      return krpc::RacePoint::first_call_ended;
    default:
      throw std::invalid_argument("Unknown race point");
  }
}

/** The standard and client error types an error is an instance of. */
template <typename T>
void add_native_type(const std::exception& exn, const char* name, Error* error) {
  if (dynamic_cast<const T*>(&exn) != nullptr) error->add_native_types(name);
}

Error to_error(const std::exception& exn) {
  Error error;
  error.set_message(exn.what());
  add_native_type<krpc::RPCError>(exn, "krpc::RPCError", &error);
  add_native_type<krpc::ConnectionError>(exn, "krpc::ConnectionError", &error);
  add_native_type<krpc::StreamError>(exn, "krpc::StreamError", &error);
  add_native_type<krpc::EncodingError>(exn, "krpc::EncodingError", &error);
  add_native_type<std::logic_error>(exn, "std::logic_error", &error);
  add_native_type<std::invalid_argument>(exn, "std::invalid_argument", &error);
  add_native_type<std::domain_error>(exn, "std::domain_error", &error);
  add_native_type<std::length_error>(exn, "std::length_error", &error);
  add_native_type<std::out_of_range>(exn, "std::out_of_range", &error);
  add_native_type<std::runtime_error>(exn, "std::runtime_error", &error);
  add_native_type<std::range_error>(exn, "std::range_error", &error);
  add_native_type<std::overflow_error>(exn, "std::overflow_error", &error);
  add_native_type<std::underflow_error>(exn, "std::underflow_error", &error);
  auto nested = dynamic_cast<const std::nested_exception*>(&exn);
  if (nested != nullptr && nested->nested_ptr() != nullptr) {
    try {
      nested->rethrow_nested();
    } catch (const std::exception& cause) {
      error.set_cause(cause.what());
    } catch (...) {
      error.set_cause("An error that is not a std::exception");
    }
  }
  if (dynamic_cast<const Unsupported*>(&exn) != nullptr) {
    error.set_kind(Error::UNSUPPORTED);
  } else if (auto connection = dynamic_cast<const krpc::ConnectionError*>(&exn)) {
    error.set_kind(Error::CONNECTION);
    error.set_status(connection->status());
    error.set_reason(connection->reason());
  } else if (dynamic_cast<const krpc::StreamError*>(&exn) != nullptr) {
    error.set_kind(Error::STREAM);
  } else if (dynamic_cast<const krpc::EncodingError*>(&exn) != nullptr) {
    error.set_kind(Error::ENCODING);
  } else if (dynamic_cast<const std::invalid_argument*>(&exn) != nullptr &&
             dynamic_cast<const krpc::RPCError*>(&exn) == nullptr) {
    // An argument the client rejects before it makes the call
    error.set_kind(Error::ARGUMENT);
  } else {
    error.set_kind(Error::RPC);
    if (!name_error_krpc(exn, &error)) name_error_test_service(exn, &error);
  }
  return error;
}

/** A timeout as the client takes it, where zero is no timeout. */
std::chrono::milliseconds to_timeout(double seconds) {
  if (std::isnan(seconds)) throw Unsupported("A std::chrono::milliseconds cannot hold NaN");
  if (std::isinf(seconds) && seconds > 0) return std::chrono::milliseconds::zero();
  return std::chrono::milliseconds(static_cast<int64_t>(seconds * 1000));
}

/** Runs the commands from the driver against one connection at a time. */
class Testee {
 public:
  explicit Testee(Writer* output) : output(output) {}

  void run(const Command& command) {
    Output message;
    *message.mutable_reply() = reply(command);
    output->write(message);
  }

  Reply reply(const Command& command) {
    Reply reply;
    reply.set_id(command.id());
    current_connection = command.connection();
    HeldClient held_client;
    reply.set_thread(thread_name());
    try {
      if (command.condition() != 0) {
        Holding holding(command.condition());
        dispatch(command, &reply);
      } else {
        dispatch(command, &reply);
      }
    } catch (const std::exception& exn) {
      *reply.mutable_error() = to_error(exn);
    }
    return reply;
  }

  void close() {
    std::lock_guard<std::mutex> lock(clients_mutex);
    clients.clear();
  }

 private:
  static std::string thread_name() {
    return std::to_string(std::hash<std::thread::id>()(std::this_thread::get_id()));
  }

  /** The releases of the conditions this thread holds, the last taken at the back. */
  /** The releases of the conditions the thread holds, each given the handle to release it
      through, or zero for the one that took it. */
  static std::vector<std::function<void(uint64_t)>>& held() {
    thread_local std::vector<std::function<void(uint64_t)>> releases;
    return releases;
  }

  /** Holds the condition of a stream or an event while in scope. */
  class Holding {
   public:
    explicit Holding(uint64_t handle) : event(Handles::instance().event(handle)) {
      if (event) {
        event->acquire();
      } else {
        stream = Handles::instance().stream(handle);
        stream->acquire();
      }
    }
    ~Holding() {
      if (event)
        event->release();
      else if (stream)
        stream->release();
    }
    /** Let go of the condition through another handle that shares it. */
    void release_through(uint64_t handle) {
      if (auto other = Handles::instance().event(handle))
        other->release();
      else
        Handles::instance().stream(handle)->release();
      event.reset();
      stream.reset();
    }
    Holding(const Holding&) = delete;
    Holding& operator=(const Holding&) = delete;

   private:
    std::shared_ptr<krpc::Event> event;
    std::shared_ptr<StreamHandle> stream;
  };

  /** Writes the notification for a call of a callback, then does what its behavior says. */
  void call_back(uint64_t callback, const krpc::conformance::Behavior& behavior,
                 const std::function<void(krpc::conformance::Notification*)>& fill) {
    Output message;
    auto notification = message.mutable_notification();
    notification->set_callback(callback);
    notification->set_thread(thread_name());
    fill(notification);
    output->write(message);
    uint32_t count;
    {
      std::lock_guard<std::mutex> lock(removals_mutex);
      count = ++calls[callback];
    }
    if (count <= behavior.skip() || (behavior.only() > 0 && count > behavior.only())) return;
    if (behavior.sleep() > 0)
      std::this_thread::sleep_for(
          std::chrono::milliseconds(static_cast<int64_t>(behavior.sleep() * 1000)));
    struct Returned {
      ~Returned() {
        if (!report) return;
        Output message;
        message.mutable_notification()->set_callback(callback);
        message.mutable_notification()->set_thread(thread_name());
        message.mutable_notification()->set_returned(true);
        writer->write(message);
      }
      bool report;
      uint64_t callback;
      Writer* writer;
    } returned{behavior.report_return(), callback, output};
    switch (behavior.action_case()) {
      case krpc::conformance::Behavior::kBlock:
        release(callback)->acquire();
        break;
      case krpc::conformance::Behavior::kRun: {
        Output result;
        result.mutable_notification()->set_callback(callback);
        result.mutable_notification()->set_thread(thread_name());
        *result.mutable_notification()->mutable_result() = reply(behavior.run());
        output->write(result);
        break;
      }
      case krpc::conformance::Behavior::kFail:
        throw std::runtime_error(behavior.fail());
      default:
        break;
    }
  }

  /** A count of releases, which a blocked callback takes one of. */
  class Release {
   public:
    void acquire() {
      std::unique_lock<std::mutex> lock(mutex);
      released.wait(lock, [this]() { return count > 0; });
      count--;
    }
    void release() {
      std::lock_guard<std::mutex> lock(mutex);
      count++;
      released.notify_one();
    }

   private:
    std::mutex mutex;
    std::condition_variable released;
    int count = 0;
  };

  std::shared_ptr<Release> release(uint64_t callback) {
    std::lock_guard<std::mutex> lock(removals_mutex);
    auto& release = releases[callback];
    if (!release) release = std::make_shared<Release>();
    return release;
  }

  void dispatch(const Command& command, Reply* reply) {
    switch (command.command_case()) {
      case Command::kConnect:
        connect(command.connect());
        *reply->mutable_value() = result_none();
        break;
      case Command::kClose:
        if (command.close().interrupted()) throw Unsupported("A C++ thread cannot be interrupted");
        // A client closes when its last copy is destroyed
        connected();
        held_clients.back().reset();
        destroy(current_connection);
        *reply->mutable_value() = result_none();
        break;
      case Command::kInvoke:
        if (command.invoke().has_through())
          throw Unsupported("A static method is called through its class");
        *reply->mutable_value() = invoke(command.invoke());
        break;
      case Command::kAddStream:
        if (command.add_stream().scoped()) throw Unsupported("A stream has no scope idiom");
        reply->set_stream(
            Handles::instance().add_stream(add_stream(command.add_stream().invoke())));
        break;
      case Command::kCall:
        *reply->mutable_value() = result_object(
            krpc::services::KRPC::Expression::call(connected(), call(command.call().invoke())));
        break;
      case Command::kAddFunctionStream: {
        const auto& add = command.add_function_stream();
        auto function = Object<functions::Expression>::arg(connected(), add.function());
        StreamHandle stream;
        if (!functions::with_type(add.type(), [&](auto tag) {
              stream = functions::add_stream<decltype(tag)>(function);
            }))
          throw Unsupported("A function of type " + add.type());
        reply->set_stream(Handles::instance().add_stream(stream));
        break;
      }
      case Command::kAddFunctionEvent: {
        auto function = Object<functions::Expression>::arg(connected(),
                                                           command.add_function_event().function());
        *reply->mutable_value() = EventHandle::result(krpc::add_event(function));
        break;
      }
      case Command::kRunFunction: {
        const auto& run = command.run_function();
        auto function = Object<functions::Expression>::arg(connected(), run.function());
        if (run.type().empty()) {
          *reply->mutable_value() = functions::run<void>(connected(), function, run);
        } else if (!functions::with_type(run.type(), [&](auto tag) {
                     *reply->mutable_value() =
                         functions::run<decltype(tag)>(connected(), function, run);
                   })) {
          throw Unsupported("A function of type " + run.type());
        }
        break;
      }
      case Command::kWaitOnCondition: {
        const auto& wait = command.wait_on_condition();
        bool woken;
        if (auto event = Handles::instance().event(wait.stream())) {
          auto duration = std::chrono::duration<double>(wait.timeout());
          woken = event->get_condition().wait_for(event->get_condition_lock(), duration) ==
                  std::cv_status::no_timeout;
        } else {
          woken = Handles::instance().stream(wait.stream())->wait_on_condition(wait.timeout());
        }
        *reply->mutable_value() = result_bool(woken);
        break;
      }
      case Command::kNotifyCondition: {
        auto handle = command.notify_condition().stream();
        if (auto event = Handles::instance().event(handle)) {
          event->acquire();
          event->get_condition().notify_all();
          event->release();
        } else {
          Handles::instance().stream(handle)->notify_condition();
        }
        *reply->mutable_value() = result_none();
        break;
      }
      case Command::kHashHandle:
        // A stream or an event object has no std::hash
        *reply->mutable_value() = result_null();
        break;
      case Command::kDropHandle:
        Handles::instance().drop(command.drop_handle().stream());
        *reply->mutable_value() = result_none();
        break;
      case Command::kCopyStream: {
        auto handle = command.copy_stream().stream();
        if (auto copied = Handles::instance().event(handle))
          reply->set_stream(Handles::instance().add_event(*copied));
        else
          reply->set_stream(
              Handles::instance().add_stream(Handles::instance().stream(handle)->copy()));
        break;
      }
      case Command::kCompareHandles: {
        auto a = command.compare_handles().a();
        auto b = command.compare_handles().b();
        bool equal;
        if (auto event_a = Handles::instance().event(a))
          equal = *event_a == *event(b);
        else
          equal = Handles::instance().stream(a)->equals(Handles::instance().stream(b)->value);
        *reply->mutable_value() = result_bool(equal);
        break;
      }
      case Command::kSetRate:
        Handles::instance()
            .stream(command.set_rate().stream())
            ->set_rate(command.set_rate().rate());
        *reply->mutable_value() = result_none();
        break;
      case Command::kRate:
        reply->mutable_value()->set_float_(
            Handles::instance().stream(command.rate().stream())->rate());
        break;
      case Command::kRemove:
        if (auto event = Handles::instance().event(command.remove().stream())) {
          event->remove();
        } else {
          Handles::instance().stream(command.remove().stream())->remove();
        }
        *reply->mutable_value() = result_none();
        break;
      case Command::kWaitEvent: {
        double timeout = std::numeric_limits<double>::infinity();
        if (command.wait_event().has_timeout()) timeout = command.wait_event().timeout();
        *reply->mutable_value() = result_bool(event(command.wait_event().event())->wait(timeout));
        break;
      }
      case Command::kAddEventCallback: {
        auto callback = command.add_event_callback().callback();
        auto event = this->event(command.add_event_callback().event());
        auto behavior = command.add_event_callback().behavior();
        check_behavior(behavior);
        // Held weakly, so that dropping the handle destroys it
        auto removal = [held = std::weak_ptr(event)](int tag) {
          return [held, tag]() {
            if (auto event = held.lock()) event->remove_callback(tag);
          };
        };
        RacePause::Adding adding(
            [this, callback, removal](int tag) { add_removal(callback, removal(tag)); });
        int tag = event->add_callback([this, callback, behavior]() {
          call_back(callback, behavior, [](krpc::conformance::Notification*) {});
        });
        add_removal(callback, removal(tag));
        *reply->mutable_value() = result_none();
        break;
      }
      case Command::kEventStream:
        reply->set_stream(Handles::instance().add_stream(
            result_stream(event(command.event_stream().event())->stream(), Bool::result)));
        break;
      case Command::kGet:
        *reply->mutable_value() = Handles::instance().stream(command.get().stream())->get();
        break;
      case Command::kWait: {
        double timeout = std::numeric_limits<double>::infinity();
        if (command.wait().has_timeout()) timeout = command.wait().timeout();
        *reply->mutable_value() =
            result_bool(Handles::instance().stream(command.wait().stream())->wait(timeout));
        break;
      }
      case Command::kAddCallback: {
        auto callback = command.add_callback().callback();
        auto stream = Handles::instance().stream(command.add_callback().stream());
        auto behavior = command.add_callback().behavior();
        check_behavior(behavior);
        // Held weakly, so that dropping the handle destroys it
        auto removal = [held = std::weak_ptr(stream)](int tag) {
          return [held, tag]() {
            if (auto stream = held.lock()) stream->remove_callback(tag);
          };
        };
        RacePause::Adding adding(
            [this, callback, removal](int tag) { add_removal(callback, removal(tag)); });
        int tag = stream->add_callback([this, callback, behavior](const Value& value) {
          call_back(callback, behavior, [&value](krpc::conformance::Notification* notification) {
            *notification->mutable_value() = value;
          });
        });
        add_removal(callback, removal(tag));
        *reply->mutable_value() = result_none();
        break;
      }
      case Command::kAddEndCallback: {
        auto callback = command.add_end_callback().callback();
        auto behavior = command.add_end_callback().behavior();
        check_behavior(behavior);
        auto notify = [this, callback, behavior](std::exception_ptr exn) {
          call_back(callback, behavior, [&exn](krpc::conformance::Notification* notification) {
            auto ending = notification->mutable_ending();
            if (exn) {
              try {
                std::rethrow_exception(exn);
              } catch (const std::exception& error) {
                *ending->mutable_error() = to_error(error);
              }
            }
          });
        };
        auto handle = command.add_end_callback().stream();
        if (auto event = Handles::instance().event(handle)) {
          int tag = event->add_end_callback(notify);
          add_removal(callback, [held = std::weak_ptr(event), tag]() {
            if (auto event = held.lock()) event->remove_end_callback(tag);
          });
        } else {
          auto stream = Handles::instance().stream(handle);
          int tag = stream->add_end_callback(notify);
          add_removal(callback, [held = std::weak_ptr(stream), tag]() {
            if (auto stream = held.lock()) stream->remove_end_callback(tag);
          });
        }
        *reply->mutable_value() = result_none();
        break;
      }
      case Command::kAddUpdateCallback: {
        auto callback = command.add_update_callback().callback();
        auto& client = connected();
        auto behavior = command.add_update_callback().behavior();
        int tag = client.add_stream_update_callback([this, callback, behavior]() {
          call_back(callback, behavior, [](krpc::conformance::Notification*) {});
        });
        add_removal(callback, [&client, tag]() { client.remove_stream_update_callback(tag); });
        *reply->mutable_value() = result_none();
        break;
      }
      case Command::kRelease:
        release(command.release().callback())->release();
        *reply->mutable_value() = result_none();
        break;
      case Command::kHoldCondition: {
        auto handle = command.hold_condition().stream();
        if (handle == 0) {
          krpc::Client client = connected();
          client.acquire_stream_update();
          held().push_back([client](uint64_t) mutable { client.release_stream_update(); });
        } else {
          auto holding = std::make_shared<Holding>(handle);
          held().push_back([holding](uint64_t through) mutable {
            if (through != 0) holding->release_through(through);
            holding.reset();
          });
        }
        *reply->mutable_value() = result_none();
        break;
      }
      case Command::kReleaseCondition: {
        auto release_held = held().back();
        held().pop_back();
        release_held(command.release_condition().stream());
        *reply->mutable_value() = result_none();
        break;
      }
      case Command::kWaitForUpdate: {
        krpc::Client client = connected();
        const auto& wait = command.wait_for_update();
        client.acquire_stream_update();
        try {
          client.wait_for_stream_update(wait.has_timeout() ? wait.timeout() : -1);
        } catch (...) {
          client.release_stream_update();
          throw;
        }
        client.release_stream_update();
        *reply->mutable_value() = result_none();
        break;
      }
      case Command::kInterrupt:
        throw Unsupported("A C++ thread cannot be interrupted");
      case Command::kRemoveCallback:
        remove(command.remove_callback().callback());
        *reply->mutable_value() = result_none();
        break;
      case Command::kRemoveEndCallback:
        remove(command.remove_end_callback().callback());
        *reply->mutable_value() = result_none();
        break;
      case Command::kRemoveUpdateCallback:
        remove(command.remove_update_callback().callback());
        *reply->mutable_value() = result_none();
        break;
      case Command::kCompare: {
        const auto& a = command.compare().a();
        const auto& b = command.compare().b();
        bool equal;
        if (a.value_case() == Value::kStruct && b.value_case() == Value::kStruct) {
          equal = TestService_TestStruct::arg(connected(), a) ==
                  TestService_TestStruct::arg(connected(), b);
        } else if (a.value_case() == Value::kEnum && b.value_case() == Value::kEnum) {
          using TestEnum = Enum<krpc::services::TestService::TestEnum>;
          equal = TestEnum::arg(connected(), a) == TestEnum::arg(connected(), b);
        } else if (a.value_case() != Value::kObject || b.value_case() != Value::kObject) {
          equal = a.value_case() == b.value_case();
        } else {
          auto held_a = Handles::instance().object(a.object().handle());
          auto held_b = Handles::instance().object(b.object().handle());
          equal = held_a.equals(held_b.value);
        }
        *reply->mutable_value() = result_bool(equal);
        break;
      }
      case Command::kEncode:
        reply->mutable_value()->set_bytes(
            codec(command.encode().type()).encode(connected(), command.encode().value()));
        break;
      case Command::kDecode:
        *reply->mutable_value() =
            codec(command.decode().type()).decode(connected(), command.decode().data());
        break;
      case Command::kMembers:
        // The stubs are compiled from the service definitions
        reply->mutable_value()->set_null(true);
        break;
      case Command::kOrder: {
        using TestClass = krpc::services::TestService::TestClass;
        const auto& a = command.order().a();
        const auto& b = command.order().b();
        if (a.value_case() == Value::kStruct)
          reply->mutable_value()->set_sint32(order(TestService_TestStruct::arg(connected(), a),
                                                   TestService_TestStruct::arg(connected(), b)));
        else
          reply->mutable_value()->set_sint32(order(Object<TestClass>::arg(connected(), a),
                                                   Object<TestClass>::arg(connected(), b)));
        break;
      }
      case Command::kHash: {
        const auto& a = command.hash().a();
        if (a.value_case() == Value::kStruct)
          reply->mutable_value()->set_uint64(std::hash<krpc::services::TestService::TestStruct>()(
              TestService_TestStruct::arg(connected(), a)));
        else
          reply->mutable_value()->set_uint64(
              Handles::instance().object(a.object().handle()).hash());
        break;
      }
      case Command::kObjectId: {
        using TestClass = krpc::services::TestService::TestClass;
        auto obj = Object<TestClass>::arg(connected(), command.object_id().a());
        reply->mutable_value()->set_uint64(krpc::get_object_id(obj));
        break;
      }
      case Command::kPause:
        RacePause::instance().arm(race_point(command.pause().point()));
        *reply->mutable_value() = result_none();
        break;
      case Command::kAwaitPause:
        *reply->mutable_value() =
            result_bool(RacePause::instance().await(command.await_pause().timeout()));
        break;
      case Command::kResume:
        RacePause::instance().resume();
        *reply->mutable_value() = result_none();
        break;
      case Command::kFailWrite:
        WriteFault::arm(command.fail_write().message());
        *reply->mutable_value() = result_none();
        break;
      default:
        throw std::invalid_argument("Unknown command");
    }
  }

  /** The sign of the order of two values by <, once each comparison operator agrees with it. */
  template <typename T>
  static int32_t order(const T& a, const T& b) {
    int32_t sign = a < b ? -1 : (b < a ? 1 : 0);
    if ((a > b) != (sign > 0) || (a <= b) != (sign <= 0) || (a >= b) != (sign >= 0) ||
        (a == b) != (sign == 0) || (a != b) != (sign != 0))
      throw std::runtime_error("The comparison operators disagree on the order");
    return sign;
  }

  static std::shared_ptr<krpc::Event> event(uint64_t handle) {
    auto event = Handles::instance().event(handle);
    if (!event) throw std::invalid_argument("Not an event handle");
    return event;
  }

  void add_removal(uint64_t callback, std::function<void()> removal) {
    std::lock_guard<std::mutex> lock(removals_mutex);
    removals[callback] = std::move(removal);
  }

  void remove(uint64_t callback) {
    std::function<void()> removal;
    {
      std::lock_guard<std::mutex> lock(removals_mutex);
      removal = removals.at(callback);
      removals.erase(callback);
    }
    removal();
  }

  /** Destroys the client of a connection, which closes it. */
  void destroy(const std::string& name) {
    std::shared_ptr<krpc::Client> client;
    {
      std::lock_guard<std::mutex> lock(clients_mutex);
      auto entry = clients.find(name);
      if (entry == clients.end()) return;
      client = std::move(entry->second);
      clients.erase(entry);
    }
  }

  void connect(const krpc::conformance::Connect& command) {
    auto name = current_connection;
    if (name.empty()) {
      {
        std::lock_guard<std::mutex> lock(removals_mutex);
        removals.clear();
        releases.clear();
      }
      std::map<std::string, std::shared_ptr<krpc::Client>> closed;
      {
        std::lock_guard<std::mutex> lock(clients_mutex);
        closed.swap(clients);
      }
      closed.clear();
      Handles::instance().clear();
    } else {
      destroy(name);
    }
    std::shared_ptr<krpc::Client> client;
    std::chrono::milliseconds handshake_timeout = std::chrono::seconds(5);
    if (command.has_handshake_timeout())
      handshake_timeout = to_timeout(command.handshake_timeout());
    std::chrono::milliseconds stall_timeout = std::chrono::seconds(30);
    if (command.has_stall_timeout()) stall_timeout = to_timeout(command.stall_timeout());
    if (command.protocol() == "localsocket") {
      client = std::make_shared<krpc::Client>(
          krpc::connect_local(command.name(), command.path(), handshake_timeout, stall_timeout));
    } else if (command.protocol() == "tcpip") {
      std::string address = command.address().empty() ? "127.0.0.1" : command.address();
      unsigned int port = command.port() > 0 ? command.port() : 50000;
      client = std::make_shared<krpc::Client>(krpc::connect(command.name(), address, port,
                                                            std::chrono::milliseconds::zero(),
                                                            handshake_timeout, stall_timeout));
    } else {
      throw std::invalid_argument("Unknown protocol " + command.protocol());
    }
    std::lock_guard<std::mutex> lock(clients_mutex);
    clients[name] = std::move(client);
  }

  /** The client of the command this thread runs. The command holds it from its first use, as a
      program making a call holds a copy, so a close on another thread leaves the client open
      until the command returns. */
  krpc::Client& connected() {
    auto& held = held_clients.back();
    if (!held) {
      std::lock_guard<std::mutex> lock(clients_mutex);
      auto entry = clients.find(current_connection);
      if (entry == clients.end() || !entry->second) throw std::invalid_argument("Not connected");
      held = entry->second;
    }
    return *held;
  }

  Value invoke(const krpc::conformance::Invoke& command) {
    if (command.service() == "KRPC")
      return invoke_krpc(connected(), command.procedure(), command.arguments());
    if (command.service() == "TestService")
      return invoke_test_service(connected(), command.procedure(), command.arguments());
    if (command.service() == "ConformanceService")
      return invoke_conformance_service(connected(), command.procedure(), command.arguments());
    throw std::invalid_argument("The testee cannot call service " + command.service());
  }

  StreamHandle add_stream(const krpc::conformance::Invoke& command) {
    if (command.service() == "KRPC")
      return add_stream_krpc(connected(), command.procedure(), command.arguments());
    if (command.service() == "TestService")
      return add_stream_test_service(connected(), command.procedure(), command.arguments());
    if (command.service() == "ConformanceService")
      return add_stream_conformance_service(connected(), command.procedure(), command.arguments());
    throw std::invalid_argument("The testee cannot stream service " + command.service());
  }

  krpc::schema::ProcedureCall call(const krpc::conformance::Invoke& command) {
    if (command.service() == "KRPC")
      return call_krpc(connected(), command.procedure(), command.arguments());
    if (command.service() == "TestService")
      return call_test_service(connected(), command.procedure(), command.arguments());
    if (command.service() == "ConformanceService")
      return call_conformance_service(connected(), command.procedure(), command.arguments());
    throw std::invalid_argument("The testee cannot build a call of service " + command.service());
  }

  Writer* output;
  // The clients, by the driver's name for each connection
  std::mutex clients_mutex;
  std::map<std::string, std::shared_ptr<krpc::Client>> clients;
  // The name of the connection the command this thread runs is for
  static thread_local std::string current_connection;
  // The client each command this thread runs holds, innermost last, as a callback can run a
  // command inside another
  static thread_local std::vector<std::shared_ptr<krpc::Client>> held_clients;
  struct HeldClient {
    HeldClient() { held_clients.emplace_back(); }
    ~HeldClient() { held_clients.pop_back(); }
  };
  // The callbacks the driver added, by its id, each with the function that removes it
  std::mutex removals_mutex;
  std::map<uint64_t, std::function<void()>> removals;
  // The releases of blocked callbacks, by callback id, guarded by removals_mutex
  std::map<uint64_t, std::shared_ptr<Release>> releases;
  // The number of calls of each callback, by callback id, guarded by removals_mutex
  std::map<uint64_t, uint32_t> calls;
};

thread_local std::string Testee::current_connection;
thread_local std::vector<std::shared_ptr<krpc::Client>> Testee::held_clients;

}  // namespace krpc_conformance

int main() {
  using krpc_conformance::Worker;
  krpc::set_race_point_hook(krpc_conformance::RacePause::reach);
  krpc::set_write_fault_hook(krpc_conformance::WriteFault::reach);
  krpc_conformance::Writer output;
  krpc::conformance::Output hello;
  hello.mutable_hello()->set_client("cpp");
  for (const char* profile :
       {"core", "error-types", "defaults", "streams", "events", "functions", "function-types",
        "connections", "threads", "callbacks", "conditions", "ordering", "races"})
    hello.mutable_hello()->add_profiles(profile);
  for (const char* protocol : {"tcpip", "localsocket"})
    hello.mutable_hello()->add_protocols(protocol);
  output.write(hello);

  krpc_conformance::Testee testee(&output);
  std::map<std::string, std::unique_ptr<Worker>> workers;
  while (auto command = krpc_conformance::read_command()) {
    auto& worker = workers[command->thread()];
    if (!worker)
      worker = std::make_unique<Worker>(
          [&testee](const krpc::conformance::Command& x) { testee.run(x); });
    worker->submit(std::move(*command));
  }
  for (auto& worker : workers) worker.second->stop();
  testee.close();
  return 0;
}
