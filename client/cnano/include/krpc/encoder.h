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

bool krpc_encode_callback_cstring(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);

typedef struct {
  krpc_schema_Argument message;
} krpc_argument_t;

typedef struct {
  krpc_schema_ProcedureCall message;
  size_t numArguments;
  krpc_argument_t * arguments;
} krpc_call_t;

krpc_error_t krpc_call(
  krpc_call_t * call, uint32_t serviceId, uint32_t procedureId,
  size_t numArguments, krpc_argument_t * arguments);

krpc_error_t krpc_add_argument(
  krpc_call_t * call,
  uint32_t position,
  bool (*encode)(pb_ostream_t * stream, const pb_field_t * field, void * const * arg),
  const void * arg);

/*[[[cog
types = [
  ('double', 'double'),
  ('float',  'float'),
  ('int32',  'int32_t'),
  ('int64',  'int64_t'),
  ('uint32', 'uint32_t'),
  ('uint64', 'uint64_t'),
  ('bool',   'bool'),
  ('string', 'const char *'),
  ('bytes',  'krpc_bytes_t'),
  ('object', 'krpc_object_t'),
  ('enum',   'krpc_enum_t')
]
for typ, ctyp in types:
  cog.outl("""krpc_error_t krpc_encode_%s(
  pb_ostream_t * stream, %s value);""" % (typ, ctyp))
cog.outl()
for typ, ctyp in types:
  cog.outl("""krpc_error_t krpc_encode_size_%s(
size_t * size, %s value);""" % (typ, ctyp))
cog.outl()
for typ, _ in types:
  cog.outl("""bool krpc_encode_callback_%s(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);""" % typ)
]]]*/
krpc_error_t krpc_encode_double(
  pb_ostream_t * stream, double value);
krpc_error_t krpc_encode_float(
  pb_ostream_t * stream, float value);
krpc_error_t krpc_encode_int32(
  pb_ostream_t * stream, int32_t value);
krpc_error_t krpc_encode_int64(
  pb_ostream_t * stream, int64_t value);
krpc_error_t krpc_encode_uint32(
  pb_ostream_t * stream, uint32_t value);
krpc_error_t krpc_encode_uint64(
  pb_ostream_t * stream, uint64_t value);
krpc_error_t krpc_encode_bool(
  pb_ostream_t * stream, bool value);
krpc_error_t krpc_encode_string(
  pb_ostream_t * stream, const char * value);
krpc_error_t krpc_encode_bytes(
  pb_ostream_t * stream, krpc_bytes_t value);
krpc_error_t krpc_encode_object(
  pb_ostream_t * stream, krpc_object_t value);
krpc_error_t krpc_encode_enum(
  pb_ostream_t * stream, krpc_enum_t value);

krpc_error_t krpc_encode_size_double(
size_t * size, double value);
krpc_error_t krpc_encode_size_float(
size_t * size, float value);
krpc_error_t krpc_encode_size_int32(
size_t * size, int32_t value);
krpc_error_t krpc_encode_size_int64(
size_t * size, int64_t value);
krpc_error_t krpc_encode_size_uint32(
size_t * size, uint32_t value);
krpc_error_t krpc_encode_size_uint64(
size_t * size, uint64_t value);
krpc_error_t krpc_encode_size_bool(
size_t * size, bool value);
krpc_error_t krpc_encode_size_string(
size_t * size, const char * value);
krpc_error_t krpc_encode_size_bytes(
size_t * size, krpc_bytes_t value);
krpc_error_t krpc_encode_size_object(
size_t * size, krpc_object_t value);
krpc_error_t krpc_encode_size_enum(
size_t * size, krpc_enum_t value);

bool krpc_encode_callback_double(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
bool krpc_encode_callback_float(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
bool krpc_encode_callback_int32(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
bool krpc_encode_callback_int64(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
bool krpc_encode_callback_uint32(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
bool krpc_encode_callback_uint64(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
bool krpc_encode_callback_bool(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
bool krpc_encode_callback_string(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
bool krpc_encode_callback_bytes(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
bool krpc_encode_callback_object(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
bool krpc_encode_callback_enum(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
// [[[end]]]

/*[[[cog
types = ['ProcedureCall', 'Tuple', 'List', 'Set', 'Dictionary', 'DictionaryEntry']
for typ in types:
  cog.outl("""krpc_error_t krpc_encode_message_%s(
  pb_ostream_t * stream, const krpc_schema_%s * value);""" % (typ, typ))
cog.outl()
for typ in types:
  cog.outl("""krpc_error_t krpc_encode_size_message_%s(
  size_t * size, const krpc_schema_%s * value);""" % (typ, typ))
cog.outl()
for typ in types:
  cog.outl("""bool krpc_encode_callback_message_%s(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);""" % typ)
]]]*/
krpc_error_t krpc_encode_message_ProcedureCall(
  pb_ostream_t * stream, const krpc_schema_ProcedureCall * value);
krpc_error_t krpc_encode_message_Tuple(
  pb_ostream_t * stream, const krpc_schema_Tuple * value);
krpc_error_t krpc_encode_message_List(
  pb_ostream_t * stream, const krpc_schema_List * value);
krpc_error_t krpc_encode_message_Set(
  pb_ostream_t * stream, const krpc_schema_Set * value);
krpc_error_t krpc_encode_message_Dictionary(
  pb_ostream_t * stream, const krpc_schema_Dictionary * value);
krpc_error_t krpc_encode_message_DictionaryEntry(
  pb_ostream_t * stream, const krpc_schema_DictionaryEntry * value);

krpc_error_t krpc_encode_size_message_ProcedureCall(
  size_t * size, const krpc_schema_ProcedureCall * value);
krpc_error_t krpc_encode_size_message_Tuple(
  size_t * size, const krpc_schema_Tuple * value);
krpc_error_t krpc_encode_size_message_List(
  size_t * size, const krpc_schema_List * value);
krpc_error_t krpc_encode_size_message_Set(
  size_t * size, const krpc_schema_Set * value);
krpc_error_t krpc_encode_size_message_Dictionary(
  size_t * size, const krpc_schema_Dictionary * value);
krpc_error_t krpc_encode_size_message_DictionaryEntry(
  size_t * size, const krpc_schema_DictionaryEntry * value);

bool krpc_encode_callback_message_ProcedureCall(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
bool krpc_encode_callback_message_Tuple(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
bool krpc_encode_callback_message_List(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
bool krpc_encode_callback_message_Set(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
bool krpc_encode_callback_message_Dictionary(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
bool krpc_encode_callback_message_DictionaryEntry(
  pb_ostream_t * stream, const pb_field_t * field, void * const * arg);
// [[[end]]]

#ifdef __cplusplus
}  // extern "C"
#endif
