import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const isp = await conn.run(
  ({ vessel }) => {
    let thrust = 0;
    let fuelConsumption = 0;
    for (const engine of vessel.parts.engines) {
      if (engine.active && engine.hasFuel) {
        thrust += engine.thrust;
        fuelConsumption += engine.thrust / engine.specificImpulse;
      }
    }
    return thrust / fuelConsumption;
  },
  { vessel },
);
// [/docs]
console.log(isp);
await conn.close();
