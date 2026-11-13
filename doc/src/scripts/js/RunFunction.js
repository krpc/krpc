import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const body = await (await vessel.getOrbit()).getBody();
const flight = await vessel.flight(await body.getReferenceFrame());

// Get the remote procedure calls as messages, so they can be passed to the server
const meanAltitude = conn.getCall(() => flight.getMeanAltitude());
const speed = conn.getCall(() => flight.getSpeed());

// Build a function that reads the altitude and the speed
const Expression = conn.krpc.Expression;
const expr = await Expression.createTuple([
  await Expression.call(meanAltitude),
  await Expression.call(speed),
]);

// Run the function once, so both values come from the same physics tick
const [altitude, velocity] = /** @type {[number, number]} */ (await conn.run(expr));
console.log(`Altitude: ${altitude.toFixed(1)} m, speed: ${velocity.toFixed(1)} m/s`);
