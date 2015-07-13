.. default-domain:: #echo $domain

Resources
=========

.. class:: Resources

   Created by calling :attr:`Vessel.Resources`,
   :attr:`Vessel.ResourcesInDecoupleStage` or :attr:`Part.Resources`.

   .. attribute:: Names

      Gets a list of resource names that can be stored.

      :rtype: :class:`List` ( string )

   .. method:: HasResource (name)

      Returns ``true`` if the named resource can be stored.

      :param string name: The name of the resource.
      :rtype: bool

   .. method:: Max (name)

      Returns the amount of a resource that can be stored.

      :param string name: The name of the resource.
      :rtype: float

   .. method:: Amount (name)

      Returns the amount of a resource that is currently stored.

      :param string name: The name of the resource.
      :rtype: float

   .. staticmethod:: Density (name)

      Returns the density of the named resource, in kg/l.

      :rtype: float

   .. staticmethod:: FlowMode (name)

      Returns the flow mode of the named resource.

      :rtype: ResourceFlowMode

.. class:: ResourceFlowMode

   .. data:: Vessel

      The resource flows to any part in the vessel. For example, electric
      charge.

   .. data:: Stage

      The resource flows from parts in the first stage, followed by the second,
      and so on. For example, mono-propellant.

   .. data:: Adjacent

      The resource flows between adjacent parts within the vessel. For example,
      liquid fuel or oxidizer.

   .. data:: None

      The resource does not flow. For example, solid fuel.
