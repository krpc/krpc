#include <krpc_cnano.h>
#include <krpc_cnano/functions.h>
#include <krpc_cnano/services/krpc.h>
#include <stdio.h>

int main() {
  krpc_connection_t conn;
  if (krpc_open_serial(&conn, "COM0") != KRPC_OK) return 1;
  if (krpc_connect(conn, "Switch statement example") != KRPC_OK) return 1;

  // total = 0;
  // for x in {1, 2, 3}:
  //   switch (x) {
  //     case 1: case 2: total += 10; break;
  //     default: total += 1; break;
  //   }
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

  // The first case tests 1 and 2, and the second, with no test values, is
  // the default
  krpc_list_object_t case_items[2] = {{2, items}, {0, NULL}};
  krpc_list_list_object_t cases = {2, case_items};
  krpc_KRPC_Expression_t ten, one, plus_ten, plus_one, bodies_items[2];
  krpc_KRPC_Expression_ConstantInt(conn, &ten, 10);
  krpc_KRPC_Expression_ConstantInt(conn, &one, 1);
  krpc_KRPC_Expression_Add(conn, &plus_ten, total, ten);
  krpc_KRPC_Expression_Add(conn, &plus_one, total, one);
  krpc_KRPC_Expression_Assign(conn, &bodies_items[0], total, plus_ten);
  krpc_KRPC_Expression_Assign(conn, &bodies_items[1], total, plus_one);
  krpc_list_object_t bodies = {2, bodies_items};
  krpc_KRPC_Expression_t body;
  krpc_KRPC_Expression_Match(conn, &body, x, &cases, &bodies);

  krpc_KRPC_Expression_t zero, start, loop;
  krpc_KRPC_Expression_ConstantInt(conn, &zero, 0);
  krpc_KRPC_Expression_Assign(conn, &start, total, zero);
  krpc_KRPC_Expression_ForEach(conn, &loop, x, values, body);

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
