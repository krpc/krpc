#pragma once

#include <stdlib.h>

#ifdef __cplusplus
extern "C" {
#endif

/* Allocate size bytes on the heap and return a pointer to it */
void * krpc_malloc(size_t size);

/* Allocate an array containing num elements of the given type
 * on the heap and return a pointer to it. Zeroes the newly allocated memory. */
void * krpc_calloc(size_t num, size_t size);

/* Realloc an array pointed to by ptr, from num elements to num+inc elements
 * of the given type. inc must be greater than 0. The new region of memory will be zeroed. */
void * krpc_recalloc(void * ptr, size_t num, size_t inc, size_t size);

/* Free memory allocated on the heap */
void krpc_free(void * ptr);

/* Block size for dynamically allocated collection types */
#ifndef KRPC_ALLOC_BLOCK_SIZE
#define KRPC_ALLOC_BLOCK_SIZE 4
#endif

#ifdef __cplusplus
}  // extern "C"
#endif
