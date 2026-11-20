import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "MechJeb Example" });
const vessel = await conn.spaceCenter.getActiveVessel();
const ascent = await conn.mechJeb.getAscentGuidance();

await ascent.setDesiredOrbitAltitude(100000);
await ascent.setDesiredInclination(6);
await (await ascent.getSettings()).setAutostage(true);
await ascent.setEnabled(true);

await (await vessel.getControl()).activateNextStage();

const event = await conn.addEvent(({ ascent }) => !ascent.enabled, {
  ascent,
});
await event.wait();

console.log("Launch complete");
await conn.close();
