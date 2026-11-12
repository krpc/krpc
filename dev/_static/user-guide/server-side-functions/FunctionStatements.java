import krpc.client.Connection;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.KRPC.Type;

import java.util.Arrays;

public class FunctionStatements {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      Expression.Bound e = Expression.bind(connection);
      Type.Bound t = Type.bind(connection);

      // total = 0; for (int x : new int[] {1, 2, 3}) total += x;
      Expression total = e.variable("total", t.int_());
      Expression x = e.variable("x", t.int_());
      Expression values = e.createList(Arrays.asList(
        e.constantInt(1), e.constantInt(2), e.constantInt(3)));
      Expression program = e.blockWithVariables(
        Arrays.asList(total, x),
        Arrays.asList(
          e.assign(total, e.constantInt(0)),
          e.forEach(x, values, e.assign(total, e.add(total, x))),
          total));

      int result = connection.run(program);
      System.out.println(result);
    }
  }
}
