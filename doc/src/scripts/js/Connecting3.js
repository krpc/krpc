import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "My Example Program" });
const status = await conn.krpc.getStatus();
console.log(status.version);
await conn.close();
