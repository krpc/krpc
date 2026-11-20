import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Launch" });
const vessel = await conn.spaceCenter.getActiveVessel();
const flight = await vessel.flight();
const autoPilot = await vessel.getAutoPilot();
const control = await vessel.getControl();
const name = await vessel.getName();

await autoPilot.targetPitchAndHeading(90, 90);
await autoPilot.setEngaged(true);
await control.setThrottle(1);
await control.activateNextStage();
console.log(`${name} has launched`);

const event = await conn.addEvent(
  ({ flight }) => flight.meanAltitude > 10000,
  { flight },
);
await event.wait();

await control.setThrottle(0);
await autoPilot.setEngaged(false);
console.log(`${name} reached 10 km`);
await conn.close();
