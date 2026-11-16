import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Sub-orbital flight" });

const vessel = await conn.spaceCenter.getActiveVessel();
const autoPilot = await vessel.getAutoPilot();
const control = await vessel.getControl();

await autoPilot.targetPitchAndHeading(90, 90);
await autoPilot.setEngaged(true);
await control.setThrottle(1);
await new Promise((resolve) => setTimeout(resolve, 1000));

console.log("Launch!");
await control.activateNextStage();

const fuelEvent = await conn.addEvent(
  ({ vessel }) => vessel.resources.amount("SolidFuel") < 0.1,
  { vessel },
);
await fuelEvent.wait();
console.log("Booster separation");
await control.activateNextStage();

const flight = await vessel.flight();
const altitudeEvent = await conn.addEvent(
  ({ flight }) => flight.meanAltitude > 10000,
  { flight },
);
await altitudeEvent.wait();

console.log("Gravity turn");
await autoPilot.targetPitchAndHeading(60, 90);

const apoapsisEvent = await conn.addEvent(
  ({ vessel }) => vessel.orbit.apoapsisAltitude > 100000,
  { vessel },
);
await apoapsisEvent.wait();

console.log("Launch stage separation");
await control.setThrottle(0);
await new Promise((resolve) => setTimeout(resolve, 1000));
await control.activateNextStage();
await autoPilot.setEngaged(false);

const surfaceEvent = await conn.addEvent(
  ({ flight }) => flight.surfaceAltitude < 1000,
  { flight },
);
await surfaceEvent.wait();

await control.activateNextStage();

const orbit = await vessel.getOrbit();
const bodyFrame = await (await orbit.getBody()).getReferenceFrame();
const bodyFlight = await vessel.flight(bodyFrame);
while ((await bodyFlight.getVerticalSpeed()) < -0.1) {
  const altitude = await flight.getSurfaceAltitude();
  console.log(`Altitude = ${altitude.toFixed(1)} meters`);
  await new Promise((resolve) => setTimeout(resolve, 1000));
}
console.log("Landed!");
await conn.close();
