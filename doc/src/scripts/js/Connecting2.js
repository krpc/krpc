import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "My Example Program", address: "192.168.1.10", port: 1000 });
const status = await conn.krpc.getStatus();
console.log(status.version);
