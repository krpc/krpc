import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const heaviest = await conn.run(
  ({ vessel }) => {
    let best = null;
    for (const engine of vessel.parts.engines) {
      if (best == null || engine.part.mass > best.part.mass) {
        best = engine;
      }
    }
    return best;
  },
  { vessel },
);
// [/docs]
console.log(heaviest);
