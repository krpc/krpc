import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const flight = await vessel.flight();
const Expression = conn.krpc.Expression;

// A function that is true above 1000 meters
const meanAltitude = await Expression.call(conn.getCall(() => flight.getMeanAltitude()));
const above = await Expression.greaterThan(meanAltitude, await Expression.constantDouble(1000));

// An event that fires when it becomes true
const event = await conn.addEvent(above);
await event.wait();
console.log("Altitude reached 1000m");

// A stream of the altitude in kilometers
/** @type {import("@krpc-org/krpc").Stream<number>} */
const kilometers = await conn.addStream(
  await Expression.divide(meanAltitude, await Expression.constantDouble(1000)),
);
console.log(`Altitude: ${(await kilometers.get()).toFixed(3)} km`);
