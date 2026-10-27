// The conformance testee for the C nano client. Reads commands from the conformance driver on
// standard input, runs them against the client in order, and writes the replies to standard
// output. See conformance/testee.proto for the protocol.

#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>

#include <cstdint>
#include <cstdio>
#include <iostream>
#include <optional>
#include <stdexcept>
#include <string>

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

/** The hook the client calls before each write. */
krpc_error_t reach_write_fault() {
  if (!write_fault) return KRPC_OK;
  write_fault = false;
  return KRPC_ERROR_IO;
}

using krpc::conformance::Command;
using krpc::conformance::Error;
using krpc::conformance::Output;
using krpc::conformance::Reply;

void write(const Output& message) { write_line(message); }

Error to_error(const std::exception& exn) {
  Error error;
  error.set_message(exn.what());
  error.set_kind(Error::RPC);
  if (dynamic_cast<const Unsupported*>(&exn) != nullptr) error.set_kind(Error::UNSUPPORTED);
  if (auto client_error = dynamic_cast<const ClientError*>(&exn)) {
    switch (client_error->code) {
      case KRPC_ERROR_IO:
      case KRPC_ERROR_EOF:
      case KRPC_ERROR_CONNECTION_FAILED:
      case KRPC_ERROR_CONNECTION_REFUSED:
        error.set_kind(Error::CONNECTION);
        break;
      case KRPC_ERROR_ENCODING_FAILED:
      case KRPC_ERROR_DECODING_FAILED:
        error.set_kind(Error::ENCODING);
        break;
      default:
        break;
    }
  }
  return error;
}

// The protocol the library is built for
#if defined(KRPC_COMMUNICATION_TCP)
const char* const PROTOCOL = "tcpip";
#elif defined(KRPC_COMMUNICATION_LOCALSOCKET)
const char* const PROTOCOL = "localsocket";
#else
const char* const PROTOCOL = "serialio";
#endif

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
    *message.mutable_reply() = reply;
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
      case Command::kEncode:
        reply->mutable_value()->set_bytes(
            codec(command.encode().type()).encode(command.encode().value()));
        break;
      case Command::kDecode:
        *reply->mutable_value() = codec(command.decode().type()).decode(command.decode().data());
        break;
      case Command::kMembers:
        // The stubs are compiled from the service definitions
        reply->mutable_value()->set_null(true);
        break;
      case Command::kHash:
        // C has no hash
        reply->mutable_value()->set_null(true);
        break;
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
      case Command::kFailWrite:
        // The client's errors carry no message, so the fault's message goes unused
        write_fault = true;
        *reply->mutable_value() = result_none();
        break;
      default:
        throw std::invalid_argument("Unknown command");
    }
  }

  void connect(const krpc::conformance::Connect& command) {
    close();
    connection.reset();
    open = false;
    Handles::instance().clear();
    if (command.protocol() != PROTOCOL)
      throw std::invalid_argument("Unknown protocol " + command.protocol());
    krpc_connection_t result;
#ifdef KRPC_COMMUNICATION_TCP
    std::string address = command.address().empty() ? "127.0.0.1" : command.address();
    krpc_connection_config_t config;
    config.address = address.c_str();
    config.port = command.port() > 0 ? static_cast<uint16_t>(command.port()) : 50000;
    check(krpc_open(&result, &config));
#else
#ifdef KRPC_COMMUNICATION_LOCALSOCKET
    if (command.path().empty())
      throw Unsupported("The client has no default path, so the caller always passes one");
#endif
    check(krpc_open(&result, command.path().c_str()));
#endif
    krpc_error_t error = krpc_connect(result, command.name().c_str());
    if (error != KRPC_OK) {
      std::string message = krpc_get_error_message();
#if !defined(KRPC_COMMUNICATION_TCP) && !defined(KRPC_COMMUNICATION_LOCALSOCKET)
      // A failed connect over a serial port leaves the line open for a retry. Every other
      // failed connect has closed the connection.
      if (error == KRPC_ERROR_CONNECTION_FAILED) krpc_close(result);
#endif
      throw ClientError(error, message);
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
  hello.mutable_hello()->add_protocols(krpc_conformance::PROTOCOL);
  krpc_conformance::write(hello);
  krpc_write_fault_hook = krpc_conformance::reach_write_fault;

  krpc_conformance::Testee testee;
  while (auto command = krpc_conformance::read_command()) testee.run(*command);
  testee.close();
  return 0;
}
