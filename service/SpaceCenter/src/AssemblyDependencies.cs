// Declares a hard dependency on the KSPCommunityPartModules mod, which provides the ModuleNameTag
// part module that Part.Tag reads and writes. KSP refuses to load this assembly unless
// KSPCommunityPartModules 0.5 or later is installed.
[assembly: KSPAssemblyDependency("KSPCommunityPartModules", 0, 5)]
