.. currentmodule:: SpaceCenter

Docking Guidance
================

The following script outputs docking guidance information. It waits until the
vessel is being controlled from a docking port, and a docking port is set as the
current target. It then prints out information about speeds and distances
relative to the docking axis.

It uses `numpy <http://www.numpy.org>`_ to do linear algebra on the vectors
returned by kRPC -- for example computing the dot product or length of a vector
-- and uses `curses <https://docs.python.org/2/howto/curses.html>`_ for terminal
output.

.. literalinclude:: /scripts/DockingGuidance.py
   :language: python
