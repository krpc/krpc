Events
======

kRPC procedures, methods and properties can return event objects to clients. This is done using the
class ``KRPC.Service.Event``. This class supports two different types of
event.

The service decides when an event fires and whether it ends. State both in the documentation of the
procedure that returns it.

Manually Triggered Events
-------------------------

This type of event must be triggered by some other piece of code running somewhere in the game. It
is created by calling the default constructor for type ``KRPC.Service.Event``.

For example, the following example is a procedure that returns an event that triggers after a given
number of milliseconds. When the event triggers it is removed.

.. literalinclude:: /scripts/extending/events/ManualEvent.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

.. note::

   Several triggers between two stream updates reach the client as one firing.

Actively Polled Events
----------------------

This type of event contains a function that is evaluated once per game update. The event fires on
each update where the function returns true. To fire once, remove the event when it fires. To fire
when a condition becomes true, keep its previous value and return true only on a change from false.
The event object is passed to the function so that it can manipulate it as desired.

For example the following creates an event that triggers when the active vessel reaches the given
altitude. The event is removed the first time it triggers.

.. literalinclude:: /scripts/extending/events/PolledEvent.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:
