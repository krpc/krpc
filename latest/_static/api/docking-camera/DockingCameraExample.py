import krpc

conn = krpc.connect(name="Docking Camera Example")
part = conn.space_center.active_vessel.parts.with_name("OnboardCamera")[0]
camera = conn.docking_camera.camera(part)
with open("camera.png", "wb") as file:
    file.write(camera.image())
