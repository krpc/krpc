import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "MechJeb Example" });
const vessel = await conn.spaceCenter.getActiveVessel();
const ascent = await conn.mechJeb.getAscentAutopilot();

await ascent.setDesiredOrbitAltitude(100000);
await ascent.setDesiredInclination(6);
await ascent.setAutostage(true);
await ascent.setEnabled(true);

await (await vessel.getControl()).activateNextStage();

while (await ascent.getEnabled()) {
  await new Promise((resolve) => setTimeout(resolve, 1000));
}

console.log("Launch complete");
