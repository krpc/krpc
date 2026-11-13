// The function compiler, against a TestServer. One test per construct the compiler accepts
import assert from "node:assert/strict";
import { after, before, describe, it } from "node:test";
import type { DynamicConnection } from "../src/connection.js";
import { CompilationError } from "../src/errors.js";
import { TestEnum, TestFlags } from "../src/services/TestService.js";
import { type TypeLike, types as t } from "../src/typevalues.js";
import { compile, connectToServer, onBothSides, run, tree } from "./harness.js";

let conn: DynamicConnection;

before(async () => {
  conn = await connectToServer();
});

after(async () => {
  await conn.close();
});

/** Checks that compiling a function raises a CompilationError whose message matches. */
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

describe("constants", () => {
  it("folds operators on constants", async () => {
    assert.equal(await run(conn, ({ limit }) => limit * 2 + 1, { limit: 5 }), 11);
    assert.equal(await run(conn, () => "fo" + "o"), "foo");
    assert.equal(await run(conn, () => 7 / 2), 3.5);
    assert.equal(await run(conn, () => 7n / 2n), 3n);
    assert.equal(await run(conn, () => true), true);
    assert.equal(await run(conn, () => `a${1 + 1}b`), "a2b");
  });

  it("reads the properties of a captured plain object on the client", async () => {
    assert.equal(
      await run(conn, ({ limits }) => limits.max - limits.min, { limits: { min: 2, max: 9 } }),
      7,
    );
  });

  it("reads a generated module's enumeration on the client", async () => {
    assert.equal(await run(conn, ({ TestEnum }) => TestEnum.valueB, { TestEnum }), TestEnum.valueB);
  });

  it("compiles a captured object as a constant", async () => {
    const obj = await conn.testService.createTestObject("constant");
    assert.equal(await run(conn, ({ obj }) => obj, { obj }), obj);
  });
});

describe("members", () => {
  it("reads a property of a service", async () => {
    await conn.testService.setStringProperty("foo");
    const service = conn.testService;
    assert.equal(
      await run(conn, ({ service }) => service.stringProperty === "foo", { service }),
      true,
    );
    assert.equal(await run(conn, ({ conn }) => conn.testService.stringProperty, { conn }), "foo");
  });

  it("reads a property of an object, each time the function runs", async () => {
    const obj = await conn.testService.createTestObject("property");
    await obj.setIntProperty(3);
    const fn = await compile(conn, ({ obj }) => obj.intProperty * 2, { obj });
    assert.equal(await conn.run(fn), 6);
    await obj.setIntProperty(4);
    assert.equal(await conn.run(fn), 8);
  });

  it("calls methods with arguments by position, by name and by default", async () => {
    const obj = await conn.testService.createTestObject("methods");
    assert.equal(
      await run(conn, ({ obj }) => obj.floatToString(0.5), { obj }),
      await obj.floatToString(0.5),
    );
    assert.equal(
      await run(conn, ({ obj }) => obj.optionalArguments("a", { z: "c" }), { obj }),
      await obj.optionalArguments("a", { z: "c" }),
    );
    assert.equal(
      await run(conn, ({ s }) => s.optionalArguments("x"), { s: conn.testService }),
      await conn.testService.optionalArguments("x"),
    );
  });

  it("calls a static method of a class", async () => {
    const { TestClass } = conn.testService;
    assert.equal(
      await run(conn, ({ TestClass }) => TestClass.staticMethod("a", "b"), { TestClass }),
      await TestClass.staticMethod("a", "b"),
    );
  });

  it("chains calls", async () => {
    const obj = await conn.testService.createTestObject("chain");
    await obj.setObjectProperty(obj);
    assert.equal(
      await run(conn, ({ obj }) => obj.objectProperty.getValue(), { obj }),
      "value=chain",
    );
  });

  it("compares objects", async () => {
    const obj = await conn.testService.createTestObject("equal");
    const other = await conn.testService.createTestObject("other");
    assert.equal(
      await run(conn, ({ s, obj }) => s.echoTestObject(obj) === obj, { s: conn.testService, obj }),
      true,
    );
    assert.equal(
      await run(conn, ({ s, obj, other }) => s.echoTestObject(obj) === other, {
        s: conn.testService,
        obj,
        other,
      }),
      false,
    );
  });

  it("names the property form of a getter", async () => {
    const obj = await conn.testService.createTestObject("getter");
    await rejects(
      ({ obj }) => obj.getIntProperty(),
      { obj },
      /read the property, as in \.intProperty in place of \.getIntProperty\(\)/,
    );
    await rejects(({ obj }) => obj.intProperty(), { obj }, /is a property/);
    await rejects(({ obj }) => obj.floatToString, { obj }, /is a method/);
    await rejects(({ obj }) => obj.missing, { obj }, /has no property missing/);
  });

  it("rejects an argument of the wrong type", async () => {
    const obj = await conn.testService.createTestObject("types");
    await rejects(
      ({ obj }) => obj.floatToString("x"),
      { obj },
      /Cannot use a string as a float for argument x/,
    );
  });
});

describe("numbers", () => {
  it("computes arithmetic in doubles", async () => {
    const obj = await conn.testService.createTestObject("arithmetic");
    await obj.setIntProperty(7);
    assert.equal(await run(conn, ({ obj }) => obj.intProperty / 2, { obj }), 3.5);
    assert.equal(await run(conn, ({ obj }) => obj.intProperty % 4, { obj }), 3);
    assert.equal(await run(conn, ({ obj }) => obj.intProperty ** 2, { obj }), 49);
    assert.equal(await run(conn, ({ obj }) => -obj.intProperty, { obj }), -7);
  });

  it("gives the results JavaScript gives", async () => {
    // V8 computes some powers, such as 7 ** -2, one bit from the correctly rounded result
    const values = [
      [7, 2],
      [-7, 2],
      [7, -1],
      [0.5, 0],
      [0, 0],
      [-0.5, 3],
      [1e300, 1e10],
    ];
    for (const fn of [
      (_: object, x: number, y: number) => x + y,
      (_: object, x: number, y: number) => x - y,
      (_: object, x: number, y: number) => x * y,
      (_: object, x: number, y: number) => x / y,
      (_: object, x: number, y: number) => x % y,
      (_: object, x: number, y: number) => x ** y,
    ]) {
      await onBothSides(conn, fn, [t.double, t.double], values);
    }
  });

  it("applies the bitwise operators to 32-bit integers", async () => {
    const values = [
      [5, 3],
      [-5, 3],
      [2 ** 32 + 5, 1],
      [1.9, 33],
      [-(2 ** 31), 31],
      [2 ** 31, 0],
    ];
    for (const fn of [
      (_: object, x: number, y: number) => x & y,
      (_: object, x: number, y: number) => x | y,
      (_: object, x: number, y: number) => x ^ y,
      (_: object, x: number, y: number) => x << y,
      (_: object, x: number, y: number) => x >> y,
      (_: object, x: number, y: number) => x >>> y,
    ]) {
      await onBothSides(conn, fn, [t.double, t.double], values);
    }
    await onBothSides(
      conn,
      (_: object, x: number) => ~x,
      [t.double],
      values.map(([x]) => [x]),
    );
  });

  it("keeps bigint arithmetic in integers", async () => {
    await onBothSides(
      conn,
      (_: object, x: bigint, y: bigint) => (x / y) * y + (x % y),
      [t.int64, t.int64],
      [
        [7n, 2n],
        [-7n, 2n],
      ],
    );
    await onBothSides(
      conn,
      (_: object, x: bigint, y: bigint) => x & (y | 1n),
      [t.int64, t.int64],
      [[6n, 2n]],
    );
    await rejects((_, x) => x + 1, {}, /A bigint and a number cannot be mixed/, [t.int64]);
  });

  it("shifts a bigint by any count", async () => {
    // Counts whose results fit in 64 bits, as JavaScript's have no limit
    const within = [
      [8n, 3n],
      [8n, -2n],
      [-8n, -2n],
    ];
    await onBothSides(
      conn,
      (_: object, x: bigint, y: bigint) => x >> y,
      [t.int64, t.int64],
      [...within, [8n, 64n], [-8n, 64n]],
    );
    await onBothSides(
      conn,
      (_: object, x: bigint, y: bigint) => x << y,
      [t.int64, t.int64],
      [...within, [8n, -70n], [-8n, -70n]],
    );
    // A bigint is a 64-bit integer, so every bit shifts out
    const left = await compile(conn, (_, x: bigint, y: bigint) => x << y, {}, [t.int64, t.int64]);
    assert.equal(await conn.run(left, 1n, 64n), 0n);
    assert.equal(await run(conn, ({ x }) => x << -1n, { x: 4n }), 2n);
  });

  it("converts an integer to a bigint", async () => {
    await onBothSides(conn, (_: object, x: number) => BigInt(x), [t.int32], [[-3]]);
    await rejects((_, x: number) => BigInt(x), {}, /BigInt of a double/, [t.double]);
  });

  it("converts a number to an integer parameter toward zero", async () => {
    assert.equal(await run(conn, ({ s }) => s.int32ToString(2.9), { s: conn.testService }), "2");
    await rejects(
      ({ s }) => s.int32ToString(2 ** 40),
      { s: conn.testService },
      /out of range for a int32/,
    );
  });

  it("compares numbers", async () => {
    const values = [
      [1, 2],
      [2, 1],
      [1, 1],
      [Number.NaN, 1],
    ];
    for (const fn of [
      (_: object, x: number, y: number) => x < y,
      (_: object, x: number, y: number) => x <= y,
      (_: object, x: number, y: number) => x > y,
      (_: object, x: number, y: number) => x >= y,
      (_: object, x: number, y: number) => x === y,
      (_: object, x: number, y: number) => x !== y,
    ]) {
      await onBothSides(conn, fn, [t.double, t.int32], values);
    }
  });
});

describe("truth", () => {
  it("tests values as JavaScript does", async () => {
    await onBothSides(
      conn,
      (_: object, x: number) => !x,
      [t.double],
      [[0], [1], [Number.NaN], [-0.5]],
    );
    await onBothSides(conn, (_: object, x: string) => !x, [t.string], [[""], ["a"]]);
    await onBothSides(conn, (_: object, x: number) => (x ? "yes" : "no"), [t.int32], [[0], [3]]);
    await onBothSides(
      conn,
      (_: object, x: number | null) => !x,
      [t.nullable(t.double)],
      [[null], [0], [2]],
    );
  });

  it("gives the value of && and || as JavaScript does", async () => {
    await onBothSides(
      conn,
      (_: object, x: number, y: number) => x && y,
      [t.double, t.double],
      [
        [0, 2],
        [1, 2],
      ],
    );
    await onBothSides(
      conn,
      (_: object, x: number, y: number) => x || y,
      [t.double, t.double],
      [
        [0, 2],
        [1, 2],
      ],
    );
    await onBothSides(
      conn,
      (_: object, x: boolean, y: boolean) => x && !y,
      [t.bool, t.bool],
      [
        [true, false],
        [false, false],
      ],
    );
  });

  it("gives a value for null with ??", async () => {
    await onBothSides(
      conn,
      (_: object, x: number | null) => x ?? 5,
      [t.nullable(t.double)],
      [[null], [2]],
    );
    await onBothSides(
      conn,
      (_: object, x: string | null) => x ?? "none",
      [t.nullable(t.string)],
      [[null], ["a"]],
    );
  });

  it("joins the branches of a conditional", async () => {
    await onBothSides(
      conn,
      (_: object, x: boolean, y: number) => (x ? y : null),
      [t.bool, t.int32],
      [
        [true, 3],
        [false, 3],
      ],
    );
    await onBothSides(
      conn,
      (_: object, x: boolean, y: number) => (x ? y : 0.5),
      [t.bool, t.int32],
      [
        [true, 3],
        [false, 3],
      ],
    );
  });

  it("types a null with cast", async () => {
    assert.equal(await run(conn, ({ t }) => t.nullable(t.double).cast(null), { t }), null);
    await rejects(({ t }) => t.double.cast(null), { t }, /A double cannot be null/);
  });

  it("compares with null", async () => {
    const obj = await conn.testService.createTestObject("nullcheck");
    await obj.setObjectProperty(null);
    // biome-ignore lint/suspicious/noDoubleEquals: == null is the test
    assert.equal(await run(conn, ({ obj }) => obj.objectProperty == null, { obj }), true);
    assert.equal(await run(conn, ({ limit }) => limit === null, { limit: null }), true);
    // biome-ignore lint/suspicious/noDoubleEquals: != undefined is the test
    assert.equal(await run(conn, ({ obj }) => obj.objectProperty != undefined, { obj }), false);
    // The server has one null, so a strict comparison cannot tell it from undefined
    await rejects(({ obj }) => obj.objectProperty === null, { obj }, /using == null/);
    await rejects(({ obj }) => obj.objectProperty !== undefined, { obj }, /using == null/);
  });
});

describe("optional chains", () => {
  it("reads a member of a value that can be null", async () => {
    const obj = await conn.testService.createTestObject("chained");
    await obj.setObjectProperty(null);
    const fn = await compile(conn, ({ obj }) => obj.objectProperty?.intProperty, { obj });
    assert.equal(await conn.run(fn), null);
    await obj.setObjectProperty(obj);
    await obj.setIntProperty(9);
    assert.equal(await conn.run(fn), 9);
  });
});

describe("strings", () => {
  it("concatenates strings and the strings of other values", async () => {
    await onBothSides(
      conn,
      // biome-ignore lint/style/useTemplate: + is the test
      (_: object, x: string, y: number, z: boolean) => x + y + z + `${y}!`,
      [t.string, t.int32, t.bool],
      [["a", 1, true]],
    );
    await onBothSides(
      conn,
      (_: object, x: string | null) => `${x}`,
      [t.nullable(t.string)],
      [[null], ["a"]],
    );
    await onBothSides(conn, (_: object, x: number) => String(x), [t.int32], [[-3]]);
  });

  it("measures, indexes and slices strings", async () => {
    const values = [["hello"], [""], ["ab"]];
    for (const fn of [
      (_: object, s: string) => s.length,
      (_: object, s: string) => s.slice(1),
      (_: object, s: string) => s.slice(-2),
      (_: object, s: string) => s.slice(1, -1),
      (_: object, s: string) => s.slice(3, 1),
      (_: object, s: string) => s.substring(4, 1),
      (_: object, s: string) => s.substring(-1, 2),
    ]) {
      await onBothSides(conn, fn, [t.string], values);
    }
    await onBothSides(conn, (_: object, s: string) => s[1] + s.at(-1), [t.string], [["hello"]]);
    // An index is truncated toward zero, with NaN as zero
    for (const fn of [
      (_: object, s: string, i: number) => s.slice(i),
      (_: object, s: string, i: number) => s.slice(-i),
      (_: object, s: string, i: number) => s.substring(i, 3),
    ]) {
      await onBothSides(
        conn,
        fn,
        [t.string, t.double],
        [
          ["abcde", 1.5],
          ["abcde", 0.5],
          ["abcde", Number.NaN],
        ],
      );
    }
    await onBothSides(conn, (_: object, s: string) => s.at(-0.5), [t.string], [["abc"]]);
    await rejects((_, s: string) => s[1.5], {}, /1\.5 is not an index/, [t.string]);
  });

  it("calls string methods", async () => {
    const values = [["  Hello World  "]];
    for (const fn of [
      (_: object, s: string) => s.toUpperCase(),
      (_: object, s: string) => s.toLowerCase(),
      (_: object, s: string) => s.trim(),
      (_: object, s: string) => s.trimStart(),
      (_: object, s: string) => s.trimEnd(),
      (_: object, s: string) => s.includes("World"),
      (_: object, s: string) => s.startsWith("  H"),
      (_: object, s: string) => s.endsWith("x"),
      (_: object, s: string) => s.indexOf("o"),
      (_: object, s: string) => s.replaceAll("o", "0"),
    ]) {
      await onBothSides(conn, fn, [t.string], values);
    }
    await rejects((_, s: string) => s.replace("a", "b"), {}, /Use replaceAll/, [t.string]);
    // The server applies no replacement patterns, and matches no empty string
    await rejects((_, s: string) => s.replaceAll("a", "$&!"), {}, /with \$/, [t.string]);
    await rejects((_, s: string) => s.replaceAll("", "-"), {}, /empty string/, [t.string]);
    await rejects(
      (_, s: string, r: string) => s.replaceAll("a", r),
      {},
      /must be a string constant/,
      [t.string, t.string],
    );
  });
});

describe("Math", () => {
  it("computes Math functions with StdLib", async () => {
    const values = [[2.5], [-2.5], [0.5], [-0.5], [3], [Number.NaN]];
    for (const fn of [
      (_: object, x: number) => Math.abs(x),
      (_: object, x: number) => Math.floor(x),
      (_: object, x: number) => Math.ceil(x),
      (_: object, x: number) => Math.round(x),
      (_: object, x: number) => Math.trunc(x),
      (_: object, x: number) => Math.max(x, 1, -1),
      (_: object, x: number) => Math.min(x, 1),
      (_: object, x: number) => x ** 2,
      (_: object, x: number) => Math.fround(x),
    ]) {
      await onBothSides(conn, fn, [t.double], values);
    }
    await onBothSides(
      conn,
      (_: object, x: number) => Math.sqrt(x) + Math.hypot(x, 4),
      [t.double],
      [[9]],
    );
    assert.equal(await run(conn, () => Math.PI * 2), Math.PI * 2);
    await onBothSides(conn, (_: object, x: number) => Math.sign(x), [t.double], values);
    // The server's base 2 logarithm is inexact, so only a constant has one
    assert.equal(await run(conn, () => Math.log2(2 ** 29)), 29);
    await rejects((_, x: number) => Math.log2(x), {}, /Math\.log2 of a value/, [t.double]);
    await rejects(() => Math.random(), {}, /Math\.random is not supported/);
  });

  it("prints with console.log", async () => {
    const obj = await conn.testService.createTestObject("print");
    assert.equal(
      await run(conn, ({ obj }) => console.log("value", obj.intProperty, true), { obj }),
      undefined,
    );
  });
});

describe("flags", () => {
  it("combines flags with operators and methods", async () => {
    const s = conn.testService;
    const { valueA, valueB } = TestFlags;
    assert.equal(
      await run(conn, ({ s, valueA, valueB }) => s.flagsEcho(valueA) | valueB, {
        s,
        valueA,
        valueB,
      }),
      TestFlags.valueAB,
    );
    assert.equal(
      await run(conn, ({ s, valueA, valueB }) => s.flagsEcho(valueA).union(valueB), {
        s,
        valueA,
        valueB,
      }),
      TestFlags.valueAB,
    );
    assert.equal(
      await run(
        conn,
        ({ s, valueA, TestFlags }) => s.flagsEcho(TestFlags.valueAB).contains(valueA),
        { s, valueA, TestFlags },
      ),
      true,
    );
  });
});

describe("parameters", () => {
  it("compiles a function with parameters", async () => {
    const fn = await compile(
      conn,
      ({ s }, x: number) => s.int32ToString(x * 2),
      { s: conn.testService },
      [t.int32],
    );
    assert.equal(await conn.run(fn, 21), "42");
  });

  it("takes a service's class as a parameter type", async () => {
    const obj = await conn.testService.createTestObject("parameter");
    const fn = await compile(conn, (_, o) => o.getValue(), {}, [conn.testService.TestClass]);
    assert.equal(await conn.run(fn, obj), "value=parameter");
  });
});

describe("streams and events", () => {
  it("streams a compiled function", async () => {
    const obj = await conn.testService.createTestObject("stream");
    await obj.setIntProperty(5);
    const stream = await conn.addStream(({ obj }) => obj.intProperty + 1, { obj });
    assert.equal(await stream.get(), 6);
    await stream.remove();
  });

  it("adds an event over a compiled function", async () => {
    const obj = await conn.testService.createTestObject("event");
    await obj.setIntProperty(1);
    const event = await conn.addEvent(({ obj }) => obj.intProperty > 0, { obj });
    await event.remove();
    await assert.rejects(
      conn.addEvent(({ obj }) => obj.intProperty, { obj }),
      /returns a bool/,
    );
    await assert.rejects(
      conn.addStream(({ x }) => x + 1, { x: 1 }),
      /same on every update/,
    );
  });
});

describe("errors", () => {
  it("names the construct and its position", async () => {
    await rejects(
      () => missing,
      {},
      /missing is not a capture or a parameter.*\(line 1, column 7 of the function\)/,
    );
    await rejects(({ a }) => a, {}, /The captured values have no a/);
    await rejects((x: number) => x, {}, /object pattern/);
    await rejects(() => [1, 2], {}, /An array is not supported/);
    await rejects(async () => 1, {}, /An async function cannot be compiled/);
    await rejects(({ f }) => f(), { f: () => 1 }, /cannot be called in a compiled function/);
    await rejects(() => null, {}, /null has no type here/);
    await rejects((_, a: string, b: string) => a < b, {}, /cannot be ordered/, [
      t.string,
      t.string,
    ]);
  });
});

declare const missing: number;

describe("trees", () => {
  it("builds the tree of a property comparison", async () => {
    const obj = await conn.testService.createTestObject("tree");
    const expected = [
      "GreaterThan<System.Boolean>",
      "  Block<System.Int32>",
      "    Call<System.Void> static KRPC.Service.Services.CheckExpressionAvailable",
      "      Constant<KRPC.Service.Scanner.ProcedureSignature> TestService.TestClass_get_IntProperty",
      "    Call<System.Int32> TestServer.TestService.TestClass.get_IntProperty",
      "      Constant<TestServer.TestService.TestClass> <TestServer.TestService.TestClass>",
      "  Constant<System.Int32> 1000",
      "",
    ];
    const fn = ({ obj, limit }: any) => obj.intProperty > limit;
    assert.equal(await tree(conn, fn, { obj, limit: 1000 }), expected.join("\n"));
  });

  it("builds the tree of a function with a parameter", async () => {
    const expected = [
      "Lambda<System.Func<System.Int32, System.String>> (x#0)",
      "  Label<System.String> label#0",
      "    Conditional<System.String>",
      "      Not<System.Boolean>",
      "        Equal<System.Boolean>",
      "          Parameter<System.Int32> x#0",
      "          Constant<System.Int32> 0",
      "      Call<System.String> static System.String.Concat",
      "        NewArrayInit<System.String[]>",
      "          Call<System.String> static KRPC.Service.KRPC.Expression.ConvertToStringHelper",
      "            Convert<System.Object>",
      "              Parameter<System.Int32> x#0",
      '          Constant<System.String> "!"',
      '      Constant<System.String> "none"',
      "",
    ];
    const fn = (_: object, x: number) => (x ? `${x}!` : "none");
    assert.equal(await tree(conn, fn, {}, [t.int32]), expected.join("\n"));
  });
});
