## client/python

- Add `Connection.get_call_template`, a call that names a procedure through its class and
  carries no arguments, for `Expression.call_with_arguments` (#1352)

## client/lua

- Add `Connection:get_call_template`, a call that names a procedure through its class and
  carries no arguments, for `Expression.call_with_arguments` (#1352)

## client/csharp

- Add `Connection.GetCallTemplate` and `Connection.GetSetCallTemplate`, a call that names a
  procedure through its class and carries no arguments, for `Expression.CallWithArguments` (#1352)

## client/java

- Add `Connection.getCallTemplate`, a call that names a procedure through its class and carries
  no arguments, for `Expression.callWithArguments` (#1352)

## client/cpp

- Add a static `_call_template` function for each member, a call that names the procedure and
  carries no arguments, for `Expression::call_with_arguments` (#1352)

## client/cnano

- Add a `_call_template` function for each member, a call that names the procedure and carries no
  arguments, for `krpc_KRPC_Expression_CallWithArguments` (#1352)
