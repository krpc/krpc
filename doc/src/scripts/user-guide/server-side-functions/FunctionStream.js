import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const flight = await vessel.flight();

// Build a function that computes
// the vessel's altitude in kilometers
const Expression = conn.krpc.Expression;
const meanAltitude = conn.getCall(() => flight.getMeanAltitude());
const expr = await Expression.divide(
  await Expression.call(meanAltitude),
  await Expression.constantDouble(1000),
);

// Stream the value of the function
/** @type {import("@krpc-org/krpc").Stream<number>} */
const stream = await conn.addStream(expr);
stream.addCallback((x) => console.log(`Altitude: ${x.toFixed(3)} km`));
