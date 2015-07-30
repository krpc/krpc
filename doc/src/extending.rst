.. _extending:

Extending kRPC
==============

The kRPC Architecture
---------------------

kRPC consists of two components: a server and a client. The server plugin
(provided by ``kRPC.dll``) runs inside KSP. It provides a collection of
*procedures* that clients can run. These procedures are arranged in groups
called *services* to keep things organized. It also provides an in-game user
interface that can be used to start/stop the server, change settings and monitor
active clients.

Clients run outside of KSP. This gives you the freedom to run scripts in
whatever environment you want. A client communicates with the server to run
procedures. kRPC comes with a `Python client library
<https://pypi.python.org/pypi/krpc>`_ that implements the :ref:`communication
protocol <communication-protocol>`, making it easy to write Python scripts that
can talk to the server. Client libraries for other languages may be added in the
future. kRPC supports multiple concurrent clients.

kRPC comes with a collection of standard functionality for interacting with
craft, contained in a service called :class:`SpaceCenter`. This service provides
procedures for things like getting flight/orbital data and controlling the
active vessel. This service is provided by
``kRPCSpaceCenter.dll``.

Service API
-----------

Third party mods can add functionality to kRPC using the *Service API*. This is
done by adding :ref:`attributes <service-api-attributes>` to your own classes,
methods and properties to make them visible through the server. When the kRPC
server starts, it scans all loaded assemblies looking for classes, methods and
properties with these attributes.

The following example implements a service that can control the throttle and
staging of the active vessel:

.. code-block:: csharp

   using KRPC.Service;
   using KRPC.Service.Attributes;

   namespace LaunchControl {

       [KRPCService (GameScene = GameScene.Flight)]
       public static class LaunchControl {

           [KRPCProperty]
           public static void Throttle {
               get { return FlightInputHandler.state.mainThrottle; }
               set { FlightInputHandler.state.mainThrottle = value; }
           }

           [KRPCProcedure]
           public static void ActivateStage ()
           {
               Staging.ActivateNextStage ();
           }
       }
   }

The following example shows how this service can be used from a python client:

.. code-block:: python

   import krpc
   conn = krpc.connect()
   conn.launch_control.throttle = 1
   conn.launch_control.activate_stage()

.. _service-api-attributes:

Attributes
^^^^^^^^^^

The following `C# attributes
<https://msdn.microsoft.com/en-us/library/aa287992.aspx>`_ can be used to add
functionality to the kRPC server.

.. class:: KRPCService ([Name], [GameScene])

   :param string Name: Optional name for the service. If omitted, the service
                       name is set to the name of the class this attribute is
                       applied to.
   :param GameScene GameScene: The game scenes in which the services procedures
                               are available.

   This `attribute <https://msdn.microsoft.com/en-us/library/aa287992.aspx>`_ is
   applied to a static class, to indicate that all methods, properties and
   classes declared within it are part of the the same service. The name of the
   service is set to the name of the class, or -- if present -- the ``Name``
   parameter.

   Multiple services with the same name can be declared, as long the classes,
   procedures and methods they contain have unique names. The classes will be
   merged to appear as a single service on the server.

   The type to which this attribute is applied must satisfy the following
   criteria:

   * The type must be a class.

   * The class must be ``public static``.

   * The name of the class, or the ``Name`` parameter if specified, must be a
     valid :ref:`kRPC identifier <service-api-identifiers>`.

   * The class must not be declared within another class that has the
     :class:`KRPCService` attribute. Nesting of services is not permitted.

   Services are configured to be available in specific :ref:`game scenes
   <service-api-game-scenes>` via the ``GameScene`` parameter. If the
   ``GameScene`` parameter is not specified, the service is available in any
   scene. If a procedure is called when the service is not available, it will
   throw an exception.

   **Examples**

   * Declare a service called ``EVA``:

     .. code-block:: csharp

        [KRPCService]
        public static class EVA {
            ...
        }

   * Declare a service called ``MyEVAService`` (different to the name of the
     class):

     .. code-block:: csharp

        [KRPCService (Name = "MyEVAService")]
        public static class EVA {
            ...
        }

   * Declare a service called ``FlightTools`` that is only available during the
     ``Flight`` game scene:

     .. code-block:: csharp

        [KRPCService (GameScene = GameScene.Flight)]
        public static class FlightTools {
            ...
        }

.. class:: KRPCProcedure

   This `attribute <https://msdn.microsoft.com/en-us/library/aa287992.aspx>`_ is
   applied to static methods, to add them to the server as procedures.

   The method to which this attribute is applied must satisfy the following
   criteria:

   * The method must be ``public static``.

   * The name of the method must be a valid :ref:`kRPC identifier
     <service-api-identifiers>`.

   * The method must be declared inside a class that is a :class:`KRPCService`.

   * The parameter types and return type must be :ref:`types that kRPC knows how
     to serialize <service-api-serializable-types>`.

   * Parameters can have default arguments.

   **Example**

   The following defines a service called ``EVA`` with a ``PlantFlag`` procedure
   that takes a name and an optional description, and returns a ``Flag`` object.

   .. code-block:: csharp

      [KRPCService]
      public static class EVA {
          [KRPCProcedure]
          public static Flag PlantFlag (string name, string description = "")
          {
              ...
          }
      }

   This can be called from a python client as follows:

   .. code-block:: python

      import krpc
      conn = krpc.connect()
      flag = conn.eva.plant_flag('Landing Site', 'One small step for Kerbal-kind')

.. class:: KRPCClass ([Service])

   :param string Service: Optional name of the service to add this class to. If
                          omitted, the class is added to the service that
                          contains its definition.

   This `attribute <https://msdn.microsoft.com/en-us/library/aa287992.aspx>`_ is
   applied to non-static classes. It adds the class to the server, so that
   references to instances of the class can be passed between client and server.

   A :class:`KRPCClass` must be part of a service, just like a
   :class:`KRPCProcedure`. However, it would be restrictive if the class had to
   be declared as a nested class inside a class with the :class:`KRPCService`
   attribute. Therefore, a :class:`KRPCClass` can be declared outside of any
   service if it has the ``Service`` parameter set to the name of the service
   that it is part of. Also, the service that the ``Service`` parameter refers
   to does not have to exist. If it does not exist, a service with the given
   name is created.

   The class to which this attribute is applied must satisfy the following
   criteria:

   * The class must be ``public`` and *not* ``static``.

   * The name of the class must be a valid :ref:`kRPC identifier
     <service-api-identifiers>`.

   * The class must either be declared inside a class that is a
     :class:`KRPCService`, or have its ``Service`` parameter set to the name of
     the service it is part of.

   **Examples**

   * Declare a class called ``Flag`` in the ``EVA`` service:

     .. code-block:: csharp

        [KRPCService]
        public static class EVA {
            [KRPCClass]
            public class Flag {
                ...
            }
        }

   * Declare a class called ``Flag``, without nesting the class definition in a
     service class:

     .. code-block:: csharp

        [KRPCClass (Service = "EVA")]
        public class Flag {
            ...
        }

.. class:: KRPCMethod

   This `attribute <https://msdn.microsoft.com/en-us/library/aa287992.aspx>`_ is
   applied to methods inside a :class:`KRPCClass`. This allows a client to call
   methods on an instance, or static methods in the class.

   The method to which this attribute is applied must satisfy the following
   criteria:

   * The method must be ``public``.

   * The name of the method must be a valid :ref:`kRPC identifier
     <service-api-identifiers>`.

   * The method must be declared in a :class:`KRPCClass`.

   * The parameter types and return type must be :ref:`types that kRPC can
     serialize <service-api-serializable-types>`.

   * Parameters can have default arguments.

   **Example**

   Declare a ``Remove`` method in the ``Flag`` class:

   .. code-block:: csharp

      [KRPCClass (Service = "EVA")]
      public class Flag {
          [KRPCMethod]
          void Remove()
          {
              ...
          }
      }

.. class:: KRPCProperty

   This `attribute <https://msdn.microsoft.com/en-us/library/aa287992.aspx>`_ is
   applied to class properties, and comes in two flavors:

   1. Applied to static properties in a :class:`KRPCService`. In this case, the
      property must satisfy the following criteria:

      * Must be ``public static`` and have at least one publicly accessible
        getter or setter.

      * The name of the property must be a valid :ref:`kRPC identifier
        <service-api-identifiers>`.

      * Must be declared inside a :class:`KRPCService`.

   2. Applied to non-static properties in a :class:`KRPCClass`. In this case,
      the property must satisfy the following criteria:

      * Must be ``public`` and *not* ``static``, and have at least one publicly
        accessible getter or setter.

      * The name of the property must be a valid :ref:`kRPC identifier
        <service-api-identifiers>`.

      * Must be declared inside a :class:`KRPCClass`.

   **Examples**

   * Applied to a static property in a service:

     .. code-block:: csharp

        [KRPCService]
        public static class EVA {
            [KRPCProperty]
            public Flag LastFlag
            {
                get { ... }
            }
        }

     This property can be accessed from a python client as follows:

     .. code-block:: python

        import krpc
        conn = krpc.connect()
        flag = conn.eva.last_flag

   * Applied to a non-static property in a class:

     .. code-block:: csharp

        [KRPCClass (Service = "EVA")]
        public class Flag {
            [KRPCProperty]
            public void Name { get; set; }

            [KRPCProperty]
            public void Description { get; set; }
        }

.. class:: KRPCEnum ([Service])

   :param string Service: Optional name of the service to add this enum to. If
                          omitted, the enum is added to the service that
                          contains its definition.

   This `attribute <https://msdn.microsoft.com/en-us/library/aa287992.aspx>`_ is
   applied to enumeration types. It adds the enumeration and its permissible
   values to the server. This attribute works similarly to :class:`KRPCClass`,
   but is applied to enumeration types.

   A :class:`KRPCEnum` must be part of a service, just like a
   :class:`KRPCClass`. Similarly, a :class:`KRPCEnum` can be declared outside of
   a service if it has its ``Service`` parameter set to the name of the service
   that it is part of.

   The enumeration type to which this attribute is applied must satisfy the
   following criteria:

   * The enumeration must be ``public``.

   * The name of the enumeration must be a valid :ref:`kRPC identifier
     <service-api-identifiers>`.

   * The enumeration must either be declared inside a :class:`KRPCService`, or
     have it's ``Service`` parameter set to the name of the service it is part of.

   * The `underlying C# type
     <https://msdn.microsoft.com/en-gb/library/sbbt4032.aspx>`_ must be an
     ``int``.

   **Examples**

   * Declare an enumeration type with two values:

     .. code-block:: csharp

        [KRPCEnum (Service = "EVA")]
        public enum FlagState {
            Raised,
            Lowered
        }

     This can be used from a python client as follows:

     .. code-block:: python

        import krpc
        conn = krpc.connect()
        state = conn.eva.FlagState.lowered

.. _service-api-identifiers:

Identifiers
^^^^^^^^^^^

An identifier must only contain alphanumeric characters and underscores. An
identifier must not start with an underscore. Identifiers should follow
`CamelCase <http://en.wikipedia.org/wiki/CamelCase>`_ capitalization
conventions.

.. note:: Although underscores are permitted, they should be avoided as they are
          used for internal name mangling.

.. _service-api-serializable-types:

Serializable Types
^^^^^^^^^^^^^^^^^^

A type can only be used as a parameter or return type if kRPC knows how to
serialize it. The following types are serializable:

* The C# types ``double``, ``float``, ``int``, ``long``, ``uint``, ``ulong``,
  ``bool``, ``string`` and ``byte[]``

* Any protocol buffer message type

* Any type annotated with :class:`KRPCClass`

* Any type annotated with :class:`KRPCEnum`

* Collections of serializable types:

  * ``System.Collections.Generic.IList<T>`` where ``T`` is a serializable type

  * ``System.Collections.Generic.IDictionary<K,V>`` where ``K`` is one of
    ``int``, ``long``, ``uint``, ``ulong``, ``bool`` or ``string`` and ``V`` is
    a serializable type

  * ``System.Collections.HashSet<V>`` where ``V`` is a serializable type

* Return types can be ``void``

.. _service-api-game-scenes:

Game Scenes
^^^^^^^^^^^

Each service is configured to be available from a particular game scene, or
scenes.

.. class:: GameScene

   .. attribute:: SpaceCenter

      The game scene showing the Kerbal Space Center buildings.

   .. attribute:: Flight

      The game scene showing a vessel in flight (or on the launchpad/runway).

   .. attribute:: TrackingStation

      The tracking station.

   .. attribute:: EditorVAB

      The Vehicle Assembly Building.

   .. attribute:: EditorSPH

      The Space Plane Hangar.

   .. attribute:: Editor

      Either the VAB or the SPH.

   .. attribute:: All

      All game scenes.

**Examples**

* Declare a service that is available in the :attr:`GameScene.Flight` game
  scene:

  .. code-block:: csharp

     [KRPCService (GameScene = GameScene.Flight)]
     public static class MyService {
        ...
     }

* Declare a service that is available in the :attr:`GameScene.Flight` and
  :attr:`GameScene.Editor` game scenes:

  .. code-block:: csharp

     [KRPCService (GameScene = (GameScene.Flight | GameScene.Editor))]
     public static class MyService {
        ...
     }

Documentation
-------------

Documentation can be added using
`C# XML documentation <https://msdn.microsoft.com/en-us/library/aa288481%28v=vs.71%29.aspx>`_.
The documentation will be automatically exported to clients when they connect.

Further Examples
----------------

See the `SpaceCenter service implementation
<https://github.com/djungelorm/krpc/tree/latest-version/src/kRPCSpaceCenter/Services>`_
for more extensive examples.
