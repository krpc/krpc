#pragma once

#include <cstdint>
#include <optional>
#include <string>
#include <vector>

#include "krpc/event.hpp"
#include "krpc/krpc.pb.hpp"
#include "krpc/services/krpc.hpp"

namespace krpc {

class Client;

/** Calls the internal procedures of the KRPC service. */
namespace internal_procedures {

uint64_t add_stream(Client* client, const schema::ProcedureCall& call);
void set_stream_rate(Client* client, uint64_t id, float rate);
void remove_stream(Client* client, uint64_t id);
uint64_t add_function_stream(const services::KRPC::Expression& function);
std::optional<std::string> run_function(
    const services::KRPC::Expression& function,
    const std::vector<std::optional<std::string>>& arguments = {});
Event add_event(const services::KRPC::Expression& function);

}  // namespace internal_procedures

}  // namespace krpc
