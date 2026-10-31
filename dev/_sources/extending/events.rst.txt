Events
======

kRPC procedures, methods and properties can return event objects to clients. This is done using the
class ``KRPC.Service.Event``. This class supports two different types of
event.

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

Actively Polled Events
----------------------

This type of event contains a function that is evaluated once per game update. When the function
returns true, the event is triggered. The event object is passed to the function so that it can
manipulate it as desired.

For example the following creates an event that triggers when the active vessel reaches the given
altitude. The event is removed the first time it triggers.

.. literalinclude:: /scripts/extending/events/PolledEvent.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:
