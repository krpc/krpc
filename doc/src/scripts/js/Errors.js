import { connect, ServerError } from "@krpc-org/krpc";

const conn = await connect();
try {
  await conn.spaceCenter.load("missing");
} catch (error) {
  if (!(error instanceof ServerError)) {
    throw error;
  }
  console.log(`Failed to load the save: ${error.message}`);
}
