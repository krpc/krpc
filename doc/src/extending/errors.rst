.. _service-api-errors:

Errors and Warnings
===================

The server checks every service as the game loads. A member with an error is left out, along with
every member that uses it, and the rest of kRPC keeps running. A call to a member that was left out
fails with a "procedure not found" error.

.. list-table::
   :header-rows: 1

   * - Error
     - Left out
   * - A procedure, method or property that is not valid, such as one with a parameter type kRPC
       cannot serialize
     - That member
   * - A class, enumeration, structure or exception that is not valid
     - That type, and each member or structure that uses it
   * - An assembly with types that fail to load, such as from a missing dependency
     - Its services
   * - A service that is not valid, or declared by two classes
     - The service, its types, and the extension members of its classes
   * - Two services whose names differ only in case
     - Both services
   * - Two types of a service whose names differ only in case, or a type named as its service
     - Those types, and each member or structure that uses them
   * - Two members of a scope that share a :ref:`name <service-api-identifiers>`
     - Both members. An extension member that shares a name with a member of the class is left
       out, and the class keeps its own member
   * - Two parameters of a procedure, values of an enumeration or fields of a structure that share
       a name
     - The procedure, enumeration or structure
   * - An identifier of 0, or one shared by two services, two procedures of a service or two types
       of a service
     - Each service, procedure or type with that identifier

kRPC does not run without its ``KRPC`` service, so a clash that leaves it out disables kRPC.

A warning leaves the members in. A class of :ref:`extension members
<service-api-extension-members>` that is not public gets one, as its members are out of
reach.

The kRPC Problems window opens at the main menu, with a category for each service with errors,
one for the warnings and one for the :ref:`mod integrations <service-api-other-mods>` that failed
to load. Click a category to expand it. The log has the full messages.

The window's "Don't show again" button hides it until the problems change. The "Show problems"
option in the server window opens it again.

``krpc-servicedefs`` stops at the first error or warning. Running it on your assembly before a
release checks the service, see :ref:`service-api-clientgen`.
