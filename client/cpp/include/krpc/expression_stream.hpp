#pragma once

#include "krpc/services/krpc.hpp"
#include "krpc/stream.hpp"

namespace krpc {

/**
 * Create a stream from a server side expression. On each update, the value of
 * the stream is the result of evaluating the expression on the server.
 * The template type must correspond to the expression's return type.
 */
template <typename T>
inline Stream<T> add_expression_stream(const services::KRPC::Expression& expression) {
  services::KRPC krpc_service(expression._client);
  krpc::schema::Stream stream = krpc_service.add_expression_stream(expression, false);
  return Stream<T>(expression._client, stream.id());
}

/**
 * Run a function on the server, within a single physics tick, and return the
 * value it produces. The template type must correspond to the expression's
 * return type.
 */
template <typename T>
inline T run_function(const services::KRPC::Expression& expression) {
  services::KRPC krpc_service(expression._client);
  T result;
  using decoder::decode;
  decode(result, krpc_service.run_function(expression), expression._client);
  return result;
}

/**
 * Run a function with no result on the server, within a single physics tick,
 * for its effects.
 */
inline void run_function(const services::KRPC::Expression& expression) {
  services::KRPC krpc_service(expression._client);
  krpc_service.run_function(expression);
}

}  // namespace krpc
