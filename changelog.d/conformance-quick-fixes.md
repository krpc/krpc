## client/csharp

- Order remote objects by id, with `CompareTo` and the comparison operators (#1377)
- Throw `EncodingException` for an enumeration result the enumeration does not declare (#1377)
- Throw `ArgumentNullException` for a null passed for a parameter that cannot be null, before the
  call is sent (#1377)

## client/cpp

- Throw `std::invalid_argument` for a default-constructed object passed as an argument or called
  through, before the call is sent (#1377)

## client/java

- Fix remote objects with an id past 63 bits ordering first (#1377)
- Throw `EncodingException` for an enumeration result the enumeration does not declare, in place
  of returning null (#1377)
- Throw `IllegalArgumentException` for a null passed for a parameter that cannot be null, before
  the call is sent (#1377)

## client/lua

- Order remote objects by id with `<` (#1377)
- Fix a returned enumeration value comparing unequal to the same named value (#1377)
- Raise an error for a handshake timeout that is not positive (#1377)

## client/python

- Raise `StreamError` for a stream of a procedure that returns an event (#1377)
- Raise an error for a number outside the range of its parameter's type, in place of sending a
  wrong value (#1377)
- Raise `TypeError` for `None` passed for a parameter that cannot be null, before the
  call is sent (#1377)
- Raise an error for a string, bytes or a dictionary passed for a list, a tuple or a
  structure (#1377)
