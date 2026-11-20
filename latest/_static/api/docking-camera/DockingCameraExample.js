import { connect } from "@krpc-org/krpc";

const conn = await connect({ name: "Docking Camera Example" });
const vessel = await conn.spaceCenter.getActiveVessel();
const [part] = await (await vessel.getParts()).withName("OnboardCamera");
const camera = await conn.dockingCamera.camera(part);
const image = await camera.image();

const img = document.createElement("img");
img.src = URL.createObjectURL(
  new Blob([new Uint8Array(image)], { type: "image/png" }),
);
document.body.append(img);
