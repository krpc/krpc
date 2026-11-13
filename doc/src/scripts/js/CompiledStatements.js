import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();

// The engines with fuel, by title, and the total thrust of those firing
const report = await conn.run(
  ({ vessel }) => {
    const titles = new Set();
    let thrust = 0;
    for (const engine of vessel.parts.engines) {
      if (!engine.hasFuel) {
        continue;
      }
      titles.add(engine.part.title);
      thrust += engine.thrust;
    }
    return `${Array.from(titles).join(", ")}: ${thrust} N`;
  },
  { vessel },
);
console.log(report);
