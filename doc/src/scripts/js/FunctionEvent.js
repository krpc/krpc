import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const flight = await vessel.flight();

// An event that fires when the vessel climbs above 1000 meters
const event = await conn.addEvent(({ flight }) => flight.meanAltitude > 1000, { flight });
await event.wait();
console.log("Altitude reached 1000m");

// A stream of the altitude in kilometers
/** @type {import("@krpc-org/krpc").Stream<number>} */
const kilometers = await conn.addStream(({ flight }) => flight.meanAltitude / 1000, { flight });
console.log(`Altitude: ${(await kilometers.get()).toFixed(3)} km`);
