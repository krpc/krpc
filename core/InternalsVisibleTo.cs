using System.Runtime.CompilerServices;

// For the Visual Studio solution and the dotnet build. The Bazel build injects the
// attribute through rules_dotnet, so this file sits outside the src/ directory that
// Bazel globs.
[assembly: InternalsVisibleTo("KRPC.Core.Test")]
[assembly: InternalsVisibleTo("TestingTools")]
// TestServer exposes the expression tree printer as a test-only RPC. Its source is
// built into three assemblies: the server, its debug flavor and the service on its
// own, which the definitions are generated from
[assembly: InternalsVisibleTo("TestServer")]
[assembly: InternalsVisibleTo("TestServer.Debug")]
[assembly: InternalsVisibleTo("TestService")]
// The Benchmark service runs a call through Services, the server's own dispatch path
[assembly: InternalsVisibleTo("KRPC.Benchmark")]
