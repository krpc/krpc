.. default-domain:: js
.. highlight:: js

.. module:: KRPC
   :no-index:


.. module:: KRPC

Main kRPC service, used by clients to interact with basic server functionality.


.. function:: getClients()

   The clients that are currently connected to the server.

   :Property: Read-only, cannot be set
   :rtype: ``Array`` of :class:`KRPC.ClientInfo`
   




.. function:: getGameScene()
              setGameScene(value)

   The current game scene. Setting this switches the game to the given
   scene, or opens/closes the corresponding facility for the pseudo-scenes.
   Scene changes happen asynchronously: setting this property returns
   immediately, and clients should poll it until it reports the requested
   scene. Setting it to :attr:`KRPC.GameScene.flight` resumes the save's
   active vessel, and fails if there is none. Moving between
   :attr:`KRPC.GameScene.editorVAB` and :attr:`KRPC.GameScene.editorSPH`
   keeps the vessel that is being constructed, as the game's own switch
   editor button does. Leaving flight saves the game first, unless the game
   cannot be saved, e.g. while under acceleration.

   :Property: Can be read or written
   :rtype: :class:`KRPC.GameScene`
   




.. function:: getGameStateLoads()

   The number of game states that have finished loading.

   :Property: Read-only, cannot be set
   :rtype: ``number``
   

   .. note::

      Increases once a game load, quickload, revert or scene change has finished.




.. function:: getClientID()

   Returns the identifier for the current client.

   :rtype: ``Uint8Array``
   




.. function:: getClientName()

   Returns the name of the current client.
   This is an empty string if the client has no name.

   :rtype: ``string``
   




.. function:: getServices()

   The services the server provides, with their procedures, classes, enumerations,
   structures and exceptions.

   :rtype: ``schema.Services``
   




.. function:: getStatus()

   Returns the status of the server, with its version, settings and performance statistics.

   :rtype: :class:`KRPC.ServerStatus`
   




.. function:: holdTick()

   Hold the game on its current physics tick until :func:`KRPC.releaseTick` is called
   or the hold times out. Every call made in between is executed before the game
   advances, so a read, compute and write all happen in the state of one tick.

   

   .. note::

      The game renders no frame, takes no input and runs no physics while the tick is
      held. Keep a hold as short as possible, and release it even when the code between
      the two calls fails.

      A tick is held once, so this waits for the next tick when the current one has
      already been released. One client holds the tick at a time, and only a client
      making a call can hold it: a stream or an event cannot.

      A hold ends by itself when it times out, when the client disconnects, and when the
      client calls a procedure that takes more than one tick to finish, such as staging
      or warping. Stream updates are sent once the tick is released, so waiting for a
      stream or an event inside a hold waits out the timeout.




.. function:: nextTick()

   Let the game move on from the tick being held and hold the one after it, so that a
   program acts on consecutive physics ticks. Holds the soonest tick it can if no tick
   is currently being held.

   

   .. note::

      A release and a hold made as two separate calls let the game advance between them.
      This takes the next tick without that gap. Call it at the top of a loop that has to
      act on every tick, work, and come back to it.

      The game advances one tick per call, so it runs at the rate of the loop for as long
      as the loop runs. End the loop with :func:`KRPC.releaseTick`.

      Every limit of :func:`KRPC.holdTick` applies, the timeout included. A hold lost to
      the timeout is not reported, and the next call takes whatever tick it lands on.




.. function:: getPaused()
              setPaused(value)

   Whether the game is paused.

   :Property: Can be read or written
   :rtype: ``boolean``
   




.. function:: releaseTick()

   Let the game move on from the tick that :func:`KRPC.holdTick` held it on. Does
   nothing when no tick is held, which is the case once a hold has timed out.

   





.. class:: Expression

   A server side expression.

   .. note::

      Numeric operands of differing types are converted to a common type.

   .. function:: add(arg0, arg1)

      Numerical addition.

      :param arg0: The first operand.
      :type arg0: :class:`KRPC.Expression`
      :param arg1: The second operand.
      :type arg1: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

   .. function:: aggregate(arg, func)

      The result of applying an accumulator function over a collection.

      :param arg: The collection.
      :type arg: :class:`KRPC.Expression`
      :param func: The accumulator function.
      :type func: :class:`KRPC.Expression`
      :returns: The accumulated value.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Throws an exception if the collection is empty.

   .. function:: aggregateWithSeed(arg, seed, func)

      The result of applying an accumulator function over a collection, starting from a seed.

      :param arg: The collection.
      :type arg: :class:`KRPC.Expression`
      :param seed: The seed value.
      :type seed: :class:`KRPC.Expression`
      :param func: The accumulator function.
      :type func: :class:`KRPC.Expression`
      :returns: The accumulated value.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The seed widens to the type of the accumulated value, the function's result.

   .. function:: all(arg, predicate)

      Whether a predicate function returns true for every value of a collection.

      :param arg: The collection.
      :type arg: :class:`KRPC.Expression`
      :param predicate: The predicate function.
      :type predicate: :class:`KRPC.Expression`
      :returns: Whether all items satisfy the predicate.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: and(arg0, arg1)

      Logical or bitwise and, which evaluates both operands.

      :param arg0: The first operand.
      :type arg0: :class:`KRPC.Expression`
      :param arg1: The second operand.
      :type arg1: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Also takes two values of a flags enumeration, and gives the values they share.

   .. function:: any(arg, predicate)

      Whether a predicate function returns true for any value of a collection.

      :param arg: The collection.
      :type arg: :class:`KRPC.Expression`
      :param predicate: The predicate function.
      :type predicate: :class:`KRPC.Expression`
      :returns: Whether any item satisfies the predicate.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: append(collection, value)

      A statement adding a value to a list or a set.

      :param collection: The list or set to add to.
      :type collection: :class:`KRPC.Expression`
      :param value: The value to add.
      :type value: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The value goes on the end of a list. A set holding the value is left unchanged.

   .. function:: assign(variable, value)

      An assignment to a local variable, state variable or function parameter.

      :param variable: The variable to assign to.
      :type variable: :class:`KRPC.Expression`
      :param value: The value to assign.
      :type value: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The value's type must be assignable to the variable's type. A numeric value of a
         different type is widened, and narrowing it requires a cast.

   .. function:: average(arg)

      The average of the values of a collection.

      :param arg: The list or set.
      :type arg: :class:`KRPC.Expression`
      :returns: The average of the elements in the collection.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Throws an exception if the collection is empty and its values are not nullable.

   .. function:: block(statements)

      A block of statements, whose value is that of its last statement.

      :param statements: The statements.
      :type statements: ``Array`` of :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

   .. function:: blockWithVariables(variables, statements)

      A block of statements with local variables.

      :param variables: The local variables of the block.
      :type variables: ``Array`` of :class:`KRPC.Expression`
      :param statements: The statements.
      :type statements: ``Array`` of :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The value of the block is that of its last statement. The variables are in scope
         for the statements of the block, including within nested functions.

   .. function:: break()

      A break statement, which ends the enclosing loop or :meth:`KRPC.Expression.switch`.

      :rtype: :class:`KRPC.Expression`
   

   .. function:: buildDictionary(arg, keyFunc, valueFunc)

      A dictionary built from a collection, with a key and a value computed from each
      element.

      :param arg: The list or set.
      :type arg: :class:`KRPC.Expression`
      :param keyFunc: The function computing an element's key.
      :type keyFunc: :class:`KRPC.Expression`
      :param valueFunc: The function computing an element's value.
      :type valueFunc: :class:`KRPC.Expression`
      :returns: The dictionary.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         A repeated key takes the last of its values.

   .. function:: call(call)

      An RPC call.

      :param call: The procedure call.
      :type call: ``schema.ProcedureCall``
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The instance the call is made on, and the values of its arguments, are fixed
         when the expression is created. A call to a procedure with no return value is
         evaluated for its effects, as a statement in a :meth:`KRPC.Expression.block`. A collection the
         procedure returns is copied, as a client would receive it.

   .. function:: callWithArguments(call, args)

      An RPC call, where some or all of the arguments are computed by expressions.

      :param call: The RPC to call.
      :type call: ``schema.ProcedureCall``
      :param args: Expressions computing the call's arguments, by position.
      :type args: ``Map`` from ``number`` to :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Position 0 is the instance of a class method or property. A position with no
         expression takes the argument encoded in the call, or the parameter's default. A
         collection the procedure returns is copied, as a client would receive it.

   .. function:: cast(arg, type)

      A value cast to the given type.

      :param arg: The value to cast.
      :type arg: :class:`KRPC.Expression`
      :param type: Type to cast the argument to.
      :type type: :class:`KRPC.Type`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         A tuple or collection is cast element by element.

   .. function:: clear(collection)

      A statement removing every value from a list or a set, or every entry from a dictionary.

      :param collection: The list, set or dictionary.
      :type collection: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

   .. function:: concat(arg1, arg2)

      The values of one collection followed by those of another.

      :param arg1: The first collection.
      :type arg1: :class:`KRPC.Expression`
      :param arg2: The second collection.
      :type arg2: :class:`KRPC.Expression`
      :returns: The first collection followed by the second.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.toList` makes it concrete.

   .. function:: conditional(condition, ifTrue, ifFalse)

      The second argument if the condition is true, and the third argument otherwise.

      :param condition: The boolean condition.
      :type condition: :class:`KRPC.Expression`
      :param ifTrue: The value when the condition is true.
      :type ifTrue: :class:`KRPC.Expression`
      :param ifFalse: The value when the condition is false.
      :type ifFalse: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Throws an exception if the two values have no common type.

   .. function:: conditionalAnd(arg0, arg1)

      Logical and, which evaluates the second operand only when the first is true.

      :param arg0: The first operand.
      :type arg0: :class:`KRPC.Expression`
      :param arg1: The second operand.
      :type arg1: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

   .. function:: conditionalOr(arg0, arg1)

      Logical or, which evaluates the second operand only when the first is false.

      :param arg0: The first operand.
      :type arg0: :class:`KRPC.Expression`
      :param arg1: The second operand.
      :type arg1: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

   .. function:: constantBool(value)

      A constant value of boolean type.

      :param value: The value.
      :type value: ``boolean``
      :rtype: :class:`KRPC.Expression`
   

   .. function:: constantBytes(value)

      A constant value of bytes type.

      :param value: The value.
      :type value: ``Uint8Array``
      :rtype: :class:`KRPC.Expression`
   

   .. function:: constantDouble(value)

      A constant value of double precision floating point type.

      :param value: The value.
      :type value: ``number``
      :rtype: :class:`KRPC.Expression`
   

   .. function:: constantEnum(service, name, value)

      A constant value of an enumeration a service defines.

      :param service: The name of the service the enumeration is defined in.
      :type service: ``string``
      :param name: The name of the enumeration.
      :type name: ``string``
      :param value: The value of the member of the enumeration.
      :type value: ``number``
      :rtype: :class:`KRPC.Expression`
   

   .. function:: constantEnumById(serviceId, enumerationId, value)

      A constant value of an enumeration a service defines, named by ids.

      :param serviceId: The id of the service the enumeration is defined in.
      :type serviceId: ``number``
      :param enumerationId: The id of the enumeration.
      :type enumerationId: ``number``
      :param value: The value of the member of the enumeration.
      :type value: ``number``
      :rtype: :class:`KRPC.Expression`
   

   .. function:: constantFloat(value)

      A constant value of single precision floating point type.

      :param value: The value.
      :type value: ``number``
      :rtype: :class:`KRPC.Expression`
   

   .. function:: constantInt(value)

      A constant value of integer type.

      :param value: The value.
      :type value: ``number``
      :rtype: :class:`KRPC.Expression`
   

   .. function:: constantLong(value)

      A constant value of long integer type.

      :param value: The value.
      :type value: ``number`` or ``bigint``
      :rtype: :class:`KRPC.Expression`
   

   .. function:: constantNull(type)

      A null constant of the given type.

      :param type: The type of the value.
      :type type: :class:`KRPC.Type`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The type is made nullable, as by :meth:`KRPC.Type.nullableType`.

   .. function:: constantObject(value)

      A constant object, an instance of a class a service defines.

      :param value: The object identifier of the object.
      :type value: ``number`` or ``bigint``
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The object is given by its object identifier.

   .. function:: constantString(value)

      A constant value of string type.

      :param value: The value.
      :type value: ``string``
      :rtype: :class:`KRPC.Expression`
   

   .. function:: constantUInt(value)

      A constant value of unsigned integer type.

      :param value: The value.
      :type value: ``number``
      :rtype: :class:`KRPC.Expression`
   

   .. function:: constantULong(value)

      A constant value of unsigned long integer type.

      :param value: The value.
      :type value: ``number`` or ``bigint``
      :rtype: :class:`KRPC.Expression`
   

   .. function:: contains(arg, value)

      Whether a collection contains a value.

      :param arg: The collection.
      :type arg: :class:`KRPC.Expression`
      :param value: The value to look for.
      :type value: :class:`KRPC.Expression`
      :returns: Whether the collection contains the value.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: containsKey(dictionary, key)

      Whether a dictionary contains a key.

      :param dictionary: The dictionary.
      :type dictionary: :class:`KRPC.Expression`
      :param key: The key to look for.
      :type key: :class:`KRPC.Expression`
      :returns: Whether the dictionary contains the key.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: continue()

      A continue statement, which skips to the next iteration of the enclosing loop.

      :rtype: :class:`KRPC.Expression`
   

   .. function:: convertToString(arg)

      The string representation of a value.

      :param arg: The value to convert.
      :type arg: :class:`KRPC.Expression`
      :returns: The string representation of the value.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         A value other than a number, boolean, string or enumeration converts as .NET
         formats it, which for a collection is the name of its type. Throws an exception if
         the value is null.

   .. function:: count(arg)

      The number of values in a collection.

      :param arg: The list, set or dictionary.
      :type arg: :class:`KRPC.Expression`
      :returns: The number of elements in the collection.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: createDictionary(keys, values)

      A dictionary of the given keys and corresponding values, with numbers widened to
      their common type. Throws an exception if there are no keys, the counts differ, the
      keys are not of a valid key type, or a key is null.

      :param keys: The keys.
      :type keys: ``Array`` of :class:`KRPC.Expression`
      :param values: The values.
      :type values: ``Array`` of :class:`KRPC.Expression`
      :returns: The dictionary.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         A repeated key takes the last of its values.

   .. function:: createEmptyDictionary(keyType, valueType)

      An empty dictionary of the given key and value types. Throws an exception if the
      key type is nullable or not a valid key type.

      :param keyType: The type of the dictionary's keys.
      :type keyType: :class:`KRPC.Type`
      :param valueType: The type of the dictionary's values.
      :type valueType: :class:`KRPC.Type`
      :returns: The empty dictionary.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: createEmptyList(valueType)

      An empty list of the given value type.

      :param valueType: The type of the values the list holds.
      :type valueType: :class:`KRPC.Type`
      :returns: The empty list.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: createEmptySet(valueType)

      An empty set of the given value type. Throws an exception if the value type is
      nullable.

      :param valueType: The type of the values the set holds.
      :type valueType: :class:`KRPC.Type`
      :returns: The empty set.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: createList(values)

      A list of the given values, with numbers widened to their common type. Throws an
      exception if there are no values.

      :param values: The values.
      :type values: ``Array`` of :class:`KRPC.Expression`
      :returns: The list.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: createSet(values)

      A set of the given values, with numbers widened to their common type. Throws an
      exception if there are no values, or a value is null.

      :param values: The values.
      :type values: ``Set`` of :class:`KRPC.Expression`
      :returns: The set.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The values are evaluated in an unspecified order.

   .. function:: createStruct(type, fieldValues)

      A structure with the given field values. Throws an exception if the field values do
      not match its fields.

      :param type: The type of the structure.
      :type type: :class:`KRPC.Type`
      :param fieldValues: The values of the structure's fields, in the order the structure declares them.
      :type fieldValues: ``Array`` of :class:`KRPC.Expression`
      :returns: The structure.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: createTuple(elements)

      A tuple of the given elements. Throws an exception if there are no elements or more
      than seven.

      :param elements: The elements.
      :type elements: ``Array`` of :class:`KRPC.Expression`
      :returns: The tuple.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: dictionaryKeys(dictionary)

      The keys of a dictionary, as a list.

      :param dictionary: The dictionary.
      :type dictionary: :class:`KRPC.Expression`
      :returns: The keys of the dictionary.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The collection operations take the keys of a dictionary from here, and its values
         from :meth:`KRPC.Expression.dictionaryValues`.

   .. function:: dictionaryValues(dictionary)

      The values of a dictionary, as a list.

      :param dictionary: The dictionary.
      :type dictionary: :class:`KRPC.Expression`
      :returns: The values of the dictionary.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: distinct(arg)

      The values of a collection with duplicates removed, keeping the first
      occurrence of each.

      :param arg: The collection.
      :type arg: :class:`KRPC.Expression`
      :returns: The distinct values of the collection.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.toList` makes it concrete.

   .. function:: divide(arg0, arg1)

      Numerical division.

      :param arg0: The first operand.
      :type arg0: :class:`KRPC.Expression`
      :param arg1: The second operand.
      :type arg1: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

   .. function:: elementAt(arg, index)

      The value at the given position in a collection.

      :param arg: The collection.
      :type arg: :class:`KRPC.Expression`
      :param index: The position of the value, counting from zero.
      :type index: :class:`KRPC.Expression`
      :returns: The value at the given position.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Throws an exception if the index is out of range.

   .. function:: equal(arg0, arg1)

      Equality comparison.

      :param arg0: The first operand.
      :type arg0: :class:`KRPC.Expression`
      :param arg1: The second operand.
      :type arg1: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Collections, tuples, bytes and structures compare by their contents. Throws an
         exception if the operands are of unrelated types.

   .. function:: except(arg1, arg2)

      The values of the first collection that are not in the second, with
      duplicates removed.

      :param arg1: The first collection.
      :type arg1: :class:`KRPC.Expression`
      :param arg2: The second collection.
      :type arg2: :class:`KRPC.Expression`
      :returns: The difference between the two collections.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.toList` makes it concrete.

   .. function:: exclusiveOr(arg0, arg1)

      Logical or bitwise exclusive or.

      :param arg0: The first operand.
      :type arg0: :class:`KRPC.Expression`
      :param arg1: The second operand.
      :type arg1: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Also takes two values of a flags enumeration, and gives the values in only one of them.

   .. function:: first(arg)

      The first value of a collection.

      :param arg: The collection.
      :type arg: :class:`KRPC.Expression`
      :returns: The first value.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Throws an exception if the collection is empty.

   .. function:: forEach(variable, collection, body)

      A loop that evaluates the body once for each value of a collection.

      :param variable: The loop variable.
      :type variable: :class:`KRPC.Expression`
      :param collection: The collection to iterate over.
      :type collection: :class:`KRPC.Expression`
      :param body: The statement evaluated on each iteration.
      :type body: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The loop variable is a local variable declared by an enclosing
         :meth:`KRPC.Expression.blockWithVariables`, or a parameter of an enclosing :meth:`KRPC.Expression.lambda`.
         A :meth:`KRPC.Expression.break` or :meth:`KRPC.Expression.continue` in the body applies to this loop.

   .. function:: get(arg, index)

      An element of a tuple, list or dictionary.

      :param arg: The tuple, list or dictionary.
      :type arg: :class:`KRPC.Expression`
      :param index: The index or key of the element.
      :type index: :class:`KRPC.Expression`
      :returns: The element.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         A tuple takes a constant integer index, a list a zero indexed integer and a
         dictionary a key. Throws an exception if the index is out of range or the key is
         missing, and for a tuple when the expression is created.

   .. function:: getField(arg, name)

      The value of a field of a structure. Throws an exception if the value is not a
      structure, or has no field of the given name.

      :param arg: The structure.
      :type arg: :class:`KRPC.Expression`
      :param name: The name of the field to access.
      :type name: ``string``
      :returns: The value of the field.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: getFieldByIndex(arg, index)

      The value of a field of a structure, named by its position. Throws an exception if
      the value is not a structure, or the index is out of range.

      :param arg: The structure.
      :type arg: :class:`KRPC.Expression`
      :param index: The position of the field to access.
      :type index: ``number``
      :returns: The value of the field.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The fields are numbered from 0, in the order the structure defines them.

   .. function:: greaterThan(arg0, arg1)

      Greater than numerical comparison.

      :param arg0: The first operand.
      :type arg0: :class:`KRPC.Expression`
      :param arg1: The second operand.
      :type arg1: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

   .. function:: greaterThanOrEqual(arg0, arg1)

      Greater than or equal numerical comparison.

      :param arg0: The first operand.
      :type arg0: :class:`KRPC.Expression`
      :param arg1: The second operand.
      :type arg1: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

   .. function:: groupBy(arg, key)

      The values of a collection grouped by the key a function produces for each of them.

      :param arg: The collection.
      :type arg: :class:`KRPC.Expression`
      :param key: A function producing the key to group values by.
      :type key: :class:`KRPC.Expression`
      :returns: A dictionary of each key to the values that produced it.
      :rtype: :class:`KRPC.Expression`
   

   .. method:: getHasReturnType()

      Whether the expression evaluates to a value.

      :Property: Read-only, cannot be set
      :rtype: ``boolean``
   

      .. note::

         An expression with no value has no :meth:`KRPC.Expression.getReturnType`. An empty
         collection and no value both encode to an empty sequence of bytes.

   .. function:: ifThen(condition, body)

      An if statement, with no value.

      :param condition: The boolean condition.
      :type condition: :class:`KRPC.Expression`
      :param body: The statement evaluated when the condition is true.
      :type body: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Use :meth:`KRPC.Expression.conditional` for an if-then-else that produces a value.

   .. function:: ifThenElse(condition, body, elseBody)

      An if-else statement, with no value.

      :param condition: The boolean condition.
      :type condition: :class:`KRPC.Expression`
      :param body: The statement evaluated when the condition is true.
      :type body: :class:`KRPC.Expression`
      :param elseBody: The statement evaluated when the condition is false.
      :type elseBody: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Use :meth:`KRPC.Expression.conditional` for an if-then-else that produces a value.

   .. function:: intersect(arg1, arg2)

      The values in both of two collections, with duplicates removed.

      :param arg1: The first collection.
      :type arg1: :class:`KRPC.Expression`
      :param arg2: The second collection.
      :type arg2: :class:`KRPC.Expression`
      :returns: The intersection of the two collections.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.toList` makes it concrete.

   .. function:: invoke(function, args)

      A function call.

      :param function: The function to call.
      :type function: :class:`KRPC.Expression`
      :param args: The arguments to call the function with.
      :type args: ``Map`` from ``string`` to :class:`KRPC.Expression`
      :returns: A function call.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: isNull(value)

      Whether a value is null. Throws an exception if the type of the value cannot be null.

      :param value: The value.
      :type value: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

   .. function:: lambda(parameters, body)

      A function with the given parameters and body.

      :param parameters: The parameters of the function.
      :type parameters: ``Array`` of :class:`KRPC.Expression`
      :param body: The body of the function.
      :type body: :class:`KRPC.Expression`
      :returns: A function.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The body is an expression or a block of statements, and a body with no value is
         evaluated for its effects. A :meth:`KRPC.Expression.return` or :meth:`KRPC.Expression.returnNothing`
         statement ends the evaluation.

   .. function:: last(arg)

      The last value of a collection.

      :param arg: The collection.
      :type arg: :class:`KRPC.Expression`
      :returns: The last value.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Throws an exception if the collection is empty.

   .. function:: leftShift(arg0, arg1)

      Bitwise left shift.

      :param arg0: The first operand.
      :type arg0: :class:`KRPC.Expression`
      :param arg1: The second operand.
      :type arg1: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Only the low 5 bits of the count are used for a 32-bit value, and the low 6 bits
         for a 64-bit value.

   .. function:: lessThan(arg0, arg1)

      Less than numerical comparison.

      :param arg0: The first operand.
      :type arg0: :class:`KRPC.Expression`
      :param arg1: The second operand.
      :type arg1: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

   .. function:: lessThanOrEqual(arg0, arg1)

      Less than or equal numerical comparison.

      :param arg0: The first operand.
      :type arg0: :class:`KRPC.Expression`
      :param arg1: The second operand.
      :type arg1: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

   .. function:: match(value, cases, bodies)

      A match statement, which evaluates the first matching case.

      :param value: The value to match.
      :type value: :class:`KRPC.Expression`
      :param cases: The test values of each case.
      :type cases: ``Array`` of ``Array`` of :class:`KRPC.Expression`
      :param bodies: The statement of each case.
      :type bodies: ``Array`` of :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Test values compare as for :meth:`KRPC.Expression.equal`, and a case with none is the default.
         A :meth:`KRPC.Expression.break` in a body ends the enclosing loop.

   .. function:: max(arg)

      The largest value of a collection.

      :param arg: The list or set.
      :type arg: :class:`KRPC.Expression`
      :returns: The largest value in the collection.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Throws an exception if the collection is empty and its values are not nullable.

   .. function:: maxBy(arg, key)

      The value of a collection with the largest key.

      :param arg: The collection.
      :type arg: :class:`KRPC.Expression`
      :param key: A function producing the key to compare values by.
      :type key: :class:`KRPC.Expression`
      :returns: The value with the largest key.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Throws an exception if the collection is empty.

   .. function:: min(arg)

      The smallest value of a collection.

      :param arg: The list or set.
      :type arg: :class:`KRPC.Expression`
      :returns: The smallest value in the collection.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Throws an exception if the collection is empty and its values are not nullable.

   .. function:: minBy(arg, key)

      The value of a collection with the smallest key.

      :param arg: The collection.
      :type arg: :class:`KRPC.Expression`
      :param key: A function producing the key to compare values by.
      :type key: :class:`KRPC.Expression`
      :returns: The value with the smallest key.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Throws an exception if the collection is empty.

   .. function:: modulo(arg0, arg1)

      Numerical modulo operator.

      :param arg0: The first operand.
      :type arg0: :class:`KRPC.Expression`
      :param arg1: The second operand.
      :type arg1: :class:`KRPC.Expression`
      :returns: The remainder of arg0 divided by arg1
      :rtype: :class:`KRPC.Expression`
   

   .. function:: multiply(arg0, arg1)

      Numerical multiplication.

      :param arg0: The first operand.
      :type arg0: :class:`KRPC.Expression`
      :param arg1: The second operand.
      :type arg1: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

   .. function:: negate(arg)

      Numerical negation, which negates a uint as a long.

      :param arg: The operand.
      :type arg: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

   .. function:: not(arg)

      Logical or bitwise negation.

      :param arg: The operand.
      :type arg: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Also takes a value of a flags enumeration, and gives the declared values it does not hold.

   .. function:: notEqual(arg0, arg1)

      Inequality comparison.

      :param arg0: The first operand.
      :type arg0: :class:`KRPC.Expression`
      :param arg1: The second operand.
      :type arg1: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Collections, tuples, bytes and structures compare by their contents. Throws an
         exception if the operands are of unrelated types.

   .. function:: or(arg0, arg1)

      Logical or bitwise or, which evaluates both operands.

      :param arg0: The first operand.
      :type arg0: :class:`KRPC.Expression`
      :param arg1: The second operand.
      :type arg1: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Also takes two values of a flags enumeration, and gives their combination.

   .. function:: orderBy(arg, key)

      The values of a collection ordered by the key a function produces for each of them.

      :param arg: The collection to order.
      :type arg: :class:`KRPC.Expression`
      :param key: A function producing the key to order values by.
      :type key: :class:`KRPC.Expression`
      :returns: The ordered collection.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.toList` makes it concrete.

   .. function:: parameter(name, type)

      A named parameter of a function.

      :param name: The name of the parameter.
      :type name: ``string``
      :param type: The type of the parameter.
      :type type: :class:`KRPC.Type`
      :returns: A named parameter.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         A value is bound to it by name with :meth:`KRPC.Expression.invoke`, or by its position in
         the parameters of a :meth:`KRPC.Expression.lambda`.

   .. method:: getParameterTypes()

      The types of the function's parameters.

      :Property: Read-only, cannot be set
      :rtype: ``Array`` of :class:`KRPC.Type`
   

      .. note::

         Empty for a function that is not created with :meth:`KRPC.Expression.lambda`.

   .. function:: power(arg0, arg1)

      Numerical power operator.

      :param arg0: The first operand.
      :type arg0: :class:`KRPC.Expression`
      :param arg1: The second operand.
      :type arg1: :class:`KRPC.Expression`
      :returns: arg0 raised to the power of arg1, at the common type of the operands
      :rtype: :class:`KRPC.Expression`
   

   .. function:: remove(collection, value)

      A removal of a value from a list or a set, or of an entry from a dictionary.

      :param collection: The list, set or dictionary.
      :type collection: :class:`KRPC.Expression`
      :param value: The value to remove, or the key of the entry to remove.
      :type value: :class:`KRPC.Expression`
      :returns: Whether the collection held the value.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The first occurrence is removed from a list.

   .. function:: removeAt(list, index)

      A statement removing the element at an index of a list.

      :param list: The list.
      :type list: :class:`KRPC.Expression`
      :param index: The index of the element, counting from zero.
      :type index: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Throws an exception if the index is out of range.

   .. function:: return(value)

      A return statement, which ends the enclosing function with a value.

      :param value: The value to return.
      :type value: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

   .. function:: returnNothing()

      A return statement, which ends the enclosing function with no value.

      :rtype: :class:`KRPC.Expression`
   

   .. method:: getReturnType()

      The type of the value the expression evaluates to. Throws an exception if the
      value cannot be sent to a client.

      :Property: Read-only, cannot be set
      :rtype: :class:`KRPC.Type`
   

      .. note::

         A function created with :meth:`KRPC.Expression.lambda` evaluates to the value it returns. A
         lazily evaluated collection, such as from :meth:`KRPC.Expression.select`, is sent once
         converted by :meth:`KRPC.Expression.toList` or :meth:`KRPC.Expression.toSet`.

   .. function:: reverse(arg)

      The values of a collection in the opposite order.

      :param arg: The collection.
      :type arg: :class:`KRPC.Expression`
      :returns: The reversed collection.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.toList` makes it concrete.

   .. function:: rightShift(arg0, arg1)

      Bitwise right shift.

      :param arg0: The first operand.
      :type arg0: :class:`KRPC.Expression`
      :param arg1: The second operand.
      :type arg1: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Only the low 5 bits of the count are used for a 32-bit value, and the low 6 bits
         for a 64-bit value.

   .. function:: select(arg, func)

      The result of a function for each value of a collection.

      :param arg: The list or set.
      :type arg: :class:`KRPC.Expression`
      :param func: The function, taking an element of the collection.
      :type func: :class:`KRPC.Expression`
      :returns: The collection of function results.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.toList` makes it concrete.

   .. function:: selectMany(arg, func)

      The values of the collections a function returns for each value of a collection,
      flattened into one.

      :param arg: The list or set.
      :type arg: :class:`KRPC.Expression`
      :param func: The function, taking an element of the collection and returning a collection.
      :type func: :class:`KRPC.Expression`
      :returns: The flattened collection of function results.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.toList` makes it concrete.

   .. function:: set(collection, index, value)

      A statement setting an element of a list, or an entry of a dictionary.

      :param collection: The list or dictionary.
      :type collection: :class:`KRPC.Expression`
      :param index: The index of the element, counting from zero, or the key of the entry.
      :type index: :class:`KRPC.Expression`
      :param value: The value to set.
      :type value: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         A key the dictionary does not hold is added. Throws an exception if the index
         is out of range for the list.

   .. function:: skip(arg, count)

      A collection without its first count values.

      :param arg: The collection.
      :type arg: :class:`KRPC.Expression`
      :param count: The number of values to skip.
      :type count: :class:`KRPC.Expression`
      :returns: The collection without its first count values.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.toList` makes it concrete.

   .. function:: stateVariable(name, initialValue)

      A variable that keeps its value between evaluations of a function.

      :param name: The name of the variable.
      :type name: ``string``
      :param initialValue: The initial value of the variable.
      :type initialValue: :class:`KRPC.Expression`
      :returns: A state variable.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The initial value is evaluated once, when the variable is created, and gives the
         variable its type. The value belongs to the variable, so every function that
         contains it shares it.

   .. function:: stringConcat(args)

      The concatenation of strings.

      :param args: The strings to concatenate, in order.
      :type args: ``Array`` of :class:`KRPC.Expression`
      :returns: The concatenated string.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Use :meth:`KRPC.Expression.convertToString` to convert other values to strings.

   .. function:: stringContains(arg, value)

      Whether one string occurs within another.

      :param arg: The string to search.
      :type arg: :class:`KRPC.Expression`
      :param value: The string to search for.
      :type value: :class:`KRPC.Expression`
      :returns: Whether the value occurs in the string.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: stringEndsWith(arg, value)

      Whether a string ends with another.

      :param arg: The string to test.
      :type arg: :class:`KRPC.Expression`
      :param value: The string to test for.
      :type value: :class:`KRPC.Expression`
      :returns: Whether the string ends with the value.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: stringGet(arg, index)

      The character at the given position in a string, as a string of length one.

      :param arg: The string.
      :type arg: :class:`KRPC.Expression`
      :param index: The position of the character, counting from zero.
      :type index: :class:`KRPC.Expression`
      :returns: The character at the given position.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Throws an exception if the index is out of range.

   .. function:: stringIndexOf(arg, value)

      The position of the first occurrence of one string within another.

      :param arg: The string to search.
      :type arg: :class:`KRPC.Expression`
      :param value: The string to search for.
      :type value: :class:`KRPC.Expression`
      :returns: The position of the value, counting from zero, or -1 if it does not occur.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: stringJoin(separator, values)

      Strings joined together, separated by another string.

      :param separator: The string to put between the values.
      :type separator: :class:`KRPC.Expression`
      :param values: The strings to join.
      :type values: :class:`KRPC.Expression`
      :returns: The joined string.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: stringLength(arg)

      The number of characters in a string.

      :param arg: The string.
      :type arg: :class:`KRPC.Expression`
      :returns: The length of the string.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: stringReplace(arg, oldValue, newValue)

      A string with every occurrence of one string replaced by another.

      :param arg: The string to search.
      :type arg: :class:`KRPC.Expression`
      :param oldValue: The string to replace.
      :type oldValue: :class:`KRPC.Expression`
      :param newValue: The string to replace it with.
      :type newValue: :class:`KRPC.Expression`
      :returns: The string with the replacements made.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Throws an exception if the string to replace is empty.

   .. function:: stringSplit(arg, separator)

      The parts of a string separated by another string.

      :param arg: The string to split.
      :type arg: :class:`KRPC.Expression`
      :param separator: The string that separates the parts.
      :type separator: :class:`KRPC.Expression`
      :returns: The parts of the string.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: stringStartsWith(arg, value)

      Whether a string starts with another.

      :param arg: The string to test.
      :type arg: :class:`KRPC.Expression`
      :param value: The string to test for.
      :type value: :class:`KRPC.Expression`
      :returns: Whether the string starts with the value.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: stringSubstring(arg, start, length)

      Part of a string.

      :param arg: The string.
      :type arg: :class:`KRPC.Expression`
      :param start: The position to start at, counting from zero.
      :type start: :class:`KRPC.Expression`
      :param length: The number of characters to take.
      :type length: :class:`KRPC.Expression`
      :returns: The substring.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Throws an exception if the start or the length is out of range.

   .. function:: stringToLower(arg)

      A string in lower case, independently of the game's language.

      :param arg: The string.
      :type arg: :class:`KRPC.Expression`
      :returns: The string in lower case.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: stringToUpper(arg)

      A string in upper case, independently of the game's language.

      :param arg: The string.
      :type arg: :class:`KRPC.Expression`
      :returns: The string in upper case.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: stringTrim(arg)

      A string with white space removed from both ends.

      :param arg: The string.
      :type arg: :class:`KRPC.Expression`
      :returns: The trimmed string.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: stringTrimEnd(arg)

      A string with white space removed from its end.

      :param arg: The string.
      :type arg: :class:`KRPC.Expression`
      :returns: The trimmed string.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: stringTrimStart(arg)

      A string with white space removed from its start.

      :param arg: The string.
      :type arg: :class:`KRPC.Expression`
      :returns: The trimmed string.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: subtract(arg0, arg1)

      Numerical subtraction.

      :param arg0: The first operand.
      :type arg0: :class:`KRPC.Expression`
      :param arg1: The second operand.
      :type arg1: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

   .. function:: sum(arg)

      The sum of the values of a collection.

      :param arg: The list or set.
      :type arg: :class:`KRPC.Expression`
      :returns: The sum of the elements in the collection.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: switch(value, cases, bodies)

      A switch statement, which evaluates from the first matching case on.

      :param value: The value to match.
      :type value: :class:`KRPC.Expression`
      :param cases: The test values of each case.
      :type cases: ``Array`` of ``Array`` of :class:`KRPC.Expression`
      :param bodies: The statement of each case.
      :type bodies: ``Array`` of :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Test values compare as for :meth:`KRPC.Expression.equal`, and a case with none is the default.
         A :meth:`KRPC.Expression.break` in a body ends the switch.

   .. function:: take(arg, count)

      The first count values of a collection.

      :param arg: The collection.
      :type arg: :class:`KRPC.Expression`
      :param count: The number of values to take.
      :type count: :class:`KRPC.Expression`
      :returns: The first count values of the collection.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.toList` makes it concrete.

   .. function:: throw(service, name, message)

      A throw statement, which raises the named exception.

      :param service: The name of the service the exception is defined in.
      :type service: ``string``
      :param name: The name of the exception.
      :type name: ``string``
      :param message: The message of the exception.
      :type message: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The client receives the exception as an error from the function, or from the stream
         or event evaluating it. For example, the service ``KRPC`` and the name
         ``Error`` raise :class:`KRPC.Error`. Throws an exception if the service does not
         declare it.

   .. function:: throwById(serviceId, exceptionId, message)

      A throw statement, which raises the exception with the given ids.

      :param serviceId: The id of the service the exception is defined in.
      :type serviceId: ``number``
      :param exceptionId: The id of the exception.
      :type exceptionId: ``number``
      :param message: The message of the exception.
      :type message: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Raises the exception as :meth:`KRPC.Expression.throw` does. Throws an exception if the
         service does not declare it.

   .. function:: toList(arg)

      A collection as a list.

      :param arg: The collection.
      :type arg: :class:`KRPC.Expression`
      :returns: The collection as a list.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: toSet(arg)

      A collection as a set.

      :param arg: The collection.
      :type arg: :class:`KRPC.Expression`
      :returns: The collection as a set.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Throws an exception if a value is null.

   .. function:: tryCatch(body, service, name, message, handler)

      A try-catch statement that handles the named exception.

      :param body: The statement to evaluate.
      :type body: :class:`KRPC.Expression`
      :param service: The name of the service the exception is defined in.
      :type service: ``string``
      :param name: The name of the exception.
      :type name: ``string``
      :param message: A string variable assigned the exception's message, or null.
      :type message: :class:`KRPC.Expression` or ``null``
      :param handler: The statement evaluated when the exception is caught.
      :type handler: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Only an exception a service declares can be named, and :meth:`KRPC.Expression.tryCatchAll`
         handles any exception. Throws an exception if the service does not declare it.

   .. function:: tryCatchAll(body, message, handler)

      A try-catch statement that handles any exception.

      :param body: The statement to evaluate.
      :type body: :class:`KRPC.Expression`
      :param message: A string variable assigned the exception's message, or null.
      :type message: :class:`KRPC.Expression` or ``null``
      :param handler: The statement evaluated when an exception is caught.
      :type handler: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

   .. function:: tryCatchById(body, serviceId, exceptionId, message, handler)

      A try-catch statement that handles the exception with the given ids.

      :param body: The statement to evaluate.
      :type body: :class:`KRPC.Expression`
      :param serviceId: The id of the service the exception is defined in.
      :type serviceId: ``number``
      :param exceptionId: The id of the exception.
      :type exceptionId: ``number``
      :param message: A string variable assigned the exception's message, or null.
      :type message: :class:`KRPC.Expression` or ``null``
      :param handler: The statement evaluated when the exception is caught.
      :type handler: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         Handles the exception as :meth:`KRPC.Expression.tryCatch` does. Throws an exception if the
         service does not declare it.

   .. function:: tryFinally(body, finalizer)

      A try-finally statement.

      :param body: The statement to evaluate.
      :type body: :class:`KRPC.Expression`
      :param finalizer: The statement evaluated after the body.
      :type finalizer: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The finalizer is evaluated after the body, whether or not the body raises an
         exception. A break, continue or return cannot leave the finalizer.

   .. function:: union(arg1, arg2)

      The values in either of two collections, with duplicates removed.

      :param arg1: The first collection.
      :type arg1: :class:`KRPC.Expression`
      :param arg2: The second collection.
      :type arg2: :class:`KRPC.Expression`
      :returns: The union of the two collections.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.toList` makes it concrete.

   .. function:: variable(name, type)

      A local variable, declared by :meth:`KRPC.Expression.blockWithVariables`.

      :param name: The name of the variable.
      :type name: ``string``
      :param type: The type of the variable.
      :type type: :class:`KRPC.Type`
      :returns: A local variable.
      :rtype: :class:`KRPC.Expression`
   

   .. function:: where(arg, func)

      The values of a collection for which a predicate function returns true.

      :param arg: The list or set.
      :type arg: :class:`KRPC.Expression`
      :param func: The predicate function, taking an element of the collection.
      :type func: :class:`KRPC.Expression`
      :returns: The filtered collection.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`KRPC.Expression.toList` makes it concrete.

   .. function:: while(condition, body)

      A while loop, which evaluates the body while the condition is true.

      :param condition: The boolean condition.
      :type condition: :class:`KRPC.Expression`
      :param body: The statement evaluated on each iteration.
      :type body: :class:`KRPC.Expression`
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         A :meth:`KRPC.Expression.break` or :meth:`KRPC.Expression.continue` in the body applies to this loop.

   .. function:: zip(arg1, arg2, func)

      The results of a function applied to the values of two collections in pairs.

      :param arg1: The first collection.
      :type arg1: :class:`KRPC.Expression`
      :param arg2: The second collection.
      :type arg2: :class:`KRPC.Expression`
      :param func: A function combining a value from each collection.
      :type func: :class:`KRPC.Expression`
      :returns: The combined collection.
      :rtype: :class:`KRPC.Expression`
   

      .. note::

         The result is as long as the shorter of the two collections. It is lazily
         evaluated, and :meth:`KRPC.Expression.toList` makes it concrete.



.. class:: Type

   The type of a value a procedure takes or returns.

   .. function:: bool()

      A boolean type.

      :rtype: :class:`KRPC.Type`
   

   .. function:: bytes()

      A byte sequence type.

      :rtype: :class:`KRPC.Type`
   

   .. function:: classType(service, name)

      The type of a class defined by a service.

      :param service: The name of the service the class is defined in.
      :type service: ``string``
      :param name: The name of the class.
      :type name: ``string``
      :rtype: :class:`KRPC.Type`
   

   .. function:: classTypeById(serviceId, classId)

      The type of a class defined by a service, named by ids.

      :param serviceId: The id of the service the class is defined in.
      :type serviceId: ``number``
      :param classId: The id of the class.
      :type classId: ``number``
      :rtype: :class:`KRPC.Type`
   

   .. method:: getCode()

      The kind of the type.

      :Property: Read-only, cannot be set
      :rtype: :class:`KRPC.TypeCode`
   

   .. function:: dictionaryType(keyType, valueType)

      A dictionary type. Throws an exception if the key type is nullable or not a valid
      key type.

      :param keyType: The type of the keys of the dictionary.
      :type keyType: :class:`KRPC.Type`
      :param valueType: The type of the values of the dictionary.
      :type valueType: :class:`KRPC.Type`
      :rtype: :class:`KRPC.Type`
   

   .. function:: double()

      A double precision floating point number type.

      :rtype: :class:`KRPC.Type`
   

   .. function:: enumerationType(service, name)

      The type of an enumeration defined by a service.

      :param service: The name of the service the enumeration is defined in.
      :type service: ``string``
      :param name: The name of the enumeration.
      :type name: ``string``
      :rtype: :class:`KRPC.Type`
   

   .. function:: enumerationTypeById(serviceId, enumerationId)

      The type of an enumeration defined by a service, named by ids.

      :param serviceId: The id of the service the enumeration is defined in.
      :type serviceId: ``number``
      :param enumerationId: The id of the enumeration.
      :type enumerationId: ``number``
      :rtype: :class:`KRPC.Type`
   

   .. function:: float()

      A single precision floating point number type.

      :rtype: :class:`KRPC.Type`
   

   .. function:: int()

      A signed 32-bit integer type.

      :rtype: :class:`KRPC.Type`
   

   .. function:: listType(valueType)

      A list type.

      :param valueType: The type of the elements of the list.
      :type valueType: :class:`KRPC.Type`
      :rtype: :class:`KRPC.Type`
   

   .. function:: long()

      A signed 64-bit integer type.

      :rtype: :class:`KRPC.Type`
   

   .. method:: getName()

      The name of a class, enumeration or structure type, or an empty string.

      :Property: Read-only, cannot be set
      :rtype: ``string``
   

   .. method:: getNullable()

      Whether a value of the type may be null.

      :Property: Read-only, cannot be set
      :rtype: ``boolean``
   

      .. note::

         Any type can be nullable. A string or class value of a type that is not nullable
         is never null.

   .. function:: nullableType(valueType)

      A nullable type, holding a value of the given type or null.

      :param valueType: The type of the value.
      :type valueType: :class:`KRPC.Type`
      :rtype: :class:`KRPC.Type`
   

   .. method:: getService()

      The service a class, enumeration or structure type is defined in, or an empty string.

      :Property: Read-only, cannot be set
      :rtype: ``string``
   

   .. function:: setType(valueType)

      A set type. Throws an exception if the element type is nullable.

      :param valueType: The type of the elements of the set.
      :type valueType: :class:`KRPC.Type`
      :rtype: :class:`KRPC.Type`
   

   .. function:: string()

      A string type.

      :rtype: :class:`KRPC.Type`
   

   .. function:: structType(service, name)

      The type of a structure defined by a service.

      :param service: The name of the service the structure is defined in.
      :type service: ``string``
      :param name: The name of the structure.
      :type name: ``string``
      :rtype: :class:`KRPC.Type`
   

   .. function:: structTypeById(serviceId, structId)

      The type of a structure defined by a service, named by ids.

      :param serviceId: The id of the service the structure is defined in.
      :type serviceId: ``number``
      :param structId: The id of the structure.
      :type structId: ``number``
      :rtype: :class:`KRPC.Type`
   

   .. function:: tupleType(valueTypes)

      A tuple type. Throws an exception if there are no element types or more than seven.

      :param valueTypes: The types of the elements of the tuple.
      :type valueTypes: ``Array`` of :class:`KRPC.Type`
      :rtype: :class:`KRPC.Type`
   

   .. method:: getTypes()

      The types of the elements of a tuple, list, set or dictionary type.

      :Property: Read-only, cannot be set
      :rtype: ``Array`` of :class:`KRPC.Type`
   

      .. note::

         The value types of a tuple, the value type of a list or set, or the key and value
         types of a dictionary. An empty list for any other type.

   .. function:: uint()

      An unsigned 32-bit integer type.

      :rtype: :class:`KRPC.Type`
   

   .. function:: ulong()

      An unsigned 64-bit integer type.

      :rtype: :class:`KRPC.Type`
   



.. class:: GameScene

   The game scene. See :func:`KRPC.getGameScene`.


   .. attribute:: administration

      The administration facility. This is a pseudo-scene, shown when the
      administration facility is open within the space center scene.


   .. attribute:: astronautComplex

      The astronaut complex. This is a pseudo-scene, shown when the
      astronaut complex facility is open within the space center scene.


   .. attribute:: editorSPH

      The Space Plane Hangar.


   .. attribute:: editorVAB

      The Vehicle Assembly Building.


   .. attribute:: flight

      The game scene showing a vessel in flight (or on the launchpad/runway).


   .. attribute:: missionBuilder

      The mission builder.


   .. attribute:: missionControl

      Mission control. This is a pseudo-scene, shown when the
      mission control facility is open within the space center scene.


   .. attribute:: researchAndDevelopment

      Research and development. This is a pseudo-scene, shown when the
      research and development facility is open within the space center scene.


   .. attribute:: spaceCenter

      The game scene showing the Kerbal Space Center buildings.


   .. attribute:: trackingStation

      The tracking station.



.. class:: TypeCode

   The kind of a type, with the values of the communication protocol's type codes.


   .. attribute:: bool

      A boolean.


   .. attribute:: bytes

      A sequence of bytes.


   .. attribute:: class

      A class defined by a service.


   .. attribute:: dictionary

      A dictionary, with the types of its keys and values in :meth:`KRPC.Type.getTypes`.


   .. attribute:: double

      A double precision floating point number.


   .. attribute:: enumeration

      An enumeration defined by a service.


   .. attribute:: float

      A single precision floating point number.


   .. attribute:: list

      A list, with the type of its values in :meth:`KRPC.Type.getTypes`.


   .. attribute:: sint32

      A signed 32-bit integer.


   .. attribute:: sint64

      A signed 64-bit integer.


   .. attribute:: set

      A set, with the type of its values in :meth:`KRPC.Type.getTypes`.


   .. attribute:: string

      A string.


   .. attribute:: struct

      A structure defined by a service.


   .. attribute:: tuple

      A tuple, with the types of its values in :meth:`KRPC.Type.getTypes`.


   .. attribute:: uint32

      An unsigned 32-bit integer.


   .. attribute:: uint64

      An unsigned 64-bit integer.



.. class:: ClientInfo(fields)

   A client connected to the server.

   .. note::

      See :func:`KRPC.getClients`.
   .. attribute:: id

      The identifier of the client.

      :Type: ``Uint8Array``

   .. attribute:: name

      The name of the client, or an empty string if it has no name.

      :Type: ``string``

   .. attribute:: address

      The address of the client.

      :Type: ``string``

   .. attribute:: protocolVersion

      The protocol version of the client's connection.

      :Type: ``number``


.. class:: ServerSettings(fields)

   The settings that control how the server executes calls.

   .. note::

      See :attr:`KRPC.ServerStatus.settings`.
   .. attribute:: oneRpcPerUpdate

      Whether the server executes one call per update.

      :Type: ``boolean``

   .. attribute:: maxTimePerUpdate

      The current maximum time spent executing calls per update, in microseconds.

      :Type: ``number``

   .. attribute:: adaptiveRateControl

      Whether the server adjusts :attr:`KRPC.ServerSettings.maxTimePerUpdate` to hold the frame rate.

      :Type: ``boolean``

   .. attribute:: blockingRecv

      Whether the server waits for the next call in an update.

      :Type: ``boolean``

   .. attribute:: recvTimeout

      The time the server waits for the next call, in microseconds.

      :Type: ``number``

   .. attribute:: tickHoldTimeout

      The longest time a client may hold the game on one physics tick, in microseconds.

      :Type: ``number``


.. class:: ServerStatistics(fields)

   The performance statistics of the server.

   .. note::

      See :attr:`KRPC.ServerStatus.statistics`.
   .. attribute:: bytesRead

      The number of bytes received from clients.

      :Type: ``bigint``

   .. attribute:: bytesWritten

      The number of bytes sent to clients.

      :Type: ``bigint``

   .. attribute:: bytesReadRate

      The rate bytes are received from clients, in bytes per second.

      :Type: ``number``

   .. attribute:: bytesWrittenRate

      The rate bytes are sent to clients, in bytes per second.

      :Type: ``number``

   .. attribute:: rpcsExecuted

      The number of calls executed.

      :Type: ``bigint``

   .. attribute:: rpcRate

      The number of calls executed per second.

      :Type: ``number``

   .. attribute:: timePerRpcUpdate

      The time spent receiving and executing calls per update, in seconds.

      :Type: ``number``

   .. attribute:: pollTimePerRpcUpdate

      The time spent receiving calls per update, in seconds.

      :Type: ``number``

   .. attribute:: execTimePerRpcUpdate

      The time spent executing calls per update, in seconds.

      :Type: ``number``

   .. attribute:: deferredCalls

      The number of calls left for the next update, because the update ran out of time
      or the call yielded.

      :Type: ``number``

   .. attribute:: streams

      The number of open streams, events included.

      :Type: ``number``

   .. attribute:: streamEvaluations

      The number of stream evaluations, one per stream per update.

      :Type: ``bigint``

   .. attribute:: streamEvaluationRate

      The number of stream evaluations per second.

      :Type: ``number``

   .. attribute:: streamUpdatesSent

      The number of stream updates sent to clients.

      :Type: ``bigint``

   .. attribute:: timePerStreamUpdate

      The time spent updating streams per update, in seconds.

      :Type: ``number``


.. class:: ServerStatus(fields)

   The status of the server.

   .. note::

      See :func:`KRPC.getStatus`.
   .. attribute:: version

      The version of the server, or ``unknown``.

      :Type: ``string``

   .. attribute:: protocolVersion

      The protocol version of the connection.

      :Type: ``number``

   .. attribute:: settings

      The settings of the server.

      :Type: :class:`KRPC.ServerSettings`

   .. attribute:: statistics

      The performance statistics of the server.

      :Type: :class:`KRPC.ServerStatistics`


.. class:: ArgumentException

   A method was invoked where at least one of the passed arguments does not
   meet the parameter specification of the method.

   A subclass of the builtin ``TypeError`` alone.



.. class:: ArgumentNullException

   A null reference was passed to a method that does not accept it as a valid argument.

   A subclass of the builtin ``TypeError`` alone.



.. class:: ArgumentOutOfRangeException

   The value of an argument is outside the allowable range of values as defined by the invoked method.

   A subclass of the builtin ``RangeError`` alone.



.. class:: DivideByZeroException

   An integer was divided by zero.

   A subclass of the builtin ``RangeError`` alone.



.. class:: Error

   A general error, described by its message.

   A subclass of :class:`~@krpc-org/krpc.ServerError`.



.. class:: IndexOutOfRangeException

   An index was outside the bounds of a collection, string or array.

   A subclass of the builtin ``RangeError`` alone.



.. class:: InvalidOperationException

   A method call was made to a method that is invalid
   given the current state of the object.

   A subclass of :class:`~@krpc-org/krpc.ServerError`.



.. class:: KeyNotFoundException

   A key was not found in a dictionary.

   A subclass of :class:`~@krpc-org/krpc.ServerError`.



.. class:: NotSupportedException

   A method was invoked that is not supported.

   A subclass of :class:`~@krpc-org/krpc.ServerError`.



.. class:: NullReferenceException

   A value was null where its type does not allow one.

   A subclass of the builtin ``TypeError`` alone.



.. class:: ObjectDestroyedException

   The game object that a service object refers to no longer exists,
   so the object cannot be used. For example, the part it represents
   was destroyed, or the vessel it belongs to was recovered.

   A subclass of :class:`~@krpc-org/krpc.ServerError`.

   .. note::

      See :class:`KRPC.ObjectNotLoadedException` for a game object that exists but is not
      loaded.

.. class:: ObjectNotLoadedException

   The game object that a service object refers to exists, but is not loaded.
   For example, the game is loading, or the vessel carrying the part is unloaded.

   A subclass of :class:`~@krpc-org/krpc.ServerError`.

   .. note::

      See :class:`KRPC.ObjectDestroyedException` for a game object that no longer exists.

.. class:: OverflowException

   An arithmetic or conversion operation overflowed.

   A subclass of the builtin ``RangeError`` alone.



.. class:: StreamRemovedException

   A stream was removed by the server, e.g. an event that has finished.

   An alias of :class:`~@krpc-org/krpc.StreamError`.



.. class:: TimeoutException

   An operation did not complete in the time allotted to it.

   A subclass of :class:`~@krpc-org/krpc.ServerError`.



.. class:: UnsupportedSceneException

   The procedure is not available in the current game scene.

   A subclass of :class:`~@krpc-org/krpc.ServerError`.
