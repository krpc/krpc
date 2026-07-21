#pragma once

#include <stddef.h>
#include <stdint.h>

#define SERIAL_8N1 0x06

/* Minimal stand-in for the Arduino HardwareSerial class, enough to exercise the Arduino
   communication backend on the host. Reads are scripted: data holds the bytes available to be
   read and chunks gives the number of them each successive call to readBytes hands back, where
   0 means the serial timeout expired without a single byte arriving.

   Kept free of the C++ standard library, as this header is included by the backend itself when
   it is compiled, and as the Arduino platform does not provide it either. */
class HardwareSerial {
 public:
  HardwareSerial(const uint8_t* data, size_t data_size, const size_t* chunks, size_t num_chunks)
      : data(data), data_size(data_size), chunks(chunks), num_chunks(num_chunks) {}

  void begin(uint32_t speed, uint8_t config) {
    (void)speed;
    (void)config;
  }

  void end() {}

  operator bool() const { return true; }

  size_t readBytes(uint8_t* buf, size_t count) {
    size_t n = chunk < num_chunks ? chunks[chunk++] : 0;
    if (n > count) n = count;
    if (n > data_size - position) n = data_size - position;
    for (size_t i = 0; i < n; i++) buf[i] = data[position++];
    return n;
  }

  size_t write(const uint8_t* buf, size_t count) {
    for (size_t i = 0; i < count && num_written < sizeof(written); i++)
      written[num_written++] = buf[i];
    return count;
  }

  uint8_t written[64];
  size_t num_written = 0;

 private:
  const uint8_t* data;
  size_t data_size;
  const size_t* chunks;
  size_t num_chunks;
  size_t position = 0;
  size_t chunk = 0;
};
