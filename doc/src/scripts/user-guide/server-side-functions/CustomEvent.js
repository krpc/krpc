import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const flight = await vessel.flight();

// Get the remote procedure call as a message,
// so it can be passed to the server
const meanAltitude = conn.getCall(() => flight.getMeanAltitude());

// Build the function on the server
const Expression = conn.krpc.Expression;
const expr = await Expression.greaterThan(
  await Expression.call(meanAltitude),
  await Expression.constantDouble(1000),
);

const event = await conn.addEvent(expr);
await event.wait();
console.log("Altitude reached 1000m");
