#pragma once

#include "krpc/services/krpc.hpp"
#include "krpc/stream.hpp"

namespace krpc {

/**
 * Create a stream from a server side function. On each update, the value of
 * the stream is the result of evaluating the function on the server.
 * The template type must correspond to the function's return type.
 */
template <typename T>
inline Stream<T> add_function_stream(const services::KRPC::Expression& function) {
  services::KRPC krpc_service(function._client);
  krpc::schema::Stream stream = krpc_service.add_function_stream(function, false);
  return Stream<T>(function._client, stream.id());
}

/**
 * Run a function on the server, within a single physics tick, and return the
 * value it produces. The template type must correspond to the function's
 * return type. A function whose value is null leaves the result default
 * constructed, which for a std::optional is an empty one.
 */
template <typename T>
inline T run_function(const services::KRPC::Expression& function) {
  services::KRPC krpc_service(function._client);
  auto data = krpc_service.run_function(function);
  // A null value is signaled out-of-band by is_null, and leaves the value default
  // constructed, as it does for a stream
  T result{};
  if (data) {
    // Unqualified, so that argument dependent lookup finds the overload for a type a
    // service defines at the point this template is used
    using decoder::decode;
    decode(result, *data, function._client);
  }
  return result;
}

/**
 * Run a function with no result on the server, within a single physics tick,
 * for its effects.
 */
inline void run_function(const services::KRPC::Expression& function) {
  services::KRPC krpc_service(function._client);
  krpc_service.run_function(function);
}

}  // namespace krpc
