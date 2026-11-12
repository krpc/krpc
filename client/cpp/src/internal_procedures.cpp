#include "krpc/internal_procedures.hpp"

#include <cstdint>
#include <optional>
#include <string>
#include <vector>

#include "krpc/connection.hpp"
#include "krpc/decoder.hpp"
#include "krpc/encoder.hpp"
#include "krpc/error.hpp"

namespace krpc {
namespace internal_procedures {

namespace {

template <typename T>
T decode_result(Connection* connection, const schema::ProcedureResult& result) {
  T value{};
  if (!result.is_null()) decoder::decode(value, result.value(), connection);
  return value;
}

// A stream id or an event, which cannot be null
template <typename T>
T decode_non_null_result(Connection* connection, const schema::ProcedureResult& result) {
  if (result.is_null()) throw EncodingError("A null result for a value that cannot be null");
  T value{};
  decoder::decode(value, result.value(), connection);
  return value;
}

}  // namespace

uint64_t add_stream(Connection* connection, const schema::ProcedureCall& call) {
  auto result = connection->invoke("KRPC", "AddStream", {encoder::encode_arg(call)},
                                   Connection::Returns::stream);
  return decode_non_null_result<uint64_t>(connection, result);
}

void set_stream_rate(Connection* connection, uint64_t id, float rate) {
  connection->invoke("KRPC", "SetStreamRate", {encoder::encode_arg(id), encoder::encode_arg(rate)},
                     Connection::Returns::value);
}

void remove_stream(Connection* connection, uint64_t id) {
  connection->invoke("KRPC", "RemoveStream", {encoder::encode_arg(id)}, Connection::Returns::value);
}

uint64_t add_function_stream(const services::KRPC::Expression& function) {
  Connection* connection = &function._connection;
  auto result = connection->invoke("KRPC", "AddFunctionStream", {encoder::encode_arg(function)},
                                   Connection::Returns::stream);
  return decode_non_null_result<uint64_t>(connection, result);
}

void compile_function(const services::KRPC::Expression& function) {
  function._connection.invoke("KRPC", "CompileFunction", {encoder::encode_arg(function)},
                              Connection::Returns::value);
}

std::optional<std::string> run_function(const services::KRPC::Expression& function,
                                        const std::vector<std::optional<std::string>>& arguments) {
  Connection* connection = &function._connection;
  auto result = connection->invoke("KRPC", "RunFunction",
                                   {encoder::encode_arg(function), encoder::encode_arg(arguments)},
                                   Connection::Returns::value);
  return decode_result<std::optional<std::string>>(connection, result);
}

Event add_event(const services::KRPC::Expression& function) {
  Connection* connection = &function._connection;
  auto result = connection->invoke("KRPC", "AddEvent", {encoder::encode_arg(function)},
                                   Connection::Returns::stream);
  return decode_non_null_result<Event>(connection, result);
}

}  // namespace internal_procedures
}  // namespace krpc
