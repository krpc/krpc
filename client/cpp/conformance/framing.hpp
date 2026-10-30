// The framing of the testee protocol: each message is its JSON form on a line of its own.
#pragma once

#include <google/protobuf/util/json_util.h>

#include <iostream>
#include <optional>
#include <stdexcept>
#include <string>

#include "testee.pb.h"

namespace krpc_conformance {

/** Writes a message to standard output, as a line. */
inline void write_line(const google::protobuf::Message& message) {
  std::string line;
  if (!google::protobuf::util::MessageToJsonString(message, &line).ok())
    throw std::runtime_error("Failed to encode an output");
  std::cout << line << '\n';
  std::cout.flush();
}

/** The next command from standard input, or none at the end of the input. */
inline std::optional<krpc::conformance::Command> read_command() {
  std::string line;
  if (!std::getline(std::cin, line)) return std::nullopt;
  // A carriage return before the newline, written in text mode
  if (!line.empty() && line.back() == '\r') line.pop_back();
  krpc::conformance::Command command;
  if (!google::protobuf::util::JsonStringToMessage(line, &command).ok())
    throw std::runtime_error("Failed to decode a command");
  return command;
}

}  // namespace krpc_conformance
