#pragma once

#include <krpc_cnano/krpc.pb.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct krpc_bytes_s krpc_bytes_t;
struct krpc_bytes_s {
  size_t size;
  uint8_t * data;
};

#define KRPC_BYTES(name, size)                          \
  uint8_t krpc_buffer_##name[(size)];                   \
  krpc_bytes_t name = { (size), (krpc_buffer_##name) };
#define KRPC_BYTES_FROM_BUFFER(buffer, size) { (size), (buffer) }
#define KRPC_BYTES_NULL { 0, NULL }
#define KRPC_FREE_BYTES(b) { \
  krpc_free(b.data);         \
  b.data = NULL;             \
  b.size = 0;                \
}

typedef uint64_t krpc_object_t;
#define KRPC_NULL ((uint64_t)0)

typedef int krpc_enum_t;

#define KRPC_NULL_LIST { 0, NULL }
#define KRPC_FREE_LIST(l) { \
  krpc_free(l.items);       \
  l.items = NULL;           \
  l.size = 0;               \
}

#define KRPC_NULL_SET { 0, NULL }
#define KRPC_FREE_SET(s) { \
  krpc_free(s.items);      \
  s.items = NULL;          \
  s.size = 0;              \
}

#define KRPC_NULL_DICTIONARY { 0, NULL }
#define KRPC_FREE_DICTIONARY(d) { \
  krpc_free(d.entries);           \
  d.entries = NULL;               \
  d.size = 0;                     \
}

#ifdef __cplusplus
}  // extern "C"
#endif
