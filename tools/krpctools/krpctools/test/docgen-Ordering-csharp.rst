.. default-domain:: cs
.. highlight:: csharp

.. namespace:: KRPC.Client.Services.ServiceA


.. class:: ServiceA

   A service whose procedures use another service's enumeration.

   .. function:: void Frob(ServiceB.Mode mode = ServiceB.Mode.Fast)

      Defaults to a member of another service's enumeration.

      :parameters:



      :Game Scenes: All

   .. function:: void FrobAll(System.Collections.Generic.IList<ServiceB.Mode> modes = null)

      Defaults to a collection containing a member of another service's enumeration.

      :parameters:

       * **modes** -- If null, defaults to ``new System.Collections.Generic.List<ServiceB.Mode> { ServiceB.Mode.Fast }``.

      :Game Scenes: All
