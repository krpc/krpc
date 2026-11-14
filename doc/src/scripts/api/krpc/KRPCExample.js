import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "KRPC Example" });
console.log((await conn.krpc.getStatus()).version);
