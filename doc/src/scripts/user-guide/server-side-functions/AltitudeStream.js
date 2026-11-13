import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const flight = await vessel.flight();
// [docs]
// The vessel's altitude, in kilometers, computed on the server
const stream = await conn.addStream(({ flight }) => flight.meanAltitude / 1000, { flight });
console.log("Altitude:", await stream.get(), "km");
// [/docs]
