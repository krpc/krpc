#pragma once

#include <krpc_cnano/services/krpc.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Compile a server side function built with the expression API, so that its first run does not
 * compile it.
 * @param connection A connection object.
 * @param function The function to compile.
 */
static inline krpc_error_t krpc_compile_function(krpc_connection_t connection,
                                                 krpc_KRPC_Expression_t function) {
  krpc_call_t _call;
  krpc_argument_t _arguments[1];
  // KRPC.CompileFunction, which the KRPC stubs leave out as it is internal. A procedure's id is
  // the first four bytes of the SHA-256 of its name, little-endian.
  KRPC_CHECK(krpc_call(&_call, 1, 1025785238u, 1, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &function));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

/**
 * Run a server side function within a single physics tick. Each argument is encoded at the type
 * of its parameter with the krpc_encode_ functions, and the value is decoded at the function's
 * type with the krpc_decode_ functions.
 * @param connection A connection object.
 * @param result The encoded value of the function. No bytes for a function with no value. Pass
 * it empty, as a null value or a shorter value leaves bytes it already holds in place.
 * @param isNull Set to whether the value is null.
 * @param function The function to run.
 * @param arguments The encoded arguments, one per parameter, or NULL for none.
 */
static inline krpc_error_t krpc_run_function(krpc_connection_t connection, krpc_bytes_t* result,
                                             bool* isNull, krpc_KRPC_Expression_t function,
                                             const krpc_list_nullable_bytes_t* arguments) {
  krpc_list_nullable_bytes_t _none = KRPC_NULL_LIST;
  krpc_call_t _call;
  krpc_argument_t _arguments[2];
  // KRPC.RunFunction, with its id worked out as for KRPC.CompileFunction
  KRPC_CHECK(krpc_call(&_call, 1, 3583116652u, 2, _arguments));
  KRPC_CHECK(krpc_add_argument(&_call, 0, &krpc_encode_callback_object, &function));
  KRPC_CHECK(krpc_add_argument(&_call, 1, &krpc_encode_callback_list_nullable_bytes,
                               arguments ? arguments : &_none));
  krpc_result_t _result = KRPC_RESULT_INIT_DEFAULT;
  KRPC_CHECK(krpc_init_result(&_result));
  KRPC_CHECK(krpc_invoke(connection, &_result.message, &_call.message));
  if (isNull) *isNull = _result.message.is_null;
  if (result && !_result.message.is_null) {
    pb_istream_t _stream;
    KRPC_CHECK(krpc_get_return_value(&_result, &_stream));
    KRPC_CHECK(krpc_decode_bytes(&_stream, result));
  }
  KRPC_CHECK(krpc_free_result(&_result));
  return KRPC_OK;
}

#ifdef __cplusplus
}  // extern "C"
#endif
