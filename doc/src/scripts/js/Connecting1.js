import { connect } from "@krpc-org/krpc";

const conn = await connect();
const status = await conn.krpc.getStatus();
console.log(status.version);
