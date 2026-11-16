import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();

const activeEngines = [];
for (const engine of await (await vessel.getParts()).getEngines()) {
  if ((await engine.getActive()) && (await engine.getHasFuel())) {
    activeEngines.push(engine);
  }
}

console.log("Active engines:");
for (const engine of activeEngines) {
  const part = await engine.getPart();
  console.log(
    `   ${await part.getTitle()} in stage ${await part.getStage()}`,
  );
}

let thrust = 0;
let fuelConsumption = 0;
for (const engine of activeEngines) {
  const engineThrust = await engine.getThrust();
  thrust += engineThrust;
  fuelConsumption += engineThrust / (await engine.getSpecificImpulse());
}
const isp = thrust / fuelConsumption;

console.log(`Combined vacuum Isp = ${isp.toFixed(0)} seconds`);
await conn.close();
