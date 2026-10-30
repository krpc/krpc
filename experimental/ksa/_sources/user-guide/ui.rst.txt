User Interfaces
===============

``UI.AddWindow`` adds a window to the game's UI, and the window's methods add widgets to it.
This script shows the altitude of the active vehicle, and sets its throttle and engines from the
window until the Done button is clicked:

.. literalinclude:: /scripts/user-guide/ui/Throttle.py
   :language: python
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

A window lays out its widgets one per line, and fits its size to them until ``Size`` is set. A
``Row`` lays out its widgets from left to right.

Events
------

A widget records each click or edit in a property that stays ``true`` until the script sets it to
``false``: ``Button.Clicked``, and ``Changed`` on the toggle, input field, slider and dropdown. A
script polls these, or waits on them with a stream or an event. Setting a widget's value from a
script leaves ``Changed`` unchanged.

Lifetime
--------

The windows are removed when the client that added them disconnects, along with their widgets.
``Remove`` removes one widget, or a window and its widgets, and ``UI.Clear`` removes every window.
A call through a removed widget raises ``ObjectDestroyedException``.

The windows are hidden with the game's UI. Closing a window makes it invisible, and setting
``Visible`` shows it again.
