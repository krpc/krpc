.. default-domain:: py
.. highlight:: py

.. currentmodule:: KRPC


.. module:: KRPC

Main kRPC service, used by clients to interact with basic server functionality.


.. attribute:: clients

   The clients that are currently connected to the server.

   :Attribute: Read-only, cannot be set
   :rtype: list(:class:`ClientInfo`)
   




.. attribute:: game_scene

   The current game scene. Setting this switches the game to the given
   scene, or opens/closes the corresponding facility for the pseudo-scenes.
   Scene changes happen asynchronously: setting this property returns
   immediately, and clients should poll it until it reports the requested
   scene. Setting it to :attr:`GameScene.flight` resumes the save's
   active vessel, and fails if there is none. Moving between
   :attr:`GameScene.editor_vab` and :attr:`GameScene.editor_sph`
   keeps the vessel that is being constructed, as the game's own switch
   editor button does. Leaving flight saves the game first, unless the game
   cannot be saved, e.g. while under acceleration.

   :Attribute: Can be read or written
   :rtype: :class:`GameScene`
   




.. attribute:: game_state_loads

   The number of game states that have finished loading.

   :Attribute: Read-only, cannot be set
   :rtype: int
   

   .. note::

      Increases once a game load, quickload, revert or scene change has finished.




.. staticmethod:: get_client_id()

   Returns the identifier for the current client.

   :rtype: bytes
   




.. staticmethod:: get_client_name()

   Returns the name of the current client.
   This is an empty string if the client has no name.

   :rtype: str
   




.. staticmethod:: get_services()

   The services the server provides, with their procedures, classes, enumerations,
   structures and exceptions.

   :rtype: :class:`krpc.schema.KRPC.Services`
   




.. staticmethod:: get_status()

   Returns the status of the server, with its version, settings and performance statistics.

   :rtype: :class:`ServerStatus`
   




.. staticmethod:: hold_tick()

   Hold the game on its current physics tick until :meth:`release_tick` is called
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




.. staticmethod:: next_tick()

   Let the game move on from the tick being held and hold the one after it, so that a
   program acts on consecutive physics ticks. Holds the soonest tick it can if no tick
   is currently being held.

   

   .. note::

      A release and a hold made as two separate calls let the game advance between them.
      This takes the next tick without that gap. Call it at the top of a loop that has to
      act on every tick, work, and come back to it.

      The game advances one tick per call, so it runs at the rate of the loop for as long
      as the loop runs. End the loop with :meth:`release_tick`.

      Every limit of :meth:`hold_tick` applies, the timeout included. A hold lost to
      the timeout is not reported, and the next call takes whatever tick it lands on.




.. attribute:: paused

   Whether the game is paused.

   :Attribute: Can be read or written
   :rtype: bool
   




.. staticmethod:: release_tick()

   Let the game move on from the tick that :meth:`hold_tick` held it on. Does
   nothing when no tick is held, which is the case once a hold has timed out.

   





.. class:: Expression

   A server side expression.

   .. note::

      Numeric operands of differing types are converted to a common type.

   .. staticmethod:: add(arg0, arg1)

      Numerical addition.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
      :rtype: :class:`Expression`
   

   .. staticmethod:: aggregate(arg, func)

      The result of applying an accumulator function over a collection.

      :param Expression arg: The collection.
      :param Expression func: The accumulator function.
      :returns: The accumulated value.
      :rtype: :class:`Expression`
   

      .. note::

         Throws an exception if the collection is empty.

   .. staticmethod:: aggregate_with_seed(arg, seed, func)

      The result of applying an accumulator function over a collection, starting from a seed.

      :param Expression arg: The collection.
      :param Expression seed: The seed value.
      :param Expression func: The accumulator function.
      :returns: The accumulated value.
      :rtype: :class:`Expression`
   

      .. note::

         The seed widens to the type of the accumulated value, the function's result.

   .. staticmethod:: all(arg, predicate)

      Whether a predicate function returns true for every value of a collection.

      :param Expression arg: The collection.
      :param Expression predicate: The predicate function.
      :returns: Whether all items satisfy the predicate.
      :rtype: :class:`Expression`
   

   .. staticmethod:: and_(arg0, arg1)

      Logical or bitwise and, which evaluates both operands.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
      :rtype: :class:`Expression`
   

      .. note::

         Also takes two values of a flags enumeration, and gives the values they share.

   .. staticmethod:: any(arg, predicate)

      Whether a predicate function returns true for any value of a collection.

      :param Expression arg: The collection.
      :param Expression predicate: The predicate function.
      :returns: Whether any item satisfies the predicate.
      :rtype: :class:`Expression`
   

   .. staticmethod:: append(collection, value)

      A statement adding a value to a list or a set.

      :param Expression collection: The list or set to add to.
      :param Expression value: The value to add.
      :rtype: :class:`Expression`
   

      .. note::

         The value goes on the end of a list. A set holding the value is left unchanged.

   .. staticmethod:: assign(variable, value)

      An assignment to a local variable, state variable or function parameter.

      :param Expression variable: The variable to assign to.
      :param Expression value: The value to assign.
      :rtype: :class:`Expression`
   

      .. note::

         The value's type must be assignable to the variable's type. A numeric value of a
         different type is widened, and narrowing it requires a cast.

   .. staticmethod:: average(arg)

      The average of the values of a collection.

      :param Expression arg: The list or set.
      :returns: The average of the elements in the collection.
      :rtype: :class:`Expression`
   

      .. note::

         Throws an exception if the collection is empty and its values are not nullable.

   .. staticmethod:: block(statements)

      A block of statements, whose value is that of its last statement.

      :param list statements: The statements.
      :rtype: :class:`Expression`
   

   .. staticmethod:: block_with_variables(variables, statements)

      A block of statements with local variables.

      :param list variables: The local variables of the block.
      :param list statements: The statements.
      :rtype: :class:`Expression`
   

      .. note::

         The value of the block is that of its last statement. The variables are in scope
         for the statements of the block, including within nested functions.

   .. staticmethod:: break_()

      A break statement, which ends the enclosing loop or :meth:`Expression.switch`.

      :rtype: :class:`Expression`
   

   .. staticmethod:: build_dictionary(arg, key_func, value_func)

      A dictionary built from a collection, with a key and a value computed from each
      element.

      :param Expression arg: The list or set.
      :param Expression key_func: The function computing an element's key.
      :param Expression value_func: The function computing an element's value.
      :returns: The dictionary.
      :rtype: :class:`Expression`
   

      .. note::

         A repeated key takes the last of its values.

   .. staticmethod:: call(call)

      An RPC call.

      :param krpc.schema.KRPC.ProcedureCall call: The procedure call.
      :rtype: :class:`Expression`
   

      .. note::

         The instance the call is made on, and the values of its arguments, are fixed
         when the expression is created. A call to a procedure with no return value is
         evaluated for its effects, as a statement in a :meth:`Expression.block`. A collection the
         procedure returns is copied, as a client would receive it.

   .. staticmethod:: call_with_arguments(call, args)

      An RPC call, where some or all of the arguments are computed by expressions.

      :param krpc.schema.KRPC.ProcedureCall call: The RPC to call.
      :param dict args: Expressions computing the call's arguments, by position.
      :rtype: :class:`Expression`
   

      .. note::

         Position 0 is the instance of a class method or property. A position with no
         expression takes the argument encoded in the call, or the parameter's default. A
         collection the procedure returns is copied, as a client would receive it.

   .. staticmethod:: cast(arg, type)

      A value cast to the given type.

      :param Expression arg: The value to cast.
      :param Type type: Type to cast the argument to.
      :rtype: :class:`Expression`
   

      .. note::

         A tuple or collection is cast element by element.

   .. staticmethod:: clear(collection)

      A statement removing every value from a list or a set, or every entry from a dictionary.

      :param Expression collection: The list, set or dictionary.
      :rtype: :class:`Expression`
   

   .. staticmethod:: concat(arg1, arg2)

      The values of one collection followed by those of another.

      :param Expression arg1: The first collection.
      :param Expression arg2: The second collection.
      :returns: The first collection followed by the second.
      :rtype: :class:`Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.to_list` makes it concrete.

   .. staticmethod:: conditional(condition, if_true, if_false)

      The second argument if the condition is true, and the third argument otherwise.

      :param Expression condition: The boolean condition.
      :param Expression if_true: The value when the condition is true.
      :param Expression if_false: The value when the condition is false.
      :rtype: :class:`Expression`
   

      .. note::

         Throws an exception if the two values have no common type.

   .. staticmethod:: conditional_and(arg0, arg1)

      Logical and, which evaluates the second operand only when the first is true.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
      :rtype: :class:`Expression`
   

   .. staticmethod:: conditional_or(arg0, arg1)

      Logical or, which evaluates the second operand only when the first is false.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
      :rtype: :class:`Expression`
   

   .. staticmethod:: constant_bool(value)

      A constant value of boolean type.

      :param bool value: The value.
      :rtype: :class:`Expression`
   

   .. staticmethod:: constant_bytes(value)

      A constant value of bytes type.

      :param bytes value: The value.
      :rtype: :class:`Expression`
   

   .. staticmethod:: constant_double(value)

      A constant value of double precision floating point type.

      :param float value: The value.
      :rtype: :class:`Expression`
   

   .. staticmethod:: constant_enum(service, name, value)

      A constant value of an enumeration a service defines.

      :param str service: The name of the service the enumeration is defined in.
      :param str name: The name of the enumeration.
      :param int value: The value of the member of the enumeration.
      :rtype: :class:`Expression`
   

   .. staticmethod:: constant_enum_by_id(service_id, enumeration_id, value)

      A constant value of an enumeration a service defines, named by ids.

      :param int service_id: The id of the service the enumeration is defined in.
      :param int enumeration_id: The id of the enumeration.
      :param int value: The value of the member of the enumeration.
      :rtype: :class:`Expression`
   

   .. staticmethod:: constant_float(value)

      A constant value of single precision floating point type.

      :param float value: The value.
      :rtype: :class:`Expression`
   

   .. staticmethod:: constant_int(value)

      A constant value of integer type.

      :param int value: The value.
      :rtype: :class:`Expression`
   

   .. staticmethod:: constant_long(value)

      A constant value of long integer type.

      :param int value: The value.
      :rtype: :class:`Expression`
   

   .. staticmethod:: constant_null(type)

      A null constant of the given type.

      :param Type type: The type of the value.
      :rtype: :class:`Expression`
   

      .. note::

         The type is made nullable, as by :meth:`Type.nullable_type`.

   .. staticmethod:: constant_object(value)

      A constant object, an instance of a class a service defines.

      :param int value: The object identifier of the object.
      :rtype: :class:`Expression`
   

      .. note::

         The object is given by its object identifier.

   .. staticmethod:: constant_string(value)

      A constant value of string type.

      :param str value: The value.
      :rtype: :class:`Expression`
   

   .. staticmethod:: constant_uint(value)

      A constant value of unsigned integer type.

      :param int value: The value.
      :rtype: :class:`Expression`
   

   .. staticmethod:: constant_ulong(value)

      A constant value of unsigned long integer type.

      :param int value: The value.
      :rtype: :class:`Expression`
   

   .. staticmethod:: contains(arg, value)

      Whether a collection contains a value.

      :param Expression arg: The collection.
      :param Expression value: The value to look for.
      :returns: Whether the collection contains the value.
      :rtype: :class:`Expression`
   

   .. staticmethod:: contains_key(dictionary, key)

      Whether a dictionary contains a key.

      :param Expression dictionary: The dictionary.
      :param Expression key: The key to look for.
      :returns: Whether the dictionary contains the key.
      :rtype: :class:`Expression`
   

   .. staticmethod:: continue_()

      A continue statement, which skips to the next iteration of the enclosing loop.

      :rtype: :class:`Expression`
   

   .. staticmethod:: convert_to_string(arg)

      The string representation of a value.

      :param Expression arg: The value to convert.
      :returns: The string representation of the value.
      :rtype: :class:`Expression`
   

      .. note::

         A value other than a number, boolean, string or enumeration converts as .NET
         formats it, which for a collection is the name of its type. Throws an exception if
         the value is null.

   .. staticmethod:: count(arg)

      The number of values in a collection.

      :param Expression arg: The list, set or dictionary.
      :returns: The number of elements in the collection.
      :rtype: :class:`Expression`
   

   .. staticmethod:: create_dictionary(keys, values)

      A dictionary of the given keys and corresponding values, with numbers widened to
      their common type. Throws an exception if there are no keys, the counts differ, the
      keys are not of a valid key type, or a key is null.

      :param list keys: The keys.
      :param list values: The values.
      :returns: The dictionary.
      :rtype: :class:`Expression`
   

      .. note::

         A repeated key takes the last of its values.

   .. staticmethod:: create_empty_dictionary(key_type, value_type)

      An empty dictionary of the given key and value types. Throws an exception if the
      key type is nullable or not a valid key type.

      :param Type key_type: The type of the dictionary's keys.
      :param Type value_type: The type of the dictionary's values.
      :returns: The empty dictionary.
      :rtype: :class:`Expression`
   

   .. staticmethod:: create_empty_list(value_type)

      An empty list of the given value type.

      :param Type value_type: The type of the values the list holds.
      :returns: The empty list.
      :rtype: :class:`Expression`
   

   .. staticmethod:: create_empty_set(value_type)

      An empty set of the given value type. Throws an exception if the value type is
      nullable.

      :param Type value_type: The type of the values the set holds.
      :returns: The empty set.
      :rtype: :class:`Expression`
   

   .. staticmethod:: create_list(values)

      A list of the given values, with numbers widened to their common type. Throws an
      exception if there are no values.

      :param list values: The values.
      :returns: The list.
      :rtype: :class:`Expression`
   

   .. staticmethod:: create_set(values)

      A set of the given values, with numbers widened to their common type. Throws an
      exception if there are no values, or a value is null.

      :param set values: The values.
      :returns: The set.
      :rtype: :class:`Expression`
   

      .. note::

         The values are evaluated in an unspecified order.

   .. staticmethod:: create_struct(type, field_values)

      A structure with the given field values. Throws an exception if the field values do
      not match its fields.

      :param Type type: The type of the structure.
      :param list field_values: The values of the structure's fields, in the order the structure declares them.
      :returns: The structure.
      :rtype: :class:`Expression`
   

   .. staticmethod:: create_tuple(elements)

      A tuple of the given elements. Throws an exception if there are no elements or more
      than seven.

      :param list elements: The elements.
      :returns: The tuple.
      :rtype: :class:`Expression`
   

   .. staticmethod:: dictionary_keys(dictionary)

      The keys of a dictionary, as a list.

      :param Expression dictionary: The dictionary.
      :returns: The keys of the dictionary.
      :rtype: :class:`Expression`
   

      .. note::

         The collection operations take the keys of a dictionary from here, and its values
         from :meth:`Expression.dictionary_values`.

   .. staticmethod:: dictionary_values(dictionary)

      The values of a dictionary, as a list.

      :param Expression dictionary: The dictionary.
      :returns: The values of the dictionary.
      :rtype: :class:`Expression`
   

   .. staticmethod:: distinct(arg)

      The values of a collection with duplicates removed, keeping the first
      occurrence of each.

      :param Expression arg: The collection.
      :returns: The distinct values of the collection.
      :rtype: :class:`Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.to_list` makes it concrete.

   .. staticmethod:: divide(arg0, arg1)

      Numerical division.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
      :rtype: :class:`Expression`
   

   .. staticmethod:: element_at(arg, index)

      The value at the given position in a collection.

      :param Expression arg: The collection.
      :param Expression index: The position of the value, counting from zero.
      :returns: The value at the given position.
      :rtype: :class:`Expression`
   

      .. note::

         Throws an exception if the index is out of range.

   .. staticmethod:: equal(arg0, arg1)

      Equality comparison.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
      :rtype: :class:`Expression`
   

      .. note::

         Collections, tuples, bytes and structures compare by their contents. Throws an
         exception if the operands are of unrelated types.

   .. staticmethod:: except_(arg1, arg2)

      The values of the first collection that are not in the second, with
      duplicates removed.

      :param Expression arg1: The first collection.
      :param Expression arg2: The second collection.
      :returns: The difference between the two collections.
      :rtype: :class:`Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.to_list` makes it concrete.

   .. staticmethod:: exclusive_or(arg0, arg1)

      Logical or bitwise exclusive or.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
      :rtype: :class:`Expression`
   

      .. note::

         Also takes two values of a flags enumeration, and gives the values in only one of them.

   .. staticmethod:: first(arg)

      The first value of a collection.

      :param Expression arg: The collection.
      :returns: The first value.
      :rtype: :class:`Expression`
   

      .. note::

         Throws an exception if the collection is empty.

   .. staticmethod:: for_each(variable, collection, body)

      A loop that evaluates the body once for each value of a collection.

      :param Expression variable: The loop variable.
      :param Expression collection: The collection to iterate over.
      :param Expression body: The statement evaluated on each iteration.
      :rtype: :class:`Expression`
   

      .. note::

         The loop variable is a local variable declared by an enclosing
         :meth:`Expression.block_with_variables`, or a parameter of an enclosing :meth:`Expression.lambda_`.
         A :meth:`Expression.break_` or :meth:`Expression.continue_` in the body applies to this loop.

   .. staticmethod:: get(arg, index)

      An element of a tuple, list or dictionary.

      :param Expression arg: The tuple, list or dictionary.
      :param Expression index: The index or key of the element.
      :returns: The element.
      :rtype: :class:`Expression`
   

      .. note::

         A tuple takes a constant integer index, a list a zero indexed integer and a
         dictionary a key. Throws an exception if the index is out of range or the key is
         missing, and for a tuple when the expression is created.

   .. staticmethod:: get_field(arg, name)

      The value of a field of a structure. Throws an exception if the value is not a
      structure, or has no field of the given name.

      :param Expression arg: The structure.
      :param str name: The name of the field to access.
      :returns: The value of the field.
      :rtype: :class:`Expression`
   

   .. staticmethod:: get_field_by_index(arg, index)

      The value of a field of a structure, named by its position. Throws an exception if
      the value is not a structure, or the index is out of range.

      :param Expression arg: The structure.
      :param int index: The position of the field to access.
      :returns: The value of the field.
      :rtype: :class:`Expression`
   

      .. note::

         The fields are numbered from 0, in the order the structure defines them.

   .. staticmethod:: greater_than(arg0, arg1)

      Greater than numerical comparison.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
      :rtype: :class:`Expression`
   

   .. staticmethod:: greater_than_or_equal(arg0, arg1)

      Greater than or equal numerical comparison.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
      :rtype: :class:`Expression`
   

   .. staticmethod:: group_by(arg, key)

      The values of a collection grouped by the key a function produces for each of them.

      :param Expression arg: The collection.
      :param Expression key: A function producing the key to group values by.
      :returns: A dictionary of each key to the values that produced it.
      :rtype: :class:`Expression`
   

   .. attribute:: has_return_type

      Whether the expression evaluates to a value.

      :Attribute: Read-only, cannot be set
      :rtype: bool
   

      .. note::

         An expression with no value has no :attr:`Expression.return_type`. An empty
         collection and no value both encode to an empty sequence of bytes.

   .. staticmethod:: if_then(condition, body)

      An if statement, with no value.

      :param Expression condition: The boolean condition.
      :param Expression body: The statement evaluated when the condition is true.
      :rtype: :class:`Expression`
   

      .. note::

         Use :meth:`Expression.conditional` for an if-then-else that produces a value.

   .. staticmethod:: if_then_else(condition, body, else_body)

      An if-else statement, with no value.

      :param Expression condition: The boolean condition.
      :param Expression body: The statement evaluated when the condition is true.
      :param Expression else_body: The statement evaluated when the condition is false.
      :rtype: :class:`Expression`
   

      .. note::

         Use :meth:`Expression.conditional` for an if-then-else that produces a value.

   .. staticmethod:: intersect(arg1, arg2)

      The values in both of two collections, with duplicates removed.

      :param Expression arg1: The first collection.
      :param Expression arg2: The second collection.
      :returns: The intersection of the two collections.
      :rtype: :class:`Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.to_list` makes it concrete.

   .. staticmethod:: invoke(function, args)

      A function call.

      :param Expression function: The function to call.
      :param dict args: The arguments to call the function with.
      :returns: A function call.
      :rtype: :class:`Expression`
   

   .. staticmethod:: is_null(value)

      Whether a value is null. Throws an exception if the type of the value cannot be null.

      :param Expression value: The value.
      :rtype: :class:`Expression`
   

   .. staticmethod:: lambda_(parameters, body)

      A function with the given parameters and body.

      :param list parameters: The parameters of the function.
      :param Expression body: The body of the function.
      :returns: A function.
      :rtype: :class:`Expression`
   

      .. note::

         The body is an expression or a block of statements, and a body with no value is
         evaluated for its effects. A :meth:`Expression.return_` or :meth:`Expression.return_nothing`
         statement ends the evaluation.

   .. staticmethod:: last(arg)

      The last value of a collection.

      :param Expression arg: The collection.
      :returns: The last value.
      :rtype: :class:`Expression`
   

      .. note::

         Throws an exception if the collection is empty.

   .. staticmethod:: left_shift(arg0, arg1)

      Bitwise left shift.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
      :rtype: :class:`Expression`
   

      .. note::

         Only the low 5 bits of the count are used for a 32-bit value, and the low 6 bits
         for a 64-bit value.

   .. staticmethod:: less_than(arg0, arg1)

      Less than numerical comparison.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
      :rtype: :class:`Expression`
   

   .. staticmethod:: less_than_or_equal(arg0, arg1)

      Less than or equal numerical comparison.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
      :rtype: :class:`Expression`
   

   .. staticmethod:: match(value, cases, bodies)

      A match statement, which evaluates the first matching case.

      :param Expression value: The value to match.
      :param list cases: The test values of each case.
      :param list bodies: The statement of each case.
      :rtype: :class:`Expression`
   

      .. note::

         Test values compare as for :meth:`Expression.equal`, and a case with none is the default.
         A :meth:`Expression.break_` in a body ends the enclosing loop.

   .. staticmethod:: max(arg)

      The largest value of a collection.

      :param Expression arg: The list or set.
      :returns: The largest value in the collection.
      :rtype: :class:`Expression`
   

      .. note::

         Throws an exception if the collection is empty and its values are not nullable.

   .. staticmethod:: max_by(arg, key)

      The value of a collection with the largest key.

      :param Expression arg: The collection.
      :param Expression key: A function producing the key to compare values by.
      :returns: The value with the largest key.
      :rtype: :class:`Expression`
   

      .. note::

         Throws an exception if the collection is empty.

   .. staticmethod:: min(arg)

      The smallest value of a collection.

      :param Expression arg: The list or set.
      :returns: The smallest value in the collection.
      :rtype: :class:`Expression`
   

      .. note::

         Throws an exception if the collection is empty and its values are not nullable.

   .. staticmethod:: min_by(arg, key)

      The value of a collection with the smallest key.

      :param Expression arg: The collection.
      :param Expression key: A function producing the key to compare values by.
      :returns: The value with the smallest key.
      :rtype: :class:`Expression`
   

      .. note::

         Throws an exception if the collection is empty.

   .. staticmethod:: modulo(arg0, arg1)

      Numerical modulo operator.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
      :returns: The remainder of arg0 divided by arg1
      :rtype: :class:`Expression`
   

   .. staticmethod:: multiply(arg0, arg1)

      Numerical multiplication.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
      :rtype: :class:`Expression`
   

   .. staticmethod:: negate(arg)

      Numerical negation, which negates a uint as a long.

      :param Expression arg: The operand.
      :rtype: :class:`Expression`
   

   .. staticmethod:: not_(arg)

      Logical or bitwise negation.

      :param Expression arg: The operand.
      :rtype: :class:`Expression`
   

      .. note::

         Also takes a value of a flags enumeration, and gives the declared values it does not hold.

   .. staticmethod:: not_equal(arg0, arg1)

      Inequality comparison.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
      :rtype: :class:`Expression`
   

      .. note::

         Collections, tuples, bytes and structures compare by their contents. Throws an
         exception if the operands are of unrelated types.

   .. staticmethod:: or_(arg0, arg1)

      Logical or bitwise or, which evaluates both operands.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
      :rtype: :class:`Expression`
   

      .. note::

         Also takes two values of a flags enumeration, and gives their combination.

   .. staticmethod:: order_by(arg, key)

      The values of a collection ordered by the key a function produces for each of them.

      :param Expression arg: The collection to order.
      :param Expression key: A function producing the key to order values by.
      :returns: The ordered collection.
      :rtype: :class:`Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.to_list` makes it concrete.

   .. staticmethod:: parameter(name, type)

      A named parameter of a function.

      :param str name: The name of the parameter.
      :param Type type: The type of the parameter.
      :returns: A named parameter.
      :rtype: :class:`Expression`
   

      .. note::

         A value is bound to it by name with :meth:`Expression.invoke`, or by its position in
         the parameters of a :meth:`Expression.lambda_`.

   .. attribute:: parameter_types

      The types of the function's parameters.

      :Attribute: Read-only, cannot be set
      :rtype: list(:class:`Type`)
   

      .. note::

         Empty for a function that is not created with :meth:`Expression.lambda_`.

   .. staticmethod:: power(arg0, arg1)

      Numerical power operator.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
      :returns: arg0 raised to the power of arg1, at the common type of the operands
      :rtype: :class:`Expression`
   

   .. staticmethod:: remove(collection, value)

      A removal of a value from a list or a set, or of an entry from a dictionary.

      :param Expression collection: The list, set or dictionary.
      :param Expression value: The value to remove, or the key of the entry to remove.
      :returns: Whether the collection held the value.
      :rtype: :class:`Expression`
   

      .. note::

         The first occurrence is removed from a list.

   .. staticmethod:: remove_at(list, index)

      A statement removing the element at an index of a list.

      :param Expression list: The list.
      :param Expression index: The index of the element, counting from zero.
      :rtype: :class:`Expression`
   

      .. note::

         Throws an exception if the index is out of range.

   .. staticmethod:: return_(value)

      A return statement, which ends the enclosing function with a value.

      :param Expression value: The value to return.
      :rtype: :class:`Expression`
   

   .. staticmethod:: return_nothing()

      A return statement, which ends the enclosing function with no value.

      :rtype: :class:`Expression`
   

   .. attribute:: return_type

      The type of the value the expression evaluates to. Throws an exception if the
      value cannot be sent to a client.

      :Attribute: Read-only, cannot be set
      :rtype: :class:`Type`
   

      .. note::

         A function created with :meth:`Expression.lambda_` evaluates to the value it returns. A
         lazily evaluated collection, such as from :meth:`Expression.select`, is sent once
         converted by :meth:`Expression.to_list` or :meth:`Expression.to_set`.

   .. staticmethod:: reverse(arg)

      The values of a collection in the opposite order.

      :param Expression arg: The collection.
      :returns: The reversed collection.
      :rtype: :class:`Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.to_list` makes it concrete.

   .. staticmethod:: right_shift(arg0, arg1)

      Bitwise right shift.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
      :rtype: :class:`Expression`
   

      .. note::

         Only the low 5 bits of the count are used for a 32-bit value, and the low 6 bits
         for a 64-bit value.

   .. staticmethod:: select(arg, func)

      The result of a function for each value of a collection.

      :param Expression arg: The list or set.
      :param Expression func: The function, taking an element of the collection.
      :returns: The collection of function results.
      :rtype: :class:`Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.to_list` makes it concrete.

   .. staticmethod:: select_many(arg, func)

      The values of the collections a function returns for each value of a collection,
      flattened into one.

      :param Expression arg: The list or set.
      :param Expression func: The function, taking an element of the collection and returning a collection.
      :returns: The flattened collection of function results.
      :rtype: :class:`Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.to_list` makes it concrete.

   .. staticmethod:: set(collection, index, value)

      A statement setting an element of a list, or an entry of a dictionary.

      :param Expression collection: The list or dictionary.
      :param Expression index: The index of the element, counting from zero, or the key of the entry.
      :param Expression value: The value to set.
      :rtype: :class:`Expression`
   

      .. note::

         A key the dictionary does not hold is added. Throws an exception if the index
         is out of range for the list.

   .. staticmethod:: skip(arg, count)

      A collection without its first count values.

      :param Expression arg: The collection.
      :param Expression count: The number of values to skip.
      :returns: The collection without its first count values.
      :rtype: :class:`Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.to_list` makes it concrete.

   .. staticmethod:: state_variable(name, initial_value)

      A variable that keeps its value between evaluations of a function.

      :param str name: The name of the variable.
      :param Expression initial_value: The initial value of the variable.
      :returns: A state variable.
      :rtype: :class:`Expression`
   

      .. note::

         The initial value is evaluated once, when the variable is created, and gives the
         variable its type. The value belongs to the variable, so every function that
         contains it shares it.

   .. staticmethod:: string_concat(args)

      The concatenation of strings.

      :param list args: The strings to concatenate, in order.
      :returns: The concatenated string.
      :rtype: :class:`Expression`
   

      .. note::

         Use :meth:`Expression.convert_to_string` to convert other values to strings.

   .. staticmethod:: string_contains(arg, value)

      Whether one string occurs within another.

      :param Expression arg: The string to search.
      :param Expression value: The string to search for.
      :returns: Whether the value occurs in the string.
      :rtype: :class:`Expression`
   

   .. staticmethod:: string_ends_with(arg, value)

      Whether a string ends with another.

      :param Expression arg: The string to test.
      :param Expression value: The string to test for.
      :returns: Whether the string ends with the value.
      :rtype: :class:`Expression`
   

   .. staticmethod:: string_get(arg, index)

      The character at the given position in a string, as a string of length one.

      :param Expression arg: The string.
      :param Expression index: The position of the character, counting from zero.
      :returns: The character at the given position.
      :rtype: :class:`Expression`
   

      .. note::

         Throws an exception if the index is out of range.

   .. staticmethod:: string_index_of(arg, value)

      The position of the first occurrence of one string within another.

      :param Expression arg: The string to search.
      :param Expression value: The string to search for.
      :returns: The position of the value, counting from zero, or -1 if it does not occur.
      :rtype: :class:`Expression`
   

   .. staticmethod:: string_join(separator, values)

      Strings joined together, separated by another string.

      :param Expression separator: The string to put between the values.
      :param Expression values: The strings to join.
      :returns: The joined string.
      :rtype: :class:`Expression`
   

   .. staticmethod:: string_length(arg)

      The number of characters in a string.

      :param Expression arg: The string.
      :returns: The length of the string.
      :rtype: :class:`Expression`
   

   .. staticmethod:: string_replace(arg, old_value, new_value)

      A string with every occurrence of one string replaced by another.

      :param Expression arg: The string to search.
      :param Expression old_value: The string to replace.
      :param Expression new_value: The string to replace it with.
      :returns: The string with the replacements made.
      :rtype: :class:`Expression`
   

      .. note::

         Throws an exception if the string to replace is empty.

   .. staticmethod:: string_split(arg, separator)

      The parts of a string separated by another string.

      :param Expression arg: The string to split.
      :param Expression separator: The string that separates the parts.
      :returns: The parts of the string.
      :rtype: :class:`Expression`
   

   .. staticmethod:: string_starts_with(arg, value)

      Whether a string starts with another.

      :param Expression arg: The string to test.
      :param Expression value: The string to test for.
      :returns: Whether the string starts with the value.
      :rtype: :class:`Expression`
   

   .. staticmethod:: string_substring(arg, start, length)

      Part of a string.

      :param Expression arg: The string.
      :param Expression start: The position to start at, counting from zero.
      :param Expression length: The number of characters to take.
      :returns: The substring.
      :rtype: :class:`Expression`
   

      .. note::

         Throws an exception if the start or the length is out of range.

   .. staticmethod:: string_to_lower(arg)

      A string in lower case, independently of the game's language.

      :param Expression arg: The string.
      :returns: The string in lower case.
      :rtype: :class:`Expression`
   

   .. staticmethod:: string_to_upper(arg)

      A string in upper case, independently of the game's language.

      :param Expression arg: The string.
      :returns: The string in upper case.
      :rtype: :class:`Expression`
   

   .. staticmethod:: string_trim(arg)

      A string with white space removed from both ends.

      :param Expression arg: The string.
      :returns: The trimmed string.
      :rtype: :class:`Expression`
   

   .. staticmethod:: string_trim_end(arg)

      A string with white space removed from its end.

      :param Expression arg: The string.
      :returns: The trimmed string.
      :rtype: :class:`Expression`
   

   .. staticmethod:: string_trim_start(arg)

      A string with white space removed from its start.

      :param Expression arg: The string.
      :returns: The trimmed string.
      :rtype: :class:`Expression`
   

   .. staticmethod:: subtract(arg0, arg1)

      Numerical subtraction.

      :param Expression arg0: The first operand.
      :param Expression arg1: The second operand.
      :rtype: :class:`Expression`
   

   .. staticmethod:: sum(arg)

      The sum of the values of a collection.

      :param Expression arg: The list or set.
      :returns: The sum of the elements in the collection.
      :rtype: :class:`Expression`
   

   .. staticmethod:: switch(value, cases, bodies)

      A switch statement, which evaluates from the first matching case on.

      :param Expression value: The value to match.
      :param list cases: The test values of each case.
      :param list bodies: The statement of each case.
      :rtype: :class:`Expression`
   

      .. note::

         Test values compare as for :meth:`Expression.equal`, and a case with none is the default.
         A :meth:`Expression.break_` in a body ends the switch.

   .. staticmethod:: take(arg, count)

      The first count values of a collection.

      :param Expression arg: The collection.
      :param Expression count: The number of values to take.
      :returns: The first count values of the collection.
      :rtype: :class:`Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.to_list` makes it concrete.

   .. staticmethod:: throw(service, name, message)

      A throw statement, which raises the named exception.

      :param str service: The name of the service the exception is defined in.
      :param str name: The name of the exception.
      :param Expression message: The message of the exception.
      :rtype: :class:`Expression`
   

      .. note::

         The client receives the exception as an error from the function, or from the stream
         or event evaluating it. For example, the service ``KRPC`` and the name
         ``Error`` raise :class:`Error`. Throws an exception if the service does not
         declare it.

   .. staticmethod:: throw_by_id(service_id, exception_id, message)

      A throw statement, which raises the exception with the given ids.

      :param int service_id: The id of the service the exception is defined in.
      :param int exception_id: The id of the exception.
      :param Expression message: The message of the exception.
      :rtype: :class:`Expression`
   

      .. note::

         Raises the exception as :meth:`Expression.throw` does. Throws an exception if the
         service does not declare it.

   .. staticmethod:: to_list(arg)

      A collection as a list.

      :param Expression arg: The collection.
      :returns: The collection as a list.
      :rtype: :class:`Expression`
   

   .. staticmethod:: to_set(arg)

      A collection as a set.

      :param Expression arg: The collection.
      :returns: The collection as a set.
      :rtype: :class:`Expression`
   

      .. note::

         Throws an exception if a value is null.

   .. staticmethod:: try_catch(body, service, name, message, handler)

      A try-catch statement that handles the named exception.

      :param Expression body: The statement to evaluate.
      :param str service: The name of the service the exception is defined in.
      :param str name: The name of the exception.
      :param Expression or None message: A string variable assigned the exception's message, or null.
      :param Expression handler: The statement evaluated when the exception is caught.
      :rtype: :class:`Expression`
   

      .. note::

         Only an exception a service declares can be named, and :meth:`Expression.try_catch_all`
         handles any exception. Throws an exception if the service does not declare it.

   .. staticmethod:: try_catch_all(body, message, handler)

      A try-catch statement that handles any exception.

      :param Expression body: The statement to evaluate.
      :param Expression or None message: A string variable assigned the exception's message, or null.
      :param Expression handler: The statement evaluated when an exception is caught.
      :rtype: :class:`Expression`
   

   .. staticmethod:: try_catch_by_id(body, service_id, exception_id, message, handler)

      A try-catch statement that handles the exception with the given ids.

      :param Expression body: The statement to evaluate.
      :param int service_id: The id of the service the exception is defined in.
      :param int exception_id: The id of the exception.
      :param Expression or None message: A string variable assigned the exception's message, or null.
      :param Expression handler: The statement evaluated when the exception is caught.
      :rtype: :class:`Expression`
   

      .. note::

         Handles the exception as :meth:`Expression.try_catch` does. Throws an exception if the
         service does not declare it.

   .. staticmethod:: try_finally(body, finalizer)

      A try-finally statement.

      :param Expression body: The statement to evaluate.
      :param Expression finalizer: The statement evaluated after the body.
      :rtype: :class:`Expression`
   

      .. note::

         The finalizer is evaluated after the body, whether or not the body raises an
         exception. A break, continue or return cannot leave the finalizer.

   .. staticmethod:: union(arg1, arg2)

      The values in either of two collections, with duplicates removed.

      :param Expression arg1: The first collection.
      :param Expression arg2: The second collection.
      :returns: The union of the two collections.
      :rtype: :class:`Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.to_list` makes it concrete.

   .. staticmethod:: variable(name, type)

      A local variable, declared by :meth:`Expression.block_with_variables`.

      :param str name: The name of the variable.
      :param Type type: The type of the variable.
      :returns: A local variable.
      :rtype: :class:`Expression`
   

   .. staticmethod:: where(arg, func)

      The values of a collection for which a predicate function returns true.

      :param Expression arg: The list or set.
      :param Expression func: The predicate function, taking an element of the collection.
      :returns: The filtered collection.
      :rtype: :class:`Expression`
   

      .. note::

         The result is lazily evaluated. :meth:`Expression.to_list` makes it concrete.

   .. staticmethod:: while_(condition, body)

      A while loop, which evaluates the body while the condition is true.

      :param Expression condition: The boolean condition.
      :param Expression body: The statement evaluated on each iteration.
      :rtype: :class:`Expression`
   

      .. note::

         A :meth:`Expression.break_` or :meth:`Expression.continue_` in the body applies to this loop.

   .. staticmethod:: zip(arg1, arg2, func)

      The results of a function applied to the values of two collections in pairs.

      :param Expression arg1: The first collection.
      :param Expression arg2: The second collection.
      :param Expression func: A function combining a value from each collection.
      :returns: The combined collection.
      :rtype: :class:`Expression`
   

      .. note::

         The result is as long as the shorter of the two collections. It is lazily
         evaluated, and :meth:`Expression.to_list` makes it concrete.



.. class:: Type

   The type of a value a procedure takes or returns.

   .. staticmethod:: bool()

      A boolean type.

      :rtype: :class:`Type`
   

   .. staticmethod:: bytes()

      A byte sequence type.

      :rtype: :class:`Type`
   

   .. staticmethod:: class_type(service, name)

      The type of a class defined by a service.

      :param str service: The name of the service the class is defined in.
      :param str name: The name of the class.
      :rtype: :class:`Type`
   

   .. staticmethod:: class_type_by_id(service_id, class_id)

      The type of a class defined by a service, named by ids.

      :param int service_id: The id of the service the class is defined in.
      :param int class_id: The id of the class.
      :rtype: :class:`Type`
   

   .. attribute:: code

      The kind of the type.

      :Attribute: Read-only, cannot be set
      :rtype: :class:`TypeCode`
   

   .. staticmethod:: dictionary_type(key_type, value_type)

      A dictionary type. Throws an exception if the key type is nullable or not a valid
      key type.

      :param Type key_type: The type of the keys of the dictionary.
      :param Type value_type: The type of the values of the dictionary.
      :rtype: :class:`Type`
   

   .. staticmethod:: double()

      A double precision floating point number type.

      :rtype: :class:`Type`
   

   .. staticmethod:: enumeration_type(service, name)

      The type of an enumeration defined by a service.

      :param str service: The name of the service the enumeration is defined in.
      :param str name: The name of the enumeration.
      :rtype: :class:`Type`
   

   .. staticmethod:: enumeration_type_by_id(service_id, enumeration_id)

      The type of an enumeration defined by a service, named by ids.

      :param int service_id: The id of the service the enumeration is defined in.
      :param int enumeration_id: The id of the enumeration.
      :rtype: :class:`Type`
   

   .. staticmethod:: float()

      A single precision floating point number type.

      :rtype: :class:`Type`
   

   .. staticmethod:: int()

      A signed 32-bit integer type.

      :rtype: :class:`Type`
   

   .. staticmethod:: list_type(value_type)

      A list type.

      :param Type value_type: The type of the elements of the list.
      :rtype: :class:`Type`
   

   .. staticmethod:: long()

      A signed 64-bit integer type.

      :rtype: :class:`Type`
   

   .. attribute:: name

      The name of a class, enumeration or structure type, or an empty string.

      :Attribute: Read-only, cannot be set
      :rtype: str
   

   .. attribute:: nullable

      Whether a value of the type may be null.

      :Attribute: Read-only, cannot be set
      :rtype: bool
   

      .. note::

         Any type can be nullable. A string or class value of a type that is not nullable
         is never null.

   .. staticmethod:: nullable_type(value_type)

      A nullable type, holding a value of the given type or null.

      :param Type value_type: The type of the value.
      :rtype: :class:`Type`
   

   .. attribute:: service

      The service a class, enumeration or structure type is defined in, or an empty string.

      :Attribute: Read-only, cannot be set
      :rtype: str
   

   .. staticmethod:: set_type(value_type)

      A set type. Throws an exception if the element type is nullable.

      :param Type value_type: The type of the elements of the set.
      :rtype: :class:`Type`
   

   .. staticmethod:: string()

      A string type.

      :rtype: :class:`Type`
   

   .. staticmethod:: struct_type(service, name)

      The type of a structure defined by a service.

      :param str service: The name of the service the structure is defined in.
      :param str name: The name of the structure.
      :rtype: :class:`Type`
   

   .. staticmethod:: struct_type_by_id(service_id, struct_id)

      The type of a structure defined by a service, named by ids.

      :param int service_id: The id of the service the structure is defined in.
      :param int struct_id: The id of the structure.
      :rtype: :class:`Type`
   

   .. staticmethod:: tuple_type(value_types)

      A tuple type. Throws an exception if there are no element types or more than seven.

      :param list value_types: The types of the elements of the tuple.
      :rtype: :class:`Type`
   

   .. attribute:: types

      The types of the elements of a tuple, list, set or dictionary type.

      :Attribute: Read-only, cannot be set
      :rtype: list(:class:`Type`)
   

      .. note::

         The value types of a tuple, the value type of a list or set, or the key and value
         types of a dictionary. An empty list for any other type.

   .. staticmethod:: uint()

      An unsigned 32-bit integer type.

      :rtype: :class:`Type`
   

   .. staticmethod:: ulong()

      An unsigned 64-bit integer type.

      :rtype: :class:`Type`
   



.. class:: GameScene

   The game scene. See :attr:`game_scene`.


   .. data:: administration

      The administration facility. This is a pseudo-scene, shown when the
      administration facility is open within the space center scene.


   .. data:: astronaut_complex

      The astronaut complex. This is a pseudo-scene, shown when the
      astronaut complex facility is open within the space center scene.


   .. data:: editor_sph

      The Space Plane Hangar.


   .. data:: editor_vab

      The Vehicle Assembly Building.


   .. data:: flight

      The game scene showing a vessel in flight (or on the launchpad/runway).


   .. data:: mission_builder

      The mission builder.


   .. data:: mission_control

      Mission control. This is a pseudo-scene, shown when the
      mission control facility is open within the space center scene.


   .. data:: research_and_development

      Research and development. This is a pseudo-scene, shown when the
      research and development facility is open within the space center scene.


   .. data:: space_center

      The game scene showing the Kerbal Space Center buildings.


   .. data:: tracking_station

      The tracking station.



.. class:: TypeCode

   The kind of a type, with the values of the communication protocol's type codes.


   .. data:: bool

      A boolean.


   .. data:: bytes

      A sequence of bytes.


   .. data:: class

      A class defined by a service.


   .. data:: dictionary

      A dictionary, with the types of its keys and values in :attr:`Type.types`.


   .. data:: double

      A double precision floating point number.


   .. data:: enumeration

      An enumeration defined by a service.


   .. data:: float

      A single precision floating point number.


   .. data:: list

      A list, with the type of its values in :attr:`Type.types`.


   .. data:: sint32

      A signed 32-bit integer.


   .. data:: sint64

      A signed 64-bit integer.


   .. data:: set

      A set, with the type of its values in :attr:`Type.types`.


   .. data:: string

      A string.


   .. data:: struct

      A structure defined by a service.


   .. data:: tuple

      A tuple, with the types of its values in :attr:`Type.types`.


   .. data:: uint32

      An unsigned 32-bit integer.


   .. data:: uint64

      An unsigned 64-bit integer.



.. class:: ClientInfo(id, name, address, protocol_version)

   A client connected to the server.

   .. note::

      See :attr:`clients`.
   .. attribute:: id

      The identifier of the client.

      :rtype: bytes

   .. attribute:: name

      The name of the client, or an empty string if it has no name.

      :rtype: str

   .. attribute:: address

      The address of the client.

      :rtype: str

   .. attribute:: protocol_version

      The protocol version of the client's connection.

      :rtype: int


.. class:: ServerSettings(one_rpc_per_update, max_time_per_update, adaptive_rate_control, blocking_recv, recv_timeout, tick_hold_timeout)

   The settings that control how the server executes calls.

   .. note::

      See :attr:`ServerStatus.settings`.
   .. attribute:: one_rpc_per_update

      Whether the server executes one call per update.

      :rtype: bool

   .. attribute:: max_time_per_update

      The current maximum time spent executing calls per update, in microseconds.

      :rtype: int

   .. attribute:: adaptive_rate_control

      Whether the server adjusts :attr:`ServerSettings.max_time_per_update` to hold the frame rate.

      :rtype: bool

   .. attribute:: blocking_recv

      Whether the server waits for the next call in an update.

      :rtype: bool

   .. attribute:: recv_timeout

      The time the server waits for the next call, in microseconds.

      :rtype: int

   .. attribute:: tick_hold_timeout

      The longest time a client may hold the game on one physics tick, in microseconds.

      :rtype: int


.. class:: ServerStatistics(bytes_read, bytes_written, bytes_read_rate, bytes_written_rate, rpcs_executed, rpc_rate, time_per_rpc_update, poll_time_per_rpc_update, exec_time_per_rpc_update, deferred_calls, streams, stream_evaluations, stream_evaluation_rate, stream_updates_sent, time_per_stream_update)

   The performance statistics of the server.

   .. note::

      See :attr:`ServerStatus.statistics`.
   .. attribute:: bytes_read

      The number of bytes received from clients.

      :rtype: int

   .. attribute:: bytes_written

      The number of bytes sent to clients.

      :rtype: int

   .. attribute:: bytes_read_rate

      The rate bytes are received from clients, in bytes per second.

      :rtype: float

   .. attribute:: bytes_written_rate

      The rate bytes are sent to clients, in bytes per second.

      :rtype: float

   .. attribute:: rpcs_executed

      The number of calls executed.

      :rtype: int

   .. attribute:: rpc_rate

      The number of calls executed per second.

      :rtype: float

   .. attribute:: time_per_rpc_update

      The time spent receiving and executing calls per update, in seconds.

      :rtype: float

   .. attribute:: poll_time_per_rpc_update

      The time spent receiving calls per update, in seconds.

      :rtype: float

   .. attribute:: exec_time_per_rpc_update

      The time spent executing calls per update, in seconds.

      :rtype: float

   .. attribute:: deferred_calls

      The number of calls left for the next update, because the update ran out of time
      or the call yielded.

      :rtype: int

   .. attribute:: streams

      The number of open streams, events included.

      :rtype: int

   .. attribute:: stream_evaluations

      The number of stream evaluations, one per stream per update.

      :rtype: int

   .. attribute:: stream_evaluation_rate

      The number of stream evaluations per second.

      :rtype: float

   .. attribute:: stream_updates_sent

      The number of stream updates sent to clients.

      :rtype: int

   .. attribute:: time_per_stream_update

      The time spent updating streams per update, in seconds.

      :rtype: float


.. class:: ServerStatus(version, protocol_version, settings, statistics)

   The status of the server.

   .. note::

      See :meth:`get_status`.
   .. attribute:: version

      The version of the server, or ``unknown``.

      :rtype: str

   .. attribute:: protocol_version

      The protocol version of the connection.

      :rtype: int

   .. attribute:: settings

      The settings of the server.

      :rtype: :class:`ServerSettings`

   .. attribute:: statistics

      The performance statistics of the server.

      :rtype: :class:`ServerStatistics`


.. class:: ArgumentException

   A method was invoked where at least one of the passed arguments does not
   meet the parameter specification of the method.

   A subclass of the builtin ``ValueError`` alone.



.. class:: ArgumentNullException

   A null reference was passed to a method that does not accept it as a valid argument.

   A subclass of the builtin ``ValueError`` alone.



.. class:: ArgumentOutOfRangeException

   The value of an argument is outside the allowable range of values as defined by the invoked method.

   A subclass of the builtin ``ValueError`` alone.



.. class:: DivideByZeroException

   An integer was divided by zero.

   A subclass of the builtin ``ZeroDivisionError`` alone.



.. class:: Error

   A general error, described by its message.

   A subclass of :class:`krpc.error.ServerError`.



.. class:: IndexOutOfRangeException

   An index was outside the bounds of a collection, string or array.

   A subclass of the builtin ``IndexError`` alone.



.. class:: InvalidOperationException

   A method call was made to a method that is invalid
   given the current state of the object.

   A subclass of the builtin ``RuntimeError`` alone.



.. class:: KeyNotFoundException

   A key was not found in a dictionary.

   A subclass of the builtin ``KeyError`` alone.



.. class:: NotSupportedException

   A method was invoked that is not supported.

   A subclass of the builtin ``NotImplementedError`` alone.



.. class:: NullReferenceException

   A value was null where its type does not allow one.

   A subclass of the builtin ``TypeError`` alone.



.. class:: ObjectDestroyedException

   The game object that a service object refers to no longer exists,
   so the object cannot be used. For example, the part it represents
   was destroyed, or the vessel it belongs to was recovered.

   A subclass of :class:`krpc.error.ServerError`.

   .. note::

      See :class:`ObjectNotLoadedException` for a game object that exists but is not
      loaded.

.. class:: ObjectNotLoadedException

   The game object that a service object refers to exists, but is not loaded.
   For example, the game is loading, or the vessel carrying the part is unloaded.

   A subclass of :class:`krpc.error.ServerError`.

   .. note::

      See :class:`ObjectDestroyedException` for a game object that no longer exists.

.. class:: OverflowException

   An arithmetic or conversion operation overflowed.

   A subclass of the builtin ``OverflowError`` alone.



.. class:: StreamRemovedException

   A stream was removed by the server, e.g. an event that has finished.

   An alias of :class:`krpc.error.StreamError`.



.. class:: TimeoutException

   An operation did not complete in the time allotted to it.

   A subclass of the builtin ``TimeoutError`` alone.



.. class:: UnsupportedSceneException

   The procedure is not available in the current game scene.

   A subclass of :class:`krpc.error.ServerError`.
