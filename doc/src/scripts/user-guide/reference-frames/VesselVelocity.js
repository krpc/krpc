import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Vessel velocity" });
const vessel = await conn.spaceCenter.getActiveVessel();
const body = await (await vessel.getOrbit()).getBody();
const refFrame = await conn.spaceCenter.ReferenceFrame.createHybrid(
  await body.getReferenceFrame(),
  { rotation: await vessel.getSurfaceReferenceFrame() },
);
const flight = await vessel.flight(refFrame);

while (true) {
  const velocity = await flight.getVelocity();
  const components = velocity.map((x) => x.toFixed(1)).join(", ");
  console.log(`Surface velocity = (${components})`);
  await new Promise((resolve) => setTimeout(resolve, 1000));
}
