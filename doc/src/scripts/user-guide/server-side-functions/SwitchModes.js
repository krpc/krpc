import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const switchModes = await conn.compile(
  ({ vessel }) => {
    try {
      for (const engine of vessel.parts.engines) {
        engine.toggleMode();
      }
      return "switched";
    } catch (error) {
      return `could not switch: ${/** @type {Error} */ (error).message}`;
    }
  },
  { vessel },
);
// [/docs]
console.log(await conn.run(switchModes));
