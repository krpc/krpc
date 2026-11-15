import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Trajectories Example" });
const vessel = await conn.spaceCenter.getActiveVessel();
const body = await (await vessel.getOrbit()).getBody();
console.log(
  await conn.trajectories.impactPosition(await body.getReferenceFrame()),
);
