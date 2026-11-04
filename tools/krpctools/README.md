# krpctools

Development tools for [kRPC](https://github.com/krpc/krpc), a Remote Procedure Call server for
Kerbal Space Program. Installs the command line tools for working with kRPC services:

 * `krpc-clientgen` - generate client stub code for a service
 * `krpc-docgen` - generate service documentation
 * `krpc-servicedefs` - generate service definition JSON files
 * `krpc-conformance` - run the conformance tests against a client library
 * `krpc-testserver` - run a kRPC server without the game, for testing clients. Pass `--help`
   for its options.

`krpc-servicedefs`, `krpc-testserver` and `krpc-conformance` run on Linux and Windows, and need
the [.NET 10 runtime](https://dotnet.microsoft.com/download/dotnet/10.0).

## Documentation

 * [kRPC documentation](https://krpc.github.io/krpc)
 * [Source and issue tracker](https://github.com/krpc/krpc)
