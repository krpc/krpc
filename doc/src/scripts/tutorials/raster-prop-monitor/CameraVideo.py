import io
import tkinter as tk

from PIL import Image
from PIL.ImageTk import PhotoImage

import krpc

conn = krpc.connect(name="Camera Video")
vessel = conn.space_center.active_vessel
camera = conn.raster_prop_monitor.cameras(vessel)[0]

# The camera's view as JPEG frames of 640 by 480 pixels, 30 times a second
jpeg = conn.raster_prop_monitor.ImageFormat.jpeg
frames = conn.add_stream(camera.image, 640, 480, jpeg)
frames.rate = 30

window = tk.Tk()
window.title(camera.name)
label = tk.Label(window)
label.pack()
# Tk keeps no reference to the image it shows
shown = []


def show():
    image = PhotoImage(Image.open(io.BytesIO(frames())))
    label.configure(image=image)
    shown[:] = [image]
    window.after(30, show)


show()
window.mainloop()
