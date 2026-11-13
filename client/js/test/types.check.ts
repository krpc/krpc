// The types of compiled functions, checked when the tests compile. Nothing runs these
import type { Connection } from "../src/connection.js";
import type * as TestService from "../src/services/TestService.js";
import { types as t } from "../src/typevalues.js";

export async function check(
  conn: Connection,
  s: TestService.Service,
  obj: TestService.TestClass,
): Promise<void> {
  // A property is a property, and a method returns its value
  const doubled: number = await conn.run(({ obj }) => obj.intProperty * 2, { obj });
  const echoed: TestService.TestClass | null = await conn.run(
    ({ s, obj }) => s.echoTestObject(obj),
    { s, obj },
  );
  // @ts-expect-error getIntProperty is the client's form of the property
  await conn.run(({ obj }) => obj.getIntProperty(), { obj });
  // @ts-expect-error a property read is not a Promise
  await conn.run(({ obj }) => obj.intProperty.then(), { obj });

  // A compiled function's parameters and value are checked where it runs
  const fn = await conn.compile(({ s }, x) => s.int32ToString(x), { s }, [t.int32]);
  const text: string = await conn.run(fn, 1);
  // @ts-expect-error the argument is a number
  await conn.run(fn, "a");
  // @ts-expect-error a stream of a function with parameters has no arguments to pass
  await conn.addStream(fn);
  const byClass = await conn.compile((_, x) => x.getValue(), {}, [s.TestClass]);
  const value: string = await conn.run(byClass, obj);

  // Streams and events
  const stream = await conn.addStream(({ obj }) => obj.intProperty > 1, { obj });
  const above: boolean = await stream.get();
  await conn.addEvent(({ obj }) => obj.intProperty > 1, { obj });
  // @ts-expect-error the function of an event returns a boolean
  await conn.addEvent(({ obj }) => obj.intProperty, { obj });

  // Collections, assignments and state
  const values: number[] = await conn.run(({ obj }) => [obj.intProperty, 2].map((x) => x * 2), {
    obj,
  });
  const state = { count: 0 };
  await conn.run(
    ({ obj, state }) => {
      obj.intProperty = state.count;
      state.count += 1;
    },
    { obj, state },
  );

  // A Map is a Map in the body and in the value
  const counts: Map<string, number> = await conn.run(
    ({ s, m }) => {
      const incremented = s.incrementDictionary(m);
      return new Map([["a", incremented.get("a") ?? 0]]);
    },
    { s, m: new Map([["a", 1]]) },
  );
  // A captured types value names a type in the body, as does a service's class
  const empty: number[] = await conn.run(({ t }) => t.list(t.double).cast([]), { t });
  const objects: TestService.TestClass[] = await conn.run(
    ({ t, s }) => t.list(s.TestClass).cast([]),
    { t, s },
  );
  // A 64-bit integer argument is a number or a bigint
  const long = await conn.compile(({ s }, x) => s.int64ToString(x), { s }, [t.int64]);
  await conn.run(long, 1);
  await conn.run(long, 1n);
  // A property with no getter is assigned
  await conn.run(
    ({ obj }) => {
      obj.stringPropertyPrivateGet = "a";
    },
    { obj },
  );
  void [doubled, echoed, text, value, above, values, counts, empty, objects];
}
