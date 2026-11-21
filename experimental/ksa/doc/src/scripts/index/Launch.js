import { connect } from "@krpc-org/krpc";
import "./KSA.js";

const conn = await connect({ name: "Launch" });
const vehicle = await conn.ksa.launchVehicle("Rocket", "CCSFS LC-39A");
const flight = await vehicle.getFlight();
const autoPilot = await vehicle.getAutoPilot();
const control = await vehicle.getControl();
const name = await vehicle.getName();

await autoPilot.targetPitchAndHeading(90, 90);
await autoPilot.engage();
await control.setThrottle(1);
await control.activateNextSequence();
await control.setEngineOn(true);
console.log(`${name} has launched`);

const event = await conn.addEvent(
  ({ flight }) => flight.meanAltitude > 10000,
  { flight },
);
await event.wait();

await control.setEngineOn(false);
await autoPilot.disengage();
console.log(`${name} reached 10 km`);
await conn.close();
