#include <krpc_cnano/encoder.h>
#include <krpc_cnano/error.h>
#include <krpc_cnano/krpc.pb.h>
#include <krpc_cnano/memory.h>
#include <krpc_cnano/pb.h>
#include <krpc_cnano/pb_common.h>
#include <krpc_cnano/pb_encode.h>
#include <stddef.h>
#include <stdint.h>

/* An argument of a procedure call that krpc_procedure_call fills in. The value is the
   Argument.value field as written, tag and length included, so encoding the call copies it. */
typedef struct {
  krpc_schema_Argument message;
  uint8_t* value;
  size_t size;
} krpc_encoded_argument_t;

typedef struct {
  size_t count;
  krpc_encoded_argument_t* arguments;
} krpc_encoded_arguments_t;

static bool krpc_encode_callback_encoded_value(pb_ostream_t* stream, const pb_field_t* field,
                                               void* const* arg) {
  const krpc_encoded_argument_t* argument = (const krpc_encoded_argument_t*)(*arg);
  (void)field;
  if (!pb_write(stream, argument->value, argument->size))
    KRPC_CALLBACK_RETURN_STREAM_ERROR("writing an encoded argument", stream);
  return true;
}

static bool krpc_encode_callback_encoded_arguments(pb_ostream_t* stream, const pb_field_t* field,
                                                   void* const* arg) {
  const krpc_encoded_arguments_t* arguments = (const krpc_encoded_arguments_t*)(*arg);
  size_t i = 0;
  if (arguments == NULL)
    KRPC_CALLBACK_FAIL(ARGUMENT, "The procedure call has no arguments allocated");
  for (; i < arguments->count; i++) {
    // An unfilled position would reach the server as an empty argument at position 0
    if (arguments->arguments[i].message.value.funcs.encode == NULL &&
        !arguments->arguments[i].message.is_null)
      KRPC_CALLBACK_FAIL(ARGUMENT, "The procedure call has an argument not filled in");
    if (!pb_encode_tag_for_field(stream, field))
      KRPC_CALLBACK_RETURN_ERROR("encoding tag for argument");
    if (!pb_encode_submessage(stream, krpc_schema_Argument_fields,
                              &arguments->arguments[i].message))
      KRPC_CALLBACK_RETURN_STREAM_ERROR("encoding submessage for argument", stream);
  }
  return true;
}

krpc_error_t krpc_procedure_call(krpc_schema_ProcedureCall* call, uint32_t serviceId,
                                 uint32_t procedureId, size_t numArguments) {
  krpc_encoded_arguments_t* arguments;
  call->service_id = serviceId;
  call->procedure_id = procedureId;
  call->arguments.funcs.encode = &krpc_encode_callback_encoded_arguments;
  call->arguments.arg = NULL;
  arguments = (krpc_encoded_arguments_t*)krpc_calloc(1, sizeof(krpc_encoded_arguments_t));
  if (arguments == NULL) KRPC_FAIL(NO_MEMORY, "Out of memory for a procedure call");
  call->arguments.arg = arguments;
  if (numArguments > 0) {
    arguments->arguments =
        (krpc_encoded_argument_t*)krpc_calloc(numArguments, sizeof(krpc_encoded_argument_t));
    if (arguments->arguments == NULL) KRPC_FAIL(NO_MEMORY, "Out of memory for a procedure call");
    arguments->count = numArguments;
  }
  return KRPC_OK;
}

/* The argument at a position of a procedure call that krpc_procedure_call fills in */
static krpc_encoded_argument_t* krpc_procedure_call_get_argument(krpc_schema_ProcedureCall* call,
                                                                 uint32_t position) {
  krpc_encoded_arguments_t* arguments = (krpc_encoded_arguments_t*)call->arguments.arg;
  if (call->arguments.funcs.encode != &krpc_encode_callback_encoded_arguments ||
      arguments == NULL || position >= arguments->count)
    return NULL;
  return &arguments->arguments[position];
}

krpc_error_t krpc_procedure_call_argument(krpc_schema_ProcedureCall* call, uint32_t position,
                                          bool (*encode)(pb_ostream_t* stream,
                                                         const pb_field_t* field, void* const* arg),
                                          const void* arg) {
  krpc_encoded_argument_t* argument = krpc_procedure_call_get_argument(call, position);
  void* value = (void*)arg;
  pb_field_iter_t field;
  pb_ostream_t sizing = PB_OSTREAM_SIZING;
  pb_ostream_t stream;
  if (argument == NULL) KRPC_FAIL(ARGUMENT, "No such argument in the call");
  krpc_callback_error = KRPC_OK;
  // Filling in a position again replaces its value, and a failure leaves it unfilled
  krpc_free(argument->value);
  argument->value = NULL;
  argument->size = 0;
  argument->message.value.funcs.encode = NULL;
  argument->message.position = position;
  argument->message.is_null = false;
  // The encode callback writes the value as the Argument.value field, so it is passed that field
  if (!pb_field_iter_begin(&field, krpc_schema_Argument_fields, &argument->message) ||
      !pb_field_iter_find(&field, krpc_schema_Argument_value_tag))
    KRPC_FAIL(ENCODING, "No value field in an argument");
  if (!encode(&sizing, &field, &value))
    KRPC_FAIL_STREAM(ENCODING, "Failed to encode argument", &sizing);
  argument->value = (uint8_t*)krpc_malloc(sizing.bytes_written);
  if (argument->value == NULL) KRPC_FAIL(NO_MEMORY, "Out of memory for an argument");
  argument->size = sizing.bytes_written;
  stream = pb_ostream_from_buffer(argument->value, argument->size);
  if (!encode(&stream, &field, &value))
    KRPC_FAIL_STREAM(ENCODING, "Failed to encode argument", &stream);
  argument->message.value.funcs.encode = &krpc_encode_callback_encoded_value;
  argument->message.value.arg = argument;
  return KRPC_OK;
}

krpc_error_t krpc_procedure_call_null_argument(krpc_schema_ProcedureCall* call, uint32_t position) {
  krpc_encoded_argument_t* argument = krpc_procedure_call_get_argument(call, position);
  if (argument == NULL) KRPC_FAIL(ARGUMENT, "No such argument in the call");
  krpc_free(argument->value);
  argument->value = NULL;
  argument->size = 0;
  argument->message.position = position;
  // A null argument is signaled by is_null with the value field left unset
  argument->message.is_null = true;
  argument->message.value.funcs.encode = NULL;
  return KRPC_OK;
}

krpc_error_t krpc_free_procedure_call(krpc_schema_ProcedureCall* call) {
  krpc_encoded_arguments_t* arguments = (krpc_encoded_arguments_t*)call->arguments.arg;
  size_t i = 0;
  if (call->arguments.funcs.encode != &krpc_encode_callback_encoded_arguments || arguments == NULL)
    return KRPC_OK;
  for (; i < arguments->count; i++) krpc_free(arguments->arguments[i].value);
  krpc_free(arguments->arguments);
  krpc_free(arguments);
  call->arguments.arg = NULL;
  return KRPC_OK;
}
