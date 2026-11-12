#pragma once

#include <cstdint>
#include <optional>
#include <string>
#include <vector>

#include "krpc/event.hpp"
#include "krpc/krpc.pb.hpp"
#include "krpc/services/krpc.hpp"

namespace krpc {

class Connection;

/** Calls the internal procedures of the KRPC service. */
namespace internal_procedures {

uint64_t add_stream(Connection* connection, const schema::ProcedureCall& call);
void set_stream_rate(Connection* connection, uint64_t id, float rate);
void remove_stream(Connection* connection, uint64_t id);
uint64_t add_function_stream(const services::KRPC::Expression& function);
void compile_function(const services::KRPC::Expression& function);
std::optional<std::string> run_function(
    const services::KRPC::Expression& function,
    const std::vector<std::optional<std::string>>& arguments = {});
Event add_event(const services::KRPC::Expression& function);

}  // namespace internal_procedures

}  // namespace krpc
