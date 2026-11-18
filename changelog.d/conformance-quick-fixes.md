## client/cpp

- Throw `std::invalid_argument` for a default-constructed object passed as an argument or called
  through, before the call is sent

## client/python

- Raise `StreamError` for a stream of a procedure that returns an event
- Raise an error for a number outside the range of its parameter's type, in place of sending a
  wrong value
- Raise `TypeError` for `None` passed for a parameter that cannot be null, before the call is sent
- Raise an error for a string, bytes or a dictionary passed for a list

## client/csharp

- Order remote objects by id, with `CompareTo` and the comparison operators
- Throw `EncodingException` for an enumeration result the enumeration does not declare
- Throw `ArgumentNullException` for a null passed for a parameter that cannot be null, before the
  call is sent

## client/java

- Fix remote objects with an id past 63 bits ordering first
- Throw `EncodingException` for an enumeration result the enumeration does not declare, in place
  of returning null

## client/lua

- Order remote objects by id with `<`
- Fix a returned enumeration value comparing unequal to the same named value
- Raise an error for a handshake timeout that is not positive
