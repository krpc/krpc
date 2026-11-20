import { connect } from "@krpc-org/krpc";

const conn = await connect();
const spaceCenter = conn.spaceCenter;

// [docs]
for (const name of await spaceCenter.launchableVessels("VAB")) {
  console.log(name);
}
await spaceCenter.launchVesselFromVAB("Kerbal X");
console.log(await (await spaceCenter.getActiveVessel()).getName());
// [/docs]
await conn.close();
