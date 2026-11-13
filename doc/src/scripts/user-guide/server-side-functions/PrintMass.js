import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
await conn.run(({ vessel }) => console.log("Mass:", vessel.mass, "kg"), { vessel });
// [/docs]
