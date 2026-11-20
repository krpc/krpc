import { connect } from "@krpc-org/krpc";

const conn = await connect();
const spaceCenter = conn.spaceCenter;
const body = (await spaceCenter.getBodies()).get("Kerbin");
// [docs]
const vessel = await (await spaceCenter.getEditor()).getVessel();
const vesselFrame = await vessel.getReferenceFrame();
const flight = await vessel.flight(vesselFrame);
// The vessel frame's z axis points out of the bottom, so up is its -z axis
const side = Math.sqrt(0.5);
await flight.setSurfaceReferenceFrame(
  await spaceCenter.ReferenceFrame.createRelative(vesselFrame, {
    rotation: [0, side, 0, side],
  }),
);
const torque = await flight.buoyantTorqueAt(
  await body.getOceanDensity(),
  await body.getSurfaceGravity(),
);
// [/docs]
const components = torque.map((x) => x.toFixed(0)).join(", ");
console.log(`Torque = (${components}) N m`);
await conn.close();
