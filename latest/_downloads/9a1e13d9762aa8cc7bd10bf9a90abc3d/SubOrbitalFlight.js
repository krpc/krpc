import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Sub-orbital flight" });

const vessel = await conn.spaceCenter.getActiveVessel();
const autoPilot = await vessel.getAutoPilot();
const control = await vessel.getControl();

await autoPilot.targetPitchAndHeading(90, 90);
await autoPilot.setEngaged(true);
await control.setThrottle(0.5);
await new Promise((resolve) => setTimeout(resolve, 1000));

console.log("Launch!");
await control.activateNextStage();

const flight = await vessel.flight();
const altitudeEvent = await conn.addEvent(
  ({ flight }) => flight.meanAltitude > 500,
  { flight },
);
await altitudeEvent.wait();

console.log("Pitching over");
await autoPilot.setTargetSmoothingTime(10);
await autoPilot.targetPitchAndHeading(70, 90);

const fuelEvent = await conn.addEvent(
  ({ vessel }) => vessel.resources.amount("LiquidFuel") < 0.1,
  { vessel },
);
await fuelEvent.wait();

console.log("Flame out");
await control.setThrottle(0);
await autoPilot.setReferenceFrame(
  await vessel.getSurfaceVelocityReferenceFrame(),
);
await autoPilot.setTargetDirection([0, 1, 0]);

const orbit = await vessel.getOrbit();
const bodyFrame = await (await orbit.getBody()).getReferenceFrame();
const bodyFlight = await vessel.flight(bodyFrame);
const apoapsisEvent = await conn.addEvent(
  ({ bodyFlight }) => bodyFlight.verticalSpeed < 0,
  { bodyFlight },
);
await apoapsisEvent.wait();

console.log("Apoapsis reached, separating");
await control.activateNextStage();
await autoPilot.setTargetDirection([0, -1, 0]);
await autoPilot.wait();
await autoPilot.setEngaged(false);

const surfaceEvent = await conn.addEvent(
  ({ flight }) => flight.surfaceAltitude < 2000,
  { flight },
);
await surfaceEvent.wait();

console.log("Deploying the parachute");
const parachutes = await (await vessel.getParts()).getParachutes();
await parachutes[0].setDeployAltitude(550);
await control.activateNextStage();

const { landed, splashed } = conn.spaceCenter.VesselSituation;
let situation = await vessel.getSituation();
while (situation !== landed && situation !== splashed) {
  const altitude = await flight.getSurfaceAltitude();
  console.log(`Altitude = ${altitude.toFixed(1)} meters`);
  await new Promise((resolve) => setTimeout(resolve, 1000));
  situation = await vessel.getSituation();
}
console.log("Landed!");
await conn.close();
