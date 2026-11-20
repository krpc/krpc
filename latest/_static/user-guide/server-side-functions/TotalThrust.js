import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const total = await conn.run(
  ({ vessel }) => {
    let total = 0;
    for (const engine of vessel.parts.engines) {
      total += engine.thrust;
    }
    return total;
  },
  { vessel },
);
console.log(total);
// [/docs]
