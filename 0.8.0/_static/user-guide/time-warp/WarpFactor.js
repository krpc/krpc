import { connect } from "@krpc-org/krpc";

const conn = await connect();
const spaceCenter = conn.spaceCenter;

// [docs]
console.log(await spaceCenter.getMaximumRailsWarpFactor());
await spaceCenter.setRailsWarpFactor(3);
// [/docs]
await conn.close();
