import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
for (const part of await (await vessel.getParts()).getAll()) {
  const pressure =
    (await part.getStaticPressure()) + (await part.getDynamicPressure());
  if (pressure > (await part.getMaxPressure())) {
    console.log(await part.getTitle(), "is over its pressure limit");
  }
}
// [/docs]
await conn.close();
