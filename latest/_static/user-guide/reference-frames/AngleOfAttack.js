import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Angle of attack" });
const vessel = await conn.spaceCenter.getActiveVessel();
const body = await (await vessel.getOrbit()).getBody();
const bodyFrame = await body.getReferenceFrame();

while (true) {
  const d = await vessel.direction(bodyFrame);
  const v = await vessel.velocity(bodyFrame);

  // Compute the dot product of d and v
  const dotprod = d[0] * v[0] + d[1] * v[1] + d[2] * v[2];

  // Compute the magnitude of v, as d is a unit vector
  const vmag = Math.sqrt(v[0] ** 2 + v[1] ** 2 + v[2] ** 2);

  // Compute the angle between the vectors
  let angle = 0;
  if (vmag > 0) {
    angle = Math.acos(dotprod / vmag) * (180 / Math.PI);
  }

  console.log(`Angle of attack = ${angle.toFixed(1)} degrees`);

  await new Promise((resolve) => setTimeout(resolve, 1000));
}
