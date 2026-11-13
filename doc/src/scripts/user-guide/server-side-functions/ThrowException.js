import { connect } from "@krpc-org/krpc";

const conn = await connect();
const Expression = conn.krpc.Expression;
const vessel = await conn.spaceCenter.getActiveVessel();
const resources = await vessel.getResources();
// [docs]
const amount = conn.getCall(() => resources.amount("LiquidFuel"));
const expr = await Expression.ifThen(
  await Expression.lessThan(await Expression.call(amount), await Expression.constantDouble(100)),
  await Expression.throw(
    "KRPC",
    "InvalidOperationException",
    await Expression.constantString("not enough fuel"),
  ),
);
await conn.run(expr);
// [/docs]
