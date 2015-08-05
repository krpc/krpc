Interacting with Parts
======================

The following examples demonstrate use of the :ref:`python-api-parts` functionality to
achieve various tasks. More details on specific topics can also be found in the
API documentation:

* :ref:`python-api-parts-trees-of-parts`
* :ref:`python-api-parts-attachment-modes`
* :ref:`python-api-parts-fuel-lines`
* :ref:`python-api-parts-staging`

Deploying all Parachutes
------------------------

Sometimes things go horribly wrong. The following script does its best to save
your Kerbals by deploying all the parachutes:

.. literalinclude:: /scripts/DeployParachutes.py

'Control From Here' for Docking Ports
-------------------------------------

The following example will find a standard sized Clamp-O-Tron docking port, and
control the vessel from it:

.. literalinclude:: /scripts/ControlFromHere.py

Combined Specific Impulse
-------------------------

The following script calculates the combined specific impulse of all currently
active and fueled engines on a rocket. See here for a description of the maths:
http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse#Multiple_engines

.. literalinclude:: /scripts/CombinedISP.py
