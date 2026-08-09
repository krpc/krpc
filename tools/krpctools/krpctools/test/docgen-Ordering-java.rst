.. default-domain:: java
.. highlight:: java

.. package:: krpc.client.services.ServiceA


.. type:: public class ServiceA

   A service whose procedures use another service's enumeration.

   .. method:: void frob(ServiceB.Mode mode)

      Defaults to a member of another service's enumeration.

      :param ServiceB.Mode mode:
   

   .. method:: void frobAll(java.util.List<ServiceB.Mode> modes)

      Defaults to a collection containing a member of another service's enumeration.

      :param java.util.List<ServiceB.Mode> modes:
