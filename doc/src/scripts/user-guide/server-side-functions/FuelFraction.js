import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const stream = await conn.addStream(
  ({ vessel }) => {
    const resources = vessel.resources;
    const amount = resources.amount("LiquidFuel");
    const capacity = resources.max("LiquidFuel");
    return amount / capacity;
  },
  { vessel },
);
// [/docs]
console.log(await stream.get());
