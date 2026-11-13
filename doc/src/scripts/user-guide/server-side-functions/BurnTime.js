import { connect, types } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const burnTime = await conn.compile(
  ({ vessel }, deltaV) => {
    const exhaustVelocity = vessel.specificImpulse * 9.82;
    const finalMass = vessel.mass / Math.exp(deltaV / exhaustVelocity);
    return ((vessel.mass - finalMass) * exhaustVelocity) / vessel.availableThrust;
  },
  { vessel },
  [types.double],
);

for (const deltaV of [100, 250, 400]) {
  console.log(await conn.run(burnTime, deltaV));
}
// [/docs]
