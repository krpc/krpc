import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
// [docs]
const overLimit = await conn.run(
  ({ vessel }) =>
    vessel.parts.all
      .filter(
        (part) =>
          part.staticPressure + part.dynamicPressure > part.maxPressure,
      )
      .map((part) => part.title),
  { vessel },
);
// [/docs]
for (const title of overLimit) {
  console.log(title);
}
await conn.close();
