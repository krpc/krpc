.. _extending:

Extending kRPC
==============

The kRPC Architecture
---------------------

kRPC consists of two components: a server and a client. The server plugin (provided by ``KRPC.dll``)
runs inside KSP. It provides a collection of *procedures* that clients can run. These procedures are
arranged in groups called *services* to keep things organized. It also provides an in-game user
interface that can be used to start/stop the server, change settings and monitor active clients.

Clients run outside of KSP. This gives you the freedom to run scripts in whatever environment you
want. A client communicates with the server to run procedures using one of the supported
:doc:`communication-protocols`.  kRPC comes with several client libraries that implement one of
these protocols, making it easier to write programs in these languages.

kRPC comes with a collection of standard functionality for interacting with vessels, contained in a
service called ``SpaceCenter``. This service provides procedures for things like getting
flight/orbital data and controlling the active vessel. This service is provided by
``KRPC.SpaceCenter.dll``.

Service API
-----------

Third party mods can add functionality to kRPC using the *Service API*. This is done by adding
:ref:`attributes <service-api-attributes>` to your own classes, methods and properties to make them
visible through the server. When the kRPC server starts, it scans all the assemblies loaded by the
game, looking for classes, methods and properties with these attributes.

The following example implements a service that can control the throttle and staging of the active
vessel. To add this to the server, compile the code and place the DLL in your GameData directory.

.. literalinclude:: /scripts/extending/ServiceAPIExample.lib.cs
   :language: csharp

The following example shows how this service can then be used from a python client:

.. literalinclude:: /scripts/extending/ServiceAPIExample.py
   :language: python

Some of the client libraries automatically pick up changes to the functionality provided by the
server, including the Python and Lua clients. However, some clients require code to be generated
from the service assembly so that they can interact with new or changed functionality. See
:ref:`clientgen <service-api-clientgen>` for details on how to generate this code.

.. _service-api-attributes:

Attributes
^^^^^^^^^^

The following `C# attributes <https://msdn.microsoft.com/en-us/library/aa287992.aspx>`_ can be used
to add functionality to the kRPC server.

.. cs:attribute:: KRPCService (string Name, uint Id = 0, KRPC.Service.GameScene GameScene = KRPC.Service.GameScene.All)

   :parameters:

    * **Name** -- Optional name for the service. If omitted, the service name is set to the name of
      the class this attribute is applied to.

    * **Id** -- Optional integer identifier for the service, used by clients that call procedures
      by id rather than by name. If omitted or set to 0, an identifier is generated from a hash of
      the service name.

    * **GameScene** -- The game scenes in which the services procedures are available. Defaults to
      all game scenes. If this is set to :cs:value:`KRPC.Service.GameScene.Inherit` the service
      will be available in all game scenes.

   This `attribute <https://msdn.microsoft.com/en-us/library/aa287992.aspx>`_ is applied to a static
   class, to indicate that all methods, properties and classes declared within it are part of the
   the same service. The name of the service is set to the name of the class, or -- if present --
   the ``Name`` parameter.

   Multiple services with the same name can be declared, as long the classes, procedures and methods
   they contain have unique names. The classes will be merged to appear as a single service on the
   server.

   The type to which this attribute is applied must satisfy the following criteria:

   * The type must be a class.

   * The class must be ``public static``.

   * The name of the class, or the ``Name`` parameter if specified, must be a valid :ref:`kRPC
     identifier <service-api-identifiers>`.

   * The class must not be declared within another class that has the :cs:attr:`KRPCService`
     attribute. Nesting of services is not permitted.

   Services are configured to be available in specific :ref:`game scenes <service-api-game-scenes>`
   via the ``GameScene`` parameter. If the ``GameScene`` parameter is not specified, the service is
   available in any scene. If a procedure is called when the service is not available, it will throw
   an exception.

   **Examples**

   * Declare a service called ``EVA``:

     .. literalinclude:: /scripts/extending/ServiceAttribute.lib.cs
        :language: csharp
        :start-after: [docs]
        :end-before: [/docs]
        :dedent:

   * Declare a service called ``MyEVAService`` (different to the name of the class):

     .. literalinclude:: /scripts/extending/ServiceName.lib.cs
        :language: csharp
        :start-after: [docs]
        :end-before: [/docs]
        :dedent:

   * Declare a service called ``FlightTools`` that is only available during the ``Flight`` game
     scene:

     .. literalinclude:: /scripts/extending/ServiceGameScene.lib.cs
        :language: csharp
        :start-after: [docs]
        :end-before: [/docs]
        :dedent:

.. cs:attribute:: KRPCProcedure (bool Nullable = false, KRPC.Service.GameScene GameScene = KRPC.Service.GameScene.Inherit)

   :parameters:

    * **Nullable** -- Whether the return value of the procedure can be null. Defaults to false.

    * **GameScene** -- The game scenes in which the procedure is available. Defaults to inherit this
      setting from the service the procedure is defined in.

   This `attribute <https://msdn.microsoft.com/en-us/library/aa287992.aspx>`_ is applied to static
   methods, to add them to the server as procedures.

   The method to which this attribute is applied must satisfy the following criteria:

   * The method must be ``public static``.

   * The name of the method must be a valid :ref:`kRPC identifier <service-api-identifiers>`.

   * The method must be declared inside a class that is a :cs:attr:`KRPCService`.

   * The parameter types and return type must be :ref:`types that kRPC knows how to serialize
     <service-api-serializable-types>`.

   * Parameters can have default arguments.

   If the procedure might return a null value, the ``Nullable`` parameter of the attribute must be
   set to true. A parameter that may be ``null`` must likewise be made nullable. See
   :ref:`service-api-null-values`.

   **Example**

   The following defines a service called ``EVA`` with a ``PlantFlag`` procedure that takes a name
   and an optional description, and returns a ``Flag`` object.

   .. literalinclude:: /scripts/extending/PlantFlag.lib.cs
      :language: csharp
      :start-after: [docs]
      :end-before: [/docs]
      :dedent:

   This can be called from a python client as follows:

   .. literalinclude:: /scripts/extending/PlantFlag.py
      :language: python
      :start-after: [docs]
      :end-before: [/docs]

.. cs:attribute:: KRPCClass (string Service, KRPC.Service.GameScene GameScene = KRPC.Service.GameScene.Inherit)

   :parameters:

    * **Service** -- Optional name of the service to add this class to. If omitted, the class is
      added to the service that contains its definition.

    * **GameScene** -- The game scenes in which the class' methods and properties are
      available. Defaults to inherit this setting from the service the class is defined in.

   This `attribute <https://msdn.microsoft.com/en-us/library/aa287992.aspx>`_ is applied to
   non-static classes. It adds the class to the server, so that references to instances of the class
   can be passed between client and server.

   A :cs:attr:`KRPCClass` must be part of a service, just like a
   :cs:attr:`KRPCProcedure`. However, it would be restrictive if the class had to be declared as
   a nested class inside a class with the :cs:attr:`KRPCService` attribute. Therefore, a
   :cs:attr:`KRPCClass` can be declared outside of any service if it has the ``Service``
   parameter set to the name of the service that it is part of. Also, the service that the
   ``Service`` parameter refers to does not have to exist. If it does not exist, a service with the
   given name is created.

   A class' methods and properties can be configured to be available in specific :ref:`game scenes
   <service-api-game-scenes>` via the ``GameScene`` parameter on the class. If the ``GameScene``
   parameter is not specified, the class' methods and properties are available in the same game
   scenes as the service the class is defined in. Individual class methods and properties can
   override this setting.

   The class to which this attribute is applied must satisfy the following criteria:

   * The class must be ``public`` and *not* ``static``.

   * The name of the class must be a valid :ref:`kRPC identifier <service-api-identifiers>`.

   * The class must either be declared inside a class that is a :cs:attr:`KRPCService`, or have
     its ``Service`` parameter set to the name of the service it is part of.

   **Examples**

   * Declare a class called ``Flag`` in the ``EVA`` service:

     .. literalinclude:: /scripts/extending/NestedClass.lib.cs
        :language: csharp
        :start-after: [docs]
        :end-before: [/docs]
        :dedent:

   * Declare a class called ``Flag``, without nesting the class definition in a service class:

     .. literalinclude:: /scripts/extending/ClassAttribute.lib.cs
        :language: csharp
        :start-after: [docs]
        :end-before: [/docs]
        :dedent:

.. cs:attribute:: KRPCMethod (bool Nullable = false, KRPC.Service.GameScene GameScene = KRPC.Service.GameScene.Inherit)

   :parameters:

    * **Nullable** -- Whether the return value of the procedure can be null. Defaults to false.

    * **GameScene** -- The game scenes in which the method is available. Defaults to inherit this
      setting from the class the method is defined in.

   This `attribute <https://msdn.microsoft.com/en-us/library/aa287992.aspx>`_ is applied to methods
   inside a :cs:attr:`KRPCClass`. This allows a client to call methods on an instance, or static
   methods in the class. It can also be applied to a C# extension method, adding a method to a
   class belonging to another service; see :ref:`service-api-extension-members`.

   The method to which this attribute is applied must satisfy the following criteria:

   * The method must be ``public``.

   * The name of the method must be a valid :ref:`kRPC identifier <service-api-identifiers>`.

   * The method must be declared in a :cs:attr:`KRPCClass`, or be a ``public static`` extension
     method of one.

   * The parameter types and return type must be :ref:`types that kRPC can serialize
     <service-api-serializable-types>`.

   * Parameters can have default arguments.

   If the method might return a null value, the ``Nullable`` parameter of the attribute must be set
   to true. A parameter that may be ``null`` must likewise be made nullable. See
   :ref:`service-api-null-values`.

   **Example**

   Declare a ``Remove`` method in the ``Flag`` class:

   .. literalinclude:: /scripts/extending/ClassMethod.lib.cs
      :language: csharp
      :start-after: [docs]
      :end-before: [/docs]
      :dedent:

.. cs:attribute:: KRPCProperty (bool Nullable = false, KRPC.Service.GameScene GameScene = KRPC.Service.GameScene.Inherit)

   :parameters:

    * **Nullable** -- Whether the property can be null. This makes both the getter's return value
      and the setter's argument nullable, and on a field of a :cs:attr:`KRPCStruct` it makes
      the field nullable. Defaults to false.

    * **GameScene** -- The game scenes in which the property is available. Defaults to inherit this
      setting from the class the property is defined in.

   This `attribute <https://msdn.microsoft.com/en-us/library/aa287992.aspx>`_ is applied to class
   properties, and comes in three flavors:

   1. Applied to static properties in a :cs:attr:`KRPCService`. In this case, the property must
      satisfy the following criteria:

      * Must be ``public static`` and have at least one publicly accessible getter or setter.

      * The name of the property must be a valid :ref:`kRPC identifier <service-api-identifiers>`.

      * Must be declared inside a :cs:attr:`KRPCService`.

   2. Applied to non-static properties in a :cs:attr:`KRPCClass`. In this case, the property
      must satisfy the following criteria:

      * Must be ``public`` and *not* ``static``, and have at least one publicly accessible getter or
        setter.

      * The name of the property must be a valid :ref:`kRPC identifier <service-api-identifiers>`.

      * Must be declared inside a :cs:attr:`KRPCClass`.

   3. Applied to a ``public static`` extension method of a :cs:attr:`KRPCClass`, adding a
      property to a class belonging to another service. C# has no extension properties, so the
      getter and setter are a pair of methods; see :ref:`service-api-extension-members`.

   If the property getter might return a null value, or the setter should accept ``null``, the
   ``Nullable`` parameter of the attribute must be set to true. A ``null`` write to a property that
   is not nullable fails with an error. See :ref:`service-api-null-values`.

   **Examples**

   * Applied to a static property in a service:

     .. literalinclude:: /scripts/extending/ServiceProperty.lib.cs
        :language: csharp
        :start-after: [docs]
        :end-before: [/docs]
        :dedent:

     This property can be accessed from a python client as follows:

     .. literalinclude:: /scripts/extending/ServiceProperty.py
        :language: python
        :start-after: [docs]
        :end-before: [/docs]

   * Applied to a non-static property in a class:

     .. literalinclude:: /scripts/extending/ClassProperty.lib.cs
        :language: csharp
        :start-after: [docs]
        :end-before: [/docs]
        :dedent:

.. cs:attribute:: KRPCEnum (string Service)

   :parameters:

    * **Service** -- Optional name of the service to add this enum to. If omitted, the enum is added
      to the service that contains its definition.

   This `attribute <https://msdn.microsoft.com/en-us/library/aa287992.aspx>`_ is applied to
   enumeration types. It adds the enumeration and its permissible values to the server. This
   attribute works similarly to :cs:attr:`KRPCClass`, but is applied to enumeration types.

   A :cs:attr:`KRPCEnum` must be part of a service, just like a
   :cs:attr:`KRPCClass`. Similarly, a :cs:attr:`KRPCEnum` can be declared outside of a
   service if it has its ``Service`` parameter set to the name of the service that it is part of.

   The enumeration type to which this attribute is applied must satisfy the following criteria:

   * The enumeration must be ``public``.

   * The name of the enumeration must be a valid :ref:`kRPC identifier <service-api-identifiers>`.

   * The enumeration must either be declared inside a :cs:attr:`KRPCService`, or have it's
     ``Service`` parameter set to the name of the service it is part of.

   * The `underlying C# type
     <https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/enum>`_ must be an
     ``int``.

   **Examples**

   * Declare an enumeration type with two values:

     .. literalinclude:: /scripts/extending/Enumeration.lib.cs
        :language: csharp
        :start-after: [docs]
        :end-before: [/docs]
        :dedent:

     This can be used from a python client as follows:

     .. literalinclude:: /scripts/extending/Enumeration.py
        :language: python
        :start-after: [docs]
        :end-before: [/docs]

.. cs:attribute:: KRPCStruct (string Service)

   :parameters:

    * **Service** -- Optional name of the service to add this structure to. If omitted, the
      structure is added to the service that contains its definition.

   This `attribute <https://msdn.microsoft.com/en-us/library/aa287992.aspx>`_ is applied to a
   ``struct``. It adds the structure and its fields to the server. A structure is a compound value
   with named fields. Its value is sent to the client in full, where a class sends a reference to
   an object held on the server.

   The fields of the structure are its ``public`` instance properties that are annotated with
   :cs:attr:`KRPCProperty` and have both a getter and a setter, in the order they are declared.
   An unannotated property is invisible to a client.

   A :cs:attr:`KRPCStruct` must be part of a service, just like a
   :cs:attr:`KRPCClass`. Similarly, a :cs:attr:`KRPCStruct` can be declared outside of a
   service if it has its ``Service`` parameter set to the name of the service that it is part of.

   The struct type to which this attribute is applied must satisfy the following criteria:

   * The struct must be ``public`` and must not be generic.

   * The name of the struct must be a valid :ref:`kRPC identifier <service-api-identifiers>`.

   * The struct must either be declared inside a :cs:attr:`KRPCService`, or have its ``Service``
     parameter set to the name of the service it is part of.

   * It must have at least one field, and the type of every field must be a :ref:`serializable type
     <service-api-serializable-types>`.

   * A field must not restrict the game scenes it is available in, so the ``GameScene`` parameter
     of its :cs:attr:`KRPCProperty` attribute must not be set.

   * A field may be nullable. See :ref:`service-api-null-values`.

   * The structure must not contain itself, whether directly or through the fields of another
     structure that it contains.

   Fields may only be **appended** to a structure. A structure's value is sent as the values of its
   fields in order, so reordering them, removing one, or changing the type of one breaks clients
   generated against the previous definition.

   **Choosing between a structure and a class.** A structure sends the values of all of its fields
   in a single procedure call. Use one for compound data whose fields are almost always wanted
   together, such as a position and a velocity. Use a :cs:attr:`KRPCClass` for something with
   mutable state, or whose members are expensive enough to read one at a time.

   **Examples**

   * Declare a structure with three fields:

     .. literalinclude:: /scripts/extending/Structure.lib.cs
        :language: csharp
        :start-after: [docs]
        :end-before: [/docs]
        :dedent:

     This can be used from a python client as follows:

     .. literalinclude:: /scripts/extending/Structure.py
        :language: python
        :start-after: [docs]
        :end-before: [/docs]

   * Declare a structure with two nullable fields:

     .. literalinclude:: /scripts/extending/NullableStructure.lib.cs
        :language: csharp
        :start-after: [docs]
        :end-before: [/docs]
        :dedent:

     A reference-typed field is made nullable by the ``Nullable`` parameter of its
     :cs:attr:`KRPCProperty` attribute. A field whose type is ``Nullable<T>`` needs no
     annotation.

.. cs:attribute:: KRPCException (string Service, Type MappedException)

   :parameters:

    * **Service** -- Optional name of the service to add this enum to. If omitted, the enum is added
      to the service that contains its definition.
    * **MappedException** -- Optional type of an exception to map to this exception. For example,
      can be used to map a built-in C# exception type onto this kRPC exception type.

   This `attribute <https://msdn.microsoft.com/en-us/library/aa287992.aspx>`_ is applied to
   an exception class type.

   A :cs:attr:`KRPCException` must be part of a service, just like a
   :cs:attr:`KRPCClass`. Similarly, a :cs:attr:`KRPCException` can be declared outside of a
   service if it has its ``Service`` parameter set to the name of the service that it is part of.

   The class type to which this attribute is applied must satisfy the following criteria:

   * The class must be ``public``.

   * The name of the class must be a valid :ref:`kRPC identifier <service-api-identifiers>`.

   * The class must either be declared inside a :cs:attr:`KRPCService`, or have it's ``Service``
     parameter set to the name of the service it is part of.

.. cs:attribute:: KRPCDefaultValue (Type ValueConstructor)

   :parameters:

    * **ValueConstructor** -- Type of a static class with a ``Create`` method that returns an
      instance of the default value.

   This `attribute <https://msdn.microsoft.com/en-us/library/aa287992.aspx>`_ can be applied to a
   kRPC procedure/class method parameter. It provides a workaround to set the default value of a
   parameter to a non-compile time constant. Ordinarily, C# only allows compile time constants
   to be used as the values of default arguments.

   The ``ValueConstructor`` parameter is the type of a static class that contains a static method,
   called ``Create``. When invoked, this method should return the default value.

   Note: If you just want to set the default value to a compile time constant, use the C#
   syntax. kRPC will detect the default values and use them.

   **Examples**

   * Set the default value to a list:

     .. literalinclude:: /scripts/extending/DefaultValue.lib.cs
        :language: csharp
        :start-after: [docs]
        :end-before: [/docs]
        :dedent:

   * Set the default value to a compile time constant, which does not require the
     ``KRPCDefaultValue`` attribute:

     .. literalinclude:: /scripts/extending/DefaultConstant.lib.cs
        :language: csharp
        :start-after: [docs]
        :end-before: [/docs]
        :dedent:

.. cs:attribute:: KRPCNullable (params KRPC.Service.Attributes.Position[] Path)

   :parameters:

    * **Path** -- Optional positions to step through to reach the nullable one, outermost first.
      An empty path, the usual case, is the value itself.

   This `attribute <https://msdn.microsoft.com/en-us/library/aa287992.aspx>`_ is applied to a
   kRPC procedure or class method parameter to indicate that the parameter is permitted to be
   ``null``. It can be applied to a parameter of any serializable type, not only class types.
   Applied to a method it marks the return value instead. Applied to a property it marks both
   the getter's return value and the setter's argument, as ``Nullable`` on
   :cs:attr:`KRPCProperty` does.

   The server enforces nullability: a call that passes ``null`` for a parameter that is not
   nullable fails with an error. A parameter is nullable if it is marked with ``KRPCNullable``,
   is assigned a default value of ``null``, or has a ``Nullable<T>`` type. See
   :ref:`service-api-null-values`.

   **Example**

   .. literalinclude:: /scripts/extending/NullableParameter.lib.cs
      :language: csharp
      :start-after: [docs]
      :end-before: [/docs]
      :dedent:

.. _service-api-nullable-positions:

Nullable Positions
""""""""""""""""""

Giving :cs:attr:`KRPCNullable` a path marks a position nested inside a value, rather than the
value itself. Each step names a position of the type the step before it reached, outermost first:

.. literalinclude:: /scripts/extending/NullablePositions.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

Without the ``using static`` each position is written ``Position.Element``, as ``GameScene``
already is on :cs:attr:`KRPCProcedure`.

A path step that names no position of the type it is applied to is an error, which the server
reports when it loads the service. A dictionary key and a set element have no name here, and so
cannot be made nullable. The positions are:

.. cs:enum:: KRPC.Service.Attributes.Position

   .. cs:enumerator:: Element

      The element of a list.

   .. cs:enumerator:: Value

      The value of a dictionary.

   .. cs:enumerator:: Item1

      The first item of a tuple. ``Item2`` to ``Item7`` name the ones after it.

The attribute may be applied more than once, for a type with several nullable positions:

.. literalinclude:: /scripts/extending/NullablePositionsRepeated.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

A nested position whose type is ``Nullable<T>`` needs no attribute, exactly as a parameter of that
type does. ``IList<int?>``, ``IDictionary<string, double?>`` and ``Tuple<int?, string>`` are all
nullable at the position ``Nullable<T>`` marks.

A path that reaches a value-typed position is an error. An ``int``, an ``enum`` and a structure
hold a null only as ``Nullable<T>``, so ``IList<int>`` must be written ``IList<int?>``.

.. _service-api-identifiers:

Identifiers
^^^^^^^^^^^

An identifier can only contain letters and numbers, and must start with an upper case letter.  They
should follow `CamelCase <https://en.wikipedia.org/wiki/CamelCase>`_ capitalization conventions.

.. _service-api-serializable-types:

Serializable Types
^^^^^^^^^^^^^^^^^^

A type can only be used as a parameter or return type if kRPC knows how to serialize it. The
following types are serializable:

* The C# types ``double``, ``float``, ``int``, ``long``, ``uint``, ``ulong``, ``bool``, ``string``
  and ``byte[]``

* Any type annotated with :cs:attr:`KRPCClass`

* Any type annotated with :cs:attr:`KRPCEnum`

* Any type annotated with :cs:attr:`KRPCStruct`

* Collections of serializable types:

  * ``System.Collections.Generic.IList<T>`` where ``T`` is a serializable type

  * ``System.Collections.Generic.IDictionary<K,V>`` where ``K`` is one of ``int``, ``long``,
    ``uint``, ``ulong``, ``bool`` or ``string`` and ``V`` is a serializable type

  * ``System.Collections.Generic.HashSet<V>`` where ``V`` is a serializable type

  * ``System.Tuple<...>`` where all of the tuple's value types are serializable types

* Return types can be ``void``

* Protocol buffer message types from namespace ``KRPC.Service.Messages``

.. _service-api-null-values:

Null Values
^^^^^^^^^^^

A parameter, a return value, a structure field or a position nested inside one of them may be
``null`` only when it is declared *nullable*. This applies uniformly to every serializable
type -- class types, ``string``, collections and value types alike. The server enforces it:

* Passing ``null`` as an argument for a parameter that is not nullable fails the call with an
  error.

* Returning ``null`` from a procedure, method or property getter that is not nullable fails the
  call with an error.

* A ``null`` in a structure field that is not nullable fails the call with an error, naming the
  field.

A **parameter** is nullable if any of the following hold:

* It is annotated with :cs:attr:`KRPCNullable`.

* It has a default value of ``null`` (for example ``Vessel target = null``). A null default is
  itself a declaration that ``null`` is a valid value, so no further annotation is needed.

* Its type is a nullable value type, ``Nullable<T>`` (for example ``int?``); see below.

A **return value** is nullable if the ``Nullable`` parameter of its :cs:attr:`KRPCProcedure`,
:cs:attr:`KRPCMethod` or :cs:attr:`KRPCProperty` attribute is set to true, or if its type
is ``Nullable<T>``. For a property, ``Nullable = true`` makes the getter's return value *and* the
setter's argument nullable.

Return nullability is always declared, as kRPC reads the attribute rather than the method body.

A **structure field** is nullable if the ``Nullable`` parameter of its
:cs:attr:`KRPCProperty` attribute is set, or if its type is ``Nullable<T>``. Setting
``Nullable`` on a value-typed field is an error. Write the field as ``Nullable<T>`` instead.

.. _service-api-nullable-value-types:

Nullable Value Types
""""""""""""""""""""

A parameter, return value or structure field of type ``Nullable<T>`` -- ``int?``, ``float?``,
``bool?``, an ``enum?`` and so on -- is automatically nullable; it needs neither
:cs:attr:`KRPCNullable` nor ``Nullable = true``. To clients it appears as an ordinary value of
type ``T`` that may be null.

Only value types can be ``Nullable<T>``. A `nullable reference type
<https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references>`_ annotation such as
``string?`` or ``IList<int>?`` is a compile-time annotation, and the ``?`` is erased before kRPC
reads the type. Such a parameter or return value is treated as non-nullable.

Use :cs:attr:`KRPCNullable` or ``Nullable = true`` to make a reference-typed parameter,
return value or structure field nullable, and a path on :cs:attr:`KRPCNullable` to make a
reference-typed position nested inside one nullable. See :ref:`service-api-nullable-positions`.

Events
^^^^^^

kRPC procedures, methods and properties can return event objects to clients. This is done using the
class ``KRPC.Service.Event``. This class supports two different types of
event.

Manually Triggered Events
"""""""""""""""""""""""""

This type of event must be triggered by some other piece of code running somewhere in the game. It
is created by calling the default constructor for type ``KRPC.Service.Event``.

For example, the following example is a procedure that returns an event that triggers after a given
number of milliseconds. When the event triggers it is removed.

.. literalinclude:: /scripts/extending/ManualEvent.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

Actively Polled Events
""""""""""""""""""""""

This type of event contains a function that is evaluated once per game update. When the function
returns true, the event is triggered. The event object is passed to the function so that it can
manipulate it as desired.

For example the following creates an event that triggers when the active vessel reaches the given
altitude. The event is removed the first time it triggers.

.. literalinclude:: /scripts/extending/PolledEvent.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

.. _service-api-multi-tick:

Multi-Tick RPCs
^^^^^^^^^^^^^^^

By default kRPC executes each RPC call within a single physics tick. Some operations need to wait
for KSP to process one or more physics updates before their result is available — for example,
staging only takes effect after KSP runs its next ``FixedUpdate``. For these cases a procedure can
yield, by throwing a ``KRPC.Service.YieldException`` to hand a *continuation* back to
kRPC. kRPC suspends the call, allows the next physics tick to run, then invokes the continuation.
The continuation can itself throw another ``YieldException`` to yield again, forming a chain. The
client blocks throughout and receives the final return value (or exception) once the chain
completes.

.. cs:class:: KRPC.Service.YieldException<T>

   Thrown by a procedure or method to suspend execution until the next physics tick. ``T`` must be
   a delegate type. kRPC invokes the delegate on the following ``FixedUpdate`` and uses its return
   value as the result of the RPC call. If the delegate itself throws a ``YieldException``, kRPC
   will yield again.

   The return type of the delegate must match the return type of the RPC procedure or method:

   * ``Action`` for a ``void`` procedure.
   * ``Func<T>`` for a procedure that returns type ``T``.

Examples
""""""""

**Simple retry** — re-invoke the same method on each tick until a condition is met:

.. literalinclude:: /scripts/extending/YieldRetry.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

Each physics tick kRPC calls ``Wait`` again. As long as the condition is not met, the method
yields again by passing itself as the next continuation. Once the condition is satisfied the method
returns normally and the client receives its response.

**Two-phase with closure** — trigger an action, then poll a helper until the result is available:

.. literalinclude:: /scripts/extending/YieldDecouple.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

``Decouple`` fires the decoupler and immediately calls ``PostDecouple``. ``PostDecouple`` yields
for at least 10 ticks (to let KSP settle the new vessel list) and until the ``Decoupled`` flag
becomes true. The closure captures ``preVesselIds`` and increments ``wait`` on each tick so the
helper has the right context every time it runs.

.. _service-api-game-scenes:

Game Scenes
^^^^^^^^^^^

Anything that can be called is configured to be available from a particular game scene, or
scenes. This includes service procedures and properties, and class methods and properties. This is
controlled by the following enumeration:

.. cs:enum:: KRPC.Service.GameScene

   .. cs:enumerator:: Inherit

      Inherit the game scene from the containing service or class. This is the default.

   .. cs:enumerator:: All

      All game scenes.

   .. cs:enumerator:: SpaceCenter

      The game scene showing the Kerbal Space Center buildings.

   .. cs:enumerator:: Flight

      The game scene showing a vessel in flight (or on the launchpad/runway).

   .. cs:enumerator:: TrackingStation

      The tracking station.

   .. cs:enumerator:: EditorVAB

      The Vehicle Assembly Building.

   .. cs:enumerator:: EditorSPH

      The Space Plane Hangar.

   .. cs:enumerator:: Editor

      Either the VAB or the SPH.

   .. cs:enumerator:: MissionBuilder

      The mission builder.

   .. cs:enumerator:: AstronautComplex

      The astronaut complex facility, open within the space center scene.

   .. cs:enumerator:: MissionControl

      The mission control facility, open within the space center scene.

   .. cs:enumerator:: ResearchAndDevelopment

      The research and development facility, open within the space center scene.

   .. cs:enumerator:: Administration

      The administration facility, open within the space center scene.

The special enumeration value :cs:value:`KRPC.Service.GameScene.Inherit` can be used to inherit
the game scene setting from the containing service or class. Game scene inheritance works as
follows:

* A service set to :cs:value:`KRPC.Service.GameScene.Inherit` will be available in all game
  scenes. This is equivalent to setting it to :cs:value:`KRPC.Service.GameScene.All`.

* A service procedure or property set to :cs:value:`KRPC.Service.GameScene.Inherit` will be
  available in the same game scenes as the containing service.

* A class set to :cs:value:`KRPC.Service.GameScene.Inherit` will be available in the same game
  scenes as the containing service.

* A class method or property set to :cs:value:`KRPC.Service.GameScene.Inherit` will be available
  in the same game scenes as the containing class. If the containing class is set to
  :cs:value:`KRPC.Service.GameScene.Inherit` the class method or property will be available in
  the same game scenes as the containing service.

**Examples**

* Declare a service whose procedures are available in the
  :cs:value:`KRPC.Service.GameScene.Flight` game scene:

  .. literalinclude:: /scripts/extending/ServiceInScene.lib.cs
     :language: csharp
     :start-after: [docs]
     :end-before: [/docs]
     :dedent:

* Declare a service whose procedures are available in the
  :cs:value:`KRPC.Service.GameScene.Flight` and :cs:value:`KRPC.Service.GameScene.Editor` game
  scenes:

  .. literalinclude:: /scripts/extending/ServiceInScenes.lib.cs
     :language: csharp
     :start-after: [docs]
     :end-before: [/docs]
     :dedent:

* Declare a service whose procedures are available in the
  :cs:value:`KRPC.Service.GameScene.Flight` game scene by default, and declare a procedure which
  overrides this and is available in all game scenes:

  .. literalinclude:: /scripts/extending/ProcedureInScenes.lib.cs
     :language: csharp
     :start-after: [docs]
     :end-before: [/docs]
     :dedent:

Deprecating Service Members
^^^^^^^^^^^^^^^^^^^^^^^^^^^

To deprecate part of a service's API, annotate it with the standard C# ``System.Obsolete``
attribute. This can be applied to services, procedures, properties, classes, methods and
enumerations. For example:

.. literalinclude:: /scripts/extending/Deprecated.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

The deprecation, along with the message from the attribute, is included in the service definition
and sent to clients over the wire, as the ``deprecated`` and ``deprecated_reason`` fields returned
by ``KRPC.GetServices``. Any ``<see cref='...'/>`` references in the message are resolved to the
name of the referenced member; unqualified names are resolved against the declaring type. Clients then expose the deprecation using each language's own
mechanism: for example, the Python client emits a ``DeprecationWarning`` when a deprecated member
is called, and the generated C#, C++, C-nano and Java client code marks deprecated members with
``[Obsolete]``, ``[[deprecated]]``, ``KRPC_DEPRECATED`` and ``@Deprecated`` respectively.

.. _service-api-extension-members:

Extending Other Services
^^^^^^^^^^^^^^^^^^^^^^^^

A mod can add methods and properties to a class belonging to another service, as
:cs:attr:`KRPCClass` lets it add a class to one. A ``TestFlight`` mod can add
``vessel.parts.with_failure(...)`` to ``SpaceCenter.Parts``. The member is written as a C# extension
method, and joins the service that owns the type being extended:

.. literalinclude:: /scripts/extending/ExtensionMethod.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

The containing class needs no kRPC attribute, only ``public static``. A dynamic client, such as
Python or Lua, exposes the member as an ordinary member of the class it extends. A static client,
such as C#, C++, C-nano or Java, reaches it once its code is regenerated from the mod's assembly
and the assembly declaring the service being extended; see :ref:`service-api-clientgen`.

C# has no extension properties, so a property is a pair of extension methods named ``GetX`` and
``SetX``, each carrying :cs:attr:`KRPCProperty`. The getter takes only the instance and returns
a value. The setter takes the instance and the value to set, and returns nothing. Either one on its
own gives a read-only or a write-only property.

.. literalinclude:: /scripts/extending/ExtensionProperty.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

An extension member must satisfy the following criteria:

* The method must be a ``public static`` C# extension method, declared in a ``public static`` class
  outside any :cs:attr:`KRPCService`. A class that is not public puts its members out of reach,
  and the server names it in a warning in the log.

* The type it extends must be a :cs:attr:`KRPCClass`. A :cs:attr:`KRPCStruct` is a value
  with a fixed set of fields, and cannot be extended.

* The name of the member must be a valid :ref:`kRPC identifier <service-api-identifiers>`, and must
  not clash with a member the class already has, or with another extension member. The getter and
  the setter of an extension property must be declared in the same class.

* The parameter types and return type must be :ref:`types that kRPC can serialize
  <service-api-serializable-types>`, and may name types from any service.

Deprecation and the ``Nullable`` parameter of the attribute work as they do for a member declared
in the class. A setter's value is an ordinary parameter, so mark it nullable with
:cs:attr:`KRPCNullable`. The ``GameScene`` parameter defaults to the game scenes of the class
being extended.

Exposing a Part Module as a Class
"""""""""""""""""""""""""""""""""

A mod that adds a ``PartModule`` to a part can expose it as a class of its own, in place of the
string-keyed ``SpaceCenter.Module`` API. Declare a :cs:attr:`KRPCClass` wrapping the module, and
add a nullable extension property on ``SpaceCenter.Part`` that returns it. The wrapper names the
module with a ``KRPC.SpaceCenter.ModuleRef``, as ``SpaceCenter.Module`` does:

.. literalinclude:: /scripts/extending/PartModuleClass.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

The server keeps the object in a store keyed on the object itself, so the rules the ``SpaceCenter``
classes follow apply here too. A ``ModuleRef`` names one of a part's modules by its class and its
position among the part's modules of that class, which outlives any one of them. The wrapper holds
the part and the reference, and finds the module again on each access.

``Equals`` and ``GetHashCode`` read that identity alone, never a resolved game object, and build the
hash with ``KRPC.Utils.Hash``. The extension property builds a wrapper on each call, so value
identity is what keeps the store to one entry for the module.

``ModuleRef.Get`` raises ``KRPC.Service.KRPC.ObjectDestroyedException`` once the part no longer has
the module. ``ModuleRef.StateOn`` gives the module the state of its part, which is what lets the
store drop the object once the game destroys the part.

The reference records where it last found the module, so the field holding one is declared without
``readonly``. A ``readonly`` field is copied on each access, and the record is written to the copy.

Clients reach the module as ``part.failure_module.failed``.

Documentation
-------------

Documentation can be added using `C# XML documentation
<https://msdn.microsoft.com/en-us/library/aa288481%28v=vs.71%29.aspx>`_. For dynamic clients, such
as the Python and Lua clients, the documentation will be automatically exported to clients when they
connect.

Units and formulas are marked up with ``<unit>`` and ``<math>`` tags:

.. literalinclude:: /scripts/extending/Documentation.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

The API reference typesets them, and client documentation shows them as text, such as ``m/s²`` and
``N·m``. A unit is set upright, and a ``.`` in it joins two factors, as in ``N.m``. Both tags
hold a subset of LaTeX:

.. list-table::
   :header-rows: 1

   * - Markup
     - Example
   * - Superscripts and subscripts
     - ``m^3.s^{-2}``, ``\omega_{max}``
   * - Fractions and roots
     - ``\frac{1}{2}``, ``\sqrt{x + y}``
   * - Text
     - ``\text{air density}``
   * - Operators
     - ``\cdot``, ``\times``, ``\pm``, ``\le``, ``\ge``, ``\approx``, ``\infty``, ``\degree``
   * - Greek letters
     - ``\alpha`` to ``\omega``, and the capitals LaTeX names, such as ``\Delta``

A superscript or subscript must have a Unicode form, so ``^2`` and ``_{max}`` work but ``^q`` does
not. The documentation build rejects markup outside the subset, and ``krpc-clientgen`` warns about
it and writes it as text.

Further Examples
----------------

See the :github-tree:`SpaceCenter service implementation <service/SpaceCenter/src/Services>` for
more extensive examples.

.. _service-api-clientgen:

Generating Service Code for Static Clients
------------------------------------------

Some of the client libraries dynamically construct the code necessary to interact with the server
when they connect. This means that these libraries will automatically pick up changes to service
code. Such client libraries include those for Python and Lua.

Other client libraries required code to be generated and compiled into them statically. They do not
automatically pick up changes to service code. Such client libraries include those for C++ and C#.

Code for these 'static' libraries is generated using the krpc-clientgen tool. This is provided as
part of the `krpctools python package <https://pypi.python.org/pypi/krpctools>`_. It can be
installed using pip:

``pip install krpctools``

You can then run the script from the command line:

.. code-block:: console

   $ krpc-clientgen --help

   usage: krpc-clientgen [-h] [-v] [-o OUTPUT] [--ksp KSP]
                         [--include SERVICE=PATH] [--output-defs OUTPUT_DEFS]
                         language service input [input ...]

   Generate client source code for kRPC services.

   positional arguments:
     language              Language to generate (cnano, cpp, csharp, java,
                           python) or path to generator
     service               Name of service to generate
     input                 Path to service definition JSON file(s) or assembly
                           DLL(s). JSON files can give the definitions of the
                           services whose types a DLL's service uses.

   options:
     -h, --help            show this help message and exit
     -v, --version         show program's version number and exit
     -o OUTPUT, --output OUTPUT
                           Path to write source code to. If not specified, writes
                           source code to standard output.
     --ksp KSP             Path to Kerbal Space Program directory. Required when
                           reading from an assembly DLL(s)
     --include SERVICE=PATH
                           Include the generated code of another service, whose
                           types the service uses, from the given path. Required
                           by the C++ and C nano generators for each such
                           service.
     --output-defs OUTPUT_DEFS
                           When generating client code from a DLL, output the
                           service definitions to the given JSON file

Client code can be generated either directly from an assembly DLL containing the service, or from a
JSON file that has previously been generated from an assembly DLL (using the ``--output-defs``
flag).

.. note::

   Pass every assembly that declares part of the service. A mod adding an extension member to
   another service's class is generated alongside that service's assembly; see
   :ref:`service-api-extension-members`.

A service that uses a structure from another service also needs the definitions of that service.
Pass its JSON file from ``GameData/kRPC`` as another input, e.g. ``KRPC.StdLib.json``.

Generating client code from an assembly DLL requires a copy of Kerbal Space Program and a C# runtime
to be available on the machine. In contrast, generating client code from a JSON file does not have
these requirements and so is more portable.

Example
^^^^^^^

The following demonstrates how to generate code for the C++ and C# clients to interact with the
LaunchControl service, given in an example previously.

krpc-clientgen expects to be passed the location of your copy of Kerbal Space Program, the name of
the language to generate, the name of the service (from the :cs:attr:`KRPCService` attribute), a
path to the assembly containing the service and the path to write the generated code to.

For C++, run the following:

``krpc-clientgen --ksp=/path/to/ksp cpp LaunchControl LaunchControl.dll launch_control.hpp``

To then use the LaunchControl service from C++, you need to link your code against the C++ client
library, and include `launch_control.hpp`. If the service uses types from another service, pass
``--include`` with the path of that service's header, so that `launch_control.hpp` includes it,
e.g. ``--include=SpaceCenter=krpc/services/space_center.hpp``.

For C#, run the following:

``krpc-clientgen --ksp=/path/to/ksp csharp LaunchControl LaunchControl.dll LaunchControl.cs``

To then use the LaunchControl service from a C# client, you need to reference the `KRPC.Client.dll`
and include `LaunchControl.cs` in your project.
