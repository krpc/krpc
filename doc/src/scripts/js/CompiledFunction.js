import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const body = await (await vessel.getOrbit()).getBody();
const flight = await vessel.flight(await body.getReferenceFrame());

// The altitude the vessel would coast up to with no drag, from its altitude and speed
const gravity = 9.81;
const apex = await conn.run(
  ({ flight, gravity }) => flight.meanAltitude + flight.speed ** 2 / (2 * gravity),
  { flight, gravity },
);
console.log(`Apex: ${apex.toFixed(1)} m`);
