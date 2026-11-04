#pragma once

#include <stddef.h>
#include <stdint.h>

/* Minimal stand-in for the Arduino core, enough to exercise the stream transport on the host.
   Kept free of the C++ standard library, as the transport includes this header when it is
   compiled, and the Arduino platform does not provide one either. */

/* The base class of every Arduino serial port */
class Stream {
 public:
  virtual ~Stream() {}
  virtual size_t readBytes(uint8_t* buf, size_t count) = 0;
  virtual size_t write(const uint8_t* buf, size_t count) = 0;
};

/* A serial port with scripted reads. data holds the bytes available to be read and chunks gives
   the number of them each successive call to readBytes hands back, where 0 means the stream
   timeout expired without a single byte arriving. The port reports ready once operator bool has
   been checked unready_checks times. */
class ScriptedSerial : public Stream {
 public:
  ScriptedSerial(const uint8_t* data, size_t data_size, const size_t* chunks, size_t num_chunks)
      : data(data), data_size(data_size), chunks(chunks), num_chunks(num_chunks) {}

  void begin(unsigned long baud) { speed = baud; }

  operator bool() {
    if (unready_checks == 0) return true;
    unready_checks--;
    return false;
  }

  size_t readBytes(uint8_t* buf, size_t count) override {
    size_t n = chunk < num_chunks ? chunks[chunk++] : 0;
    if (n > count) n = count;
    if (n > data_size - position) n = data_size - position;
    for (size_t i = 0; i < n; i++) buf[i] = data[position++];
    return n;
  }

  size_t write(const uint8_t* buf, size_t count) override {
    for (size_t i = 0; i < count && num_written < sizeof(written); i++)
      written[num_written++] = buf[i];
    return count;
  }

  unsigned long speed = 0;
  int unready_checks = 0;
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
