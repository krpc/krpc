import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const stream = await conn.addStream(({ vessel }) => vessel.name.toUpperCase(), { vessel });
// [/docs]
console.log(await stream.get());
