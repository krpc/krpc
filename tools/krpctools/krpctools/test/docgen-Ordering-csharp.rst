.. default-domain:: csharp
.. highlight:: csharp

.. namespace:: KRPC.Client.Services.ServiceA


.. class:: ServiceA

   A service whose procedures use another service's enumeration.

   .. method:: void Frob(ServiceB.Mode mode = ServiceB.Mode.Fast)

      Defaults to a member of another service's enumeration.

      :parameters:



      :Game Scenes: All

   .. method:: void FrobAll(System.Collections.Generic.IList<ServiceB.Mode> modes = { ServiceB.Mode.Fast })

      Defaults to a collection containing a member of another service's enumeration.

      :parameters:



      :Game Scenes: All
