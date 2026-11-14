import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "SpaceCenter Example" });
const vessel = await conn.spaceCenter.getActiveVessel();
console.log(await vessel.getName());
