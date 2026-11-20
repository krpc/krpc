import { connect, getObjectId } from "@krpc-org/krpc";

const conn = await connect();
const Expression = conn.krpc.Expression;
const [someVessel] = await conn.spaceCenter.getVessels();
// [docs]
const target = conn.getCall(() => conn.spaceCenter.getTargetVessel());
const expr = await Expression.equal(
  await Expression.call(target),
  await Expression.constantObject(getObjectId(someVessel)),
);
const event = await conn.addEvent(expr);
// [/docs]
await event.wait();
console.log("Target set");
