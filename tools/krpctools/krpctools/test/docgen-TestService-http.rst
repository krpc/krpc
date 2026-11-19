.. default-domain:: http
.. highlight:: json




.. _http-TestService:

Service documentation string.


.. http:post:: /test-service/add-multiple-values




   :reqjson float x:
   :reqjson int32 y:
   :reqjson int64 z:
   :resjson string value:





.. http:post:: /test-service/add-to-object-dictionary




   :reqjson dictionary<string, TestService.TestClass> d:
   :reqjson string key:
   :resjson dictionary<string, TestService.TestClass> value:





.. http:post:: /test-service/add-to-object-list




   :reqjson list<TestService.TestClass> l:
   :reqjson string value:
   :resjson list<TestService.TestClass> value:





.. http:post:: /test-service/add-to-object-set




   :reqjson set<TestService.TestClass> h:
   :reqjson string value:
   :resjson set<TestService.TestClass> value:





.. http:post:: /test-service/blocking-procedure




   :reqjson int32 n:
   :reqjson int32 sum: Optional, defaults to ``0``.
   :resjson int32 value:





.. http:post:: /test-service/bool-to-string




   :reqjson bool value:
   :resjson string value:





.. http:post:: /test-service/bytes-default




   :reqjson bytes x: Optional, defaults to ``"yv4="``.
   :resjson bytes value:





.. http:post:: /test-service/bytes-to-hex-string




   :reqjson bytes value:
   :resjson string value:





.. http:post:: /test-service/counter




   :reqjson string id: Optional, defaults to ``""``.
   :reqjson int32 divisor: Optional, defaults to ``1``.
   :resjson int32 value:





.. http:post:: /test-service/counter-struct

   Returns a struct whose IntField counts the number of times it has been called,
   so that a stream on it changes on every update.


   :reqjson string id: Optional, defaults to ``""``.
   :resjson TestService.TestStruct value:





.. http:post:: /test-service/create-deprecated-class

   .. warning:: Deprecated. Use :http:post:`/test-service/create-test-object` instead.

   Create an instance of a deprecated class documentation string.


   :resjson TestService.DeprecatedClass value:





.. http:post:: /test-service/create-test-object




   :reqjson string value:
   :resjson TestService.TestClass value:





.. http:post:: /test-service/deprecated-procedure

   .. warning:: Deprecated. Use :http:post:`/test-service/float-to-string` instead.

   Deprecated procedure documentation string.


   :reqjson float value:
   :resjson string value:





.. http:post:: /test-service/deprecated-procedure-no-message

   .. warning:: Deprecated.

   Deprecated procedure with no reason documentation string.


   :reqjson float value:
   :resjson string value:





.. http:get:: /test-service/deprecated-property

   .. warning:: Deprecated. Use :http:get:`/test-service/string-property` instead.

   Deprecated property documentation string.


   :resjson string value:

.. http:put:: /test-service/deprecated-property

   .. warning:: Deprecated. Use :http:get:`/test-service/string-property` instead.

   Deprecated property documentation string.


   :reqjson string value: The new value.




.. http:post:: /test-service/deprecated-struct-procedure

   .. warning:: Deprecated. Use :ref:`TestStruct <http-TestService.TestStruct>` instead.

   Procedure returning a deprecated struct documentation string.


   :resjson TestService.DeprecatedStruct value:





.. http:post:: /test-service/dictionary-default




   :reqjson dictionary<int32, bool> x: Optional, defaults to ``[[1, false], [2, true]]``.
   :resjson dictionary<int32, bool> value:





.. http:post:: /test-service/double-special-defaults

   Procedure whose defaults are the special values of double.


   :reqjson double nan: Optional, defaults to ``"NaN"``.
   :reqjson double infinity: Optional, defaults to ``"Infinity"``.
   :reqjson double negative_infinity: Optional, defaults to ``"-Infinity"``.
   :reqjson double maximum: Optional, defaults to ``1.7976931348623157e+308``.
   :reqjson double lowest: Optional, defaults to ``-1.7976931348623157e+308``.
   :resjson list<double> value:





.. http:post:: /test-service/double-to-string




   :reqjson double value:
   :resjson string value:








.. http:post:: /test-service/echo-dictionary-of-nullable-objects




   :reqjson dictionary<string, TestService.TestClass?> d:
   :resjson dictionary<string, TestService.TestClass?> value:





.. http:post:: /test-service/echo-list-of-nullable-ints




   :reqjson list<int32?> l:
   :resjson list<int32?> value:





.. http:post:: /test-service/echo-list-of-nullable-objects




   :reqjson list<TestService.TestClass?> l:
   :resjson list<TestService.TestClass?> value:





.. http:post:: /test-service/echo-nested-list-of-nullable-objects




   :reqjson list<list<TestService.TestClass?>> l:
   :resjson list<list<TestService.TestClass?>> value:





.. http:post:: /test-service/echo-nested-nullable-struct




   :reqjson TestService.TestNestedNullableStruct value:
   :resjson TestService.TestNestedNullableStruct value:





.. http:post:: /test-service/echo-nullable-bool




   :reqjson bool? value:
   :resjson bool? value:





.. http:post:: /test-service/echo-nullable-bytes




   :reqjson bytes? value:
   :resjson bytes? value:





.. http:post:: /test-service/echo-nullable-dictionary




   :reqjson dictionary<string, int32>? d:
   :resjson dictionary<string, int32>? value:





.. http:post:: /test-service/echo-nullable-double




   :reqjson double? value:
   :resjson double? value:





.. http:post:: /test-service/echo-nullable-enum




   :reqjson TestService.TestEnum? x:
   :resjson TestService.TestEnum? value:





.. http:post:: /test-service/echo-nullable-flags




   :reqjson TestService.TestFlags? x:
   :resjson TestService.TestFlags? value:





.. http:post:: /test-service/echo-nullable-float




   :reqjson float? value:
   :resjson float? value:





.. http:post:: /test-service/echo-nullable-int

   Returns the given value.


   :reqjson int32? value: The value, or ``null``.
   :resjson int32? value: The value.





.. http:post:: /test-service/echo-nullable-list




   :reqjson list<int32>? l:
   :resjson list<int32>? value:





.. http:post:: /test-service/echo-nullable-long




   :reqjson int64? value:
   :resjson int64? value:





.. http:post:: /test-service/echo-nullable-set




   :reqjson set<int32>? h:
   :resjson set<int32>? value:





.. http:post:: /test-service/echo-nullable-string




   :reqjson string? value:
   :resjson string? value:





.. http:post:: /test-service/echo-nullable-tuple




   :reqjson tuple<int32, int64>? t:
   :resjson tuple<int32, int64>? value:





.. http:post:: /test-service/echo-nullable-uint




   :reqjson uint32? value:
   :resjson uint32? value:





.. http:post:: /test-service/echo-nullable-ulong




   :reqjson uint64? value:
   :resjson uint64? value:





.. http:post:: /test-service/echo-string-set




   :reqjson set<string> h:
   :resjson set<string> value:





.. http:post:: /test-service/echo-test-object




   :reqjson TestService.TestClass? value:
   :resjson TestService.TestClass? value:





.. http:post:: /test-service/echo-tuple-with-a-nullable-object




   :reqjson tuple<int32, TestService.TestClass?> t:
   :resjson tuple<int32, TestService.TestClass?> value:





.. http:post:: /test-service/empty-list-default




   :reqjson list<string> x: Optional, defaults to ``[]``.
   :resjson list<string> value:





.. http:post:: /test-service/enum-default




   :reqjson TestService.TestEnum x: Optional, defaults to ``"value_c"``.
   :resjson TestService.TestEnum value:





.. http:post:: /test-service/enum-echo




   :reqjson TestService.TestEnum x:
   :resjson TestService.TestEnum value:





.. http:post:: /test-service/enum-list-default




   :reqjson list<TestService.TestEnum> x: Optional, defaults to ``["value_b", "value_c"]``.
   :resjson list<TestService.TestEnum> value:





.. http:post:: /test-service/enum-to-string




   :reqjson TestService.TestEnum x:
   :resjson string value:





.. http:post:: /test-service/flags-default




   :reqjson TestService.TestFlags x: Optional, defaults to ``["value_a", "value_c"]``.
   :resjson TestService.TestFlags value:





.. http:post:: /test-service/flags-echo




   :reqjson TestService.TestFlags x:
   :resjson TestService.TestFlags value:





.. http:post:: /test-service/flags-list-echo




   :reqjson list<TestService.TestFlags> x:
   :resjson list<TestService.TestFlags> value:





.. http:post:: /test-service/flags-struct-echo




   :reqjson TestService.TestFlagsStruct x:
   :resjson TestService.TestFlagsStruct value:





.. http:post:: /test-service/flags-to-int




   :reqjson TestService.TestFlags x:
   :resjson int32 value:





.. http:post:: /test-service/float-special-defaults

   Procedure whose defaults are the special values of float, plus a finite fraction
   that no float can hold exactly.


   :reqjson float nan: Optional, defaults to ``"NaN"``.
   :reqjson float infinity: Optional, defaults to ``"Infinity"``.
   :reqjson float negative_infinity: Optional, defaults to ``"-Infinity"``.
   :reqjson float maximum: Optional, defaults to ``3.4028235e+38``.
   :reqjson float lowest: Optional, defaults to ``-3.4028235e+38``.
   :reqjson float fraction: Optional, defaults to ``0.1``.
   :resjson list<float> value:





.. http:post:: /test-service/float-to-string

   Procedure documentation string.


   :reqjson float value:
   :resjson string value:





.. http:post:: /test-service/hex-string-to-bytes




   :reqjson string value:
   :resjson bytes value:





.. http:post:: /test-service/increment-dictionary




   :reqjson dictionary<string, int32> d:
   :resjson dictionary<string, int32> value:





.. http:post:: /test-service/increment-list




   :reqjson list<int32> l:
   :resjson list<int32> value:





.. http:post:: /test-service/increment-list-of-structs




   :reqjson list<TestService.TestStruct> l:
   :resjson list<TestService.TestStruct> value:





.. http:post:: /test-service/increment-nested-collection




   :reqjson dictionary<string, list<int32>> d:
   :resjson dictionary<string, list<int32>> value:





.. http:post:: /test-service/increment-set




   :reqjson set<int32> h:
   :resjson set<int32> value:





.. http:post:: /test-service/increment-tuple




   :reqjson tuple<int32, int64> t:
   :resjson tuple<int32, int64> value:





.. http:post:: /test-service/int32-special-defaults

   Procedure whose defaults are the extremes of int.


   :reqjson int32 maximum: Optional, defaults to ``2147483647``.
   :reqjson int32 minimum: Optional, defaults to ``-2147483648``.
   :resjson list<int32> value:





.. http:post:: /test-service/int32-to-string




   :reqjson int32 value:
   :resjson string value:





.. http:post:: /test-service/int64-special-defaults

   Procedure whose defaults are the extremes of long.


   :reqjson int64 maximum: Optional, defaults to ``9223372036854775807``.
   :reqjson int64 minimum: Optional, defaults to ``-9223372036854775808``.
   :resjson list<int64> value:





.. http:post:: /test-service/int64-to-string




   :reqjson int64 value:
   :resjson string value:





.. http:post:: /test-service/int-to-flags




   :reqjson int32 x:
   :resjson TestService.TestFlags value:





.. http:post:: /test-service/join-with-at-sign

   Returns *first_value* and *from* joined by an at
   sign, e.g. ``name@host``. The parameter names are:

   * a name in camel case, and
   * a name that is a keyword in some languages.


   :reqjson string first_value: The string before the at sign.
   :reqjson string from: The string after the at sign.
   :resjson string value: The strings joined.





.. http:post:: /test-service/list-default




   :reqjson list<int32> x: Optional, defaults to ``[1, 2, 3]``.
   :resjson list<int32> value:





.. http:post:: /test-service/nested-struct-echo




   :reqjson TestService.TestNestedStruct x:
   :resjson TestService.TestNestedStruct value:





.. http:post:: /test-service/not-nullable-object




   :reqjson TestService.TestClass value:
   :resjson TestService.TestClass value:





.. http:post:: /test-service/null-default




   :reqjson TestService.TestClass? obj: Optional, defaults to ``null``.
   :resjson string value:





.. http:get:: /test-service/nullable-object




   :resjson TestService.TestClass? value:

.. http:put:: /test-service/nullable-object




   :reqjson TestService.TestClass? value: The new value.




.. http:post:: /test-service/nullable-struct-echo




   :reqjson TestService.TestNullableStruct x:
   :resjson TestService.TestNullableStruct value:





.. http:get:: /test-service/object-property

   A property holding an object.


   :resjson TestService.TestClass? value: The object, or ``null``.

.. http:put:: /test-service/object-property

   A property holding an object.


   :reqjson TestService.TestClass? value: The new value.







.. http:post:: /test-service/optional-arguments




   :reqjson string x:
   :reqjson string y: Optional, defaults to ``"foo"``.
   :reqjson string z: Optional, defaults to ``"bar"``.
   :reqjson TestService.TestClass? obj: Optional, defaults to ``null``.
   :resjson string value:





.. http:post:: /test-service/partly-deprecated-struct-procedure

   Procedure returning a partly deprecated struct documentation string.


   :resjson TestService.PartlyDeprecatedStruct value:








.. http:post:: /test-service/remove-test-object




   :reqjson TestService.TestClass value:





.. http:post:: /test-service/reset-custom-exception-later









.. http:post:: /test-service/reset-invalid-operation-exception-later









.. http:post:: /test-service/return-null-when-not-allowed




   :resjson TestService.TestClass value:





.. http:post:: /test-service/scalar-default




   :reqjson int32 x: Optional, defaults to ``42``.
   :resjson int32 value:





.. http:post:: /test-service/set-default




   :reqjson set<int32> x: Optional, defaults to ``[1, 2, 3]``.
   :resjson set<int32> value:





.. http:post:: /test-service/string-default




   :reqjson string x: Optional, defaults to ``"foo"``.
   :resjson string value:





.. http:get:: /test-service/string-property

   Property documentation string.


   :resjson string value:

.. http:put:: /test-service/string-property

   Property documentation string.


   :reqjson string value: The new value.




.. http:put:: /test-service/string-property-private-get




   :reqjson string value: The new value.




.. http:get:: /test-service/string-property-private-set




   :resjson string value:




.. http:post:: /test-service/string-to-bool




   :reqjson string value:
   :resjson bool value:





.. http:post:: /test-service/string-to-double




   :reqjson string value:
   :resjson double value:





.. http:post:: /test-service/string-to-enum




   :reqjson string value:
   :resjson TestService.TestEnum value:





.. http:post:: /test-service/string-to-float




   :reqjson string value:
   :resjson float value:





.. http:post:: /test-service/string-to-int32




   :reqjson string value:
   :resjson int32 value:





.. http:post:: /test-service/string-to-int64




   :reqjson string value:
   :resjson int64 value:





.. http:post:: /test-service/string-to-uint32




   :reqjson string value:
   :resjson uint32 value:





.. http:post:: /test-service/string-to-uint64




   :reqjson string value:
   :resjson uint64 value:





.. http:post:: /test-service/struct-default




   :reqjson TestService.TestStruct x: Optional, defaults to ``{"int_field": 42, "string_field": "alpha", "enum_field": "value_b", "list_field": [1, 2, 3]}``.
   :resjson TestService.TestStruct value:





.. http:post:: /test-service/struct-echo




   :reqjson TestService.TestStruct x:
   :resjson TestService.TestStruct value:





.. http:post:: /test-service/struct-echo-nullable




   :reqjson TestService.TestStruct? x:
   :resjson TestService.TestStruct? value:





.. http:post:: /test-service/swap-object-tuple




   :reqjson tuple<TestService.TestClass, TestService.TestClass> t:
   :resjson tuple<TestService.TestClass, TestService.TestClass> value:





.. http:post:: /test-service/throw-argument-exception




   :resjson int32 value:





.. http:post:: /test-service/throw-argument-null-exception




   :reqjson string foo:
   :resjson int32 value:





.. http:post:: /test-service/throw-argument-out-of-range-exception




   :reqjson int32 foo:
   :resjson int32 value:





.. http:post:: /test-service/throw-custom-exception

   Throws a :ref:`CustomException <http-TestService.CustomException>`.

   .. note::

      Links to each kind of member: :ref:` <http-TestService>`, :ref:`TestClass <http-TestService.TestClass>`,
      :ref:`TestEnum <http-TestService.TestEnum>`, :ref:`TestStruct <http-TestService.TestStruct>`, :ref:`KRPC.InvalidOperationException <http-KRPC.InvalidOperationException>`,
      :http:post:`/test-service/float-to-string`, :http:get:`/test-service/string-property`,
      :http:put:`/test-service/string-property-private-get`, :http:post:`/test-service/test-class/(id)/float-to-string`,
      :http:post:`/test-service/test-class/static-method`, :http:get:`/test-service/test-class/(id)/int-property`,
      :http:put:`/test-service/test-class/(id)/string-property-private-get`, ``"value_a"`` and
      ``int_field``.


   :resjson int32 value:





.. http:post:: /test-service/throw-custom-exception-later




   :resjson int32 value:





.. http:post:: /test-service/throw-invalid-operation-exception




   :resjson int32 value:





.. http:post:: /test-service/throw-invalid-operation-exception-later




   :resjson int32 value:





.. http:post:: /test-service/throw-mapped-exception

   Throws the system exception of the given name, which the server maps to the KRPC
   exception of that name.


   :reqjson string name:
   :resjson int32 value:





.. http:post:: /test-service/tuple-default




   :reqjson tuple<int32, bool> x: Optional, defaults to ``[1, false]``.
   :resjson tuple<int32, bool> value:





.. http:post:: /test-service/uint32-to-string




   :reqjson uint32 value:
   :resjson string value:





.. http:post:: /test-service/uint64-to-string




   :reqjson uint64 value:
   :resjson string value:





.. http:post:: /test-service/uint32-special-defaults

   Procedure whose default is the largest uint.


   :reqjson uint32 maximum: Optional, defaults to ``4294967295``.
   :resjson list<uint32> value:





.. http:post:: /test-service/uint64-special-defaults

   Procedure whose default is the largest ulong.


   :reqjson uint64 maximum: Optional, defaults to ``18446744073709551615``.
   :resjson list<uint64> value:






.. _http-TestService.DeprecatedClass:

.. rubric:: DeprecatedClass

.. warning:: Deprecated. Use :ref:`TestClass <http-TestService.TestClass>` instead.

Deprecated class documentation string.

A ``DeprecatedClass`` object is passed and returned as its identifier, a number. A path that acts on
an object takes it as ``(id)``.


.. http:post:: /test-service/deprecated-class/(id)/deprecated-method

   .. warning:: Deprecated. Use :http:post:`/test-service/test-class/(id)/get-value` instead.

   Deprecated method documentation string.


   :param id: The identifier of the :ref:`DeprecatedClass <http-TestService.DeprecatedClass>` object.

   :resjson string value:





.. http:post:: /test-service/deprecated-class/(id)/inherited-method

   Method with no deprecation of its own documentation string.


   :param id: The identifier of the :ref:`DeprecatedClass <http-TestService.DeprecatedClass>` object.

   :resjson string value:





.. http:get:: /test-service/deprecated-class/(id)/inherited-property

   Property with no deprecation of its own documentation string.


   :param id: The identifier of the :ref:`DeprecatedClass <http-TestService.DeprecatedClass>` object.

   :resjson string value:

.. http:put:: /test-service/deprecated-class/(id)/inherited-property

   Property with no deprecation of its own documentation string.


   :param id: The identifier of the :ref:`DeprecatedClass <http-TestService.DeprecatedClass>` object.

   :reqjson string value: The new value.




.. http:post:: /test-service/deprecated-class/inherited-static-method

   Static method with no deprecation of its own documentation string.


   :resjson string value:






.. _http-TestService.TestClass:

.. rubric:: TestClass

Class documentation string.

A ``TestClass`` object is passed and returned as its identifier, a number. A path that acts on
an object takes it as ``(id)``.


.. http:post:: /test-service/test-class/(id)/echo-nullable-object




   :param id: The identifier of the :ref:`TestClass <http-TestService.TestClass>` object.

   :reqjson TestService.TestClass? value:
   :resjson TestService.TestClass? value:





.. http:post:: /test-service/test-class/(id)/float-to-string




   :param id: The identifier of the :ref:`TestClass <http-TestService.TestClass>` object.

   :reqjson float x:
   :resjson string value:





.. http:post:: /test-service/test-class/(id)/get-value

   Method documentation string.


   :param id: The identifier of the :ref:`TestClass <http-TestService.TestClass>` object.

   :resjson string value:





.. http:get:: /test-service/test-class/(id)/int-property

   Property documentation string.


   :param id: The identifier of the :ref:`TestClass <http-TestService.TestClass>` object.

   :resjson int32 value:

.. http:put:: /test-service/test-class/(id)/int-property

   Property documentation string.


   :param id: The identifier of the :ref:`TestClass <http-TestService.TestClass>` object.

   :reqjson int32 value: The new value.




.. http:get:: /test-service/test-class/(id)/object-property

   A property of the object, holding another object.


   :param id: The identifier of the :ref:`TestClass <http-TestService.TestClass>` object.

   :resjson TestService.TestClass? value: The other object, or ``null``.

.. http:put:: /test-service/test-class/(id)/object-property

   A property of the object, holding another object.


   :param id: The identifier of the :ref:`TestClass <http-TestService.TestClass>` object.

   :reqjson TestService.TestClass? value: The new value.




.. http:post:: /test-service/test-class/(id)/object-to-string




   :param id: The identifier of the :ref:`TestClass <http-TestService.TestClass>` object.

   :reqjson TestService.TestClass? other:
   :resjson string value:





.. http:post:: /test-service/test-class/(id)/optional-arguments




   :param id: The identifier of the :ref:`TestClass <http-TestService.TestClass>` object.

   :reqjson string x:
   :reqjson string y: Optional, defaults to ``"foo"``.
   :reqjson string z: Optional, defaults to ``"bar"``.
   :reqjson TestService.TestClass? obj: Optional, defaults to ``null``.
   :resjson string value:





.. http:post:: /test-service/test-class/static-method

   Returns static followed by *a* and *b*.


   :reqjson string a: The first string. Optional, defaults to ``""``.
   :reqjson string b: The second string. Optional, defaults to ``""``.
   :resjson string value: The strings joined.





.. http:post:: /test-service/test-class/static-nullable-object




   :reqjson TestService.TestClass? value:
   :resjson TestService.TestClass? value:





.. http:post:: /test-service/test-class/static-optional-arguments




   :reqjson string x:
   :reqjson string y: Optional, defaults to ``"foo"``.
   :reqjson string z: Optional, defaults to ``"bar"``.
   :reqjson TestService.TestClass? obj: Optional, defaults to ``null``.
   :resjson string value:





.. http:put:: /test-service/test-class/(id)/string-property-private-get




   :param id: The identifier of the :ref:`TestClass <http-TestService.TestClass>` object.

   :reqjson string value: The new value.




.. http:get:: /test-service/test-class/(id)/string-property-private-set




   :param id: The identifier of the :ref:`TestClass <http-TestService.TestClass>` object.

   :resjson string value:





.. _http-TestService.DeprecatedEnum:

.. rubric:: DeprecatedEnum

.. warning:: Deprecated. Use :ref:`TestEnum <http-TestService.TestEnum>` instead.

Deprecated enum documentation string.

Passed and returned as the name of one of its values:


``"value_a"``
   Deprecated enum ValueA documentation string.


``"value_b"``
   .. warning:: Deprecated. Use ``"value_a"`` instead.

   Deprecated enum ValueB documentation string.



.. _http-TestService.PartlyDeprecatedEnum:

.. rubric:: PartlyDeprecatedEnum

Partly deprecated enum documentation string.

Passed and returned as the name of one of its values:


``"value_a"``
   Partly deprecated enum ValueA documentation string.


``"value_b"``
   .. warning:: Deprecated. Use ``"value_a"`` instead.

   Partly deprecated enum ValueB documentation string.



.. _http-TestService.TestEnum:

.. rubric:: TestEnum

Enum documentation string.

Passed and returned as the name of one of its values:


``"value_a"``
   Enum ValueA documentation string.


``"value_b"``
   Enum ValueB documentation string.


``"value_c"``
   Enum ValueC documentation string, in :math:`\mathrm{m/s^{2}}`, equal to
   :math:`\frac{1}{2} \rho v^{2}`.



.. _http-TestService.TestFlags:

.. rubric:: TestFlags

Flags documentation string.

A flags enumeration, passed and returned as a list of the names of its values:


``"none"``
   Flags None documentation string.


``"value_a"``
   Flags ValueA documentation string.


``"value_ab"``
   Flags ValueAB documentation string.


``"value_b"``
   Flags ValueB documentation string.


``"value_c"``
   Flags ValueC documentation string.



.. _http-TestService.DeprecatedStruct:

.. rubric:: DeprecatedStruct

.. warning:: Deprecated. Use :ref:`TestStruct <http-TestService.TestStruct>` instead.

Deprecated struct documentation string.

Passed and returned as an object with these fields:


``value`` (int32)
   Deprecated struct Value documentation string.


``old_value`` (int32)
   .. warning:: Deprecated. Use ``value`` instead.

   Deprecated struct OldValue documentation string.



.. _http-TestService.PartlyDeprecatedStruct:

.. rubric:: PartlyDeprecatedStruct

Partly deprecated struct documentation string.

Passed and returned as an object with these fields:


``value`` (int32)
   Partly deprecated struct Value documentation string.


``old_value`` (int32)
   .. warning:: Deprecated. Use ``value`` instead.

   Partly deprecated struct OldValue documentation string.



.. _http-TestService.TestFlagsStruct:

.. rubric:: TestFlagsStruct

Flags struct documentation string.

Passed and returned as an object with these fields:


``flags_field`` (TestService.TestFlags)




.. _http-TestService.TestNestedNullableStruct:

.. rubric:: TestNestedNullableStruct

Nested nullable struct documentation string.

Passed and returned as an object with these fields:


``list_field`` (list<TestService.TestClass?>)



``string_field`` (string)




.. _http-TestService.TestNestedStruct:

.. rubric:: TestNestedStruct

Nested struct documentation string.

Passed and returned as an object with these fields:


``struct_field`` (TestService.TestStruct)



``object_field`` (TestService.TestClass)



``string_field`` (string)




.. _http-TestService.TestNullableStruct:

.. rubric:: TestNullableStruct

Nullable struct documentation string.

Passed and returned as an object with these fields:


``int_field`` (int32)



``nullable_int_field`` (int32?)



``nullable_enum_field`` (TestService.TestEnum?)



``nullable_string_field`` (string?)



``nullable_object_field`` (TestService.TestClass?)




.. _http-TestService.TestStruct:

.. rubric:: TestStruct

Struct documentation string.

Passed and returned as an object with these fields:


``int_field`` (int32)
   Struct IntField documentation string.


``string_field`` (string)



``enum_field`` (TestService.TestEnum)



``list_field`` (list<int32>)




.. _http-TestService.CustomException:

.. rubric:: CustomException



Returned as an error whose ``service`` and ``name`` are ``"TestService"`` and ``"CustomException"``.



.. _http-TestService.DeprecatedException:

.. rubric:: DeprecatedException

.. warning:: Deprecated. Use :ref:`CustomException <http-TestService.CustomException>` instead.

Deprecated exception documentation string.

Returned as an error whose ``service`` and ``name`` are ``"TestService"`` and ``"DeprecatedException"``.
