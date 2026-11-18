## client/cpp

- Throw `std::invalid_argument` for a default-constructed object passed as an argument or called
  through, before the call is sent

## client/csharp

- Order remote objects by id, with `CompareTo` and the comparison operators
- Throw `EncodingException` for an enumeration result the enumeration does not declare

## client/java

- Fix remote objects with an id past 63 bits ordering first
- Throw `EncodingException` for an enumeration result the enumeration does not declare, in place
  of returning null

## client/lua

- Order remote objects by id with `<`
- Fix a returned enumeration value comparing unequal to the same named value
- Raise an error for a handshake timeout that is not positive
