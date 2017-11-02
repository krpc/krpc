#include <krpc/decoder.h>

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

#include <pb.h>
#include <pb_decode.h>

#include <krpc/error.h>
#include <krpc/krpc.pb.h>
#include <krpc/memory.h>
#include <krpc/types.h>
#ifdef __AVR__
#include <krpc/utils.h>
#endif

// Callback to decode a return value from a procedure result
static bool krpc_return_value_decoder(
  pb_istream_t * stream, const pb_field_t * field, void ** arg) {
  krpc_result_t * result = (krpc_result_t *)(*arg);
  uint32_t size = stream->bytes_left;
  uint8_t * buffer = (uint8_t*)krpc_malloc(sizeof(uint8_t) * size);
  if (!pb_read(stream, buffer, size))
    KRPC_CALLBACK_RETURN_ERROR("decoding return value");
  result->size = size;
  result->buffer = buffer;
  return true;
}

krpc_error_t krpc_init_result(krpc_result_t * result) {
  result->message.value.funcs.decode = &krpc_return_value_decoder;
  result->message.value.arg = result;
  return KRPC_OK;
}

krpc_error_t krpc_free_result(krpc_result_t * result) {
  if (result->buffer) {
    krpc_free(result->buffer);
    result->buffer = NULL;
  }
  return KRPC_OK;
}

krpc_error_t krpc_get_return_value(krpc_result_t * result, pb_istream_t * stream) {
  *stream = pb_istream_from_buffer(result->buffer, result->size);
  return KRPC_OK;
}

krpc_error_t krpc_decode_double(pb_istream_t * stream, double * value) {
  #ifndef __AVR__
  if (!pb_decode_fixed64(stream, value))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode double", stream);
  #else
  uint64_t tmp;
  if (!pb_decode_fixed64(stream, &tmp))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode double", stream);
  krpc_float64_to_float32(&tmp, value);
  #endif
  return KRPC_OK;
}

/*[[[cog
types = [
  ('float',  'float',    'fixed32', None),
  ('int32',  'int32_t',  'svarint',  'int64_t'),
  ('int64',  'int64_t',  'svarint',  None),
  ('uint32', 'uint32_t', 'varint32', None),
  ('uint64', 'uint64_t', 'varint',   None),
  ('bool',   'bool',     'varint32', 'uint32_t'),
  ('object', 'krpc_object_t', 'varint',   None),
  ('enum',   'krpc_enum_t',   'svarint', 'int64_t')
]

for typ, ctyp, dec, int_fix in types:
  cog.outl('krpc_error_t krpc_decode_%s(pb_istream_t * stream, %s * value) {' % (typ, ctyp))
  value = '&tmp' if int_fix else 'value'
  if int_fix:
    cog.outl('  %s tmp;' % int_fix);
  cog.outl('  if (!pb_decode_%s(stream, %s))' % (dec, value))
  cog.outl('    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode %s", stream);' % typ)
  if int_fix:
    cog.outl('  *value = (%s)tmp;' % ctyp)
  cog.outl("""  return KRPC_OK;
}
""")
]]]*/
krpc_error_t krpc_decode_float(pb_istream_t * stream, float * value) {
  if (!pb_decode_fixed32(stream, value))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode float", stream);
  return KRPC_OK;
}

krpc_error_t krpc_decode_int32(pb_istream_t * stream, int32_t * value) {
  int64_t tmp;
  if (!pb_decode_svarint(stream, &tmp))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode int32", stream);
  *value = (int32_t)tmp;
  return KRPC_OK;
}

krpc_error_t krpc_decode_int64(pb_istream_t * stream, int64_t * value) {
  if (!pb_decode_svarint(stream, value))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode int64", stream);
  return KRPC_OK;
}

krpc_error_t krpc_decode_uint32(pb_istream_t * stream, uint32_t * value) {
  if (!pb_decode_varint32(stream, value))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode uint32", stream);
  return KRPC_OK;
}

krpc_error_t krpc_decode_uint64(pb_istream_t * stream, uint64_t * value) {
  if (!pb_decode_varint(stream, value))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode uint64", stream);
  return KRPC_OK;
}

krpc_error_t krpc_decode_bool(pb_istream_t * stream, bool * value) {
  uint32_t tmp;
  if (!pb_decode_varint32(stream, &tmp))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode bool", stream);
  *value = (bool)tmp;
  return KRPC_OK;
}

krpc_error_t krpc_decode_object(pb_istream_t * stream, krpc_object_t * value) {
  if (!pb_decode_varint(stream, value))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode object", stream);
  return KRPC_OK;
}

krpc_error_t krpc_decode_enum(pb_istream_t * stream, krpc_enum_t * value) {
  int64_t tmp;
  if (!pb_decode_svarint(stream, &tmp))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode enum", stream);
  *value = (krpc_enum_t)tmp;
  return KRPC_OK;
}

// [[[end]]]

krpc_error_t krpc_decode_string(pb_istream_t * stream, char ** value) {
  uint32_t length;
  if (!pb_decode_varint32(stream, &length))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode length of string", stream);
  if (!*value)
    *value = (char*)krpc_malloc(length+1);
  if (!pb_read(stream, (pb_byte_t*)(*value), length))
    KRPC_RETURN_STREAM_ERROR(
      DECODING_FAILED, "failed to decode value of type string", stream);
  *((*value)+length) = 0;
  return KRPC_OK;
}

krpc_error_t krpc_decode_bytes(pb_istream_t * stream, krpc_bytes_t * value) {
  uint32_t size;
  if (!pb_decode_varint32(stream, &size))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode length of krpc_bytes_t", stream);
  pb_byte_t * buffer;
  if (size == 0) {
    // Special case for empty byte arrays
    value->size = 0;
    return KRPC_OK;
  }
  if (value->size > 0) {
    // Use existing buffer
    if (value->size < size)
      KRPC_RETURN_ERROR(BUFFER_TOO_SMALL, "not enough space for bytes in buffer");
    buffer = (pb_byte_t*)value->data;
  } else {
    // Allocate a new buffer
    buffer = (pb_byte_t*)krpc_malloc(size);
    if (!buffer)
      KRPC_RETURN_ERROR(DECODING_FAILED, "failed to allocate buffer for krpc_bytes_t");
    value->size = size;
    value->data = buffer;
  }
  if (!pb_read(stream, buffer, size))
    KRPC_RETURN_STREAM_ERROR(
      DECODING_FAILED, "failed to decode value of type krpc_bytes_t", stream);
  return KRPC_OK;
}

/*[[[cog
types = ['Tuple', 'List', 'Set', 'Dictionary', 'DictionaryEntry',
         'Event', 'Stream', 'Services', 'Status']
for typ in types:
  cog.outl("""
krpc_error_t krpc_decode_message_""" + typ + """(
  pb_istream_t * stream, krpc_schema_""" + typ + """ * value) {
  if (!pb_decode(stream, krpc_schema_""" + typ + """_fields, value))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode """ + typ + """", stream);
  return KRPC_OK;
}""")
]]]*/

krpc_error_t krpc_decode_message_Tuple(
  pb_istream_t * stream, krpc_schema_Tuple * value) {
  if (!pb_decode(stream, krpc_schema_Tuple_fields, value))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode Tuple", stream);
  return KRPC_OK;
}

krpc_error_t krpc_decode_message_List(
  pb_istream_t * stream, krpc_schema_List * value) {
  if (!pb_decode(stream, krpc_schema_List_fields, value))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode List", stream);
  return KRPC_OK;
}

krpc_error_t krpc_decode_message_Set(
  pb_istream_t * stream, krpc_schema_Set * value) {
  if (!pb_decode(stream, krpc_schema_Set_fields, value))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode Set", stream);
  return KRPC_OK;
}

krpc_error_t krpc_decode_message_Dictionary(
  pb_istream_t * stream, krpc_schema_Dictionary * value) {
  if (!pb_decode(stream, krpc_schema_Dictionary_fields, value))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode Dictionary", stream);
  return KRPC_OK;
}

krpc_error_t krpc_decode_message_DictionaryEntry(
  pb_istream_t * stream, krpc_schema_DictionaryEntry * value) {
  if (!pb_decode(stream, krpc_schema_DictionaryEntry_fields, value))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode DictionaryEntry", stream);
  return KRPC_OK;
}

krpc_error_t krpc_decode_message_Event(
  pb_istream_t * stream, krpc_schema_Event * value) {
  if (!pb_decode(stream, krpc_schema_Event_fields, value))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode Event", stream);
  return KRPC_OK;
}

krpc_error_t krpc_decode_message_Stream(
  pb_istream_t * stream, krpc_schema_Stream * value) {
  if (!pb_decode(stream, krpc_schema_Stream_fields, value))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode Stream", stream);
  return KRPC_OK;
}

krpc_error_t krpc_decode_message_Services(
  pb_istream_t * stream, krpc_schema_Services * value) {
  if (!pb_decode(stream, krpc_schema_Services_fields, value))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode Services", stream);
  return KRPC_OK;
}

krpc_error_t krpc_decode_message_Status(
  pb_istream_t * stream, krpc_schema_Status * value) {
  if (!pb_decode(stream, krpc_schema_Status_fields, value))
    KRPC_RETURN_STREAM_ERROR(DECODING_FAILED, "failed to decode Status", stream);
  return KRPC_OK;
}
// [[[end]]]
