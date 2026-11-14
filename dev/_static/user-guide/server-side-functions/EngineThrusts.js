import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const engines = await (await vessel.getParts()).getEngines();
const stream = await conn.addStream(
  ({ engines }) => engines.map((engine) => engine.thrust),
  { engines },
);
for (const thrust of await stream.get()) {
  console.log(thrust);
}
// [/docs]
