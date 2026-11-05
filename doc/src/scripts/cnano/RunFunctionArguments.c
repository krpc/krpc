#include <krpc_cnano.h>
#include <krpc_cnano/functions.h>
#include <krpc_cnano/services/krpc.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Function arguments example") != KRPC_OK) return 1;

  // Build a function that doubles its parameter
  krpc_KRPC_Type_t type;
  krpc_KRPC_Type_Double(conn, &type);
  krpc_KRPC_Expression_t x, two, body, function;
  krpc_KRPC_Expression_Parameter(conn, &x, "x", type);
  krpc_KRPC_Expression_ConstantDouble(conn, &two, 2);
  krpc_KRPC_Expression_Multiply(conn, &body, x, two);
  krpc_list_object_t parameters = {1, &x};
  krpc_KRPC_Expression_Lambda(conn, &function, &parameters, body);
  krpc_compile_function(conn, function);

  // Encode the argument at the type of its parameter
  uint8_t buffer[8];
  pb_ostream_t output = pb_ostream_from_buffer(buffer, sizeof(buffer));
  krpc_encode_double(&output, 21);
  krpc_nullable_bytes_t items[1];
  items[0].value.size = output.bytes_written;
  items[0].value.data = buffer;
  items[0].has_value = true;
  krpc_list_nullable_bytes_t arguments = {1, items};

  krpc_bytes_t data = KRPC_BYTES_NULL;
  bool is_null;
  krpc_run_function(conn, &data, &is_null, function, &arguments);

  // Decode the value at the function's type
  double value;
  pb_istream_t input = pb_istream_from_buffer(data.data, data.size);
  krpc_decode_double(&input, &value);
  KRPC_FREE_BYTES(data);
  printf("Result: %.1f\n", value);

  krpc_disconnect(conn);
}
