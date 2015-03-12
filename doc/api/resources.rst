Resources
=========

.. class:: Resources

   Created by calling :meth:`Vessel.Resources`.

.. attribute:: Resources.Names

   Gets a list of resource names that can be stored by the vessel.

   :rtype: :class:`List` ( `string` )

.. method:: Resources.HasResource (name)

   Returns `True` if the vessel can store the named resource.

   :param string name:
   :rtype: bool

.. method:: Resources.Max (name, stage = -1, cumulative = True)

   Returns the amount of a resource that the vessel can store.

   :param string name: The name of the resource.
   :param int32 stage: When set to -1, returns the amount of resource in all
                       stages. Otherwise returns the amount of resource in the
                       given stage.
   :param bool cumulative: When `False`, returns the amount of resource in the
                           given stage. When `True` returns the amount of
                           resource in the given stage and all subsequent stages
                           combined.
   :rtype: `double`

.. method:: Resources.Amount (name, stage = -1, cumulative = True)

   Returns the amount of a resource that the vessel is currently storing.

   :param string name: The name of the resource.
   :param int32 stage: When set to -1, returns the amount of resource in all
                       stages. Otherwise returns the amount of resource in the
                       given stage.
   :param bool cumulative: When `False`, returns the amount of resource in the
                           given stage. When `True` returns the amount of
                           resource in the given stage and all subsequent stages
                           combined.
   :rtype: `double`

Examples
--------

Using the Kerbal X stock rocket
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

.. code-block:: python

   # Total amount of liquid fuel in the craft (similar to resource box):
   resources.amount('LiquidFuel', -1, cumulative=False)
   # Or:
   resources.amount('LiquidFuel', 8, cumulative=True)

   # Amount of liquid fuel that would be jettisoned when changing from stage 5
   resources.amount('LiquidFuel', 5, cumulative=False)

   # Amount of liquid fuel remaining from stage 5 through stage 0
   resources.amount('LiquidFuel', 5, cumulative=True)

=====  ================  ===============
stage  cumulative=False  cumulative=True
=====  ================  ===============
   -1              8280             8280
    0                 0                0
    1                 0                0
    2               720              720
    3                 0              720
    4              4320             5040
    5              1080             6120
    6              1080             7200
    7              1080             8280
    8                 0             8280
=====  ================  ===============
