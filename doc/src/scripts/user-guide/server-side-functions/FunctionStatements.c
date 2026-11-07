#include <krpc_cnano.h>
#include <krpc_cnano/functions.h>
#include <krpc_cnano/services/krpc.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Function statements example") != KRPC_OK) return 1;

  // total = 0; for x in {1, 2, 3}: total += x;
  krpc_KRPC_Type_t int_type;
  krpc_KRPC_Type_Int(conn, &int_type);
  krpc_KRPC_Expression_t total, x;
  krpc_KRPC_Expression_Variable(conn, &total, "total", int_type);
  krpc_KRPC_Expression_Variable(conn, &x, "x", int_type);

  krpc_KRPC_Expression_t items[3];
  for (int i = 0; i < 3; i++)
    krpc_KRPC_Expression_ConstantInt(conn, &items[i], i + 1);
  krpc_list_object_t elements = {3, items};
  krpc_KRPC_Expression_t values;
  krpc_KRPC_Expression_CreateList(conn, &values, &elements);

  krpc_KRPC_Expression_t zero, start, sum, add, loop;
  krpc_KRPC_Expression_ConstantInt(conn, &zero, 0);
  krpc_KRPC_Expression_Assign(conn, &start, total, zero);
  krpc_KRPC_Expression_Add(conn, &sum, total, x);
  krpc_KRPC_Expression_Assign(conn, &add, total, sum);
  krpc_KRPC_Expression_ForEach(conn, &loop, x, values, add);

  krpc_KRPC_Expression_t variable_items[2] = {total, x};
  krpc_KRPC_Expression_t statement_items[3] = {start, loop, total};
  krpc_list_object_t variables = {2, variable_items};
  krpc_list_object_t statements = {3, statement_items};
  krpc_KRPC_Expression_t program;
  krpc_KRPC_Expression_BlockWithVariables(conn, &program, &variables,
                                          &statements);

  krpc_bytes_t data = KRPC_BYTES_NULL;
  krpc_run_function(conn, &data, NULL, program, NULL);
  int32_t result;
  pb_istream_t stream = pb_istream_from_buffer(data.data, data.size);
  krpc_decode_int32(&stream, &result);
  KRPC_FREE_BYTES(data);
  printf("%d\n", (int)result);

  krpc_disconnect(conn);
}
