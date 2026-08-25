import krpc.client.Connection;
import krpc.client.RPCException;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.KRPC.Type;

import java.io.IOException;
import java.util.Arrays;

public class FunctionStatements {
  public static void main(String[] args) throws IOException, RPCException {
    try (Connection connection = Connection.newInstance()) {
      // total = 0; for (int x : new int[] {1, 2, 3}) total += x;
      Expression total = Expression.variable(
        connection, "total", Type.int_(connection));
      Expression x = Expression.variable(connection, "x", Type.int_(connection));
      Expression values = Expression.createList(connection, Arrays.asList(
        Expression.constantInt(connection, 1),
        Expression.constantInt(connection, 2),
        Expression.constantInt(connection, 3)));
      Expression program = Expression.blockWithVariables(
        connection,
        Arrays.asList(total, x),
        Arrays.asList(
          Expression.assign(connection, total,
            Expression.constantInt(connection, 0)),
          Expression.forEach(connection, x, values,
            Expression.assign(connection, total,
              Expression.add(connection, total, x))),
          total));

      int result = connection.runFunction(program);
      System.out.println(result);
    }
  }
}
