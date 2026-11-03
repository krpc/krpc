#include "krpc/internal_procedures.hpp"

#include <cstdint>
#include <optional>
#include <string>
#include <vector>

#include "krpc/client.hpp"
#include "krpc/decoder.hpp"
#include "krpc/encoder.hpp"
#include "krpc/error.hpp"

namespace krpc {
namespace internal_procedures {

namespace {

template <typename T>
T decode_result(Client* client, const schema::ProcedureResult& result) {
  T value{};
  if (!result.is_null()) decoder::decode(value, result.value(), client);
  return value;
}

// A stream id or an event, which cannot be null
template <typename T>
T decode_non_null_result(Client* client, const schema::ProcedureResult& result) {
  if (result.is_null()) throw EncodingError("A null result for a value that cannot be null");
  T value{};
  decoder::decode(value, result.value(), client);
  return value;
}

}  // namespace

uint64_t add_stream(Client* client, const schema::ProcedureCall& call) {
  auto result =
      client->invoke("KRPC", "AddStream", {encoder::encode_arg(call)}, Client::Returns::stream);
  return decode_non_null_result<uint64_t>(client, result);
}

void set_stream_rate(Client* client, uint64_t id, float rate) {
  client->invoke("KRPC", "SetStreamRate", {encoder::encode_arg(id), encoder::encode_arg(rate)},
                 Client::Returns::value);
}

void remove_stream(Client* client, uint64_t id) {
  client->invoke("KRPC", "RemoveStream", {encoder::encode_arg(id)}, Client::Returns::value);
}

uint64_t add_function_stream(const services::KRPC::Expression& function) {
  Client* client = function._client;
  auto result = client->invoke("KRPC", "AddFunctionStream", {encoder::encode_arg(function)},
                               Client::Returns::stream);
  return decode_non_null_result<uint64_t>(client, result);
}

std::optional<std::string> run_function(const services::KRPC::Expression& function,
                                        const std::vector<std::optional<std::string>>& arguments) {
  Client* client = function._client;
  auto result = client->invoke("KRPC", "RunFunction",
                               {encoder::encode_arg(function), encoder::encode_arg(arguments)},
                               Client::Returns::value);
  return decode_result<std::optional<std::string>>(client, result);
}

Event add_event(const services::KRPC::Expression& function) {
  Client* client = function._client;
  auto result =
      client->invoke("KRPC", "AddEvent", {encoder::encode_arg(function)}, Client::Returns::stream);
  return decode_non_null_result<Event>(client, result);
}

}  // namespace internal_procedures
}  // namespace krpc
