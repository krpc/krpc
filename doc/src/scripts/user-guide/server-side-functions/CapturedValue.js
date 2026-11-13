import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const threshold = 50000;
const event = await conn.addEvent(
  ({ vessel, threshold }) => vessel.orbit.apoapsisAltitude > threshold * 2,
  { vessel, threshold },
);
// [/docs]
await event.remove();
