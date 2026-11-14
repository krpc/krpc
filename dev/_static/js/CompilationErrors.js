import { CompilationError, connect } from "@krpc-org/krpc";

const conn = await connect();
const vessel = await conn.spaceCenter.getActiveVessel();
const limit = 70000;

try {
  // limit is read from the program, so it must be captured
  await conn.compile(({ vessel }) => vessel.flight().meanAltitude > limit, { vessel });
} catch (error) {
  if (error instanceof CompilationError) {
    console.log(error.message);
  }
}
