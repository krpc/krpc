#include <krpc_cnano/error.h>
#include <stddef.h>
#include <string.h>

#ifdef KRPC_ERROR_CHECK_FN
void (*krpc_error_handler)(krpc_error_t);
#endif

krpc_error_t krpc_callback_error = KRPC_OK;

#if !defined(KRPC_ERROR_CODES_ONLY) || defined(KRPC_PRINTS_ERRORS)
bool krpc_failures_muted = false;
#endif

const char* krpc_get_error(krpc_error_t error) {
  switch (error) {
    case KRPC_OK:
      return "ok";
    case KRPC_ERROR_CONNECTION:
      return "connection error";
    case KRPC_ERROR_CONNECTION_REFUSED:
      return "connection refused";
    case KRPC_ERROR_SERVER:
      return "server error";
    case KRPC_ERROR_ENCODING:
      return "encoding error";
    case KRPC_ERROR_ARGUMENT:
      return "argument error";
    case KRPC_ERROR_NO_MEMORY:
      return "out of memory";
    case KRPC_ERROR_BUFFER_TOO_SMALL:
      return "buffer too small";
    case KRPC_ERROR_IO:
      return "I/O failure";
    case KRPC_ERROR_EOF:
      return "end of file";
    default:
      return "";
  }
}

#ifndef KRPC_ERROR_CODES_ONLY

krpc_error_info_t krpc_last_error = {KRPC_OK, KRPC_NO_CAUSE, 0, 0, -1, -1};

const krpc_error_info_t* krpc_get_error_info(void) { return &krpc_last_error; }

bool krpc_is_exception(uint32_t service, uint32_t exception) {
  return krpc_last_error.service == service && krpc_last_error.exception == exception;
}

#endif

#ifdef KRPC_ERROR_MESSAGES

#ifndef __AVR__
#define strlen_P strlen
#define memcpy_P memcpy
#endif

static char krpc_error_message[KRPC_ERROR_MESSAGE_LENGTH];

/* Whether the message holds the message of a cause, for the next failure to put after its own */
static bool krpc_error_cause_set = false;

const char* krpc_get_error_message(void) { return krpc_error_message; }

void krpc_append_error_message(const char* str, size_t length) {
  size_t used = strlen(krpc_error_message);
  size_t space = KRPC_ERROR_MESSAGE_LENGTH - used - 1;
  if (length > space) length = space;
  memcpy(krpc_error_message + used, str, length);
  krpc_error_message[used + length] = '\0';
}

void krpc_begin_error_cause(void) {
  krpc_error_message[0] = '\0';
  krpc_error_cause_set = true;
}

void krpc_set_error_cause(const char* message) {
  if (krpc_failures_muted) return;
  krpc_begin_error_cause();
  if (message != NULL) krpc_append_error_message(message, strlen(message));
}

void krpc_clear_error_cause(void) { krpc_error_cause_set = false; }

/* Put the sentence in front of the message, with a colon between them, cutting what does not
   fit off the end */
static void krpc_prepend_error_message(const char* sentence) {
  const size_t space = KRPC_ERROR_MESSAGE_LENGTH - 1;
  size_t length = strlen_P(sentence);
  size_t cause = strlen(krpc_error_message);
  size_t shift = length + (cause > 0 ? 2 : 0);
  if (shift > space) shift = space;
  if (cause > space - shift) cause = space - shift;
  memmove(krpc_error_message + shift, krpc_error_message, cause);
  krpc_error_message[shift + cause] = '\0';
  memcpy_P(krpc_error_message, sentence, shift < length ? shift : length);
  if (shift > length) krpc_error_message[length] = ':';
  if (shift > length + 1) krpc_error_message[length + 1] = ' ';
}

#else

void krpc_set_error_cause(const char* message) { (void)message; }

void krpc_clear_error_cause(void) {}

#endif

krpc_error_t krpc_fail(krpc_error_t code, const char* message, krpc_error_t cause) {
#if !defined(KRPC_ERROR_CODES_ONLY) || defined(KRPC_PRINTS_ERRORS)
  if (krpc_failures_muted) return code;
#endif
#ifndef KRPC_ERROR_CODES_ONLY
  krpc_last_error.code = code;
  krpc_last_error.cause = cause;
  krpc_last_error.service = 0;
  krpc_last_error.exception = 0;
  krpc_last_error.status = -1;
  krpc_last_error.reason = -1;
#else
  (void)cause;
#endif
#ifdef KRPC_ERROR_MESSAGES
  if (!krpc_error_cause_set) krpc_error_message[0] = '\0';
  if (message != NULL) krpc_prepend_error_message(message);
  krpc_error_cause_set = false;
  KRPC_PRINT_ERROR("kRPC error: %s\n", krpc_error_message);
#else
  KRPC_PRINT_ERROR("kRPC error: %s\n", message != NULL ? message : krpc_get_error(code));
  (void)message;
#endif
  return code;
}

krpc_error_t krpc_stream_failed(krpc_error_t code, const char* message) {
  krpc_error_t callback = krpc_callback_error;
  krpc_callback_error = KRPC_OK;
  if (callback != KRPC_OK) return callback;
#if defined(KRPC_ERROR_CODES_ONLY) && !defined(KRPC_PRINTS_ERRORS)
  (void)message;
  return code;
#else
  return krpc_fail(code, message, KRPC_NO_CAUSE);
#endif
}
