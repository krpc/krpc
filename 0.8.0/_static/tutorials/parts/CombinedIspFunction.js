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
        thrust += engine.availableThrust;
        fuelConsumption += engine.availableThrust / engine.specificImpulse;
      }
    }
    return thrust / fuelConsumption;
  },
  { vessel },
);
// [/docs]
console.log(`Combined Isp = ${isp.toFixed(0)} seconds`);
await conn.close();
