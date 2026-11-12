#pragma once

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>
#include <stdio.h>
#ifdef __AVR__
#include <avr/pgmspace.h>
#endif

#ifdef __cplusplus
extern "C" {
#endif

/* Error codes. A program handles each code other than KRPC_OK differently. -1 is no code, and
 * marks a field of the last error that does not apply. */
typedef enum {
  KRPC_OK = 0,
  /* Connecting failed, or the connection failed or ended */
  KRPC_ERROR_CONNECTION = -2,
  /* The server refused the connection, as it denied it or does not support the client's
     protocol version */
  KRPC_ERROR_CONNECTION_REFUSED = -3,
  /* The server returned an error */
  KRPC_ERROR_SERVER = -4,
  /* An argument or a result failed to encode or decode */
  KRPC_ERROR_ENCODING = -5,
  /* An argument the library rejects before it sends anything */
  KRPC_ERROR_ARGUMENT = -6,
  /* An allocation failed */
  KRPC_ERROR_NO_MEMORY = -7,
  /* Bytes longer than the buffer the program passed */
  KRPC_ERROR_BUFFER_TOO_SMALL = -8,
  /* A transport's read or write failed. A library function returns KRPC_ERROR_CONNECTION for
     it, with this code as the cause. */
  KRPC_ERROR_IO = -9,
  /* A transport's connection ended. A library function returns KRPC_ERROR_CONNECTION for it,
     with this code as the cause. */
  KRPC_ERROR_EOF = -10
} krpc_error_t;

/* The name of an error code */
const char* krpc_get_error(krpc_error_t error);

/* How much the library keeps about the last error, read when the library compiles:
 *
 *  - KRPC_ERROR_CODES_ONLY keeps only the code a function returns.
 *  - The default keeps the last error's details, from krpc_get_error_info.
 *  - KRPC_ERROR_MESSAGES also keeps its message, from krpc_get_error_message. This costs
 *    KRPC_ERROR_MESSAGE_LENGTH bytes of static storage.
 */
#if defined(KRPC_ERROR_CODES_ONLY) && defined(KRPC_ERROR_MESSAGES)
#error "Define at most one of KRPC_ERROR_CODES_ONLY and KRPC_ERROR_MESSAGES"
#endif

#ifndef KRPC_ERROR_CODES_ONLY

/* The details of the last error. A failure fills them in, and a success leaves them as they
 * were. */
typedef struct {
  /* The code the failing function returned */
  krpc_error_t code;
  /* KRPC_ERROR_IO or KRPC_ERROR_EOF behind a connection error, else -1 */
  krpc_error_t cause;
  /* The id of the service of the server's exception, else 0 */
  uint32_t service;
  /* The id of the exception within its service, else 0 */
  uint32_t exception;
  /* The ConnectionResponse status of a connection response other than OK, else -1 */
  int16_t status;
  /* The Disconnect reason of a connection the server ended, else -1 */
  int16_t reason;
} krpc_error_info_t;

/* The details of the last error */
const krpc_error_info_t* krpc_get_error_info(void);

/* Whether the last error is the server exception with the given service and exception ids */
bool krpc_is_exception(uint32_t service, uint32_t exception);

/* Whether the last error is a server exception, given by its KRPC_EXCEPTION_ macro, such as
 * KRPC_IS_EXCEPTION(KRPC_EXCEPTION_KRPC_ARGUMENTEXCEPTION). The macro gives both ids. */
#define KRPC_IS_EXCEPTION(exception) krpc_is_exception(exception)

#endif

#ifdef KRPC_ERROR_MESSAGES
#ifndef KRPC_ERROR_MESSAGE_LENGTH
#define KRPC_ERROR_MESSAGE_LENGTH 256
#endif

/* The message of the last error, cut to fit KRPC_ERROR_MESSAGE_LENGTH. A sentence saying what
 * failed, followed by the message of its cause after a colon. A server error is
 * "Service.Name: description", followed by the server's stack trace when it sends one. */
const char* krpc_get_error_message(void);
#endif

/* Give the message of a transport's failure, before the transport returns KRPC_ERROR_IO or
 * KRPC_ERROR_EOF. The library puts it after its own message. Does nothing unless the library
 * keeps messages. */
void krpc_set_error_cause(const char* message);

/* Print an error message when it occurs. Whether the library prints is read when it
 * compiles. */
#if defined(KRPC_PRINT_ERROR) || defined(KRPC_PRINT_ERRORS_TO_STDERR)
#define KRPC_PRINTS_ERRORS
#endif
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
#define KRPC_CHECK(x)                                              \
  {                                                                \
    krpc_error_t error = (x);                                      \
    if (error != KRPC_OK) {                                        \
      KRPC_PRINT_ERROR("kRPC error: %s\n", krpc_get_error(error)); \
      assert(error == KRPC_OK);                                    \
    }                                                              \
  }
#elif defined(KRPC_ERROR_CHECK_EXIT)
#define KRPC_CHECK(x)                                              \
  {                                                                \
    krpc_error_t error = (x);                                      \
    if (error != KRPC_OK) {                                        \
      KRPC_PRINT_ERROR("kRPC error: %s\n", krpc_get_error(error)); \
      exit((int)error);                                            \
    }                                                              \
  }
#elif defined(KRPC_ERROR_CHECK_FN)
extern void (*krpc_error_handler)(krpc_error_t);
#define KRPC_CHECK(x)                                              \
  {                                                                \
    krpc_error_t error = (x);                                      \
    if (error != KRPC_OK) {                                        \
      KRPC_PRINT_ERROR("kRPC error: %s\n", krpc_get_error(error)); \
      (*(krpc_error_handler))(error);                              \
    }                                                              \
  }
#else  // KRPC_ERROR_CHECK_RETURN
#define KRPC_CHECK(x)                                              \
  {                                                                \
    krpc_error_t error = (x);                                      \
    if (error != KRPC_OK) {                                        \
      KRPC_PRINT_ERROR("kRPC error: %s\n", krpc_get_error(error)); \
      return error;                                                \
    }                                                              \
  }
#endif
#endif  // KRPC_CHECK

/* The library's side of the errors, used by the library and the generated services */

/* The cause of an error that has none */
#define KRPC_NO_CAUSE ((krpc_error_t) - 1)

/* A sentence saying what failed, compiled only when the library keeps or prints messages. On
 * AVR a message the library keeps stays in flash. */
#if defined(KRPC_ERROR_MESSAGES) && defined(__AVR__)
#define KRPC_ERROR_TEXT(message) PSTR(message)
#elif defined(KRPC_ERROR_MESSAGES) || defined(KRPC_PRINTS_ERRORS)
#define KRPC_ERROR_TEXT(message) (message)
#else
#define KRPC_ERROR_TEXT(message) NULL
#endif

/* Record a failure in the last error, print it, and return its code. The sentence comes before
 * the message of a cause the transport or the server gave, after a colon. With no sentence,
 * the cause's message is the whole message. */
krpc_error_t krpc_fail(krpc_error_t code, const char* message, krpc_error_t cause);

/* Fail on an error from a nanopb stream. Returns the code a callback failed with, or records
 * the failure with the given code when nanopb itself failed. */
krpc_error_t krpc_stream_failed(krpc_error_t code, const char* message);

/* Forget the message of a cause no failure has used */
void krpc_clear_error_cause(void);

/* The code a nanopb callback failed with, for the function that ran nanopb to return in place
 * of its own. KRPC_OK when nanopb itself failed. */
extern krpc_error_t krpc_callback_error;

/* Whether a failure goes unrecorded and unprinted, while the library expects failures it then
 * ignores */
#if !defined(KRPC_ERROR_CODES_ONLY) || defined(KRPC_PRINTS_ERRORS)
extern bool krpc_failures_muted;
#define KRPC_MUTE_FAILURES(muted) (krpc_failures_muted = (muted))
#define KRPC_FAILURES_MUTED krpc_failures_muted
#else
#define KRPC_MUTE_FAILURES(muted) ((void)(muted))
#define KRPC_FAILURES_MUTED false
#endif

#ifndef KRPC_ERROR_CODES_ONLY
/* The last error, which a failure fills in */
extern krpc_error_info_t krpc_last_error;
/* Set a field of the last error, after krpc_fail */
#define KRPC_SET_ERROR_DETAIL(field, value) (krpc_last_error.field = (value))
#else
#define KRPC_SET_ERROR_DETAIL(field, value) ((void)(value))
#endif

#ifdef KRPC_ERROR_MESSAGES
/* Start the message of a cause the server gives, which krpc_append_error_message adds to */
void krpc_begin_error_cause(void);
/* Add to the message, cutting what does not fit */
void krpc_append_error_message(const char* str, size_t length);
#endif

/* Record a failure, as an expression */
#if defined(KRPC_ERROR_CODES_ONLY) && !defined(KRPC_PRINTS_ERRORS)
#define KRPC_FAILED(error, message, cause) (error)
#else
#define KRPC_FAILED(error, message, cause) krpc_fail((error), KRPC_ERROR_TEXT(message), (cause))
#endif

/* Fail with an error code and a message */
#define KRPC_FAIL(error, message) return KRPC_FAILED(KRPC_ERROR_##error, message, KRPC_NO_CAUSE)

/* Fail with an error code, a message and the transport's code that caused it */
#define KRPC_FAIL_CAUSE(error, message, cause) \
  return KRPC_FAILED(KRPC_ERROR_##error, message, (cause))

/* Fail on an error from a nanopb stream */
#define KRPC_FAIL_STREAM(error, message, stream) \
  return krpc_stream_failed(KRPC_ERROR_##error, KRPC_ERROR_TEXT(message))

/* Return a transport's error code, giving the message as its cause */
#ifdef KRPC_ERROR_MESSAGES
#define KRPC_CAUSE_TEXT(message) (message)
#else
#define KRPC_CAUSE_TEXT(message) NULL
#endif
/* A message the library keeps is printed with the failure it causes, so it is printed here only
 * when the library does not keep it */
#ifdef KRPC_ERROR_MESSAGES
#define KRPC_PRINT_CAUSE(message) ((void)0)
#else
#define KRPC_PRINT_CAUSE(message) KRPC_PRINT_ERROR("kRPC error: %s\n", message)
#endif
#define KRPC_TRANSPORT_FAIL(error, message)         \
  {                                                 \
    KRPC_PRINT_CAUSE(message);                      \
    krpc_set_error_cause(KRPC_CAUSE_TEXT(message)); \
    return KRPC_ERROR_##error;                      \
  }

/* Runs the code x and returns an error if it fails. */
#define KRPC_RETURN_ON_ERROR(x)                 \
  {                                             \
    krpc_error_t error;                         \
    if ((error = (x)) != KRPC_OK) return error; \
  }

/* Fail a nanopb callback with an error code and a message */
#define KRPC_CALLBACK_FAIL(error, message)                                         \
  {                                                                                \
    krpc_callback_error = KRPC_FAILED(KRPC_ERROR_##error, message, KRPC_NO_CAUSE); \
    return false;                                                                  \
  }

/* Return an error from a nanopb callback function */
#define KRPC_CALLBACK_RETURN_ERROR(msg)                      \
  {                                                          \
    KRPC_PRINT_ERROR("kRPC error: %s (in callback)\n", msg); \
    return false;                                            \
  }

/* Return a stream error from a nanopb callback function */
#define KRPC_CALLBACK_RETURN_STREAM_ERROR(msg, stream)                             \
  {                                                                                \
    KRPC_PRINT_ERROR("kRPC error: %s, %s (in callback)\n", msg, (stream)->errmsg); \
    return false;                                                                  \
  }

/* Return from a nanopb callback function if the given code returns an error, passing the error
 * on */
#define KRPC_CALLBACK_RETURN_ON_ERROR(x)    \
  {                                         \
    krpc_error_t krpc_callback_ = (x);      \
    if (krpc_callback_ != KRPC_OK) {        \
      krpc_callback_error = krpc_callback_; \
      return false;                         \
    }                                       \
  }

#ifdef __cplusplus
}  // extern "C"
#endif
