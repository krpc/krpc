#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>

HardwareSerial * conn;

void setup() {
  pinMode(LED_BUILTIN, OUTPUT);
  digitalWrite(LED_BUILTIN, LOW);

  conn = &Serial;
  // Open the serial port connection
  krpc_open(&conn, NULL);
  // Set up communication with the server, retrying while part of an earlier session's message
  // is on the line
  krpc_error_t error;
  while ((error = krpc_connect(conn, "Arduino Example")) == KRPC_ERROR_CONNECTION_FAILED)
    delay(3500);
  if (error != KRPC_OK)
    return;

  // Indicate successful connection by lighting the on-board LED
  digitalWrite(LED_BUILTIN, HIGH);
}

void loop() {
}
