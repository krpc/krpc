import krpc.client.Connection;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.KRPC.Type;

import java.util.Arrays;
import java.util.Collections;

public class SwitchStatement {
  public static void main(String[] args) {
    try (Connection connection = Connection.newInstance()) {
      Expression.Bound e = Expression.bind(connection);
      Type.Bound t = Type.bind(connection);

      // total = 0;
      // for (int x : new int[] {1, 2, 3})
      //   switch (x) {
      //     case 1: case 2: total += 10; break;
      //     default: total += 1; break;
      //   }
      Expression total = e.variable("total", t.int_());
      Expression x = e.variable("x", t.int_());
      Expression values = e.createList(Arrays.asList(
        e.constantInt(1), e.constantInt(2), e.constantInt(3)));
      Expression body = e.match(
        x,
        Arrays.asList(
          Arrays.asList(e.constantInt(1), e.constantInt(2)),
          Collections.<Expression>emptyList()),
        Arrays.asList(
          e.assign(total, e.add(total, e.constantInt(10))),
          e.assign(total, e.add(total, e.constantInt(1)))));
      Expression program = e.blockWithVariables(
        Arrays.asList(total, x),
        Arrays.asList(
          e.assign(total, e.constantInt(0)),
          e.forEach(x, values, body),
          total));

      int result = connection.run(program);
      System.out.println(result);
    }
  }
}
