#include <krpc_cnano/encoder.h>

#include <stddef.h>
#include <stdint.h>
#include <string.h>

#include <krpc_cnano/pb.h>
#include <krpc_cnano/pb_encode.h>

#include <krpc_cnano/error.h>
#include <krpc_cnano/krpc.pb.h>
#include <krpc_cnano/types.h>
#ifdef __AVR__
#include <krpc_cnano/utils.h>
#endif

bool krpc_encode_callback_cstring(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for c string");
  const char * string = (const char*)(*arg);
  if (!pb_encode_string(stream, (const pb_byte_t*)string, strlen(string)))
    KRPC_CALLBACK_RETURN_ERROR("encoding c string");
  return true;
}

static bool krpc_encode_callback_arguments(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  krpc_call_t * call = (krpc_call_t*)(*arg);
  size_t i = 0;
  for (; i < call->numArguments; i++) {
    if (!pb_encode_tag_for_field(stream, field))
      KRPC_CALLBACK_RETURN_ERROR("encoding tag for argument");
    krpc_schema_Argument * argument = &call->arguments[i].message;
    if (!pb_encode_submessage(stream, krpc_schema_Argument_fields, argument))
      KRPC_CALLBACK_RETURN_STREAM_ERROR("encoding submessage for argument", stream);
  }
  return true;
}

krpc_error_t krpc_call(
  krpc_call_t * call, uint32_t serviceId, uint32_t procedureId,
  size_t numArguments, krpc_argument_t * arguments) {
  call->message.service_id = serviceId;
  call->message.procedure_id = procedureId;
  call->numArguments = numArguments;
  call->arguments = arguments;
  call->message.arguments.funcs.encode = &krpc_encode_callback_arguments;
  call->message.arguments.arg = call;
  return KRPC_OK;
}

krpc_error_t krpc_add_argument(
  krpc_call_t * call,
  uint32_t position,
  bool (*encode)(pb_ostream_t * stream, const pb_field_t * field, void * const * arg),
  const void * arg) {
  krpc_argument_t * argument = call->arguments+position;
  argument->message.position = position;
  argument->message.value.funcs.encode = encode;
  argument->message.value.arg = (void*)arg;
  return KRPC_OK;
}

krpc_error_t krpc_encode_double(pb_ostream_t * stream, double value) {
  #ifndef __AVR__
  if (!pb_encode_fixed64(stream, &value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode double", stream);
  #else
  uint64_t tmp;
  krpc_float32_to_float64(&value, &tmp);
  if (!pb_encode_fixed64(stream, &tmp))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode double", stream);
  #endif
  return KRPC_OK;
}

/*[[[cog
types = [
  ('double', 'double',   None,      '&', 8),
  ('float',  'float',    'fixed32', '&', 4),
  ('int32',  'int32_t',  'svarint', '', None),
  ('int64',  'int64_t',  'svarint', '', None),
  ('uint32', 'uint32_t', 'varint', '', None),
  ('uint64', 'uint64_t', 'varint', '', None),
  ('bool',   'bool',     'varint', '', None),
  ('object', 'krpc_object_t', 'varint', '', None),
  ('enum',   'krpc_enum_t',   'svarint', '', None)
]

for typ, ctyp, enc, getval, _ in types:

  if enc:
    cog.outl("""
krpc_error_t krpc_encode_""" + typ + """(pb_ostream_t * stream, """ + ctyp + """ value) {
  if (!pb_encode_""" + enc + """(stream, """ + getval + """value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode """ + typ + """", stream);
  return KRPC_OK;
}""")

for typ, ctyp, enc, _, size in types:
  if size:
    cog.outl("""
krpc_error_t krpc_encode_size_""" + typ + """(size_t * size, """ + ctyp + """ value) {
  *size = """ + str(size) + """;
  return KRPC_OK;
}""")
  else:
    cog.outl("""
krpc_error_t krpc_encode_size_""" + typ + """(size_t * size, """ + ctyp + """ value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  if (!pb_encode_""" + enc + """(&stream, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to get size of """ + typ + """", &stream);
  *size = stream.bytes_written;
  return KRPC_OK;
}""")

for typ, ctyp, enc, _, size in types:
  if size:
    cog.outl("""
bool krpc_encode_callback_""" + typ + """(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for """ + typ + """");
  """ + ctyp + """ * value = (""" + ctyp + """*)(*arg);
  if (!pb_encode_varint(stream, """ + str(size) + """))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for """ + typ + """");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_""" + typ + """(stream, *value));
  return true;
}""")
  else:
    cog.outl("""
bool krpc_encode_callback_""" + typ + """(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for """ + typ + """");
  """ + ctyp + """ * value = (""" + ctyp + """*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_""" + typ + """(&size, *value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for """ + typ + """");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_""" + typ + """(stream, *value));
  return true;
}""")
]]]*/

krpc_error_t krpc_encode_float(pb_ostream_t * stream, float value) {
  if (!pb_encode_fixed32(stream, &value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode float", stream);
  return KRPC_OK;
}

krpc_error_t krpc_encode_int32(pb_ostream_t * stream, int32_t value) {
  if (!pb_encode_svarint(stream, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode int32", stream);
  return KRPC_OK;
}

krpc_error_t krpc_encode_int64(pb_ostream_t * stream, int64_t value) {
  if (!pb_encode_svarint(stream, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode int64", stream);
  return KRPC_OK;
}

krpc_error_t krpc_encode_uint32(pb_ostream_t * stream, uint32_t value) {
  if (!pb_encode_varint(stream, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode uint32", stream);
  return KRPC_OK;
}

krpc_error_t krpc_encode_uint64(pb_ostream_t * stream, uint64_t value) {
  if (!pb_encode_varint(stream, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode uint64", stream);
  return KRPC_OK;
}

krpc_error_t krpc_encode_bool(pb_ostream_t * stream, bool value) {
  if (!pb_encode_varint(stream, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode bool", stream);
  return KRPC_OK;
}

krpc_error_t krpc_encode_object(pb_ostream_t * stream, krpc_object_t value) {
  if (!pb_encode_varint(stream, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode object", stream);
  return KRPC_OK;
}

krpc_error_t krpc_encode_enum(pb_ostream_t * stream, krpc_enum_t value) {
  if (!pb_encode_svarint(stream, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode enum", stream);
  return KRPC_OK;
}

krpc_error_t krpc_encode_size_double(size_t * size, double value) {
  *size = 8;
  return KRPC_OK;
}

krpc_error_t krpc_encode_size_float(size_t * size, float value) {
  *size = 4;
  return KRPC_OK;
}

krpc_error_t krpc_encode_size_int32(size_t * size, int32_t value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  if (!pb_encode_svarint(&stream, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to get size of int32", &stream);
  *size = stream.bytes_written;
  return KRPC_OK;
}

krpc_error_t krpc_encode_size_int64(size_t * size, int64_t value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  if (!pb_encode_svarint(&stream, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to get size of int64", &stream);
  *size = stream.bytes_written;
  return KRPC_OK;
}

krpc_error_t krpc_encode_size_uint32(size_t * size, uint32_t value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  if (!pb_encode_varint(&stream, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to get size of uint32", &stream);
  *size = stream.bytes_written;
  return KRPC_OK;
}

krpc_error_t krpc_encode_size_uint64(size_t * size, uint64_t value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  if (!pb_encode_varint(&stream, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to get size of uint64", &stream);
  *size = stream.bytes_written;
  return KRPC_OK;
}

krpc_error_t krpc_encode_size_bool(size_t * size, bool value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  if (!pb_encode_varint(&stream, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to get size of bool", &stream);
  *size = stream.bytes_written;
  return KRPC_OK;
}

krpc_error_t krpc_encode_size_object(size_t * size, krpc_object_t value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  if (!pb_encode_varint(&stream, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to get size of object", &stream);
  *size = stream.bytes_written;
  return KRPC_OK;
}

krpc_error_t krpc_encode_size_enum(size_t * size, krpc_enum_t value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  if (!pb_encode_svarint(&stream, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to get size of enum", &stream);
  *size = stream.bytes_written;
  return KRPC_OK;
}

bool krpc_encode_callback_double(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for double");
  double * value = (double*)(*arg);
  if (!pb_encode_varint(stream, 8))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for double");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_double(stream, *value));
  return true;
}

bool krpc_encode_callback_float(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for float");
  float * value = (float*)(*arg);
  if (!pb_encode_varint(stream, 4))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for float");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_float(stream, *value));
  return true;
}

bool krpc_encode_callback_int32(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for int32");
  int32_t * value = (int32_t*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_int32(&size, *value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for int32");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_int32(stream, *value));
  return true;
}

bool krpc_encode_callback_int64(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for int64");
  int64_t * value = (int64_t*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_int64(&size, *value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for int64");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_int64(stream, *value));
  return true;
}

bool krpc_encode_callback_uint32(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for uint32");
  uint32_t * value = (uint32_t*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_uint32(&size, *value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for uint32");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_uint32(stream, *value));
  return true;
}

bool krpc_encode_callback_uint64(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for uint64");
  uint64_t * value = (uint64_t*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_uint64(&size, *value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for uint64");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_uint64(stream, *value));
  return true;
}

bool krpc_encode_callback_bool(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for bool");
  bool * value = (bool*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_bool(&size, *value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for bool");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_bool(stream, *value));
  return true;
}

bool krpc_encode_callback_object(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for object");
  krpc_object_t * value = (krpc_object_t*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_object(&size, *value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for object");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_object(stream, *value));
  return true;
}

bool krpc_encode_callback_enum(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for enum");
  krpc_enum_t * value = (krpc_enum_t*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_enum(&size, *value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for enum");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_enum(stream, *value));
  return true;
}
// [[[end]]]

krpc_error_t krpc_encode_string(pb_ostream_t * stream, const char * string) {
  if (!pb_encode_string(stream, (const pb_byte_t*)string, strlen(string)))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode string", stream);
  return KRPC_OK;
}

krpc_error_t krpc_encode_bytes(pb_ostream_t * stream, krpc_bytes_t bytes) {
  if (bytes.size > 0 && bytes.data == NULL)
    KRPC_RETURN_ERROR(ENCODING_FAILED, "bytes data is null");
  if (!pb_encode_string(stream, bytes.data, bytes.size))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode bytes", stream);
  return KRPC_OK;
}

krpc_error_t krpc_encode_size_string(size_t * size, const char * string) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  KRPC_RETURN_ON_ERROR(krpc_encode_string(&stream, string));
  *size = stream.bytes_written;
  return KRPC_OK;
}

krpc_error_t krpc_encode_size_bytes(size_t * size, krpc_bytes_t bytes) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  KRPC_RETURN_ON_ERROR(krpc_encode_bytes(&stream, bytes));
  *size = stream.bytes_written;
  return KRPC_OK;
}

bool krpc_encode_callback_string(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for string");
  const char * value = *(const char**)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_string(&size, value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for string");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_string(stream, value));
  return true;
}

bool krpc_encode_callback_bytes(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for bytes");
  krpc_bytes_t * value = (krpc_bytes_t*)(*arg);
  size_t size;
  if (KRPC_OK != krpc_encode_size_bytes(&size, *value))
    KRPC_CALLBACK_RETURN_ERROR("getting size for bytes");
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for bytes");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_bytes(stream, *value));
  return true;
}

/*[[[cog
types = ['ProcedureCall', 'Tuple', 'List', 'Set', 'Dictionary', 'DictionaryEntry']
for typ in types:
  cog.outl("""
krpc_error_t krpc_encode_message_""" + typ + """(
  pb_ostream_t * stream, const krpc_schema_""" + typ + """ * value) {
  if (!pb_encode(stream, krpc_schema_""" + typ + """_fields, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode """ + typ + """", stream);
  return KRPC_OK;
}""")
for typ in types:
  cog.outl("""
krpc_error_t krpc_encode_size_message_""" + typ + """(
  size_t * size, const krpc_schema_""" + typ + """ * value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  KRPC_RETURN_ON_ERROR(krpc_encode_message_""" + typ + """(&stream, value));
  *size = stream.bytes_written;
  return KRPC_OK;
}""")
for typ in types:
  cog.outl("""
bool krpc_encode_callback_message_""" + typ + """(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for """ + typ + """");
  krpc_schema_""" + typ + """ * value = (krpc_schema_""" + typ + """*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_message_""" + typ + """(&size, value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for """ + typ + """");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_message_""" + typ + """(stream, value));
  return true;
}""")
]]]*/

krpc_error_t krpc_encode_message_ProcedureCall(
  pb_ostream_t * stream, const krpc_schema_ProcedureCall * value) {
  if (!pb_encode(stream, krpc_schema_ProcedureCall_fields, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode ProcedureCall", stream);
  return KRPC_OK;
}

krpc_error_t krpc_encode_message_Tuple(
  pb_ostream_t * stream, const krpc_schema_Tuple * value) {
  if (!pb_encode(stream, krpc_schema_Tuple_fields, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode Tuple", stream);
  return KRPC_OK;
}

krpc_error_t krpc_encode_message_List(
  pb_ostream_t * stream, const krpc_schema_List * value) {
  if (!pb_encode(stream, krpc_schema_List_fields, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode List", stream);
  return KRPC_OK;
}

krpc_error_t krpc_encode_message_Set(
  pb_ostream_t * stream, const krpc_schema_Set * value) {
  if (!pb_encode(stream, krpc_schema_Set_fields, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode Set", stream);
  return KRPC_OK;
}

krpc_error_t krpc_encode_message_Dictionary(
  pb_ostream_t * stream, const krpc_schema_Dictionary * value) {
  if (!pb_encode(stream, krpc_schema_Dictionary_fields, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode Dictionary", stream);
  return KRPC_OK;
}

krpc_error_t krpc_encode_message_DictionaryEntry(
  pb_ostream_t * stream, const krpc_schema_DictionaryEntry * value) {
  if (!pb_encode(stream, krpc_schema_DictionaryEntry_fields, value))
    KRPC_RETURN_STREAM_ERROR(ENCODING_FAILED, "failed to encode DictionaryEntry", stream);
  return KRPC_OK;
}

krpc_error_t krpc_encode_size_message_ProcedureCall(
  size_t * size, const krpc_schema_ProcedureCall * value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  KRPC_RETURN_ON_ERROR(krpc_encode_message_ProcedureCall(&stream, value));
  *size = stream.bytes_written;
  return KRPC_OK;
}

krpc_error_t krpc_encode_size_message_Tuple(
  size_t * size, const krpc_schema_Tuple * value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  KRPC_RETURN_ON_ERROR(krpc_encode_message_Tuple(&stream, value));
  *size = stream.bytes_written;
  return KRPC_OK;
}

krpc_error_t krpc_encode_size_message_List(
  size_t * size, const krpc_schema_List * value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  KRPC_RETURN_ON_ERROR(krpc_encode_message_List(&stream, value));
  *size = stream.bytes_written;
  return KRPC_OK;
}

krpc_error_t krpc_encode_size_message_Set(
  size_t * size, const krpc_schema_Set * value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  KRPC_RETURN_ON_ERROR(krpc_encode_message_Set(&stream, value));
  *size = stream.bytes_written;
  return KRPC_OK;
}

krpc_error_t krpc_encode_size_message_Dictionary(
  size_t * size, const krpc_schema_Dictionary * value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  KRPC_RETURN_ON_ERROR(krpc_encode_message_Dictionary(&stream, value));
  *size = stream.bytes_written;
  return KRPC_OK;
}

krpc_error_t krpc_encode_size_message_DictionaryEntry(
  size_t * size, const krpc_schema_DictionaryEntry * value) {
  pb_ostream_t stream = PB_OSTREAM_SIZING;
  KRPC_RETURN_ON_ERROR(krpc_encode_message_DictionaryEntry(&stream, value));
  *size = stream.bytes_written;
  return KRPC_OK;
}

bool krpc_encode_callback_message_ProcedureCall(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for ProcedureCall");
  krpc_schema_ProcedureCall * value = (krpc_schema_ProcedureCall*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_message_ProcedureCall(&size, value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for ProcedureCall");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_message_ProcedureCall(stream, value));
  return true;
}

bool krpc_encode_callback_message_Tuple(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for Tuple");
  krpc_schema_Tuple * value = (krpc_schema_Tuple*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_message_Tuple(&size, value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for Tuple");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_message_Tuple(stream, value));
  return true;
}

bool krpc_encode_callback_message_List(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for List");
  krpc_schema_List * value = (krpc_schema_List*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_message_List(&size, value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for List");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_message_List(stream, value));
  return true;
}

bool krpc_encode_callback_message_Set(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for Set");
  krpc_schema_Set * value = (krpc_schema_Set*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_message_Set(&size, value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for Set");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_message_Set(stream, value));
  return true;
}

bool krpc_encode_callback_message_Dictionary(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for Dictionary");
  krpc_schema_Dictionary * value = (krpc_schema_Dictionary*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_message_Dictionary(&size, value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for Dictionary");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_message_Dictionary(stream, value));
  return true;
}

bool krpc_encode_callback_message_DictionaryEntry(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg) {
  if (!pb_encode_tag_for_field(stream, field))
    KRPC_CALLBACK_RETURN_ERROR("encoding tag for DictionaryEntry");
  krpc_schema_DictionaryEntry * value = (krpc_schema_DictionaryEntry*)(*arg);
  size_t size;
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_size_message_DictionaryEntry(&size, value));
  if (!pb_encode_varint(stream, size))
    KRPC_CALLBACK_RETURN_ERROR("encoding size for DictionaryEntry");
  KRPC_CALLBACK_RETURN_ON_ERROR(krpc_encode_message_DictionaryEntry(stream, value));
  return true;
}
// [[[end]]]
