import { connect } from "@krpc-org/krpc";

const conn = await connect();
const { Expression, Type } = conn.krpc;

// Build a function that doubles its parameter
const x = await Expression.parameter("x", await Type.double());
const fn = await conn.compile(
  await Expression.lambda([x], await Expression.multiply(x, await Expression.constantDouble(2))),
);

console.log(`Result: ${await conn.run(fn, 21)}`);
