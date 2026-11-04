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

/** The transport the connection was opened over, which the faulting transport wraps. */
krpc_transport_t wrapped;

/** Writes through the wrapped transport, unless a write fault is armed. */
krpc_error_t write_or_fault(uintptr_t handle, const uint8_t* buf, size_t count) {
  if (write_fault) {
    write_fault = false;
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
      std::string message = krpc_get_error_message();
      // A failed connect over a serial port leaves the line open for a retry. Every other
      // failed connect has closed the connection.
      if (error == KRPC_ERROR_CONNECTION_FAILED && result.transport->resumable) krpc_close(result);
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
  hello.mutable_hello()->add_protocols("tcpip");
  hello.mutable_hello()->add_protocols("localsocket");
  hello.mutable_hello()->add_protocols("serialio");
  krpc_conformance::write(hello);

  krpc_conformance::Testee testee;
  while (auto command = krpc_conformance::read_command()) testee.run(*command);
  testee.close();
  return 0;
}
