#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>

HardwareSerial * conn;

void setup() {
  pinMode(LED_BUILTIN, OUTPUT);
  digitalWrite(LED_BUILTIN, LOW);

  conn = &Serial;
  // Open the serial port connection
  krpc_open(&conn, NULL);
  // Set up communication with the server
  krpc_connect(conn, "Arduino Example");

  // Indicate succesful connection by lighting the on-board LED
  digitalWrite(LED_BUILTIN, HIGH);
}

void loop() {
}
