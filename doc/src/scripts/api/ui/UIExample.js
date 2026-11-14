import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "User Interface Example" });
await conn.ui.message("Hello, World!", { duration: 10 });
