using System.Reflection;

// Old projects use Custom.After.Microsoft.Common.targets which generates AdditionalAssemblyInfo.cs
// and defines HAVE_ASSEMBLY_VERSION. SDK-style projects don't, so they rely on these attributes.
#if !HAVE_ASSEMBLY_VERSION
[assembly: AssemblyVersion("4.0.0.0")]
[assembly: AssemblyFileVersion("4.0.0.0")]
#endif