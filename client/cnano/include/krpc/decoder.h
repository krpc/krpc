#pragma once

#include <krpc/error.h>
#include <krpc/krpc.pb.h>
#include <krpc/types.h>

#include <pb.h>

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct {
  krpc_schema_ProcedureResult message;
  size_t size;
  uint8_t * buffer;
} krpc_result_t;

#define KRPC_RESULT_INIT_DEFAULT { krpc_schema_ProcedureResult_init_default, 0, NULL }

krpc_error_t krpc_init_result(krpc_result_t * result);
krpc_error_t krpc_free_result(krpc_result_t * result);
krpc_error_t krpc_get_return_value(krpc_result_t * result, pb_istream_t * stream);

/*[[[cog
types = [
  ('double', 'double'),
  ('float',  'float'),
  ('int32',  'int32_t'),
  ('int64',  'int64_t'),
  ('uint32', 'uint32_t'),
  ('uint64', 'uint64_t'),
  ('bool',   'bool'),
  ('string', 'char *'),
  ('bytes',  'krpc_bytes_t'),
  ('object', 'krpc_object_t'),
  ('enum',   'krpc_enum_t')
]
for typ, ctyp in types:
  cog.outl('krpc_error_t krpc_decode_%s(pb_istream_t * stream, %s * value);' % (typ, ctyp))
]]]*/
krpc_error_t krpc_decode_double(pb_istream_t * stream, double * value);
krpc_error_t krpc_decode_float(pb_istream_t * stream, float * value);
krpc_error_t krpc_decode_int32(pb_istream_t * stream, int32_t * value);
krpc_error_t krpc_decode_int64(pb_istream_t * stream, int64_t * value);
krpc_error_t krpc_decode_uint32(pb_istream_t * stream, uint32_t * value);
krpc_error_t krpc_decode_uint64(pb_istream_t * stream, uint64_t * value);
krpc_error_t krpc_decode_bool(pb_istream_t * stream, bool * value);
krpc_error_t krpc_decode_string(pb_istream_t * stream, char * * value);
krpc_error_t krpc_decode_bytes(pb_istream_t * stream, krpc_bytes_t * value);
krpc_error_t krpc_decode_object(pb_istream_t * stream, krpc_object_t * value);
krpc_error_t krpc_decode_enum(pb_istream_t * stream, krpc_enum_t * value);
// [[[end]]]

/*[[[cog
types = ['Tuple', 'List', 'Set', 'Dictionary', 'DictionaryEntry',
         'Event', 'Stream', 'Services', 'Status']
for typ in types:
  cog.outl("""krpc_error_t krpc_decode_message_%s(
  pb_istream_t * stream, krpc_schema_%s * value);""" % (typ, typ))
]]]*/
krpc_error_t krpc_decode_message_Tuple(
  pb_istream_t * stream, krpc_schema_Tuple * value);
krpc_error_t krpc_decode_message_List(
  pb_istream_t * stream, krpc_schema_List * value);
krpc_error_t krpc_decode_message_Set(
  pb_istream_t * stream, krpc_schema_Set * value);
krpc_error_t krpc_decode_message_Dictionary(
  pb_istream_t * stream, krpc_schema_Dictionary * value);
krpc_error_t krpc_decode_message_DictionaryEntry(
  pb_istream_t * stream, krpc_schema_DictionaryEntry * value);
krpc_error_t krpc_decode_message_Event(
  pb_istream_t * stream, krpc_schema_Event * value);
krpc_error_t krpc_decode_message_Stream(
  pb_istream_t * stream, krpc_schema_Stream * value);
krpc_error_t krpc_decode_message_Services(
  pb_istream_t * stream, krpc_schema_Services * value);
krpc_error_t krpc_decode_message_Status(
  pb_istream_t * stream, krpc_schema_Status * value);
// [[[end]]]

#ifdef __cplusplus
}  // extern "C"
#endif
