import { connect, types } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();

// A function of the vessel's mass and a mass to add, in kilograms
const ratio = await conn.compile(
  ({ vessel }, payload) => (vessel.mass + payload) / vessel.mass,
  { vessel },
  [types.double],
);

for (const payload of [100, 500, 1000]) {
  console.log(`${payload} kg: ${await conn.run(ratio, payload)}`);
}
