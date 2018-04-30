#include <krpc_cnano/utils.h>

#include <stdint.h>

void krpc_float64_to_float32(void * float64, void * float32) {
  uint64_t data = *(uint64_t*)float64;
  uint64_t sign = (data >> 32) & 0x80000000;
  uint64_t exp = (data >> 52) & 0x7ff;
  uint64_t man = (data >> 29) & 0x7fffff;
  int32_t expValue = exp - 1023;
  if (expValue == 1024) {
    // preserve inf and nan
    expValue = 128;
  } else if (expValue < -127) {
    // round to +/- 0
    expValue = -127;
  } else if (expValue > 128) {
    // round to +/- inf
    expValue = 128;
    man = 0;
  }
  exp = (expValue + 127) << 23;
  *(uint32_t*)float32 = sign | exp | man;
}

void krpc_float32_to_float64(void * float32, void * float64) {
  uint64_t data = *(uint32_t*)float32;
  uint64_t sign = ((data >> 31) & 0x1) << 63;
  uint64_t exp = (data >> 23) & 0xff;
  uint64_t man = (data & 0x7fffff) << 29;
  int32_t expValue = exp - 127;
  if (expValue == -127)
    expValue = -1023;
  if (expValue == 128)
    expValue = 1024;
  exp = expValue + 1023;
  exp <<= 52;
  *(uint64_t*)float64 = sign | exp | man;
}
