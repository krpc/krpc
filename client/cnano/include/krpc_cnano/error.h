#pragma once

#include <stdio.h>

#ifdef __cplusplus
extern "C" {
#endif

/* Error codes */
typedef enum {
  KRPC_OK = 0,
  KRPC_ERROR_IO = -1,
  KRPC_ERROR_EOF = -2,
  KRPC_ERROR_CONNECTION_FAILED = -3,
  KRPC_ERROR_NO_RESULTS = -4,
  KRPC_ERROR_RPC_FAILED = -5,
  KRPC_ERROR_ENCODING_FAILED = -6,
  KRPC_ERROR_DECODING_FAILED = -7,
  KRPC_ERROR_BUFFER_TOO_SMALL = -8
} krpc_error_t;

/* Convert an error code to a string */
const char * krpc_get_error(krpc_error_t error);

/* Print an error message when it occurs */
#if !defined(KRPC_PRINT_ERROR)
#ifdef KRPC_PRINT_ERRORS_TO_STDERR
#define KRPC_PRINT_ERROR(...) fprintf(stderr, __VA_ARGS__)
#else
#define KRPC_PRINT_ERROR(...)
#endif
#endif

/* Check that a function did not return an error.
 * If KRPC_ERROR_CHECK_ASSERT is defined, fails an assert when an error occurs.
 * If KRPC_ERROR_CHECK_EXIT is defined, calls exit when an error occurs.
 * If KRPC_ERROR_CHECK_FN is defined, calls an error handler when an error occurs.
 * If none of these are defined, returns the error.
 */
#ifndef KRPC_CHECK
#if defined(KRPC_ERROR_CHECK_ASSERT)
#define KRPC_CHECK(x) {     \
  krpc_error_t error = (x); \
  if (error != KRPC_OK) {                                        \
    KRPC_PRINT_ERROR("kRPC error: %s\n", krpc_get_error(error)); \
    assert(error == KRPC_OK);                                    \
  }                                                              \
}
#elif defined(KRPC_ERROR_CHECK_EXIT)
#define KRPC_CHECK(x) {                                          \
  krpc_error_t error = (x);                                      \
  if (error != KRPC_OK) {                                        \
    KRPC_PRINT_ERROR("kRPC error: %s\n", krpc_get_error(error)); \
    exit((int)error);                                            \
  }                                                              \
}
#elif defined(KRPC_ERROR_CHECK_FN)
extern void (*krpc_error_handler)(krpc_error_t);
#define KRPC_CHECK(x) {                                          \
  krpc_error_t error = (x);                                      \
  if (error != KRPC_OK) {                                        \
    KRPC_PRINT_ERROR("kRPC error: %s\n", krpc_get_error(error)); \
    (*(krpc_error_handler))(error);                              \
  }                                                              \
}
#else  // KRPC_ERROR_CHECK_RETURN
#define KRPC_CHECK(x) {     \
  krpc_error_t error = (x); \
  if (error != KRPC_OK) {                                        \
    KRPC_PRINT_ERROR("kRPC error: %s\n", krpc_get_error(error)); \
    return error;                                                \
  }                                                              \
}
#endif
#endif  // KRPC_CHECK

/* Print an error message and return an error code */
#define KRPC_RETURN_ERROR(error, msg) {      \
  KRPC_PRINT_ERROR("kRPC error: %s\n", msg); \
  return (KRPC_ERROR_##error);               \
}

/* Print an error message, along with the error message from a stream, and return an error code */
#define KRPC_RETURN_STREAM_ERROR(error, msg, stream) {                \
  KRPC_PRINT_ERROR("kRPC error: %s (%s)\n", msg, (stream)->errmsg);   \
  return (KRPC_ERROR_##error);                                        \
}

/* Runs the code x and returns an error if it fails. */
#define KRPC_RETURN_ON_ERROR(x) { \
  krpc_error_t error;             \
  if ((error = (x)) != KRPC_OK)   \
    return error;                 \
}

/* Return an error from a nanopb callback function */
#define KRPC_CALLBACK_RETURN_ERROR(msg) {                  \
  KRPC_PRINT_ERROR("kRPC error: %s (in callback)\n", msg); \
  return false;                                            \
}

/* Return a stream error from a nanopb callback function */
#define KRPC_CALLBACK_RETURN_STREAM_ERROR(msg, stream) {                         \
  KRPC_PRINT_ERROR("kRPC error: %s, %s (in callback)\n", msg, (stream)->errmsg); \
  return false;                                                                  \
}

/* Return an error from a nanopb callback function if the given code returns an error */
#define KRPC_CALLBACK_RETURN_ON_ERROR(x) { \
  if ((x) != KRPC_OK)                      \
    return false;                          \
}

#ifdef __cplusplus
}  // extern "C"
#endif
