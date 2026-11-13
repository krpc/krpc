.. default-domain:: js
.. highlight:: js

.. module:: TestService
   :no-index:


.. module:: TestService

Service documentation string.


.. function:: addMultipleValues(x, y, z)



   :param x:
   :type x: ``number``
   :param y:
   :type y: ``number``
   :param z:
   :type z: ``number`` or ``bigint``
   :rtype: ``string``
   




.. function:: addToObjectDictionary(d, key)



   :param d:
   :type d: ``Map`` from ``string`` to :class:`TestService.TestClass`
   :param key:
   :type key: ``string``
   :rtype: ``Map`` from ``string`` to :class:`TestService.TestClass`
   




.. function:: addToObjectList(l, value)



   :param l:
   :type l: ``Array`` of :class:`TestService.TestClass`
   :param value:
   :type value: ``string``
   :rtype: ``Array`` of :class:`TestService.TestClass`
   




.. function:: addToObjectSet(h, value)



   :param h:
   :type h: ``Set`` of :class:`TestService.TestClass`
   :param value:
   :type value: ``string``
   :rtype: ``Set`` of :class:`TestService.TestClass`
   




.. function:: blockingProcedure(n[, sum])



   :param n:
   :type n: ``number``
   :param sum: Defaults to ``0``.
   :type sum: ``number``
   :rtype: ``number``
   




.. function:: boolToString(value)



   :param value:
   :type value: ``boolean``
   :rtype: ``string``
   




.. function:: bytesDefault([x])



   :param x: Defaults to ``new Uint8Array([202, 254])``.
   :type x: ``Uint8Array``
   :rtype: ``Uint8Array``
   




.. function:: bytesToHexString(value)



   :param value:
   :type value: ``Uint8Array``
   :rtype: ``string``
   




.. function:: counter([id[, divisor]])



   :param id: Defaults to ``""``.
   :type id: ``string``
   :param divisor: Defaults to ``1``.
   :type divisor: ``number``
   :rtype: ``number``
   




.. function:: counterStruct([id])

   Returns a struct whose IntField counts the number of times it has been called,
   so that a stream on it changes on every update.

   :param id: Defaults to ``""``.
   :type id: ``string``
   :rtype: :class:`TestService.TestStruct`
   




.. function:: createDeprecatedClass()

   .. warning:: Deprecated. Use :func:`TestService.createTestObject` instead.

   Create an instance of a deprecated class documentation string.

   :rtype: :class:`TestService.DeprecatedClass`
   




.. function:: createTestObject(value)



   :param value:
   :type value: ``string``
   :rtype: :class:`TestService.TestClass`
   




.. function:: deprecatedProcedure(value)

   .. warning:: Deprecated. Use :func:`TestService.floatToString` instead.

   Deprecated procedure documentation string.

   :param value:
   :type value: ``number``
   :rtype: ``string``
   




.. function:: deprecatedProcedureNoMessage(value)

   .. warning:: Deprecated.

   Deprecated procedure with no reason documentation string.

   :param value:
   :type value: ``number``
   :rtype: ``string``
   




.. function:: getDeprecatedProperty()
              setDeprecatedProperty(value)

   .. warning:: Deprecated. Use :func:`TestService.getStringProperty` instead.

   Deprecated property documentation string.

   :Property: Can be read or written
   :rtype: ``string``
   




.. function:: deprecatedStructProcedure()

   .. warning:: Deprecated. Use :class:`TestService.TestStruct` instead.

   Procedure returning a deprecated struct documentation string.

   :rtype: :class:`TestService.DeprecatedStruct`
   




.. function:: dictionaryDefault([x])



   :param x: Defaults to ``new Map([[1, false], [2, true]])``.
   :type x: ``Map`` from ``number`` to ``boolean``
   :rtype: ``Map`` from ``number`` to ``boolean``
   




.. function:: doubleSpecialDefaults([nan[, infinity[, negativeInfinity[, maximum[, lowest]]]]])

   Procedure whose defaults are the special values of double.

   :param nan: Defaults to ``NaN``.
   :type nan: ``number``
   :param infinity: Defaults to ``Infinity``.
   :type infinity: ``number``
   :param negativeInfinity: Defaults to ``-Infinity``.
   :type negativeInfinity: ``number``
   :param maximum: Defaults to ``Number.MAX_VALUE``.
   :type maximum: ``number``
   :param lowest: Defaults to ``-Number.MAX_VALUE``.
   :type lowest: ``number``
   :rtype: ``Array`` of ``number``
   




.. function:: doubleToString(value)



   :param value:
   :type value: ``number``
   :rtype: ``string``
   




.. function:: dumpExpressionTree(expression)

   The expression tree of a function, as text.

   :param expression:
   :type expression: :class:`KRPC.Expression`
   :rtype: ``string``
   




.. function:: echoDictionaryOfNullableObjects(d)



   :param d:
   :type d: ``Map`` from ``string`` to :class:`TestService.TestClass` or ``null``
   :rtype: ``Map`` from ``string`` to :class:`TestService.TestClass` or ``null``
   




.. function:: echoListOfNullableInts(l)



   :param l:
   :type l: ``Array`` of ``number`` or ``null``
   :rtype: ``Array`` of ``number`` or ``null``
   




.. function:: echoListOfNullableObjects(l)



   :param l:
   :type l: ``Array`` of :class:`TestService.TestClass` or ``null``
   :rtype: ``Array`` of :class:`TestService.TestClass` or ``null``
   




.. function:: echoNestedListOfNullableObjects(l)



   :param l:
   :type l: ``Array`` of ``Array`` of :class:`TestService.TestClass` or ``null``
   :rtype: ``Array`` of ``Array`` of :class:`TestService.TestClass` or ``null``
   




.. function:: echoNestedNullableStruct(value)



   :param value:
   :type value: :class:`TestService.TestNestedNullableStruct`
   :rtype: :class:`TestService.TestNestedNullableStruct`
   




.. function:: echoNullableBool(value)



   :param value:
   :type value: ``boolean`` or ``null``
   :rtype: ``boolean`` or ``null``
   




.. function:: echoNullableBytes(value)



   :param value:
   :type value: ``Uint8Array`` or ``null``
   :rtype: ``Uint8Array`` or ``null``
   




.. function:: echoNullableDictionary(d)



   :param d:
   :type d: ``Map`` from ``string`` to ``number`` or ``null``
   :rtype: ``Map`` from ``string`` to ``number`` or ``null``
   




.. function:: echoNullableDouble(value)



   :param value:
   :type value: ``number`` or ``null``
   :rtype: ``number`` or ``null``
   




.. function:: echoNullableEnum(x)



   :param x:
   :type x: :class:`TestService.TestEnum` or ``null``
   :rtype: :class:`TestService.TestEnum` or ``null``
   




.. function:: echoNullableFlags(x)



   :param x:
   :type x: :class:`TestService.TestFlags` or ``null``
   :rtype: :class:`TestService.TestFlags` or ``null``
   




.. function:: echoNullableFloat(value)



   :param value:
   :type value: ``number`` or ``null``
   :rtype: ``number`` or ``null``
   




.. function:: echoNullableInt(value)

   Returns the given value.

   :param value: The value, or ``null``.
   :type value: ``number`` or ``null``
   :returns: The value.
   :rtype: ``number`` or ``null``
   




.. function:: echoNullableList(l)



   :param l:
   :type l: ``Array`` of ``number`` or ``null``
   :rtype: ``Array`` of ``number`` or ``null``
   




.. function:: echoNullableLong(value)



   :param value:
   :type value: ``number`` or ``bigint`` or ``null``
   :rtype: ``bigint`` or ``null``
   




.. function:: echoNullableSet(h)



   :param h:
   :type h: ``Set`` of ``number`` or ``null``
   :rtype: ``Set`` of ``number`` or ``null``
   




.. function:: echoNullableString(value)



   :param value:
   :type value: ``string`` or ``null``
   :rtype: ``string`` or ``null``
   




.. function:: echoNullableTuple(t)



   :param t:
   :type t: ``[number, number | bigint]`` or ``null``
   :rtype: ``[number, bigint]`` or ``null``
   




.. function:: echoNullableUInt(value)



   :param value:
   :type value: ``number`` or ``null``
   :rtype: ``number`` or ``null``
   




.. function:: echoNullableULong(value)



   :param value:
   :type value: ``number`` or ``bigint`` or ``null``
   :rtype: ``bigint`` or ``null``
   




.. function:: echoStringSet(h)



   :param h:
   :type h: ``Set`` of ``string``
   :rtype: ``Set`` of ``string``
   




.. function:: echoTestObject(value)



   :param value:
   :type value: :class:`TestService.TestClass` or ``null``
   :rtype: :class:`TestService.TestClass` or ``null``
   




.. function:: echoTupleWithANullableObject(t)



   :param t:
   :type t: ``[number, TestService.TestClass | null]``
   :rtype: ``[number, TestService.TestClass | null]``
   




.. function:: emptyListDefault([x])



   :param x: Defaults to ``[]``.
   :type x: ``Array`` of ``string``
   :rtype: ``Array`` of ``string``
   




.. function:: enumDefault([x])



   :param x: Defaults to ``TestService.TestEnum.valueC``.
   :type x: :class:`TestService.TestEnum`
   :rtype: :class:`TestService.TestEnum`
   




.. function:: enumEcho(x)



   :param x:
   :type x: :class:`TestService.TestEnum`
   :rtype: :class:`TestService.TestEnum`
   




.. function:: enumListDefault([x])



   :param x: Defaults to ``[TestService.TestEnum.valueB, TestService.TestEnum.valueC]``.
   :type x: ``Array`` of :class:`TestService.TestEnum`
   :rtype: ``Array`` of :class:`TestService.TestEnum`
   




.. function:: enumToString(x)



   :param x:
   :type x: :class:`TestService.TestEnum`
   :rtype: ``string``
   




.. function:: flagsDefault([x])



   :param x: Defaults to ``TestService.TestFlags.valueA.union(TestService.TestFlags.valueC)``.
   :type x: :class:`TestService.TestFlags`
   :rtype: :class:`TestService.TestFlags`
   




.. function:: flagsEcho(x)



   :param x:
   :type x: :class:`TestService.TestFlags`
   :rtype: :class:`TestService.TestFlags`
   




.. function:: flagsListEcho(x)



   :param x:
   :type x: ``Array`` of :class:`TestService.TestFlags`
   :rtype: ``Array`` of :class:`TestService.TestFlags`
   




.. function:: flagsStructEcho(x)



   :param x:
   :type x: :class:`TestService.TestFlagsStruct`
   :rtype: :class:`TestService.TestFlagsStruct`
   




.. function:: flagsToInt(x)



   :param x:
   :type x: :class:`TestService.TestFlags`
   :rtype: ``number``
   




.. function:: floatSpecialDefaults([nan[, infinity[, negativeInfinity[, maximum[, lowest[, fraction]]]]]])

   Procedure whose defaults are the special values of float, plus a finite fraction
   that no float can hold exactly.

   :param nan: Defaults to ``NaN``.
   :type nan: ``number``
   :param infinity: Defaults to ``Infinity``.
   :type infinity: ``number``
   :param negativeInfinity: Defaults to ``-Infinity``.
   :type negativeInfinity: ``number``
   :param maximum: Defaults to ``3.4028235e+38``.
   :type maximum: ``number``
   :param lowest: Defaults to ``-3.4028235e+38``.
   :type lowest: ``number``
   :param fraction: Defaults to ``0.1``.
   :type fraction: ``number``
   :rtype: ``Array`` of ``number``
   




.. function:: floatToString(value)

   Procedure documentation string.

   :param value:
   :type value: ``number``
   :rtype: ``string``
   




.. function:: hexStringToBytes(value)



   :param value:
   :type value: ``string``
   :rtype: ``Uint8Array``
   




.. function:: incrementDictionary(d)



   :param d:
   :type d: ``Map`` from ``string`` to ``number``
   :rtype: ``Map`` from ``string`` to ``number``
   




.. function:: incrementList(l)



   :param l:
   :type l: ``Array`` of ``number``
   :rtype: ``Array`` of ``number``
   




.. function:: incrementListOfStructs(l)



   :param l:
   :type l: ``Array`` of :class:`TestService.TestStruct`
   :rtype: ``Array`` of :class:`TestService.TestStruct`
   




.. function:: incrementNestedCollection(d)



   :param d:
   :type d: ``Map`` from ``string`` to ``Array`` of ``number``
   :rtype: ``Map`` from ``string`` to ``Array`` of ``number``
   




.. function:: incrementSet(h)



   :param h:
   :type h: ``Set`` of ``number``
   :rtype: ``Set`` of ``number``
   




.. function:: incrementTuple(t)



   :param t:
   :type t: ``[number, number | bigint]``
   :rtype: ``[number, bigint]``
   




.. function:: int32SpecialDefaults([maximum[, minimum]])

   Procedure whose defaults are the extremes of int.

   :param maximum: Defaults to ``2147483647``.
   :type maximum: ``number``
   :param minimum: Defaults to ``-2147483648``.
   :type minimum: ``number``
   :rtype: ``Array`` of ``number``
   




.. function:: int32ToString(value)



   :param value:
   :type value: ``number``
   :rtype: ``string``
   




.. function:: int64SpecialDefaults([maximum[, minimum]])

   Procedure whose defaults are the extremes of long.

   :param maximum: Defaults to ``9223372036854775807n``.
   :type maximum: ``number`` or ``bigint``
   :param minimum: Defaults to ``-9223372036854775808n``.
   :type minimum: ``number`` or ``bigint``
   :rtype: ``Array`` of ``bigint``
   




.. function:: int64ToString(value)



   :param value:
   :type value: ``number`` or ``bigint``
   :rtype: ``string``
   




.. function:: intToFlags(x)



   :param x:
   :type x: ``number``
   :rtype: :class:`TestService.TestFlags`
   




.. function:: joinWithAtSign(firstValue, from)

   Returns *firstValue* and *from* joined by an at
   sign, e.g. ``name@host``. The parameter names are:

   * a name in camel case, and
   * a name that is a keyword in some languages.

   :param firstValue: The string before the at sign.
   :type firstValue: ``string``
   :param from: The string after the at sign.
   :type from: ``string``
   :returns: The strings joined.
   :rtype: ``string``
   




.. function:: listDefault([x])



   :param x: Defaults to ``[1, 2, 3]``.
   :type x: ``Array`` of ``number``
   :rtype: ``Array`` of ``number``
   




.. function:: nestedStructEcho(x)



   :param x:
   :type x: :class:`TestService.TestNestedStruct`
   :rtype: :class:`TestService.TestNestedStruct`
   




.. function:: notNullableObject(value)



   :param value:
   :type value: :class:`TestService.TestClass`
   :rtype: :class:`TestService.TestClass`
   




.. function:: nullDefault([obj])



   :param obj:
   :type obj: :class:`TestService.TestClass` or ``null``
   :rtype: ``string``
   




.. function:: getNullableObject()
              setNullableObject(value)



   :Property: Can be read or written
   :rtype: :class:`TestService.TestClass` or ``null``
   




.. function:: nullableStructEcho(x)



   :param x:
   :type x: :class:`TestService.TestNullableStruct`
   :rtype: :class:`TestService.TestNullableStruct`
   




.. function:: getObjectProperty()
              setObjectProperty(value)

   A property holding an object.

   :Property: Can be read or written
   :returns: The object, or ``null``.
   :rtype: :class:`TestService.TestClass` or ``null``
   




.. function:: onTimer(milliseconds[, repeats])



   :param milliseconds:
   :type milliseconds: ``number``
   :param repeats: Defaults to ``1``.
   :type repeats: ``number``
   :rtype: ``never``
   




.. function:: optionalArguments(x[, y[, z[, obj]]])



   :param x:
   :type x: ``string``
   :param y: Defaults to ``"foo"``.
   :type y: ``string``
   :param z: Defaults to ``"bar"``.
   :type z: ``string``
   :param obj:
   :type obj: :class:`TestService.TestClass` or ``null``
   :rtype: ``string``
   




.. function:: partlyDeprecatedStructProcedure()

   Procedure returning a partly deprecated struct documentation string.

   :rtype: :class:`TestService.PartlyDeprecatedStruct`
   




.. function:: removeEventAfter(milliseconds)



   :param milliseconds:
   :type milliseconds: ``number``
   :rtype: ``never``
   




.. function:: removeTestObject(value)



   :param value:
   :type value: :class:`TestService.TestClass`
   




.. function:: resetCustomExceptionLater()



   




.. function:: resetInvalidOperationExceptionLater()



   




.. function:: returnNullWhenNotAllowed()



   :rtype: :class:`TestService.TestClass`
   




.. function:: scalarDefault([x])



   :param x: Defaults to ``42``.
   :type x: ``number``
   :rtype: ``number``
   




.. function:: setDefault([x])



   :param x: Defaults to ``new Set([1, 2, 3])``.
   :type x: ``Set`` of ``number``
   :rtype: ``Set`` of ``number``
   




.. function:: stringDefault([x])



   :param x: Defaults to ``"foo"``.
   :type x: ``string``
   :rtype: ``string``
   




.. function:: getStringProperty()
              setStringProperty(value)

   Property documentation string.

   :Property: Can be read or written
   :rtype: ``string``
   




.. function:: setStringPropertyPrivateGet(value)



   :Property: Write-only, cannot be read
   :rtype: ``string``
   




.. function:: getStringPropertyPrivateSet()



   :Property: Read-only, cannot be set
   :rtype: ``string``
   




.. function:: stringToBool(value)



   :param value:
   :type value: ``string``
   :rtype: ``boolean``
   




.. function:: stringToDouble(value)



   :param value:
   :type value: ``string``
   :rtype: ``number``
   




.. function:: stringToEnum(value)



   :param value:
   :type value: ``string``
   :rtype: :class:`TestService.TestEnum`
   




.. function:: stringToFloat(value)



   :param value:
   :type value: ``string``
   :rtype: ``number``
   




.. function:: stringToInt32(value)



   :param value:
   :type value: ``string``
   :rtype: ``number``
   




.. function:: stringToInt64(value)



   :param value:
   :type value: ``string``
   :rtype: ``bigint``
   




.. function:: stringToUInt32(value)



   :param value:
   :type value: ``string``
   :rtype: ``number``
   




.. function:: stringToUInt64(value)



   :param value:
   :type value: ``string``
   :rtype: ``bigint``
   




.. function:: structDefault([x])



   :param x: Defaults to ``new TestService.TestStruct({ intField: 42, stringField: "alpha", enumField: TestService.TestEnum.valueB, listField: [1, 2, 3] })``.
   :type x: :class:`TestService.TestStruct`
   :rtype: :class:`TestService.TestStruct`
   




.. function:: structEcho(x)



   :param x:
   :type x: :class:`TestService.TestStruct`
   :rtype: :class:`TestService.TestStruct`
   




.. function:: structEchoNullable(x)



   :param x:
   :type x: :class:`TestService.TestStruct` or ``null``
   :rtype: :class:`TestService.TestStruct` or ``null``
   




.. function:: swapObjectTuple(t)



   :param t:
   :type t: ``[TestService.TestClass, TestService.TestClass]``
   :rtype: ``[TestService.TestClass, TestService.TestClass]``
   




.. function:: throwArgumentException()



   :rtype: ``number``
   




.. function:: throwArgumentNullException(foo)



   :param foo:
   :type foo: ``string``
   :rtype: ``number``
   




.. function:: throwArgumentOutOfRangeException(foo)



   :param foo:
   :type foo: ``number``
   :rtype: ``number``
   




.. function:: throwCustomException()

   Throws a :class:`TestService.CustomException`.

   :rtype: ``number``
   

   .. note::

      Links to each kind of member: :mod:`TestService`, :class:`TestService.TestClass`,
      :class:`TestService.TestEnum`, :class:`TestService.TestStruct`, :class:`KRPC.InvalidOperationException`,
      :func:`TestService.floatToString`, :func:`TestService.getStringProperty`,
      :func:`TestService.setStringPropertyPrivateGet`, :meth:`TestService.TestClass.floatToString`,
      :meth:`TestService.TestClass.staticMethod`, :meth:`TestService.TestClass.getIntProperty`,
      :meth:`TestService.TestClass.setStringPropertyPrivateGet`, :attr:`TestService.TestEnum.valueA` and
      :attr:`TestService.TestStruct.intField`.




.. function:: throwCustomExceptionLater()



   :rtype: ``number``
   




.. function:: throwInvalidOperationException()



   :rtype: ``number``
   




.. function:: throwInvalidOperationExceptionLater()



   :rtype: ``number``
   




.. function:: throwMappedException(name)

   Throws the system exception of the given name, which the server maps to the KRPC
   exception of that name.

   :param name:
   :type name: ``string``
   :rtype: ``number``
   




.. function:: tupleDefault([x])



   :param x: Defaults to ``[1, false]``.
   :type x: ``[number, boolean]``
   :rtype: ``[number, boolean]``
   




.. function:: uint32ToString(value)



   :param value:
   :type value: ``number``
   :rtype: ``string``
   




.. function:: uint64ToString(value)



   :param value:
   :type value: ``number`` or ``bigint``
   :rtype: ``string``
   




.. function:: uint32SpecialDefaults([maximum])

   Procedure whose default is the largest uint.

   :param maximum: Defaults to ``4294967295``.
   :type maximum: ``number``
   :rtype: ``Array`` of ``number``
   




.. function:: uint64SpecialDefaults([maximum])

   Procedure whose default is the largest ulong.

   :param maximum: Defaults to ``18446744073709551615n``.
   :type maximum: ``number`` or ``bigint``
   :rtype: ``Array`` of ``bigint``
   





.. class:: DeprecatedClass

   .. warning:: Deprecated. Use :class:`TestService.TestClass` instead.

   Deprecated class documentation string.

   .. method:: deprecatedMethod()

      .. warning:: Deprecated. Use :meth:`TestService.TestClass.getValue` instead.

      Deprecated method documentation string.

      :rtype: ``string``
   

   .. method:: inheritedMethod()

      Method with no deprecation of its own documentation string.

      :rtype: ``string``
   

   .. method:: getInheritedProperty()
               setInheritedProperty(value)

      Property with no deprecation of its own documentation string.

      :Property: Can be read or written
      :rtype: ``string``
   

   .. function:: inheritedStaticMethod()

      Static method with no deprecation of its own documentation string.

      :rtype: ``string``
   



.. class:: TestClass

   Class documentation string.

   .. method:: echoNullableObject(value)



      :param value:
      :type value: :class:`TestService.TestClass` or ``null``
      :rtype: :class:`TestService.TestClass` or ``null``
   

   .. method:: floatToString(x)



      :param x:
      :type x: ``number``
      :rtype: ``string``
   

   .. method:: getValue()

      Method documentation string.

      :rtype: ``string``
   

   .. method:: getIntProperty()
               setIntProperty(value)

      Property documentation string.

      :Property: Can be read or written
      :rtype: ``number``
   

   .. method:: getObjectProperty()
               setObjectProperty(value)

      A property of the object, holding another object.

      :Property: Can be read or written
      :returns: The other object, or ``null``.
      :rtype: :class:`TestService.TestClass` or ``null``
   

   .. method:: objectToString(other)



      :param other:
      :type other: :class:`TestService.TestClass` or ``null``
      :rtype: ``string``
   

   .. method:: optionalArguments(x[, y[, z[, obj]]])



      :param x:
      :type x: ``string``
      :param y: Defaults to ``"foo"``.
      :type y: ``string``
      :param z: Defaults to ``"bar"``.
      :type z: ``string``
      :param obj:
      :type obj: :class:`TestService.TestClass` or ``null``
      :rtype: ``string``
   

   .. function:: staticMethod([a[, b]])

      Returns static followed by *a* and *b*.

      :param a: The first string. Defaults to ``""``.
      :type a: ``string``
      :param b: The second string. Defaults to ``""``.
      :type b: ``string``
      :returns: The strings joined.
      :rtype: ``string``
   

   .. function:: staticNullableObject(value)



      :param value:
      :type value: :class:`TestService.TestClass` or ``null``
      :rtype: :class:`TestService.TestClass` or ``null``
   

   .. function:: staticOptionalArguments(x[, y[, z[, obj]]])



      :param x:
      :type x: ``string``
      :param y: Defaults to ``"foo"``.
      :type y: ``string``
      :param z: Defaults to ``"bar"``.
      :type z: ``string``
      :param obj:
      :type obj: :class:`TestService.TestClass` or ``null``
      :rtype: ``string``
   

   .. method:: setStringPropertyPrivateGet(value)



      :Property: Write-only, cannot be read
      :rtype: ``string``
   

   .. method:: getStringPropertyPrivateSet()



      :Property: Read-only, cannot be set
      :rtype: ``string``
   



.. class:: DeprecatedEnum

   .. warning:: Deprecated. Use :class:`TestService.TestEnum` instead.

   Deprecated enum documentation string.


   .. attribute:: valueA

      Deprecated enum ValueA documentation string.


   .. attribute:: valueB

      .. warning:: Deprecated. Use :attr:`TestService.DeprecatedEnum.valueA` instead.

      Deprecated enum ValueB documentation string.



.. class:: PartlyDeprecatedEnum

   Partly deprecated enum documentation string.


   .. attribute:: valueA

      Partly deprecated enum ValueA documentation string.


   .. attribute:: valueB

      .. warning:: Deprecated. Use :attr:`TestService.PartlyDeprecatedEnum.valueA` instead.

      Partly deprecated enum ValueB documentation string.



.. class:: TestEnum

   Enum documentation string.


   .. attribute:: valueA

      Enum ValueA documentation string.


   .. attribute:: valueB

      Enum ValueB documentation string.


   .. attribute:: valueC

      Enum ValueC documentation string, in :math:`\mathrm{m/s^{2}}`, equal to
      :math:`\frac{1}{2} \rho v^{2}`.



.. class:: TestFlags

   Flags documentation string.

   A flags enumeration. :std:ref:`js-client-flags` shows how to combine its values.


   .. attribute:: none

      Flags None documentation string.


   .. attribute:: valueA

      Flags ValueA documentation string.


   .. attribute:: valueAB

      Flags ValueAB documentation string.


   .. attribute:: valueB

      Flags ValueB documentation string.


   .. attribute:: valueC

      Flags ValueC documentation string.



.. class:: DeprecatedStruct(fields)

   .. warning:: Deprecated. Use :class:`TestService.TestStruct` instead.

   Deprecated struct documentation string.


   .. attribute:: value

      Deprecated struct Value documentation string.

      :Type: ``number``

   .. attribute:: oldValue

      .. warning:: Deprecated. Use :attr:`TestService.DeprecatedStruct.value` instead.

      Deprecated struct OldValue documentation string.

      :Type: ``number``


.. class:: PartlyDeprecatedStruct(fields)

   Partly deprecated struct documentation string.


   .. attribute:: value

      Partly deprecated struct Value documentation string.

      :Type: ``number``

   .. attribute:: oldValue

      .. warning:: Deprecated. Use :attr:`TestService.PartlyDeprecatedStruct.value` instead.

      Partly deprecated struct OldValue documentation string.

      :Type: ``number``


.. class:: TestFlagsStruct(fields)

   Flags struct documentation string.


   .. attribute:: flagsField



      :Type: :class:`TestService.TestFlags`


.. class:: TestNestedNullableStruct(fields)

   Nested nullable struct documentation string.


   .. attribute:: listField



      :Type: ``Array`` of :class:`TestService.TestClass` or ``null``

   .. attribute:: stringField



      :Type: ``string``


.. class:: TestNestedStruct(fields)

   Nested struct documentation string.


   .. attribute:: structField



      :Type: :class:`TestService.TestStruct`

   .. attribute:: objectField



      :Type: :class:`TestService.TestClass`

   .. attribute:: stringField



      :Type: ``string``


.. class:: TestNullableStruct(fields)

   Nullable struct documentation string.


   .. attribute:: intField



      :Type: ``number``

   .. attribute:: nullableIntField



      :Type: ``number`` or ``null``

   .. attribute:: nullableEnumField



      :Type: :class:`TestService.TestEnum` or ``null``

   .. attribute:: nullableStringField



      :Type: ``string`` or ``null``

   .. attribute:: nullableObjectField



      :Type: :class:`TestService.TestClass` or ``null``


.. class:: TestStruct(fields)

   Struct documentation string.


   .. attribute:: intField

      Struct IntField documentation string.

      :Type: ``number``

   .. attribute:: stringField



      :Type: ``string``

   .. attribute:: enumField



      :Type: :class:`TestService.TestEnum`

   .. attribute:: listField



      :Type: ``Array`` of ``number``


.. class:: CustomException



   A subclass of :class:`~@krpc-org/krpc.ServerError`.



.. class:: DeprecatedException

   .. warning:: Deprecated. Use :class:`TestService.CustomException` instead.

   Deprecated exception documentation string.

   A subclass of :class:`~@krpc-org/krpc.ServerError`.
