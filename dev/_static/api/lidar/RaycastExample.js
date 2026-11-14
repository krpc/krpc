import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Raycast Example" });
const sc = conn.spaceCenter;
const vessel = await sc.getActiveVessel();
const part = await (await vessel.getParts()).getRoot();
const frame = await part.getReferenceFrame();

// A fan of 8 rays below the part, 10 degrees apart
for (let i = 0; i < 8; i++) {
  const angle = ((10 * i - 35) * Math.PI) / 180;
  const distance = await sc.raycastDistance(
    [0, 0, 0],
    [Math.sin(angle), -Math.cos(angle), 0],
    frame,
  );
  console.log(distance);
}
