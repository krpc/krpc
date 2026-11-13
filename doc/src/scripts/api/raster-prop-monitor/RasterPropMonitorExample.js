import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Raster Prop Monitor Example" });
const vessel = await conn.spaceCenter.getActiveVessel();
const rpm = conn.rasterPropMonitor;

// The first screen in the IVA, and the content of its krpc page
const monitor = (await rpm.monitors(vessel))[0];
const display = await monitor.getDisplay();

// Raster Prop Monitor formats the SAS variable, and keeps it up to date
await display.setText("SAS: {0:ON;OFF;OFF} $&$ SAS");

// A press of the NAV button shows the page
await monitor.captureButton("buttonR1", "krpc");
