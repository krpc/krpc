// Statements, collections, structures, state and exceptions in compiled functions
import assert from "node:assert/strict";
import { after, before, describe, it } from "node:test";
import type { DynamicConnection } from "../src/connection.js";
import { CompilationError } from "../src/errors.js";
import { TestEnum, TestStruct } from "../src/services/TestService.js";
import { type TypeLike, types as t } from "../src/typevalues.js";
import { compile, connectToServer, onBothSides, run } from "./harness.js";

let conn: DynamicConnection;

before(async () => {
  conn = await connectToServer();
});

after(async () => {
  await conn.close();
});

async function rejects(
  fn: (captures: any, ...parameters: any[]) => unknown,
  captures: object,
  message: RegExp,
  parameters: readonly TypeLike[] = [],
) {
  await assert.rejects(conn.compile(fn, captures, parameters), (error: unknown) => {
    assert.ok(error instanceof CompilationError, `${error}`);
    assert.match(error.message, message);
    return true;
  });
}

describe("statements", () => {
  it("runs variables, branches and loops", async () => {
    await onBothSides(
      conn,
      (_: object, n: number) => {
        let total = 0;
        for (let i = 0; i < n; i++) {
          if (i % 2 === 0) {
            continue;
          }
          total += i;
        }
        let j = 0;
        while (true) {
          j++;
          if (j > 3) {
            break;
          }
        }
        let k = 0;
        do {
          k += 1;
          if (k === 2) {
          }
        } while (k < 5);
        return total * 100 + j * 10 + k;
      },
      [t.int32],
      [[0], [7]],
    );
  });

  it("falls through the cases of a switch", async () => {
    await onBothSides(
      conn,
      (_: object, x: number) => {
        let s = "";
        for (const y of [x, x + 1]) {
          switch (y) {
            // biome-ignore lint/suspicious/noFallthroughSwitchClause: the fall-through is the test
            case 1:
              s += "a";
            case 2:
              s += "b";
              break;
            case 3:
            case 4:
              s += "c";
              continue;
            default:
              s += "d";
          }
          s += ".";
        }
        return s;
      },
      [t.int32],
      [[1], [2], [3], [5]],
    );
  });

  it("checks the reads in the tests of a switch", async () => {
    await rejects(
      (_, k: number) => {
        let x: number;
        switch (k) {
          // @ts-expect-error x is read before it is assigned
          case x:
            return 1;
          default:
            x = 5;
            return 2;
        }
      },
      {},
      /x is read before it is assigned on every path/,
      [t.double],
    );
    // JavaScript scopes a declaration to the whole switch, and the server to its case
    await rejects(
      (_, k: number) => {
        switch (k) {
          case 1:
            // biome-ignore lint/correctness/noSwitchDeclarations: the declaration is the test
            const y = 1;
            return y;
          default:
            return 0;
        }
      },
      {},
      /Wrap a declaration in a case in a block/,
      [t.double],
    );
  });

  it("returns on every path", async () => {
    await onBothSides(
      conn,
      (_: object, x: number) => {
        if (x > 0) {
          return "positive";
        } else if (x < 0) {
          return "negative";
        }
        return "zero";
      },
      [t.double],
      [[1], [-1], [0]],
    );
    await rejects(
      (_, x: number) => {
        if (x > 0) {
          return 1;
        }
      },
      {},
      /returns a value, and the end of its body returns none/,
      [t.double],
    );
  });

  it("checks that a variable is assigned before a read", async () => {
    await rejects(
      (_, x: number) => {
        let y: number;
        if (x > 0) {
          y = 1;
        }
        // @ts-expect-error y is read before it is assigned on every path
        return y;
      },
      {},
      /y is read before it is assigned on every path/,
      [t.double],
    );
  });

  it("assigns properties, and destructures", async () => {
    const obj = await conn.testService.createTestObject("assign");
    await obj.setIntProperty(1);
    await run(
      conn,
      ({ obj }) => {
        obj.intProperty += 2;
        obj.intProperty++;
      },
      { obj },
    );
    assert.equal(await obj.getIntProperty(), 4);
    await onBothSides(
      conn,
      (_: object, x: number, y: number) => {
        let a = x;
        let b = y;
        [a, b] = [b, a];
        const [c, d] = [a * 10, b];
        let e = 0;
        [e] = [x, y].map((z) => z * 2);
        return c + d + e;
      },
      [t.double, t.double],
      [[1, 2]],
    );
  });

  it("types a variable assigned null first from its later value", async () => {
    await onBothSides(
      conn,
      (_: object, x: number) => {
        let found = null;
        if (x > 1) {
          found = x * 2;
        }
        return found;
      },
      [t.double],
      [[0], [2]],
    );
  });

  it("rejects null for a variable typed by its declaration", async () => {
    await rejects(
      ({ m }: any) => {
        for (let n of ["a", "b"]) {
          n = m.get(n);
        }
      },
      { m: new Map([["a", "b"]]) },
      /n cannot be null/,
    );
  });
});

describe("collections", () => {
  it("builds lists, and calls their methods", async () => {
    for (const fn of [
      (_: object, x: number) => [x, x + 1, x + 2].map((y) => y * 2),
      (_: object, x: number) => [x, 1, 2, 3].filter((y) => y > 1),
      (_: object, x: number) => [x, 1, 2].reduce((a, y) => a + y, 0),
      (_: object, x: number) => [x, 1, 2].reduce((a, y) => a * y),
      (_: object, x: number) => [x, 1].some((y) => y > 1) && [x, 2].every((y) => y > 0),
      (_: object, x: number) => [1, x, 3].find((y) => y > 1) ?? -1,
      (_: object, x: number) => [1, x, 3].findIndex((y) => y > 1),
      (_: object, x: number) => [1, x, 3].indexOf(3) + ([x].includes(2) ? 10 : 0),
      (_: object, x: number) => [1, 2, 3, x].slice(1, -1).concat([9]),
      (_: object, x: number) => [x, 3, 1].join("-"),
      (_: object, x: number) => [x, 3, 1].toSorted((a, b) => a - b),
      (_: object, x: number) => [x, 3, 1].toSorted((a, b) => b - a),
      (_: object, x: number) => [x, 3, 1].toReversed().at(-1),
      (_: object, x: number) => [x, 3, 1].length + [x, 3][1],
    ]) {
      await onBothSides(conn, fn, [t.double], [[2], [5]]);
    }
    // find stops at the first match
    await onBothSides(
      conn,
      (_: object, x: number) => {
        let tests = 0;
        const found = [1, x, 3].find((y) => {
          tests += 1;
          return y > 0;
        });
        return tests * 10 + (found ?? 0);
      },
      [t.double],
      [[2]],
    );
    // A descending order of integers reverses the smallest one too
    const obj = await conn.testService.createTestObject("sort");
    await obj.setIntProperty(-(2 ** 31));
    assert.deepEqual(
      await run(conn, ({ obj }) => [obj.intProperty, 1].toSorted((a, b) => b - a), { obj }),
      [1, -(2 ** 31)],
    );
  });

  it("types an empty array from what it is given", async () => {
    await onBothSides(
      conn,
      (_: object, n: number) => {
        const values = [];
        for (let i = 0; i < n; i++) {
          values.push(i * i);
        }
        values[0] = 7;
        const last = values.pop();
        return values.length * 100 + (last ?? 0);
      },
      [t.int32],
      [[3], [5]],
    );
    await rejects(() => [], {}, /An empty array has no type here/);
    assert.deepEqual(await run(conn, ({ t }) => t.list(t.double).cast([]), { t }), []);
  });

  it("calls the methods of sets and maps", async () => {
    await onBothSides(
      conn,
      (_: object, x: string) => {
        const seen = new Set(["a"]);
        seen.add(x);
        seen.delete("a");
        const counts = new Map([["a", 1]]);
        counts.set(x, (counts.get(x) ?? 0) + 5);
        let total = 0;
        for (const [key, count] of counts) {
          total += key.length * count;
        }
        return `${seen.has(x)} ${seen.size} ${counts.get("z") ?? -1} ${total} ${Array.from(counts.keys()).join(",")}`;
      },
      [t.string],
      [["a"], ["bb"]],
    );
  });

  it("passes an array as a tuple and as a list", async () => {
    const s = conn.testService;
    assert.deepEqual(await run(conn, ({ s }) => s.incrementTuple([1, 2n]), { s }), [2, 3n]);
    assert.deepEqual(await run(conn, ({ s }) => s.incrementList([1, 2, 3]), { s }), [2, 3, 4]);
    const values = [4, 5];
    assert.deepEqual(
      await run(conn, ({ s, values }) => s.incrementList(values), { s, values }),
      [5, 6],
    );
  });

  it("splits strings", async () => {
    await onBothSides(
      conn,
      (_: object, x: string) => x.split(",").length,
      [t.string],
      [["a,b,c"], [""]],
    );
  });
});

describe("structures", () => {
  it("reads fields and builds structures", async () => {
    const s = conn.testService;
    const value = new TestStruct({
      intField: 3,
      stringField: "x",
      enumField: TestEnum.valueB,
      listField: [1],
    });
    assert.equal(
      await run(conn, ({ s, value }) => s.structEcho(value).stringField, { s, value }),
      "x",
    );
    const echoed = await run(
      conn,
      ({ s, TestStruct, TestEnum }) =>
        s.structEcho(
          new TestStruct({
            intField: 4,
            stringField: "y",
            enumField: TestEnum.valueA,
            listField: [],
          }),
        ),
      { s, TestStruct, TestEnum },
    );
    assert.equal((echoed as TestStruct).intField, 4);
  });
});

describe("state variables", () => {
  it("keeps a captured object's property between evaluations", async () => {
    const state = { count: 0 };
    const fn = await compile(
      conn,
      ({ state }) => {
        state.count += 1;
        return state.count;
      },
      { state },
    );
    assert.equal(await conn.run(fn), 1);
    assert.equal(await conn.run(fn), 2);
    assert.equal(state.count, 0);
  });
});

describe("state variables that start empty", () => {
  it("types a state variable from its first push, or a cast", async () => {
    const state = { history: [] as number[] };
    const fn = await compile(
      conn,
      ({ state }, x: number) => {
        state.history.push(x);
        return state.history.length;
      },
      { state },
      [t.double],
    );
    assert.equal(await conn.run(fn, 1), 1);
    assert.equal(await conn.run(fn, 2), 2);
    const other = { values: [] as number[] };
    const read = await compile(conn, ({ other, t }) => t.list(t.double).cast(other.values).length, {
      other,
      t,
    });
    assert.equal(await conn.run(read), 0);
  });
});

describe("other statements", () => {
  it("returns from a try, and keeps the closest object", async () => {
    await onBothSides(
      conn,
      (_: object, x: number) => {
        try {
          if (x > 0) {
            throw new RangeError("positive");
          }
          return "none";
        } catch (e) {
          return (e as Error).message;
        }
      },
      [t.double],
      [[1], [-1]],
    );
    const a = await conn.testService.createTestObject("a");
    const b = await conn.testService.createTestObject("b");
    await a.setIntProperty(1);
    await b.setIntProperty(5);
    const best = await run(
      conn,
      ({ objects }) => {
        let best = null;
        for (const o of objects) {
          if (best == null || o.intProperty > best.intProperty) {
            best = o;
          }
        }
        return best;
      },
      { objects: [a, b] },
    );
    assert.equal(best, b);
  });
});

describe("local functions", () => {
  it("types parameters from the calls", async () => {
    await onBothSides(
      conn,
      (_: object, x: number) => {
        const ratio = (part: number, total: number) => part / total;
        function twice(y: number) {
          return y * 2;
        }
        return ratio(x, 4) + twice(x) + [1, 2].map(twice)[1];
      },
      [t.double],
      [[2]],
    );
  });

  it("rejects recursion and differing argument types", async () => {
    await rejects(
      (_, x: number) => {
        function f(y: number): number {
          return y > 0 ? f(y - 1) : 0;
        }
        return f(x);
      },
      {},
      /f calls itself/,
      [t.double],
    );
    await rejects(
      (_, x: number) => {
        const f = (y: any) => y;
        f(x);
        return f("a");
      },
      {},
      /f is called with a string here, and a double in another call/,
      [t.double],
    );
  });

  it("checks the variables a function reads at each call", async () => {
    await rejects(
      (_, x: number) => {
        const double = () => offset * 2;
        const result = double();
        const offset = x;
        return result;
      },
      {},
      /offset/,
      [t.double],
    );
    // A function that calls another reads what the other reads
    await rejects(
      (_, c: boolean) => {
        let y: number;
        function f() {
          return y;
        }
        function g() {
          return f();
        }
        if (c) {
          y = 1;
          f();
          return g();
        }
        return g();
      },
      {},
      /g reads the local variable y/,
      [t.bool],
    );
  });
});

describe("exceptions", () => {
  it("throws and catches errors", async () => {
    await assert.rejects(
      run(conn, () => {
        throw new RangeError("too big");
      }),
      (error: unknown) => error instanceof RangeError && /too big/.test(String(error)),
    );
    const s = conn.testService;
    const handled = await run(
      conn,
      ({ s }) => {
        let result = "";
        for (const n of [0, 1, 2]) {
          try {
            if (n === 0) {
              s.throwArgumentOutOfRangeException(1);
            } else if (n === 1) {
              s.throwArgumentException();
            } else {
              throw new Error("plain");
            }
          } catch (e) {
            if (e instanceof RangeError) {
              result += "range;";
            } else if (e instanceof TypeError) {
              result += "type;";
            } else {
              result += `other ${(e as Error).message};`;
            }
          } finally {
            result += "!";
          }
        }
        return result;
      },
      { s },
    );
    assert.equal(handled, "range;!type;!other plain;!");
  });

  it("rethrows an error the handler does not handle", async () => {
    const s = conn.testService;
    await assert.rejects(
      run(
        conn,
        ({ s }) => {
          try {
            s.throwArgumentException();
          } catch (e) {
            if (e instanceof RangeError) {
              return 1;
            } else {
              throw e;
            }
          }
          return 0;
        },
        { s },
      ),
      TypeError,
    );
  });
});
