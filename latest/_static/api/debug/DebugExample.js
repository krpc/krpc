import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Debug Example" });
await conn.debug.setInfinitePropellant(true);
