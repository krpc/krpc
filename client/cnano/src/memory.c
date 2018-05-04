#include <krpc_cnano/memory.h>

#include <assert.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

#ifndef KRPC_CUSTOM_MEMORY_ALLOC

void * krpc_malloc(size_t size) {
  return malloc(size);
}

void * krpc_calloc(size_t num, size_t size) {
  return calloc(num, size);
}

void * krpc_recalloc(void * ptr, size_t num, size_t inc, size_t size) {
  assert(inc > 0);
  ptr = realloc(ptr, (num+inc) * size);
  memset(((uint8_t*)ptr) + (num*size), 0, (inc*size));
  return ptr;
}

void krpc_free(void * ptr) {
  free(ptr);
}

#endif
