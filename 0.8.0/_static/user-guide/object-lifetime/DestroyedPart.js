import { connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const parts = await vessel.getParts();
const part = (await parts.getAll())[0];
// [docs]
try {
  console.log(await part.getTemperature());
} catch (error) {
  if (!(error instanceof conn.krpc.ObjectDestroyedException)) {
    throw error;
  }
  console.log("that part is gone");
}
// [/docs]
await conn.close();
