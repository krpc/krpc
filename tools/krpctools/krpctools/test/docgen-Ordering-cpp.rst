.. default-domain:: cpp
.. highlight:: cpp

.. namespace:: krpc::services::ServiceA


.. namespace:: krpc::services
.. class:: ServiceA : public krpc::Service

   A service whose procedures use another service's enumeration.

   .. function:: ServiceA(krpc::Client* client)

      Construct an instance of this service.

   .. function:: void frob(ServiceB::Mode mode = ServiceB::Mode::fast)

      Defaults to a member of another service's enumeration.

      :Parameters:



   

   .. function:: void frob_all(std::vector<ServiceB::Mode> modes = std::vector<ServiceB::Mode>{ServiceB::Mode::fast})

      Defaults to a collection containing a member of another service's enumeration.

      :Parameters:
