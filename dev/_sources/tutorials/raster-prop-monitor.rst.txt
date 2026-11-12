.. currentmodule:: RasterPropMonitor

Raster Prop Monitor
===================

`Raster Prop Monitor <https://github.com/FirstPersonKSP/RasterPropMonitor>`_ fills a pod's IVA
with working screens, and adds external cameras that the screens show. The scripts below draw a
menu on a screen, and show a camera's view as video in a window. They need a crewed pod with a
Raster Prop Monitor IVA, such as the Mk1 Lander Can, and an external camera on the vessel.

A Screen Menu
-------------

The following script turns a screen into a menu. The screen's arrow buttons move a cursor, and
its ENTER button toggles the lights, gear, brakes or SAS:

.. literalinclude:: /scripts/tutorials/raster-prop-monitor/ScreenMenu.py
   :language: python

Enter the IVA to see the menu, and click the buttons on the screen. The screen's other page
buttons go to Raster Prop Monitor's own pages. The NAV button brings back the menu.

kRPC adds a page named ``krpc`` to every Raster Prop Monitor screen. The page shows the
monitor's :class:`Display`, and the script draws the menu on it by setting
:attr:`Display.text`. The text uses Raster Prop Monitor's page format: ``[#00ff00]`` sets a
color, and the altitude line names a variable that Raster Prop Monitor formats and updates by
itself.

The script answers the screen's buttons with the :meth:`Display.button_pressed` events, which
fire only while the page is shown. :meth:`Monitor.capture_button` takes the NAV button from
Raster Prop Monitor, so that a press shows the ``krpc`` page. On the screens of the Mk1 Lander
Can, NAV is ``buttonR1``, and :attr:`Monitor.page_buttons` lists the names on other screens.

Camera Video
------------

The following script shows the view of the vessel's first external camera in a window. It
uses `Pillow <https://python-pillow.org>`_ to decode the frames, and tkinter to show them:

.. literalinclude:: /scripts/tutorials/raster-prop-monitor/CameraVideo.py
   :language: python

:meth:`ExternalCamera.image` renders the camera's view, here as a JPEG image. JPEG is lossy,
and is smaller and faster to encode than PNG. A stream of it is a video, and the stream's rate
is the frame rate. The server renders a frame on each update the rate allows, up to one per
physics tick, which is 50 a second. A 640 by 480 frame is about 25 KB, so a 30 frame a second
video takes under 1 MB/s.

The window shows the newest frame each time it redraws. To capture every frame of a recording
instead, call :meth:`ExternalCamera.image` in a loop that :ref:`holds each tick
<holding-a-tick>`. The game then waits for the script on each tick.

.. seealso::

   :class:`ExternalCamera` for the camera's field of view, and :class:`Monitor` for reading a
   screen's text and image.
