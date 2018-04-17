#pragma once

#ifdef __cplusplus
extern "C" {
#endif

void krpc_float32_to_float64(void * float32, void * float64);
void krpc_float64_to_float32(void * float64, void * float32);

#ifdef __cplusplus
}  // extern "C"
#endif
