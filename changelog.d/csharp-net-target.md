## client/csharp

- Target .NET Standard 2.0 in place of .NET Framework 4.7.2, so a .NET 5 or later project uses the
  package without a compatibility warning (#1274)
- Fix .NET Framework programs failing to load `System.Memory` when decoding a nullable value in a
  collection or structure (#1274)
