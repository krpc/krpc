import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const event = await conn.addEvent(
  ({ vessel }) => vessel.parts.engines.some((engine) => !engine.hasFuel),
  { vessel },
);
await event.wait();
console.log("An engine has run out of fuel");
// [/docs]
