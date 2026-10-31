.. _service-api-attributes:

Attributes
==========

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

     .. literalinclude:: /scripts/extending/attributes/ServiceAttribute.lib.cs
        :language: csharp
        :start-after: [docs]
        :end-before: [/docs]
        :dedent:

   * Declare a service called ``MyEVAService`` (different to the name of the class):

     .. literalinclude:: /scripts/extending/attributes/ServiceName.lib.cs
        :language: csharp
        :start-after: [docs]
        :end-before: [/docs]
        :dedent:

   * Declare a service called ``FlightTools`` that is only available during the ``Flight`` game
     scene:

     .. literalinclude:: /scripts/extending/attributes/ServiceGameScene.lib.cs
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

   .. literalinclude:: /scripts/extending/attributes/PlantFlag.lib.cs
      :language: csharp
      :start-after: [docs]
      :end-before: [/docs]
      :dedent:

   This can be called from a python client as follows:

   .. literalinclude:: /scripts/extending/attributes/PlantFlag.py
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

     .. literalinclude:: /scripts/extending/attributes/NestedClass.lib.cs
        :language: csharp
        :start-after: [docs]
        :end-before: [/docs]
        :dedent:

   * Declare a class called ``Flag``, without nesting the class definition in a service class:

     .. literalinclude:: /scripts/extending/attributes/ClassAttribute.lib.cs
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

   .. literalinclude:: /scripts/extending/attributes/ClassMethod.lib.cs
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

     .. literalinclude:: /scripts/extending/attributes/ServiceProperty.lib.cs
        :language: csharp
        :start-after: [docs]
        :end-before: [/docs]
        :dedent:

     This property can be accessed from a python client as follows:

     .. literalinclude:: /scripts/extending/attributes/ServiceProperty.py
        :language: python
        :start-after: [docs]
        :end-before: [/docs]

   * Applied to a non-static property in a class:

     .. literalinclude:: /scripts/extending/attributes/ClassProperty.lib.cs
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

     .. literalinclude:: /scripts/extending/attributes/Enumeration.lib.cs
        :language: csharp
        :start-after: [docs]
        :end-before: [/docs]
        :dedent:

     This can be used from a python client as follows:

     .. literalinclude:: /scripts/extending/attributes/Enumeration.py
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

     .. literalinclude:: /scripts/extending/attributes/Structure.lib.cs
        :language: csharp
        :start-after: [docs]
        :end-before: [/docs]
        :dedent:

     This can be used from a python client as follows:

     .. literalinclude:: /scripts/extending/attributes/Structure.py
        :language: python
        :start-after: [docs]
        :end-before: [/docs]

   * Declare a structure with two nullable fields:

     .. literalinclude:: /scripts/extending/attributes/NullableStructure.lib.cs
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

     .. literalinclude:: /scripts/extending/attributes/DefaultValue.lib.cs
        :language: csharp
        :start-after: [docs]
        :end-before: [/docs]
        :dedent:

   * Set the default value to a compile time constant, which does not require the
     ``KRPCDefaultValue`` attribute:

     .. literalinclude:: /scripts/extending/attributes/DefaultConstant.lib.cs
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

   .. literalinclude:: /scripts/extending/attributes/NullableParameter.lib.cs
      :language: csharp
      :start-after: [docs]
      :end-before: [/docs]
      :dedent:

.. _service-api-nullable-positions:

Nullable Positions
------------------

Giving :cs:attr:`KRPCNullable` a path marks a position nested inside a value, rather than the
value itself. Each step names a position of the type the step before it reached, outermost first:

.. literalinclude:: /scripts/extending/attributes/NullablePositions.lib.cs
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

.. literalinclude:: /scripts/extending/attributes/NullablePositionsRepeated.lib.cs
   :language: csharp
   :start-after: [docs]
   :end-before: [/docs]
   :dedent:

A nested position whose type is ``Nullable<T>`` needs no attribute, exactly as a parameter of that
type does. ``IList<int?>``, ``IDictionary<string, double?>`` and ``Tuple<int?, string>`` are all
nullable at the position ``Nullable<T>`` marks.

A path that reaches a value-typed position is an error. An ``int``, an ``enum`` and a structure
hold a null only as ``Nullable<T>``, so ``IList<int>`` must be written ``IList<int?>``.
