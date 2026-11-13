import { connect } from "@krpc-org/krpc";

const conn = await connect();
try {
  await conn.spaceCenter.load("missing");
} catch (error) {
  if (!(error instanceof conn.krpc.ArgumentException)) {
    throw error;
  }
  console.log(`Failed to load the save: ${error.description}`);
}
