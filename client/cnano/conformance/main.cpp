// The conformance testee for the C nano client. Reads commands from the conformance driver on
// standard input, runs them against the client in order, and writes the replies to standard
// output. See conformance/testee.proto for the protocol.

#include <krpc_cnano.h>
#include <krpc_cnano/functions.h>
#include <krpc_cnano/services/krpc.h>

#include <cstdint>
#include <cstdio>
#include <iostream>
#include <map>
#include <optional>
#include <stdexcept>
#include <string>
#include <vector>

#include "deprecation_signals.hpp"
#include "dispatch_values.hpp"
#include "framing.hpp"
#include "services/conformance_service.h"
#include "services/test_service.h"
// The generated dispatch, which uses the stubs included above
#include "conformance_service_dispatch.hpp"
#include "krpc_dispatch.hpp"
#include "test_service_dispatch.hpp"
// The codec, which uses the dispatch's struct and tuple conversions
#include "codec.hpp"

namespace krpc_conformance {

/** Whether a write fault is armed for the client's next write. */
bool write_fault = false;

/** The message of the armed write fault. */
std::string write_fault_message;

/** The transport the connection was opened over, which the faulting transport wraps. */
krpc_transport_t wrapped;

/** Writes through the wrapped transport, unless a write fault is armed. */
krpc_error_t write_or_fault(uintptr_t handle, const uint8_t* buf, size_t count) {
  if (write_fault) {
    write_fault = false;
    krpc_set_error_cause(write_fault_message.c_str());
    return KRPC_ERROR_IO;
  }
  return wrapped.write(handle, buf, count);
}

/** The wrapped transport, with writes that fail when a fault is armed. */
krpc_transport_t faulting;

/** Wraps an open connection's transport in one whose writes can fault. */
krpc_connection_t with_write_faults(krpc_connection_t connection) {
  wrapped = *connection.transport;
  faulting = wrapped;
  faulting.write = &write_or_fault;
  connection.transport = &faulting;
  return connection;
}

using krpc::conformance::Command;
using krpc::conformance::Error;
using krpc::conformance::Output;
using krpc::conformance::Reply;

void write(const Output& message) { write_line(message); }

/** The service and name of an exception, and the macro of its ids. */
struct ExceptionName {
  std::string service;
  std::string name;
  std::string macro;
};

#define KRPC_CONFORMANCE_EXCEPTION(service, name, macro)           \
  {                                                                \
    std::make_pair(macro), ExceptionName { service, name, #macro } \
  }

/** The exceptions of the services the testee calls, by their ids. */
const std::map<std::pair<uint32_t, uint32_t>, ExceptionName> kExceptions = {
    KRPC_CONFORMANCE_EXCEPTION("KRPC", "InvalidOperationException",
                               KRPC_EXCEPTION_KRPC_INVALIDOPERATIONEXCEPTION),
    KRPC_CONFORMANCE_EXCEPTION("KRPC", "ArgumentException", KRPC_EXCEPTION_KRPC_ARGUMENTEXCEPTION),
    KRPC_CONFORMANCE_EXCEPTION("KRPC", "ArgumentNullException",
                               KRPC_EXCEPTION_KRPC_ARGUMENTNULLEXCEPTION),
    KRPC_CONFORMANCE_EXCEPTION("KRPC", "ArgumentOutOfRangeException",
                               KRPC_EXCEPTION_KRPC_ARGUMENTOUTOFRANGEEXCEPTION),
    KRPC_CONFORMANCE_EXCEPTION("KRPC", "NullReferenceException",
                               KRPC_EXCEPTION_KRPC_NULLREFERENCEEXCEPTION),
    KRPC_CONFORMANCE_EXCEPTION("KRPC", "KeyNotFoundException",
                               KRPC_EXCEPTION_KRPC_KEYNOTFOUNDEXCEPTION),
    KRPC_CONFORMANCE_EXCEPTION("KRPC", "IndexOutOfRangeException",
                               KRPC_EXCEPTION_KRPC_INDEXOUTOFRANGEEXCEPTION),
    KRPC_CONFORMANCE_EXCEPTION("KRPC", "DivideByZeroException",
                               KRPC_EXCEPTION_KRPC_DIVIDEBYZEROEXCEPTION),
    KRPC_CONFORMANCE_EXCEPTION("KRPC", "NotSupportedException",
                               KRPC_EXCEPTION_KRPC_NOTSUPPORTEDEXCEPTION),
    KRPC_CONFORMANCE_EXCEPTION("KRPC", "TimeoutException", KRPC_EXCEPTION_KRPC_TIMEOUTEXCEPTION),
    KRPC_CONFORMANCE_EXCEPTION("KRPC", "OverflowException", KRPC_EXCEPTION_KRPC_OVERFLOWEXCEPTION),
    KRPC_CONFORMANCE_EXCEPTION("KRPC", "ObjectDestroyedException",
                               KRPC_EXCEPTION_KRPC_OBJECTDESTROYEDEXCEPTION),
    KRPC_CONFORMANCE_EXCEPTION("KRPC", "ObjectNotLoadedException",
                               KRPC_EXCEPTION_KRPC_OBJECTNOTLOADEDEXCEPTION),
    KRPC_CONFORMANCE_EXCEPTION("KRPC", "UnsupportedSceneException",
                               KRPC_EXCEPTION_KRPC_UNSUPPORTEDSCENEEXCEPTION),
    KRPC_CONFORMANCE_EXCEPTION("KRPC", "StreamRemovedException",
                               KRPC_EXCEPTION_KRPC_STREAMREMOVEDEXCEPTION),
    KRPC_CONFORMANCE_EXCEPTION("TestService", "CustomException",
                               KRPC_EXCEPTION_TESTSERVICE_CUSTOMEXCEPTION),
};

/** The name of an error code, the testee's native type for an error with no exception. */
std::string code_name(krpc_error_t code) {
  switch (code) {
    case KRPC_ERROR_CONNECTION:
      return "KRPC_ERROR_CONNECTION";
    case KRPC_ERROR_CONNECTION_REFUSED:
      return "KRPC_ERROR_CONNECTION_REFUSED";
    case KRPC_ERROR_SERVER:
      return "KRPC_ERROR_SERVER";
    case KRPC_ERROR_ENCODING:
      return "KRPC_ERROR_ENCODING";
    case KRPC_ERROR_ARGUMENT:
      return "KRPC_ERROR_ARGUMENT";
    case KRPC_ERROR_NO_MEMORY:
      return "KRPC_ERROR_NO_MEMORY";
    case KRPC_ERROR_BUFFER_TOO_SMALL:
      return "KRPC_ERROR_BUFFER_TOO_SMALL";
    case KRPC_ERROR_IO:
      return "KRPC_ERROR_IO";
    case KRPC_ERROR_EOF:
      return "KRPC_ERROR_EOF";
    default:
      return std::to_string(static_cast<int>(code));
  }
}

/** The name of a ConnectionResponse status, or its number for one the protocol lacks. */
std::string status_name(int16_t status) {
  switch (status) {
    case krpc_schema_ConnectionResponse_Status_OK:
      return "OK";
    case krpc_schema_ConnectionResponse_Status_MALFORMED_MESSAGE:
      return "MALFORMED_MESSAGE";
    case krpc_schema_ConnectionResponse_Status_TIMEOUT:
      return "TIMEOUT";
    case krpc_schema_ConnectionResponse_Status_WRONG_PROTOCOL_VERSION:
      return "WRONG_PROTOCOL_VERSION";
    case krpc_schema_ConnectionResponse_Status_DENIED:
      return "DENIED";
    default:
      return std::to_string(status);
  }
}

/** The name of a Disconnect reason, or its number for one the protocol lacks. */
std::string reason_name(int16_t reason) {
  switch (reason) {
    case krpc_schema_Disconnect_Reason_CLOSED:
      return "CLOSED";
    case krpc_schema_Disconnect_Reason_SERVER_STOPPED:
      return "SERVER_STOPPED";
    case krpc_schema_Disconnect_Reason_REMOVED:
      return "REMOVED";
    case krpc_schema_Disconnect_Reason_MALFORMED_MESSAGE:
      return "MALFORMED_MESSAGE";
    case krpc_schema_Disconnect_Reason_PROTOCOL_ERROR:
      return "PROTOCOL_ERROR";
    case krpc_schema_Disconnect_Reason_TIMEOUT:
      return "TIMEOUT";
    default:
      return std::to_string(reason);
  }
}

/** The kind of error a code is, and the details the client recorded for it. */
void classify(const ClientError& client_error, Error* error) {
  const auto& info = client_error.info;
  switch (client_error.code) {
    case KRPC_ERROR_CONNECTION:
    case KRPC_ERROR_CONNECTION_REFUSED:
    case KRPC_ERROR_IO:
    case KRPC_ERROR_EOF:
      error->set_kind(Error::CONNECTION);
      break;
    case KRPC_ERROR_SERVER:
      error->set_kind(Error::SERVER);
      break;
    case KRPC_ERROR_ENCODING:
      error->set_kind(Error::ENCODING);
      break;
    case KRPC_ERROR_ARGUMENT:
    case KRPC_ERROR_BUFFER_TOO_SMALL:
      error->set_kind(Error::ARGUMENT);
      break;
    default:
      error->set_kind(Error::KIND_UNSPECIFIED);
      break;
  }
  auto exception = kExceptions.find(std::make_pair(info.service, info.exception));
  if (client_error.code == KRPC_ERROR_SERVER && exception != kExceptions.end()) {
    error->set_service(exception->second.service);
    error->set_name(exception->second.name);
    error->add_native_types(exception->second.macro);
  } else {
    error->add_native_types(code_name(client_error.code));
  }
  if (info.status != -1) error->set_status(status_name(info.status));
  if (info.reason != -1) error->set_reason(reason_name(info.reason));
}

Error to_error(const std::exception& exn) {
  Error error;
  error.set_message(exn.what());
  error.set_kind(Error::SERVER);
  if (dynamic_cast<const Unsupported*>(&exn) != nullptr) error.set_kind(Error::UNSUPPORTED);
  if (auto client_error = dynamic_cast<const ClientError*>(&exn)) classify(*client_error, &error);
  return error;
}

/** Runs the commands from the driver against one connection at a time. A closed connection
    keeps its handle, so later calls go to it as they would in a program. */
class Testee {
 public:
  void run(const Command& command) {
    Reply reply;
    reply.set_id(command.id());
    try {
      dispatch(command, &reply);
    } catch (const std::exception& exn) {
      *reply.mutable_error() = to_error(exn);
    }
    Output message;
    *message.mutable_reply() = std::move(reply);
    write(message);
  }

  void close() {
    if (connection && open) krpc_disconnect(*connection);
  }

 private:
  void dispatch(const Command& command, Reply* reply) {
    switch (command.command_case()) {
      case Command::kConnect:
        connect(command.connect());
        *reply->mutable_value() = result_none();
        break;
      case Command::kClose:
        open = false;
        check(krpc_disconnect(connected()));
        *reply->mutable_value() = result_none();
        break;
      case Command::kInvoke: {
        const auto& invoke = command.invoke();
        if (invoke.has_through()) throw Unsupported("A static method is called through its class");
        if (invoke.service() == "KRPC") {
          *reply->mutable_value() =
              invoke_krpc(connected(), invoke.procedure(), invoke.arguments());
        } else if (invoke.service() == "TestService") {
          *reply->mutable_value() =
              invoke_test_service(connected(), invoke.procedure(), invoke.arguments());
        } else if (invoke.service() == "ConformanceService") {
          *reply->mutable_value() =
              invoke_conformance_service(connected(), invoke.procedure(), invoke.arguments());
        } else {
          throw std::invalid_argument("The testee cannot call service " + invoke.service());
        }
        break;
      }
      case Command::kCall:
        *reply->mutable_value() = call_function(command.call().invoke(), nullptr);
        break;
      case Command::kCallWithArguments:
        *reply->mutable_value() = call_function(command.call_with_arguments().invoke(),
                                                &command.call_with_arguments().arguments());
        break;
      case Command::kCompileFunction:
        check(
            krpc_compile_function(connected(), arg_object(command.compile_function().function())));
        *reply->mutable_value() = result_none();
        break;
      case Command::kRunFunction:
        *reply->mutable_value() = run_function(command.run_function());
        break;
      case Command::kEncode:
        reply->mutable_value()->set_bytes(
            codec(command.encode().type()).encode(command.encode().value()));
        break;
      case Command::kDecode:
        *reply->mutable_value() = codec(command.decode().type()).decode(command.decode().data());
        break;
      // The stubs are compiled from the service definitions
      case Command::kMembers:
      // C has no hash
      case Command::kHash:
        reply->mutable_value()->set_null(true);
        break;
      case Command::kDeprecationSignals: {
        const auto& signals = command.deprecation_signals();
        auto member = signals.service() + "." + signals.member();
        if (member.find("Exception") != std::string::npos)
          throw Unsupported("An exception has no C type");
        if (signals.member().empty()) throw Unsupported("A service has no C type");
        if (!signals.use().empty()) member += ":" + signals.use();
        auto* list = reply->mutable_value()->mutable_list();
        for (const auto& signal : kDeprecationSignals.at(member))
          list->add_items()->set_string(signal);
        break;
      }
      case Command::kObjectId:
        // An object is its id, so there is no helper to get it
        reply->mutable_value()->set_uint64(arg_object(command.object_id().a()));
        break;
      case Command::kOrder:
        throw Unsupported("An object is its id, so the client has no ordering of its own");
      case Command::kCompare: {
        if (command.compare().a().value_case() == krpc::conformance::Value::kStruct)
          throw Unsupported("C has no equality of structures");
        if (command.compare().a().value_case() == krpc::conformance::Value::kEnum) {
          krpc_enum_t a = command.compare().a().enum_().value();
          krpc_enum_t b = command.compare().b().enum_().value();
          *reply->mutable_value() = result_bool(a == b);
          break;
        }
        auto a = arg_object(command.compare().a());
        auto b = arg_object(command.compare().b());
        *reply->mutable_value() = result_bool(a == b);
        break;
      }
      case Command::kFlagsUnion: {
        using TestFlags = Enum<krpc_TestService_TestFlags_t>;
        auto a = TestFlags::arg(command.flags_union().a());
        auto b = TestFlags::arg(command.flags_union().b());
        *reply->mutable_value() =
            TestFlags::result(static_cast<krpc_TestService_TestFlags_t>(a | b));
        break;
      }
      case Command::kFlagsContains: {
        using TestFlags = Enum<krpc_TestService_TestFlags_t>;
        auto a = TestFlags::arg(command.flags_contains().a());
        auto b = TestFlags::arg(command.flags_contains().b());
        *reply->mutable_value() = result_bool((a & b) == b);
        break;
      }
      case Command::kFlagsDifference: {
        using TestFlags = Enum<krpc_TestService_TestFlags_t>;
        auto a = TestFlags::arg(command.flags_difference().a());
        auto b = TestFlags::arg(command.flags_difference().b());
        *reply->mutable_value() = TestFlags::result(static_cast<krpc_TestService_TestFlags_t>(
            static_cast<uint32_t>(a) & ~static_cast<uint32_t>(b)));
        break;
      }
      case Command::kFailWrite:
        write_fault = true;
        write_fault_message = command.fail_write().message();
        *reply->mutable_value() = result_none();
        break;
      default:
        throw std::invalid_argument("Unknown command");
    }
  }

  /** Fill in the call an invoke command makes. */
  static void fill_call(krpc_schema_ProcedureCall* call, const krpc::conformance::Invoke& invoke) {
    if (invoke.service() == "KRPC") {
      call_krpc(call, invoke.procedure(), invoke.arguments());
    } else if (invoke.service() == "TestService") {
      call_test_service(call, invoke.procedure(), invoke.arguments());
    } else if (invoke.service() == "ConformanceService") {
      call_conformance_service(call, invoke.procedure(), invoke.arguments());
    } else {
      throw std::invalid_argument("The testee cannot call service " + invoke.service());
    }
  }

  /** A server side function that makes the call an invoke command makes, with the arguments at
      the given positions computed by functions. */
  Value call_function(const krpc::conformance::Invoke& invoke,
                      const google::protobuf::Map<uint32_t, Value>* arguments) {
    krpc_schema_ProcedureCall call = krpc_schema_ProcedureCall_init_default;
    krpc_KRPC_Expression_t function = KRPC_NULL;
    try {
      fill_call(&call, invoke);
      if (arguments == nullptr) {
        check(krpc_KRPC_Expression_Call(connected(), &function, &call));
      } else {
        std::vector<krpc_dictionary_entry_int32_object_t> entries;
        for (const auto& [position, value] : *arguments)
          entries.push_back({static_cast<int32_t>(position), arg_object(value)});
        krpc_dictionary_int32_object_t computed{entries.size(), entries.data()};
        check(krpc_KRPC_Expression_CallWithArguments(connected(), &function, &call, &computed));
      }
    } catch (...) {
      krpc_free_procedure_call(&call);
      throw;
    }
    krpc_free_procedure_call(&call);
    return Object::result(function);
  }

  /** Run a server side function, encoding the arguments and decoding the value with the codec
      of each type. The value is in a slot, so a nullable type decodes as the type it holds. */
  Value run_function(const krpc::conformance::RunFunction& command) {
    auto slot_type = [](const std::string& type) {
      const std::string prefix = "nullable ";
      return type.rfind(prefix, 0) == 0 ? type.substr(prefix.size()) : type;
    };
    std::vector<std::string> encoded(command.arguments_size());
    std::vector<krpc_nullable_bytes_t> items(command.arguments_size());
    for (int i = 0; i < command.arguments_size(); i++) {
      if (command.arguments(i).value_case() == Value::kNull) continue;
      encoded[i] = codec(slot_type(command.argument_types(i))).encode(command.arguments(i));
      items[i].value = {encoded[i].size(), reinterpret_cast<uint8_t*>(encoded[i].data())};
      items[i].has_value = true;
    }
    krpc_list_nullable_bytes_t arguments{items.size(), items.data()};
    krpc_bytes_t data = KRPC_BYTES_NULL;
    bool is_null = false;
    check(krpc_run_function(connected(), &data, &is_null, arg_object(command.function()),
                            &arguments));
    std::string value(reinterpret_cast<const char*>(data.data), data.size);
    KRPC_FREE_BYTES(data);
    if (command.type().empty()) return result_null();
    if (is_null && command.type().rfind("nullable ", 0) == 0) return result_null();
    // A null value has no bytes, which fail to decode at a type that cannot hold null
    return codec(slot_type(command.type())).decode(value);
  }

  void connect(const krpc::conformance::Connect& command) {
    close();
    connection.reset();
    open = false;
    Handles::instance().clear();
    krpc_connection_t result;
    if (command.protocol() == "tcpip") {
      std::string address = command.address().empty() ? "127.0.0.1" : command.address();
      uint16_t port = command.port() > 0 ? static_cast<uint16_t>(command.port()) : 50000;
      check(krpc_open_tcp(&result, address.c_str(), port));
    } else if (command.protocol() == "localsocket") {
      if (command.path().empty())
        throw Unsupported("The client has no default path, so the caller always passes one");
      check(krpc_open_localsocket(&result, command.path().c_str()));
    } else if (command.protocol() == "serialio") {
      check(krpc_open_serial(&result, command.path().c_str()));
    } else {
      throw std::invalid_argument("Unknown protocol " + command.protocol());
    }
    result = with_write_faults(result);
    krpc_error_t error = krpc_connect(result, command.name().c_str());
    if (error != KRPC_OK) {
      ClientError client_error(error);
      // A failed handshake over a serial port leaves the line open for a retry. A refusal, and
      // every failed connect over another transport, has closed the connection.
      if (result.transport->resumable && error != KRPC_ERROR_CONNECTION_REFUSED) krpc_close(result);
      throw client_error;
    }
    connection = result;
    open = true;
  }

  krpc_connection_t connected() {
    if (!connection) throw std::invalid_argument("Not connected");
    return *connection;
  }

  std::optional<krpc_connection_t> connection;
  bool open = false;
};

}  // namespace krpc_conformance

int main() {
  krpc::conformance::Output hello;
  hello.mutable_hello()->set_client("cnano");
  hello.mutable_hello()->add_profiles("core");
  hello.mutable_hello()->add_profiles("error-types");
  hello.mutable_hello()->add_profiles("functions");
  hello.mutable_hello()->add_protocols("tcpip");
  hello.mutable_hello()->add_protocols("localsocket");
  hello.mutable_hello()->add_protocols("serialio");
  krpc_conformance::write(hello);

  krpc_conformance::Testee testee;
  while (auto command = krpc_conformance::read_command()) testee.run(*command);
  testee.close();
  return 0;
}
