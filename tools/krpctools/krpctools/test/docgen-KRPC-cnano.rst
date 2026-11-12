.. default-domain:: c
.. highlight:: c




Main kRPC service, used by clients to interact with basic server functionality.


.. function:: krpc_error_t krpc_KRPC_Clients(krpc_connection_t connection, krpc_list_KRPC_ClientInfo_t * returnValue)

   The clients that are currently connected to the server.

   




.. function:: krpc_error_t krpc_KRPC_GameScene(krpc_connection_t connection, krpc_KRPC_GameScene_t * returnValue)
.. function:: krpc_error_t krpc_KRPC_set_GameScene(krpc_connection_t connection, krpc_KRPC_GameScene_t value)

   The current game scene. Setting this switches the game to the given
   scene, or opens/closes the corresponding facility for the pseudo-scenes.
   Scene changes happen asynchronously: setting this property returns
   immediately, and clients should poll it until it reports the requested
   scene. Setting it to :macro:`KRPC_KRPC_GAMESCENE_FLIGHT` resumes the save's
   active vessel, and fails if there is none. Moving between
   :macro:`KRPC_KRPC_GAMESCENE_EDITORVAB` and :macro:`KRPC_KRPC_GAMESCENE_EDITORSPH`
   keeps the vessel that is being constructed, as the game's own switch
   editor button does. Leaving flight saves the game first, unless the game
   cannot be saved, e.g. while under acceleration.

   




.. function:: krpc_error_t krpc_KRPC_GameStateLoads(krpc_connection_t connection, uint32_t * returnValue)

   The number of game states that have finished loading.

   

   .. note::

      Increases once a game load, quickload, revert or scene change has finished.




.. function:: krpc_error_t krpc_KRPC_GetClientID(krpc_connection_t connection, krpc_bytes_t * returnValue)

   Returns the identifier for the current client.


   




.. function:: krpc_error_t krpc_KRPC_GetClientName(krpc_connection_t connection, char * * returnValue)

   Returns the name of the current client.
   This is an empty string if the client has no name.


   




.. function:: krpc_error_t krpc_KRPC_GetServices(krpc_connection_t connection, krpc_schema_Services * returnValue)

   The services the server provides, with their procedures, classes, enumerations,
   structures and exceptions.


   




.. function:: krpc_error_t krpc_KRPC_GetStatus(krpc_connection_t connection, krpc_KRPC_ServerStatus_t * returnValue)

   Returns the status of the server, with its version, settings and performance statistics.


   




.. function:: krpc_error_t krpc_KRPC_HoldTick(krpc_connection_t connection)

   Hold the game on its current physics tick until :func:`krpc_KRPC_ReleaseTick` is called
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




.. function:: krpc_error_t krpc_KRPC_NextTick(krpc_connection_t connection)

   Let the game move on from the tick being held and hold the one after it, so that a
   program acts on consecutive physics ticks. Holds the soonest tick it can if no tick
   is currently being held.


   

   .. note::

      A release and a hold made as two separate calls let the game advance between them.
      This takes the next tick without that gap. Call it at the top of a loop that has to
      act on every tick, work, and come back to it.

      The game advances one tick per call, so it runs at the rate of the loop for as long
      as the loop runs. End the loop with :func:`krpc_KRPC_ReleaseTick`.

      Every limit of :func:`krpc_KRPC_HoldTick` applies, the timeout included. A hold lost to
      the timeout is not reported, and the next call takes whatever tick it lands on.




.. function:: krpc_error_t krpc_KRPC_Paused(krpc_connection_t connection, bool * returnValue)
.. function:: krpc_error_t krpc_KRPC_set_Paused(krpc_connection_t connection, bool value)

   Whether the game is paused.

   




.. function:: krpc_error_t krpc_KRPC_ReleaseTick(krpc_connection_t connection)

   Let the game move on from the tick that :func:`krpc_KRPC_HoldTick` held it on. Does
   nothing when no tick is held, which is the case once a hold has timed out.


   





.. type:: krpc_KRPC_Expression_t

   A server side expression.

   .. note::

      Numeric operands of differing types are converted to a common type.

   .. function:: krpc_error_t krpc_KRPC_Expression_Add(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1)

      Numerical addition.

      :Parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_Aggregate(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t func)

      The result of applying an accumulator function over a collection.

      :Parameters:

       * **arg** -- The collection.
       * **func** -- The accumulator function.

      :returns: The accumulated value.
   

      .. note::

         Throws an exception if the collection is empty.

   .. function:: krpc_error_t krpc_KRPC_Expression_AggregateWithSeed(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t seed, krpc_KRPC_Expression_t func)

      The result of applying an accumulator function over a collection, starting from a seed.

      :Parameters:

       * **arg** -- The collection.
       * **seed** -- The seed value.
       * **func** -- The accumulator function.

      :returns: The accumulated value.
   

      .. note::

         The seed widens to the type of the accumulated value, the function's result.

   .. function:: krpc_error_t krpc_KRPC_Expression_All(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t predicate)

      Whether a predicate function returns true for every value of a collection.

      :Parameters:

       * **arg** -- The collection.
       * **predicate** -- The predicate function.

      :returns: Whether all items satisfy the predicate.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_And(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1)

      Logical or bitwise and, which evaluates both operands.

      :Parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

   

      .. note::

         Also takes two values of a flags enumeration, and gives the values they share.

   .. function:: krpc_error_t krpc_KRPC_Expression_Any(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t predicate)

      Whether a predicate function returns true for any value of a collection.

      :Parameters:

       * **arg** -- The collection.
       * **predicate** -- The predicate function.

      :returns: Whether any item satisfies the predicate.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_Append(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t collection, krpc_KRPC_Expression_t value)

      A statement adding a value to a list or a set.

      :Parameters:

       * **collection** -- The list or set to add to.
       * **value** -- The value to add.

   

      .. note::

         The value goes on the end of a list. A set holding the value is left unchanged.

   .. function:: krpc_error_t krpc_KRPC_Expression_Assign(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t variable, krpc_KRPC_Expression_t value)

      An assignment to a local variable, state variable or function parameter.

      :Parameters:

       * **variable** -- The variable to assign to.
       * **value** -- The value to assign.

   

      .. note::

         The value's type must be assignable to the variable's type. A numeric value of a
         different type is widened, and narrowing it requires a cast.

   .. function:: krpc_error_t krpc_KRPC_Expression_Average(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg)

      The average of the values of a collection.

      :Parameters:

       * **arg** -- The list or set.

      :returns: The average of the elements in the collection.
   

      .. note::

         Throws an exception if the collection is empty and its values are not nullable.

   .. function:: krpc_error_t krpc_KRPC_Expression_Block(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_list_object_t * statements)

      A block of statements, whose value is that of its last statement.

      :Parameters:

       * **statements** -- The statements.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_BlockWithVariables(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_list_object_t * variables, const krpc_list_object_t * statements)

      A block of statements with local variables.

      :Parameters:

       * **variables** -- The local variables of the block.
       * **statements** -- The statements.

   

      .. note::

         The value of the block is that of its last statement. The variables are in scope
         for the statements of the block, including within nested functions.

   .. function:: krpc_error_t krpc_KRPC_Expression_Break(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue)

      A break statement, which ends the enclosing loop.


   

   .. function:: krpc_error_t krpc_KRPC_Expression_BuildDictionary(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t keyFunc, krpc_KRPC_Expression_t valueFunc)

      A dictionary built from a collection, with a key and a value computed from each
      element.

      :Parameters:

       * **arg** -- The list or set.
       * **keyFunc** -- The function computing an element's key.
       * **valueFunc** -- The function computing an element's value.

      :returns: The dictionary.
   

      .. note::

         A repeated key takes the last of its values.

   .. function:: krpc_error_t krpc_KRPC_Expression_Call(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_schema_ProcedureCall * call)

      An RPC call.

      :Parameters:

       * **call** -- The procedure call.

   

      .. note::

         The instance the call is made on, and the values of its arguments, are fixed
         when the expression is created. A call to a procedure with no return value is
         evaluated for its effects, as a statement in a :func:`krpc_KRPC_Expression_Block`. A collection the
         procedure returns is copied, as a client would receive it.

   .. function:: krpc_error_t krpc_KRPC_Expression_CallWithArguments(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_schema_ProcedureCall * call, const krpc_dictionary_int32_object_t * args)

      An RPC call, where some or all of the arguments are computed by expressions.

      :Parameters:

       * **call** -- The RPC to call.
       * **args** -- Expressions computing the call's arguments, by position.

   

      .. note::

         Position 0 is the instance of a class method or property. A position with no
         expression takes the argument encoded in the call, or the parameter's default. A
         collection the procedure returns is copied, as a client would receive it.

   .. function:: krpc_error_t krpc_KRPC_Expression_Cast(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Type_t type)

      A value cast to the given type.

      :Parameters:

       * **arg** -- The value to cast.
       * **type** -- Type to cast the argument to.

   

      .. note::

         A tuple or collection is cast element by element.

   .. function:: krpc_error_t krpc_KRPC_Expression_Clear(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t collection)

      A statement removing every value from a list or a set, or every entry from a dictionary.

      :Parameters:

       * **collection** -- The list, set or dictionary.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_Concat(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg1, krpc_KRPC_Expression_t arg2)

      The values of one collection followed by those of another.

      :Parameters:

       * **arg1** -- The first collection.
       * **arg2** -- The second collection.

      :returns: The first collection followed by the second.
   

      .. note::

         The result is lazily evaluated. :func:`krpc_KRPC_Expression_ToList` makes it concrete.

   .. function:: krpc_error_t krpc_KRPC_Expression_Conditional(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t condition, krpc_KRPC_Expression_t ifTrue, krpc_KRPC_Expression_t ifFalse)

      The second argument if the condition is true, and the third argument otherwise.

      :Parameters:

       * **condition** -- The boolean condition.
       * **ifTrue** -- The value when the condition is true.
       * **ifFalse** -- The value when the condition is false.

   

      .. note::

         Throws an exception if the two values have no common type.

   .. function:: krpc_error_t krpc_KRPC_Expression_ConditionalAnd(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1)

      Logical and, which evaluates the second operand only when the first is true.

      :Parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_ConditionalOr(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1)

      Logical or, which evaluates the second operand only when the first is false.

      :Parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_ConstantBool(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, bool value)

      A constant value of boolean type.

      :Parameters:

       * **value** -- The value.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_ConstantBytes(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_bytes_t value)

      A constant value of bytes type.

      :Parameters:

       * **value** -- The value.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_ConstantDouble(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, double value)

      A constant value of double precision floating point type.

      :Parameters:

       * **value** -- The value.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_ConstantEnum(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const char * service, const char * name, int32_t value)

      A constant value of an enumeration a service defines.

      :Parameters:

       * **service** -- The name of the service the enumeration is defined in.
       * **name** -- The name of the enumeration.
       * **value** -- The value of the member of the enumeration.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_ConstantFloat(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, float value)

      A constant value of single precision floating point type.

      :Parameters:

       * **value** -- The value.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_ConstantInt(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, int32_t value)

      A constant value of integer type.

      :Parameters:

       * **value** -- The value.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_ConstantLong(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, int64_t value)

      A constant value of long integer type.

      :Parameters:

       * **value** -- The value.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_ConstantNull(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Type_t type)

      A null constant of the given type.

      :Parameters:

       * **type** -- The type of the value.

   

      .. note::

         The type is made nullable, as by :func:`krpc_KRPC_Type_NullableType`.

   .. function:: krpc_error_t krpc_KRPC_Expression_ConstantObject(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, uint64_t value)

      A constant object, an instance of a class a service defines.

      :Parameters:

       * **value** -- The object identifier of the object.

   

      .. note::

         The object is given by its object identifier.

   .. function:: krpc_error_t krpc_KRPC_Expression_ConstantString(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const char * value)

      A constant value of string type.

      :Parameters:

       * **value** -- The value.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_ConstantUInt(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, uint32_t value)

      A constant value of unsigned integer type.

      :Parameters:

       * **value** -- The value.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_ConstantULong(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, uint64_t value)

      A constant value of unsigned long integer type.

      :Parameters:

       * **value** -- The value.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_Contains(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t value)

      Whether a collection contains a value.

      :Parameters:

       * **arg** -- The collection.
       * **value** -- The value to look for.

      :returns: Whether the collection contains the value.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_ContainsKey(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t dictionary, krpc_KRPC_Expression_t key)

      Whether a dictionary contains a key.

      :Parameters:

       * **dictionary** -- The dictionary.
       * **key** -- The key to look for.

      :returns: Whether the dictionary contains the key.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_Continue(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue)

      A continue statement, which skips to the next iteration of the enclosing loop.


   

   .. function:: krpc_error_t krpc_KRPC_Expression_ConvertToString(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg)

      The string representation of a value.

      :Parameters:

       * **arg** -- The value to convert.

      :returns: The string representation of the value.
   

      .. note::

         A value other than a number, boolean, string or enumeration converts as .NET
         formats it, which for a collection is the name of its type. Throws an exception if
         the value is null.

   .. function:: krpc_error_t krpc_KRPC_Expression_Count(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg)

      The number of values in a collection.

      :Parameters:

       * **arg** -- The list, set or dictionary.

      :returns: The number of elements in the collection.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_CreateDictionary(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_list_object_t * keys, const krpc_list_object_t * values)

      A dictionary of the given keys and corresponding values, with numbers widened to
      their common type. Throws an exception if there are no keys, the counts differ, the
      keys are not of a valid key type, or a key is null.

      :Parameters:

       * **keys** -- The keys.
       * **values** -- The values.

      :returns: The dictionary.
   

      .. note::

         A repeated key takes the last of its values.

   .. function:: krpc_error_t krpc_KRPC_Expression_CreateEmptyDictionary(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Type_t keyType, krpc_KRPC_Type_t valueType)

      An empty dictionary of the given key and value types. Throws an exception if the
      key type is nullable or not a valid key type.

      :Parameters:

       * **keyType** -- The type of the dictionary's keys.
       * **valueType** -- The type of the dictionary's values.

      :returns: The empty dictionary.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_CreateEmptyList(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Type_t valueType)

      An empty list of the given value type.

      :Parameters:

       * **valueType** -- The type of the values the list holds.

      :returns: The empty list.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_CreateEmptySet(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Type_t valueType)

      An empty set of the given value type. Throws an exception if the value type is
      nullable.

      :Parameters:

       * **valueType** -- The type of the values the set holds.

      :returns: The empty set.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_CreateList(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_list_object_t * values)

      A list of the given values, with numbers widened to their common type. Throws an
      exception if there are no values.

      :Parameters:

       * **values** -- The values.

      :returns: The list.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_CreateSet(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_set_object_t * values)

      A set of the given values, with numbers widened to their common type. Throws an
      exception if there are no values, or a value is null.

      :Parameters:

       * **values** -- The values.

      :returns: The set.
   

      .. note::

         The values are evaluated in an unspecified order.

   .. function:: krpc_error_t krpc_KRPC_Expression_CreateStruct(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Type_t type, const krpc_list_object_t * fieldValues)

      A structure with the given field values. Throws an exception if the field values do
      not match its fields.

      :Parameters:

       * **type** -- The type of the structure.
       * **fieldValues** -- The values of the structure's fields, in the order the structure declares them.

      :returns: The structure.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_CreateTuple(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_list_object_t * elements)

      A tuple of the given elements. Throws an exception if there are no elements or more
      than seven.

      :Parameters:

       * **elements** -- The elements.

      :returns: The tuple.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_DictionaryKeys(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t dictionary)

      The keys of a dictionary, as a list.

      :Parameters:

       * **dictionary** -- The dictionary.

      :returns: The keys of the dictionary.
   

      .. note::

         The collection operations take the keys of a dictionary from here, and its values
         from :func:`krpc_KRPC_Expression_DictionaryValues`.

   .. function:: krpc_error_t krpc_KRPC_Expression_DictionaryValues(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t dictionary)

      The values of a dictionary, as a list.

      :Parameters:

       * **dictionary** -- The dictionary.

      :returns: The values of the dictionary.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_Distinct(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg)

      The values of a collection with duplicates removed, keeping the first
      occurrence of each.

      :Parameters:

       * **arg** -- The collection.

      :returns: The distinct values of the collection.
   

      .. note::

         The result is lazily evaluated. :func:`krpc_KRPC_Expression_ToList` makes it concrete.

   .. function:: krpc_error_t krpc_KRPC_Expression_Divide(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1)

      Numerical division.

      :Parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_ElementAt(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t index)

      The value at the given position in a collection.

      :Parameters:

       * **arg** -- The collection.
       * **index** -- The position of the value, counting from zero.

      :returns: The value at the given position.
   

      .. note::

         Throws an exception if the index is out of range.

   .. function:: krpc_error_t krpc_KRPC_Expression_Equal(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1)

      Equality comparison.

      :Parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

   

      .. note::

         Collections, tuples, bytes and structures compare by their contents. Throws an
         exception if the operands are of unrelated types.

   .. function:: krpc_error_t krpc_KRPC_Expression_Except(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg1, krpc_KRPC_Expression_t arg2)

      The values of the first collection that are not in the second, with
      duplicates removed.

      :Parameters:

       * **arg1** -- The first collection.
       * **arg2** -- The second collection.

      :returns: The difference between the two collections.
   

      .. note::

         The result is lazily evaluated. :func:`krpc_KRPC_Expression_ToList` makes it concrete.

   .. function:: krpc_error_t krpc_KRPC_Expression_ExclusiveOr(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1)

      Logical or bitwise exclusive or.

      :Parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

   

      .. note::

         Also takes two values of a flags enumeration, and gives the values in only one of them.

   .. function:: krpc_error_t krpc_KRPC_Expression_First(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg)

      The first value of a collection.

      :Parameters:

       * **arg** -- The collection.

      :returns: The first value.
   

      .. note::

         Throws an exception if the collection is empty.

   .. function:: krpc_error_t krpc_KRPC_Expression_ForEach(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t variable, krpc_KRPC_Expression_t collection, krpc_KRPC_Expression_t body)

      A loop that evaluates the body once for each value of a collection.

      :Parameters:

       * **variable** -- The loop variable.
       * **collection** -- The collection to iterate over.
       * **body** -- The statement evaluated on each iteration.

   

      .. note::

         The loop variable is a local variable declared by an enclosing
         :func:`krpc_KRPC_Expression_BlockWithVariables`, or a parameter of an enclosing :func:`krpc_KRPC_Expression_Lambda`.
         A :func:`krpc_KRPC_Expression_Break` or :func:`krpc_KRPC_Expression_Continue` in the body applies to this loop.

   .. function:: krpc_error_t krpc_KRPC_Expression_Get(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t index)

      An element of a tuple, list or dictionary.

      :Parameters:

       * **arg** -- The tuple, list or dictionary.
       * **index** -- The index or key of the element.

      :returns: The element.
   

      .. note::

         A tuple takes a constant integer index, a list a zero indexed integer and a
         dictionary a key. Throws an exception if the index is out of range or the key is
         missing, and for a tuple when the expression is created.

   .. function:: krpc_error_t krpc_KRPC_Expression_GetField(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, const char * name)

      The value of a field of a structure. Throws an exception if the value is not a
      structure, or has no field of the given name.

      :Parameters:

       * **arg** -- The structure.
       * **name** -- The name of the field to access.

      :returns: The value of the field.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_GreaterThan(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1)

      Greater than numerical comparison.

      :Parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_GreaterThanOrEqual(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1)

      Greater than or equal numerical comparison.

      :Parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_GroupBy(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t key)

      The values of a collection grouped by the key a function produces for each of them.

      :Parameters:

       * **arg** -- The collection.
       * **key** -- A function producing the key to group values by.

      :returns: A dictionary of each key to the values that produced it.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_HasReturnType(krpc_connection_t connection, bool * returnValue, krpc_KRPC_Expression_t instance)

      Whether the expression evaluates to a value.

   

      .. note::

         An expression with no value has no :func:`krpc_KRPC_Expression_ReturnType`. An empty
         collection and no value both encode to an empty sequence of bytes.

   .. function:: krpc_error_t krpc_KRPC_Expression_IfThen(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t condition, krpc_KRPC_Expression_t body)

      An if statement, with no value.

      :Parameters:

       * **condition** -- The boolean condition.
       * **body** -- The statement evaluated when the condition is true.

   

      .. note::

         Use :func:`krpc_KRPC_Expression_Conditional` for an if-then-else that produces a value.

   .. function:: krpc_error_t krpc_KRPC_Expression_IfThenElse(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t condition, krpc_KRPC_Expression_t body, krpc_KRPC_Expression_t elseBody)

      An if-else statement, with no value.

      :Parameters:

       * **condition** -- The boolean condition.
       * **body** -- The statement evaluated when the condition is true.
       * **elseBody** -- The statement evaluated when the condition is false.

   

      .. note::

         Use :func:`krpc_KRPC_Expression_Conditional` for an if-then-else that produces a value.

   .. function:: krpc_error_t krpc_KRPC_Expression_Intersect(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg1, krpc_KRPC_Expression_t arg2)

      The values in both of two collections, with duplicates removed.

      :Parameters:

       * **arg1** -- The first collection.
       * **arg2** -- The second collection.

      :returns: The intersection of the two collections.
   

      .. note::

         The result is lazily evaluated. :func:`krpc_KRPC_Expression_ToList` makes it concrete.

   .. function:: krpc_error_t krpc_KRPC_Expression_Invoke(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t function, const krpc_dictionary_string_object_t * args)

      A function call.

      :Parameters:

       * **function** -- The function to call.
       * **args** -- The arguments to call the function with.

      :returns: A function call.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_IsNull(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t value)

      Whether a value is null. Throws an exception if the type of the value cannot be null.

      :Parameters:

       * **value** -- The value.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_Lambda(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_list_object_t * parameters, krpc_KRPC_Expression_t body)

      A function with the given parameters and body.

      :Parameters:

       * **parameters** -- The parameters of the function.
       * **body** -- The body of the function.

      :returns: A function.
   

      .. note::

         The body is an expression or a block of statements, and a body with no value is
         evaluated for its effects. A :func:`krpc_KRPC_Expression_Return` or :func:`krpc_KRPC_Expression_ReturnNothing`
         statement ends the evaluation.

   .. function:: krpc_error_t krpc_KRPC_Expression_Last(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg)

      The last value of a collection.

      :Parameters:

       * **arg** -- The collection.

      :returns: The last value.
   

      .. note::

         Throws an exception if the collection is empty.

   .. function:: krpc_error_t krpc_KRPC_Expression_LeftShift(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1)

      Bitwise left shift.

      :Parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

   

      .. note::

         Only the low 5 bits of the count are used for a 32-bit value, and the low 6 bits
         for a 64-bit value.

   .. function:: krpc_error_t krpc_KRPC_Expression_LessThan(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1)

      Less than numerical comparison.

      :Parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_LessThanOrEqual(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1)

      Less than or equal numerical comparison.

      :Parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_Max(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg)

      The largest value of a collection.

      :Parameters:

       * **arg** -- The list or set.

      :returns: The largest value in the collection.
   

      .. note::

         Throws an exception if the collection is empty and its values are not nullable.

   .. function:: krpc_error_t krpc_KRPC_Expression_MaxBy(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t key)

      The value of a collection with the largest key.

      :Parameters:

       * **arg** -- The collection.
       * **key** -- A function producing the key to compare values by.

      :returns: The value with the largest key.
   

      .. note::

         Throws an exception if the collection is empty.

   .. function:: krpc_error_t krpc_KRPC_Expression_Min(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg)

      The smallest value of a collection.

      :Parameters:

       * **arg** -- The list or set.

      :returns: The smallest value in the collection.
   

      .. note::

         Throws an exception if the collection is empty and its values are not nullable.

   .. function:: krpc_error_t krpc_KRPC_Expression_MinBy(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t key)

      The value of a collection with the smallest key.

      :Parameters:

       * **arg** -- The collection.
       * **key** -- A function producing the key to compare values by.

      :returns: The value with the smallest key.
   

      .. note::

         Throws an exception if the collection is empty.

   .. function:: krpc_error_t krpc_KRPC_Expression_Modulo(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1)

      Numerical modulo operator.

      :Parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

      :returns: The remainder of arg0 divided by arg1
   

   .. function:: krpc_error_t krpc_KRPC_Expression_Multiply(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1)

      Numerical multiplication.

      :Parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_Negate(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg)

      Numerical negation, which negates a uint as a long.

      :Parameters:

       * **arg** -- The operand.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_Not(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg)

      Logical or bitwise negation.

      :Parameters:

       * **arg** -- The operand.

   

      .. note::

         Also takes a value of a flags enumeration, and gives the declared values it does not hold.

   .. function:: krpc_error_t krpc_KRPC_Expression_NotEqual(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1)

      Inequality comparison.

      :Parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

   

      .. note::

         Collections, tuples, bytes and structures compare by their contents. Throws an
         exception if the operands are of unrelated types.

   .. function:: krpc_error_t krpc_KRPC_Expression_Or(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1)

      Logical or bitwise or, which evaluates both operands.

      :Parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

   

      .. note::

         Also takes two values of a flags enumeration, and gives their combination.

   .. function:: krpc_error_t krpc_KRPC_Expression_OrderBy(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t key)

      The values of a collection ordered by the key a function produces for each of them.

      :Parameters:

       * **arg** -- The collection to order.
       * **key** -- A function producing the key to order values by.

      :returns: The ordered collection.
   

      .. note::

         The result is lazily evaluated. :func:`krpc_KRPC_Expression_ToList` makes it concrete.

   .. function:: krpc_error_t krpc_KRPC_Expression_Parameter(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const char * name, krpc_KRPC_Type_t type)

      A named parameter of a function.

      :Parameters:

       * **name** -- The name of the parameter.
       * **type** -- The type of the parameter.

      :returns: A named parameter.
   

      .. note::

         A value is bound to it by name with :func:`krpc_KRPC_Expression_Invoke`, or by its position in
         the parameters of a :func:`krpc_KRPC_Expression_Lambda`.

   .. function:: krpc_error_t krpc_KRPC_Expression_ParameterTypes(krpc_connection_t connection, krpc_list_object_t * returnValue, krpc_KRPC_Expression_t instance)

      The types of the function's parameters.

   

      .. note::

         Empty for a function that is not created with :func:`krpc_KRPC_Expression_Lambda`.

   .. function:: krpc_error_t krpc_KRPC_Expression_Power(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1)

      Numerical power operator.

      :Parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

      :returns: arg0 raised to the power of arg1, at the common type of the operands
   

   .. function:: krpc_error_t krpc_KRPC_Expression_Remove(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t collection, krpc_KRPC_Expression_t value)

      A removal of a value from a list or a set, or of an entry from a dictionary.

      :Parameters:

       * **collection** -- The list, set or dictionary.
       * **value** -- The value to remove, or the key of the entry to remove.

      :returns: Whether the collection held the value.
   

      .. note::

         The first occurrence is removed from a list.

   .. function:: krpc_error_t krpc_KRPC_Expression_RemoveAt(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t list, krpc_KRPC_Expression_t index)

      A statement removing the element at an index of a list.

      :Parameters:

       * **list** -- The list.
       * **index** -- The index of the element, counting from zero.

   

      .. note::

         Throws an exception if the index is out of range.

   .. function:: krpc_error_t krpc_KRPC_Expression_Return(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t value)

      A return statement, which ends the enclosing function with a value.

      :Parameters:

       * **value** -- The value to return.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_ReturnNothing(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue)

      A return statement, which ends the enclosing function with no value.


   

   .. function:: krpc_error_t krpc_KRPC_Expression_ReturnType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, krpc_KRPC_Expression_t instance)

      The type of the value the expression evaluates to. Throws an exception if the
      value cannot be sent to a client.

   

      .. note::

         A function created with :func:`krpc_KRPC_Expression_Lambda` evaluates to the value it returns. A
         lazily evaluated collection, such as from :func:`krpc_KRPC_Expression_Select`, is sent once
         converted by :func:`krpc_KRPC_Expression_ToList` or :func:`krpc_KRPC_Expression_ToSet`.

   .. function:: krpc_error_t krpc_KRPC_Expression_Reverse(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg)

      The values of a collection in the opposite order.

      :Parameters:

       * **arg** -- The collection.

      :returns: The reversed collection.
   

      .. note::

         The result is lazily evaluated. :func:`krpc_KRPC_Expression_ToList` makes it concrete.

   .. function:: krpc_error_t krpc_KRPC_Expression_RightShift(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1)

      Bitwise right shift.

      :Parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

   

      .. note::

         Only the low 5 bits of the count are used for a 32-bit value, and the low 6 bits
         for a 64-bit value.

   .. function:: krpc_error_t krpc_KRPC_Expression_Select(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t func)

      The result of a function for each value of a collection.

      :Parameters:

       * **arg** -- The list or set.
       * **func** -- The function, taking an element of the collection.

      :returns: The collection of function results.
   

      .. note::

         The result is lazily evaluated. :func:`krpc_KRPC_Expression_ToList` makes it concrete.

   .. function:: krpc_error_t krpc_KRPC_Expression_SelectMany(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t func)

      The values of the collections a function returns for each value of a collection,
      flattened into one.

      :Parameters:

       * **arg** -- The list or set.
       * **func** -- The function, taking an element of the collection and returning a collection.

      :returns: The flattened collection of function results.
   

      .. note::

         The result is lazily evaluated. :func:`krpc_KRPC_Expression_ToList` makes it concrete.

   .. function:: krpc_error_t krpc_KRPC_Expression_Set(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t collection, krpc_KRPC_Expression_t index, krpc_KRPC_Expression_t value)

      A statement setting an element of a list, or an entry of a dictionary.

      :Parameters:

       * **collection** -- The list or dictionary.
       * **index** -- The index of the element, counting from zero, or the key of the entry.
       * **value** -- The value to set.

   

      .. note::

         A key the dictionary does not hold is added. Throws an exception if the index
         is out of range for the list.

   .. function:: krpc_error_t krpc_KRPC_Expression_Skip(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t count)

      A collection without its first count values.

      :Parameters:

       * **arg** -- The collection.
       * **count** -- The number of values to skip.

      :returns: The collection without its first count values.
   

      .. note::

         The result is lazily evaluated. :func:`krpc_KRPC_Expression_ToList` makes it concrete.

   .. function:: krpc_error_t krpc_KRPC_Expression_StateVariable(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const char * name, krpc_KRPC_Expression_t initialValue)

      A variable that keeps its value between evaluations of a function.

      :Parameters:

       * **name** -- The name of the variable.
       * **initialValue** -- The initial value of the variable.

      :returns: A state variable.
   

      .. note::

         The initial value is evaluated once, when the variable is created, and gives the
         variable its type. The value belongs to the variable, so every function that
         contains it shares it.

   .. function:: krpc_error_t krpc_KRPC_Expression_StringConcat(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const krpc_list_object_t * args)

      The concatenation of strings.

      :Parameters:

       * **args** -- The strings to concatenate, in order.

      :returns: The concatenated string.
   

      .. note::

         Use :func:`krpc_KRPC_Expression_ConvertToString` to convert other values to strings.

   .. function:: krpc_error_t krpc_KRPC_Expression_StringContains(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t value)

      Whether one string occurs within another.

      :Parameters:

       * **arg** -- The string to search.
       * **value** -- The string to search for.

      :returns: Whether the value occurs in the string.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_StringEndsWith(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t value)

      Whether a string ends with another.

      :Parameters:

       * **arg** -- The string to test.
       * **value** -- The string to test for.

      :returns: Whether the string ends with the value.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_StringGet(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t index)

      The character at the given position in a string, as a string of length one.

      :Parameters:

       * **arg** -- The string.
       * **index** -- The position of the character, counting from zero.

      :returns: The character at the given position.
   

      .. note::

         Throws an exception if the index is out of range.

   .. function:: krpc_error_t krpc_KRPC_Expression_StringIndexOf(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t value)

      The position of the first occurrence of one string within another.

      :Parameters:

       * **arg** -- The string to search.
       * **value** -- The string to search for.

      :returns: The position of the value, counting from zero, or -1 if it does not occur.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_StringJoin(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t separator, krpc_KRPC_Expression_t values)

      Strings joined together, separated by another string.

      :Parameters:

       * **separator** -- The string to put between the values.
       * **values** -- The strings to join.

      :returns: The joined string.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_StringLength(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg)

      The number of characters in a string.

      :Parameters:

       * **arg** -- The string.

      :returns: The length of the string.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_StringReplace(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t oldValue, krpc_KRPC_Expression_t newValue)

      A string with every occurrence of one string replaced by another.

      :Parameters:

       * **arg** -- The string to search.
       * **oldValue** -- The string to replace.
       * **newValue** -- The string to replace it with.

      :returns: The string with the replacements made.
   

      .. note::

         Throws an exception if the string to replace is empty.

   .. function:: krpc_error_t krpc_KRPC_Expression_StringSplit(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t separator)

      The parts of a string separated by another string.

      :Parameters:

       * **arg** -- The string to split.
       * **separator** -- The string that separates the parts.

      :returns: The parts of the string.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_StringStartsWith(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t value)

      Whether a string starts with another.

      :Parameters:

       * **arg** -- The string to test.
       * **value** -- The string to test for.

      :returns: Whether the string starts with the value.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_StringSubstring(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t start, krpc_KRPC_Expression_t length)

      Part of a string.

      :Parameters:

       * **arg** -- The string.
       * **start** -- The position to start at, counting from zero.
       * **length** -- The number of characters to take.

      :returns: The substring.
   

      .. note::

         Throws an exception if the start or the length is out of range.

   .. function:: krpc_error_t krpc_KRPC_Expression_StringToLower(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg)

      A string in lower case, independently of the game's language.

      :Parameters:

       * **arg** -- The string.

      :returns: The string in lower case.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_StringToUpper(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg)

      A string in upper case, independently of the game's language.

      :Parameters:

       * **arg** -- The string.

      :returns: The string in upper case.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_StringTrim(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg)

      A string with white space removed from both ends.

      :Parameters:

       * **arg** -- The string.

      :returns: The trimmed string.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_StringTrimEnd(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg)

      A string with white space removed from its end.

      :Parameters:

       * **arg** -- The string.

      :returns: The trimmed string.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_StringTrimStart(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg)

      A string with white space removed from its start.

      :Parameters:

       * **arg** -- The string.

      :returns: The trimmed string.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_Subtract(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg0, krpc_KRPC_Expression_t arg1)

      Numerical subtraction.

      :Parameters:

       * **arg0** -- The first operand.
       * **arg1** -- The second operand.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_Sum(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg)

      The sum of the values of a collection.

      :Parameters:

       * **arg** -- The list or set.

      :returns: The sum of the elements in the collection.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_Take(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t count)

      The first count values of a collection.

      :Parameters:

       * **arg** -- The collection.
       * **count** -- The number of values to take.

      :returns: The first count values of the collection.
   

      .. note::

         The result is lazily evaluated. :func:`krpc_KRPC_Expression_ToList` makes it concrete.

   .. function:: krpc_error_t krpc_KRPC_Expression_Throw(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const char * service, const char * name, krpc_KRPC_Expression_t message)

      A throw statement, which raises the named exception.

      :Parameters:

       * **service** -- The name of the service the exception is defined in.
       * **name** -- The name of the exception.
       * **message** -- The message of the exception.

   

      .. note::

         The client receives the exception as an error from the function, or from the stream
         or event evaluating it. For example, the service ``KRPC`` and the name
         ``Error`` raise :macro:`KRPC_EXCEPTION_KRPC_ERROR`. Throws an exception if the service does not
         declare it.

   .. function:: krpc_error_t krpc_KRPC_Expression_ToList(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg)

      A collection as a list.

      :Parameters:

       * **arg** -- The collection.

      :returns: The collection as a list.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_ToSet(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg)

      A collection as a set.

      :Parameters:

       * **arg** -- The collection.

      :returns: The collection as a set.
   

      .. note::

         Throws an exception if a value is null.

   .. function:: krpc_error_t krpc_KRPC_Expression_TryCatch(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t body, const char * service, const char * name, krpc_KRPC_Expression_t message, krpc_KRPC_Expression_t handler)

      A try-catch statement that handles the named exception.

      :Parameters:

       * **body** -- The statement to evaluate.
       * **service** -- The name of the service the exception is defined in.
       * **name** -- The name of the exception.
       * **message** -- A string variable assigned the exception's message, or null.
       * **handler** -- The statement evaluated when the exception is caught.

   

      .. note::

         Only an exception a service declares can be named, and :func:`krpc_KRPC_Expression_TryCatchAll`
         handles any exception. Throws an exception if the service does not declare it.

   .. function:: krpc_error_t krpc_KRPC_Expression_TryCatchAll(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t body, krpc_KRPC_Expression_t message, krpc_KRPC_Expression_t handler)

      A try-catch statement that handles any exception.

      :Parameters:

       * **body** -- The statement to evaluate.
       * **message** -- A string variable assigned the exception's message, or null.
       * **handler** -- The statement evaluated when an exception is caught.

   

   .. function:: krpc_error_t krpc_KRPC_Expression_TryFinally(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t body, krpc_KRPC_Expression_t finalizer)

      A try-finally statement.

      :Parameters:

       * **body** -- The statement to evaluate.
       * **finalizer** -- The statement evaluated after the body.

   

      .. note::

         The finalizer is evaluated after the body, whether or not the body raises an
         exception. A break, continue or return cannot leave the finalizer.

   .. function:: krpc_error_t krpc_KRPC_Expression_Union(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg1, krpc_KRPC_Expression_t arg2)

      The values in either of two collections, with duplicates removed.

      :Parameters:

       * **arg1** -- The first collection.
       * **arg2** -- The second collection.

      :returns: The union of the two collections.
   

      .. note::

         The result is lazily evaluated. :func:`krpc_KRPC_Expression_ToList` makes it concrete.

   .. function:: krpc_error_t krpc_KRPC_Expression_Variable(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, const char * name, krpc_KRPC_Type_t type)

      A local variable, declared by :func:`krpc_KRPC_Expression_BlockWithVariables`.

      :Parameters:

       * **name** -- The name of the variable.
       * **type** -- The type of the variable.

      :returns: A local variable.
   

   .. function:: krpc_error_t krpc_KRPC_Expression_Where(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg, krpc_KRPC_Expression_t func)

      The values of a collection for which a predicate function returns true.

      :Parameters:

       * **arg** -- The list or set.
       * **func** -- The predicate function, taking an element of the collection.

      :returns: The filtered collection.
   

      .. note::

         The result is lazily evaluated. :func:`krpc_KRPC_Expression_ToList` makes it concrete.

   .. function:: krpc_error_t krpc_KRPC_Expression_While(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t condition, krpc_KRPC_Expression_t body)

      A while loop, which evaluates the body while the condition is true.

      :Parameters:

       * **condition** -- The boolean condition.
       * **body** -- The statement evaluated on each iteration.

   

      .. note::

         A :func:`krpc_KRPC_Expression_Break` or :func:`krpc_KRPC_Expression_Continue` in the body applies to this loop.

   .. function:: krpc_error_t krpc_KRPC_Expression_Zip(krpc_connection_t connection, krpc_KRPC_Expression_t * returnValue, krpc_KRPC_Expression_t arg1, krpc_KRPC_Expression_t arg2, krpc_KRPC_Expression_t func)

      The results of a function applied to the values of two collections in pairs.

      :Parameters:

       * **arg1** -- The first collection.
       * **arg2** -- The second collection.
       * **func** -- A function combining a value from each collection.

      :returns: The combined collection.
   

      .. note::

         The result is as long as the shorter of the two collections. It is lazily
         evaluated, and :func:`krpc_KRPC_Expression_ToList` makes it concrete.



.. type:: krpc_KRPC_Type_t

   The type of a value a procedure takes or returns.

   .. function:: krpc_error_t krpc_KRPC_Type_Bool(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue)

      A boolean type.


   

   .. function:: krpc_error_t krpc_KRPC_Type_Bytes(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue)

      A byte sequence type.


   

   .. function:: krpc_error_t krpc_KRPC_Type_ClassType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, const char * service, const char * name)

      The type of a class defined by a service.

      :Parameters:

       * **service** -- The name of the service the class is defined in.
       * **name** -- The name of the class.

   

   .. function:: krpc_error_t krpc_KRPC_Type_Code(krpc_connection_t connection, krpc_KRPC_TypeCode_t * returnValue, krpc_KRPC_Type_t instance)

      The kind of the type.

   

   .. function:: krpc_error_t krpc_KRPC_Type_DictionaryType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, krpc_KRPC_Type_t keyType, krpc_KRPC_Type_t valueType)

      A dictionary type. Throws an exception if the key type is nullable or not a valid
      key type.

      :Parameters:

       * **keyType** -- The type of the keys of the dictionary.
       * **valueType** -- The type of the values of the dictionary.

   

   .. function:: krpc_error_t krpc_KRPC_Type_Double(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue)

      A double precision floating point number type.


   

   .. function:: krpc_error_t krpc_KRPC_Type_EnumerationType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, const char * service, const char * name)

      The type of an enumeration defined by a service.

      :Parameters:

       * **service** -- The name of the service the enumeration is defined in.
       * **name** -- The name of the enumeration.

   

   .. function:: krpc_error_t krpc_KRPC_Type_Float(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue)

      A single precision floating point number type.


   

   .. function:: krpc_error_t krpc_KRPC_Type_Int(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue)

      A signed 32-bit integer type.


   

   .. function:: krpc_error_t krpc_KRPC_Type_ListType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, krpc_KRPC_Type_t valueType)

      A list type.

      :Parameters:

       * **valueType** -- The type of the elements of the list.

   

   .. function:: krpc_error_t krpc_KRPC_Type_Long(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue)

      A signed 64-bit integer type.


   

   .. function:: krpc_error_t krpc_KRPC_Type_Name(krpc_connection_t connection, char * * returnValue, krpc_KRPC_Type_t instance)

      The name of a class, enumeration or structure type, or an empty string.

   

   .. function:: krpc_error_t krpc_KRPC_Type_Nullable(krpc_connection_t connection, bool * returnValue, krpc_KRPC_Type_t instance)

      Whether a value of the type may be null.

   

      .. note::

         Any type can be nullable. A string or class value of a type that is not nullable
         is never null.

   .. function:: krpc_error_t krpc_KRPC_Type_NullableType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, krpc_KRPC_Type_t valueType)

      A nullable type, holding a value of the given type or null.

      :Parameters:

       * **valueType** -- The type of the value.

   

   .. function:: krpc_error_t krpc_KRPC_Type_Service(krpc_connection_t connection, char * * returnValue, krpc_KRPC_Type_t instance)

      The service a class, enumeration or structure type is defined in, or an empty string.

   

   .. function:: krpc_error_t krpc_KRPC_Type_SetType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, krpc_KRPC_Type_t valueType)

      A set type. Throws an exception if the element type is nullable.

      :Parameters:

       * **valueType** -- The type of the elements of the set.

   

   .. function:: krpc_error_t krpc_KRPC_Type_String(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue)

      A string type.


   

   .. function:: krpc_error_t krpc_KRPC_Type_StructType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, const char * service, const char * name)

      The type of a structure defined by a service.

      :Parameters:

       * **service** -- The name of the service the structure is defined in.
       * **name** -- The name of the structure.

   

   .. function:: krpc_error_t krpc_KRPC_Type_TupleType(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue, const krpc_list_object_t * valueTypes)

      A tuple type. Throws an exception if there are no element types or more than seven.

      :Parameters:

       * **valueTypes** -- The types of the elements of the tuple.

   

   .. function:: krpc_error_t krpc_KRPC_Type_Types(krpc_connection_t connection, krpc_list_object_t * returnValue, krpc_KRPC_Type_t instance)

      The types of the elements of a tuple, list, set or dictionary type.

   

      .. note::

         The value types of a tuple, the value type of a list or set, or the key and value
         types of a dictionary. An empty list for any other type.

   .. function:: krpc_error_t krpc_KRPC_Type_UInt(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue)

      An unsigned 32-bit integer type.


   

   .. function:: krpc_error_t krpc_KRPC_Type_ULong(krpc_connection_t connection, krpc_KRPC_Type_t * returnValue)

      An unsigned 64-bit integer type.


   



.. type:: krpc_KRPC_GameScene_t

   The game scene. See :func:`krpc_KRPC_GameScene`.


   .. macro:: KRPC_KRPC_GAMESCENE_ADMINISTRATION

      The administration facility. This is a pseudo-scene, shown when the
      administration facility is open within the space center scene.


   .. macro:: KRPC_KRPC_GAMESCENE_ASTRONAUTCOMPLEX

      The astronaut complex. This is a pseudo-scene, shown when the
      astronaut complex facility is open within the space center scene.


   .. macro:: KRPC_KRPC_GAMESCENE_EDITORSPH

      The Space Plane Hangar.


   .. macro:: KRPC_KRPC_GAMESCENE_EDITORVAB

      The Vehicle Assembly Building.


   .. macro:: KRPC_KRPC_GAMESCENE_FLIGHT

      The game scene showing a vessel in flight (or on the launchpad/runway).


   .. macro:: KRPC_KRPC_GAMESCENE_MISSIONBUILDER

      The mission builder.


   .. macro:: KRPC_KRPC_GAMESCENE_MISSIONCONTROL

      Mission control. This is a pseudo-scene, shown when the
      mission control facility is open within the space center scene.


   .. macro:: KRPC_KRPC_GAMESCENE_RESEARCHANDDEVELOPMENT

      Research and development. This is a pseudo-scene, shown when the
      research and development facility is open within the space center scene.


   .. macro:: KRPC_KRPC_GAMESCENE_SPACECENTER

      The game scene showing the Kerbal Space Center buildings.


   .. macro:: KRPC_KRPC_GAMESCENE_TRACKINGSTATION

      The tracking station.



.. type:: krpc_KRPC_TypeCode_t

   The kind of a type, with the values of the communication protocol's type codes.


   .. macro:: KRPC_KRPC_TYPECODE_BOOL

      A boolean.


   .. macro:: KRPC_KRPC_TYPECODE_BYTES

      A sequence of bytes.


   .. macro:: KRPC_KRPC_TYPECODE_CLASS

      A class defined by a service.


   .. macro:: KRPC_KRPC_TYPECODE_DICTIONARY

      A dictionary, with the types of its keys and values in :func:`krpc_KRPC_Type_Types`.


   .. macro:: KRPC_KRPC_TYPECODE_DOUBLE

      A double precision floating point number.


   .. macro:: KRPC_KRPC_TYPECODE_ENUMERATION

      An enumeration defined by a service.


   .. macro:: KRPC_KRPC_TYPECODE_FLOAT

      A single precision floating point number.


   .. macro:: KRPC_KRPC_TYPECODE_LIST

      A list, with the type of its values in :func:`krpc_KRPC_Type_Types`.


   .. macro:: KRPC_KRPC_TYPECODE_SINT32

      A signed 32-bit integer.


   .. macro:: KRPC_KRPC_TYPECODE_SINT64

      A signed 64-bit integer.


   .. macro:: KRPC_KRPC_TYPECODE_SET

      A set, with the type of its values in :func:`krpc_KRPC_Type_Types`.


   .. macro:: KRPC_KRPC_TYPECODE_STRING

      A string.


   .. macro:: KRPC_KRPC_TYPECODE_STRUCT

      A structure defined by a service.


   .. macro:: KRPC_KRPC_TYPECODE_TUPLE

      A tuple, with the types of its values in :func:`krpc_KRPC_Type_Types`.


   .. macro:: KRPC_KRPC_TYPECODE_UINT32

      An unsigned 32-bit integer.


   .. macro:: KRPC_KRPC_TYPECODE_UINT64

      An unsigned 64-bit integer.



.. type:: krpc_KRPC_ClientInfo_t

   A client connected to the server.

   .. note::

      See :func:`krpc_KRPC_Clients`.
   .. member:: krpc_bytes_t id

      The identifier of the client.

   .. member:: char * name

      The name of the client, or an empty string if it has no name.

   .. member:: char * address

      The address of the client.

   .. member:: uint32_t protocol_version

      The protocol version of the client's connection.


.. type:: krpc_KRPC_ServerSettings_t

   The settings that control how the server executes calls.

   .. note::

      See :member:`krpc_KRPC_ServerStatus_t.settings`.
   .. member:: bool one_rpc_per_update

      Whether the server executes one call per update.

   .. member:: uint32_t max_time_per_update

      The current maximum time spent executing calls per update, in microseconds.

   .. member:: bool adaptive_rate_control

      Whether the server adjusts :member:`krpc_KRPC_ServerSettings_t.max_time_per_update` to hold the frame rate.

   .. member:: bool blocking_recv

      Whether the server waits for the next call in an update.

   .. member:: uint32_t recv_timeout

      The time the server waits for the next call, in microseconds.

   .. member:: uint32_t tick_hold_timeout

      The longest time a client may hold the game on one physics tick, in microseconds.


.. type:: krpc_KRPC_ServerStatistics_t

   The performance statistics of the server.

   .. note::

      See :member:`krpc_KRPC_ServerStatus_t.statistics`.
   .. member:: uint64_t bytes_read

      The number of bytes received from clients.

   .. member:: uint64_t bytes_written

      The number of bytes sent to clients.

   .. member:: float bytes_read_rate

      The rate bytes are received from clients, in bytes per second.

   .. member:: float bytes_written_rate

      The rate bytes are sent to clients, in bytes per second.

   .. member:: uint64_t rpcs_executed

      The number of calls executed.

   .. member:: float rpc_rate

      The number of calls executed per second.

   .. member:: float time_per_rpc_update

      The time spent receiving and executing calls per update, in seconds.

   .. member:: float poll_time_per_rpc_update

      The time spent receiving calls per update, in seconds.

   .. member:: float exec_time_per_rpc_update

      The time spent executing calls per update, in seconds.

   .. member:: uint32_t deferred_calls

      The number of calls left for the next update, because the update ran out of time
      or the call yielded.

   .. member:: uint32_t streams

      The number of open streams, events included.

   .. member:: uint64_t stream_evaluations

      The number of stream evaluations, one per stream per update.

   .. member:: float stream_evaluation_rate

      The number of stream evaluations per second.

   .. member:: uint64_t stream_updates_sent

      The number of stream updates sent to clients.

   .. member:: float time_per_stream_update

      The time spent updating streams per update, in seconds.


.. type:: krpc_KRPC_ServerStatus_t

   The status of the server.

   .. note::

      See :func:`krpc_KRPC_GetStatus`.
   .. member:: char * version

      The version of the server, or ``unknown``.

   .. member:: uint32_t protocol_version

      The protocol version of the connection.

   .. member:: krpc_KRPC_ServerSettings_t settings

      The settings of the server.

   .. member:: krpc_KRPC_ServerStatistics_t statistics

      The performance statistics of the server.


.. macro:: KRPC_EXCEPTION_KRPC_ARGUMENTEXCEPTION

   A method was invoked where at least one of the passed arguments does not
   meet the parameter specification of the method.

   The service and exception ids of the exception, for :macro:`KRPC_IS_EXCEPTION`. A call
   that fails with it returns :macro:`KRPC_ERROR_SERVER`.



.. macro:: KRPC_EXCEPTION_KRPC_ARGUMENTNULLEXCEPTION

   A null reference was passed to a method that does not accept it as a valid argument.

   The service and exception ids of the exception, for :macro:`KRPC_IS_EXCEPTION`. A call
   that fails with it returns :macro:`KRPC_ERROR_SERVER`.



.. macro:: KRPC_EXCEPTION_KRPC_ARGUMENTOUTOFRANGEEXCEPTION

   The value of an argument is outside the allowable range of values as defined by the invoked method.

   The service and exception ids of the exception, for :macro:`KRPC_IS_EXCEPTION`. A call
   that fails with it returns :macro:`KRPC_ERROR_SERVER`.



.. macro:: KRPC_EXCEPTION_KRPC_DIVIDEBYZEROEXCEPTION

   An integer was divided by zero.

   The service and exception ids of the exception, for :macro:`KRPC_IS_EXCEPTION`. A call
   that fails with it returns :macro:`KRPC_ERROR_SERVER`.



.. macro:: KRPC_EXCEPTION_KRPC_ERROR

   A general error, described by its message.

   The service and exception ids of the exception, for :macro:`KRPC_IS_EXCEPTION`. A call
   that fails with it returns :macro:`KRPC_ERROR_SERVER`.



.. macro:: KRPC_EXCEPTION_KRPC_INDEXOUTOFRANGEEXCEPTION

   An index was outside the bounds of a collection, string or array.

   The service and exception ids of the exception, for :macro:`KRPC_IS_EXCEPTION`. A call
   that fails with it returns :macro:`KRPC_ERROR_SERVER`.



.. macro:: KRPC_EXCEPTION_KRPC_INVALIDOPERATIONEXCEPTION

   A method call was made to a method that is invalid
   given the current state of the object.

   The service and exception ids of the exception, for :macro:`KRPC_IS_EXCEPTION`. A call
   that fails with it returns :macro:`KRPC_ERROR_SERVER`.



.. macro:: KRPC_EXCEPTION_KRPC_KEYNOTFOUNDEXCEPTION

   A key was not found in a dictionary.

   The service and exception ids of the exception, for :macro:`KRPC_IS_EXCEPTION`. A call
   that fails with it returns :macro:`KRPC_ERROR_SERVER`.



.. macro:: KRPC_EXCEPTION_KRPC_NOTSUPPORTEDEXCEPTION

   A method was invoked that is not supported.

   The service and exception ids of the exception, for :macro:`KRPC_IS_EXCEPTION`. A call
   that fails with it returns :macro:`KRPC_ERROR_SERVER`.



.. macro:: KRPC_EXCEPTION_KRPC_NULLREFERENCEEXCEPTION

   A value was null where its type does not allow one.

   The service and exception ids of the exception, for :macro:`KRPC_IS_EXCEPTION`. A call
   that fails with it returns :macro:`KRPC_ERROR_SERVER`.



.. macro:: KRPC_EXCEPTION_KRPC_OBJECTDESTROYEDEXCEPTION

   The game object that a service object refers to no longer exists,
   so the object cannot be used. For example, the part it represents
   was destroyed, or the vessel it belongs to was recovered.

   The service and exception ids of the exception, for :macro:`KRPC_IS_EXCEPTION`. A call
   that fails with it returns :macro:`KRPC_ERROR_SERVER`.

   .. note::

      See :macro:`KRPC_EXCEPTION_KRPC_OBJECTNOTLOADEDEXCEPTION` for a game object that exists but is not
      loaded.

.. macro:: KRPC_EXCEPTION_KRPC_OBJECTNOTLOADEDEXCEPTION

   The game object that a service object refers to exists, but is not loaded.
   For example, the game is loading, or the vessel carrying the part is unloaded.

   The service and exception ids of the exception, for :macro:`KRPC_IS_EXCEPTION`. A call
   that fails with it returns :macro:`KRPC_ERROR_SERVER`.

   .. note::

      See :macro:`KRPC_EXCEPTION_KRPC_OBJECTDESTROYEDEXCEPTION` for a game object that no longer exists.

.. macro:: KRPC_EXCEPTION_KRPC_OVERFLOWEXCEPTION

   An arithmetic or conversion operation overflowed.

   The service and exception ids of the exception, for :macro:`KRPC_IS_EXCEPTION`. A call
   that fails with it returns :macro:`KRPC_ERROR_SERVER`.



.. macro:: KRPC_EXCEPTION_KRPC_STREAMREMOVEDEXCEPTION

   A stream was removed by the server, e.g. an event that has finished.

   The service and exception ids of the exception, for :macro:`KRPC_IS_EXCEPTION`. A call
   that fails with it returns :macro:`KRPC_ERROR_SERVER`.



.. macro:: KRPC_EXCEPTION_KRPC_TIMEOUTEXCEPTION

   An operation did not complete in the time allotted to it.

   The service and exception ids of the exception, for :macro:`KRPC_IS_EXCEPTION`. A call
   that fails with it returns :macro:`KRPC_ERROR_SERVER`.



.. macro:: KRPC_EXCEPTION_KRPC_UNSUPPORTEDSCENEEXCEPTION

   The procedure is not available in the current game scene.

   The service and exception ids of the exception, for :macro:`KRPC_IS_EXCEPTION`. A call
   that fails with it returns :macro:`KRPC_ERROR_SERVER`.
