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
