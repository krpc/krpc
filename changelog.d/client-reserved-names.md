## client/js

- Suffix a member named after a member of `Object`, such as `toString`, with an underscore (#1413)
- Suffix a static method named `name`, `length` or `prototype` with an underscore (#1413)
- Suffix a type named `Service`, `Server` or `ClientService` with an underscore (#1413)
- Suffix a service named as a member of the connection, such as `Close`, with an underscore, as
  `conn.close_` (#1413)

## client/lua

- Suffix a member named `is_a` or `class_of` with an underscore (#1413)
- Suffix a service named as a member of the connection, such as `Close`, with an underscore, as
  `conn.close_` (#1413)

## client/python

- Fix a procedure with a parameter named `cls`, `invoke`, `paramTypes` or `returnType` (#1413)
- Fix an enumeration value named `mro`, which now takes a trailing underscore (#1413)
- Suffix a service named as a keyword or a member of the connection, such as `Close`, with an
  underscore, as `conn.close_` (#1413)
- Suffix a type named as a keyword, such as `None`, or as the connection type of its module, such
  as `SpaceCenterConnection`, with an underscore (#1413)

## tools/krpctools

- Fix generated client code for a service that declares a name the code uses, such as a class named
  `Object` or `String`, or a parameter named `self` (#1413)
- Suffix a member or parameter named as a base class method or a name the generated code declares,
  such as `hashCode` in Java or `Bind` in C#, with an underscore (#1413)
- Suffix a type named as a declaration of the generated code, such as `Service` in C# and
  JavaScript, with an underscore (#1413)
- Suffix the C# accessor of a service named as a member of the connection, such as `Dispose`, with
  an underscore (#1413)
- Separate the service with a double underscore in a C-nano enumeration value macro whose service is
  named as a prefix of the library, such as `KRPC_ERROR__<ENUM>_<VALUE>` (#1413)
