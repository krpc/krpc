#pragma once

// A krpc_malloc, krpc_calloc and krpc_free that count the heap blocks in use, and can fail an
// allocation, for tests linked against krpc_cnano_custom_memory
namespace memory {

// The heap blocks in use
extern int live_blocks;

// The number of allocations left before one fails, or -1 for none to fail
extern int allocations_left;

}  // namespace memory
