import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const flight = await vessel.flight();

// [docs]
const event = await conn.addEvent(
  ({ flight }) => flight.meanAltitude > 10000,
  { flight },
);
await event.wait();
console.log("Above 10 km");
// [/docs]
await conn.close();
