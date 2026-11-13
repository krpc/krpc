import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const ratings = await conn.run(
  ({ vessel }) => {
    const ratings = [];
    for (const engine of vessel.parts.engines) {
      if (!engine.active) {
        continue;
      }
      if (engine.thrust > 0.9 * engine.availableThrust) {
        ratings.push("full");
      } else {
        ratings.push("partial");
      }
    }
    return ratings;
  },
  { vessel },
);
console.log(ratings);
// [/docs]
