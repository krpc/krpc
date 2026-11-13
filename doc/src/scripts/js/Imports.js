import { connect, SpaceCenter } from "@krpc-org/krpc";

/** Points a vessel with its SAS, prograde by default.
 * @param {SpaceCenter.Vessel} vessel
 * @param {SpaceCenter.SASMode} mode */
async function point(vessel, mode = SpaceCenter.SASMode.prograde) {
  const control = await vessel.getControl();
  await control.setSAS(true);
  await control.setSASMode(mode);
}

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
await point(vessel);

// The connection's values are the package's values
const control = await vessel.getControl();
console.log((await control.getSASMode()) === SpaceCenter.SASMode.prograde);
