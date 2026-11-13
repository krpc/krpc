import { connect } from "@krpc-org/krpc";

const conn = await connect();
await conn.spaceCenter.launchVessel("VAB", "My Rocket", "LaunchPad", { recover: false });
