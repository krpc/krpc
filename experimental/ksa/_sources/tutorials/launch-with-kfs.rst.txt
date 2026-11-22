Launching with kFS
==================

This tutorial launches the game's Rocket with a kFS program flying the ascent. kFS is the
game's flight software: programs in kC, a subset of C, that run on a vehicle's flight
computer. The kRPC script loads the program, reads its telemetry, and tells it when to lift off
and when to stage, so the two talk in both directions.

The complete script is :download:`launch_with_kfs.py
</scripts/tutorials/launch-with-kfs/launch_with_kfs.py>`, and the program is
:download:`ascent.c </scripts/tutorials/launch-with-kfs/ascent.c>`. Save them in the same
folder.

.. note::

   kFS is a prototype in the game. The script turns it on, as the game's ``kfs on`` console
   command does.

The Program
-----------

The program runs a task ten times a second. It subscribes to two of the messages the game
publishes to it: the flight data, with the altitude and apoapsis, and the vehicle's
properties, with its engines:

.. literalinclude:: /scripts/tutorials/launch-with-kfs/ascent.c
   :language: c
   :start-at: #pragma kfs tick_hz 10
   :end-at: #pragma kfs task ascent rate_hz=10

The program shares two global variables with the script. The script reads the telemetry, and
writes the request:

.. literalinclude:: /scripts/tutorials/launch-with-kfs/ascent.c
   :language: c
   :start-at: /* The telemetry, which the client reads */
   :end-at: request_t request;

The program waits on the pad until the script asks it to lift off. It then points the rocket
straight up and lights the first stage, with the game's flight computer services. Once the
engine has thrust, the ascent starts. The program steers along the same gravity turn as
:doc:`launch-into-orbit`, and cuts the engines once the apoapsis reaches 400 km. The first stage
runs dry on the way, at an apoapsis of about 360 km:

.. literalinclude:: /scripts/tutorials/launch-with-kfs/ascent.c
   :language: c
   :start-at: if (telemetry.phase == PHASE_LAUNCH) {
   :end-at: telemetry.thrust = thrust;

The program stages only when the script asks. It acknowledges a staging in its telemetry once
the next stage has thrust, so the script never asks twice for the same stage.

Loading the Program
-------------------

The script launches the vehicle, and seats a kitten in it. The flight computer obeys a program
only while the vehicle is under control, and the Rocket launches with an empty seat. The script
then loads the program onto the flight computer. ``load`` builds the program, and throws an
exception with the compiler's errors if it has any:

.. literalinclude:: /scripts/tutorials/launch-with-kfs/launch_with_kfs.py
   :language: python
   :start-at: kfs.available = True
   :end-at: computer.start()

Reading the Telemetry
---------------------

Each field of the telemetry is a stream of ``read_global``. The server reads the program's
memory after each of its ticks:

.. literalinclude:: /scripts/tutorials/launch-with-kfs/launch_with_kfs.py
   :language: python
   :start-at: def telemetry(name: str) -> Any:
   :end-at: stages = telemetry("stages")

Lifting Off
-----------

The script counts down, and writes the lift off request to the program.
``write_globals`` returns once the program can see the new value:

.. literalinclude:: /scripts/tutorials/launch-with-kfs/launch_with_kfs.py
   :language: python
   :start-at: for count in range(3, 0, -1):
   :end-at: computer.write_globals({"request.launch": 1})

Staging
-------

The script watches the thrust the program reports. When the first stage runs dry, it counts
up the stagings it wants in the program's request. It also streams the computer's state, and
stops if the program halts, such as on a trap:

.. literalinclude:: /scripts/tutorials/launch-with-kfs/launch_with_kfs.py
   :language: python
   :start-at: state = conn.add_stream(getattr, computer, "state")
   :end-at: computer.write_globals({"request.stages": requested})

Handing Back
------------

Once the engines are off, the script waits for the rocket to leave the atmosphere. It then
stops the program, which hands the rocket back to the pilot, and prints the program's log:

.. literalinclude:: /scripts/tutorials/launch-with-kfs/launch_with_kfs.py
   :language: python
   :start-at: # Hand the rocket back to the pilot
   :end-at: print("  " + line)

The rocket is on its way to a 400 km apoapsis. :doc:`circularize` makes the orbit circular.
