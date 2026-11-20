import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
await conn.run(
  ({ vessel }) => {
    for (const parachute of vessel.parts.parachutes) {
      parachute.deploy();
    }
  },
  { vessel },
);
// [/docs]
await conn.close();
