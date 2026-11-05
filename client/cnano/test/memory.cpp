#include "memory.h"

#include <cstddef>
#include <cstdint>
#include <cstdlib>
#include <cstring>

namespace memory {

int live_blocks = 0;
int allocations_left = -1;

}  // namespace memory

namespace {

void* allocated(void* block) {
  if (block != nullptr) memory::live_blocks++;
  return block;
}

bool allocation_fails() {
  if (memory::allocations_left < 0) return false;
  memory::allocations_left--;
  return memory::allocations_left < 0;
}

}  // namespace

extern "C" {

void* krpc_malloc(size_t size) { return allocation_fails() ? nullptr : allocated(malloc(size)); }

void* krpc_calloc(size_t num, size_t size) {
  return allocation_fails() ? nullptr : allocated(calloc(num, size));
}

void* krpc_recalloc(void* ptr, size_t num, size_t inc, size_t size) {
  if (allocation_fails()) return nullptr;
  // NOLINTNEXTLINE(bugprone-suspicious-realloc-usage)
  auto* block = static_cast<uint8_t*>(realloc(ptr, (num + inc) * size));
  if (block == nullptr) return nullptr;
  if (ptr == nullptr) memory::live_blocks++;
  memset(block + (num * size), 0, inc * size);
  return block;
}

void krpc_free(void* ptr) {
  if (ptr != nullptr) memory::live_blocks--;
  free(ptr);
}
}
