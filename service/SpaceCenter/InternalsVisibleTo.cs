using System.Runtime.CompilerServices;

// For the Visual Studio solution and the dotnet build. The Bazel build injects the
// attribute through rules_dotnet, so this file sits outside the src/ directory that
// Bazel globs.
[assembly: InternalsVisibleTo("TestingTools")]
