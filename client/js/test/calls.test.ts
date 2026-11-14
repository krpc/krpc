// Members passed where a call is expected
import assert from "node:assert/strict";
import { after, before, describe, it } from "node:test";
import type { DynamicConnection } from "../dist/connection.js";
import { connectToServer } from "./harness.js";

let conn: DynamicConnection;

before(async () => {
  conn = await connectToServer();
});

after(async () => {
  await conn.close();
});

describe("calls", () => {
  it("takes a member function as its call template", async () => {
    const { Expression } = conn.krpc;
    const service = conn.testService;
    const { TestClass } = service;
    const instance = await Expression.call(conn.getCall(() => service.createTestObject("member")));
    const floatToString = await Expression.callWithArguments(
      TestClass.prototype.floatToString,
      new Map([
        [0, instance],
        [1, await Expression.constantFloat(2.5)],
      ]),
    );
    assert.equal(await conn.run(floatToString), "member2.5");
    const intProperty = await Expression.callWithArguments(
      TestClass.prototype.getIntProperty,
      new Map([[0, instance]]),
    );
    assert.equal(await conn.run(intProperty), 0);
    const staticMethod = await Expression.callWithArguments(
      TestClass.staticMethod,
      new Map([[0, await Expression.constantString("a")]]),
    );
    assert.equal(await conn.run(staticMethod), "statica");
    const procedure = await Expression.callWithArguments(
      service.int32ToString,
      new Map([[0, await Expression.constantInt(3)]]),
    );
    assert.equal(await conn.run(procedure), "3");
    assert.deepEqual(
      conn.getCallTemplate(TestClass.prototype.getIntProperty),
      conn.getCallTemplate(TestClass.prototype.getIntProperty),
    );
    assert.throws(() => conn.getCallTemplate(() => 1), TypeError);
  });
});
