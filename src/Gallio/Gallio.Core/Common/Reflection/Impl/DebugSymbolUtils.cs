// Copyright 2005-2010 Gallio Project - http://www.gallio.org/
// Licensed under the Apache License, Version 2.0 (the "License").

using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Gallio.Common.Reflection.Impl
{
    /// <summary>
    /// Helpers for working with <see cref="IDebugSymbolResolver" />.
    /// .NET 8 replacement: CciDebugSymbolResolver not available; returns a NullDebugSymbolResolver.
    /// </summary>
    public static class DebugSymbolUtils
    {
        private static IDebugSymbolResolver resolver;

        /// <summary>
        /// Gets the singleton debug symbol resolver used by these utilities.
        /// </summary>
        public static IDebugSymbolResolver Resolver
        {
            get
            {
                if (resolver == null)
                    Interlocked.CompareExchange(ref resolver, CreateResolver(), null);
                return resolver;
            }
        }

        /// <summary>
        /// Creates a new debug symbol resolver appropriate for this platform.
        /// On .NET 8 this returns a NullDebugSymbolResolver (CCI libraries not available).
        /// </summary>
        public static IDebugSymbolResolver CreateResolver()
        {
            return new NullDebugSymbolResolver();
        }

        /// <summary>
        /// Gets the location of a source file that contains the declaration of a type, or
        /// unknown if not available.
        /// </summary>
        public static CodeLocation GetSourceLocation(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            CodeLocation location = GuessSourceLocationForType(type,
                BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
            if (location == CodeLocation.Unknown)
                location = GuessSourceLocationForType(type,
                    BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
            return location;
        }

        private static CodeLocation GuessSourceLocationForType(Type type, BindingFlags bindingFlags)
        {
            CodeLocation location = GuessSourceLocationForTypeFromItsMethods(type.GetConstructors(bindingFlags));
            if (location == CodeLocation.Unknown)
                location = GuessSourceLocationForTypeFromItsMethods(type.GetMethods(bindingFlags));
            return location;
        }

        private static CodeLocation GuessSourceLocationForTypeFromItsMethods(
            System.Collections.Generic.IEnumerable<MethodBase> methods)
        {
            foreach (MethodBase method in methods)
            {
                CodeLocation codeLocation = GetSourceLocation(method);
                if (codeLocation != CodeLocation.Unknown)
                    return new CodeLocation(codeLocation.Path, 0, 0);
            }
            return CodeLocation.Unknown;
        }

        /// <summary>
        /// Gets the location of a source file that contains the declaration of a method, or
        /// unknown if not available.
        /// </summary>
        public static CodeLocation GetSourceLocation(MethodBase method)
        {
            if (method == null)
                throw new ArgumentNullException("method");

            if (method.DeclaringType == null)
                return CodeLocation.Unknown;

            Assembly assembly     = method.DeclaringType.Assembly;
            string assemblyPath   = AssemblyUtils.GetAssemblyLocation(assembly);
            if (assemblyPath == null)
                return CodeLocation.Unknown;

            return Resolver.GetSourceLocationForMethod(assemblyPath, method.MetadataToken);
        }

        /// <summary>
        /// A no-op resolver used when CCI libraries are not available (.NET 8+).
        /// </summary>
        private sealed class NullDebugSymbolResolver : IDebugSymbolResolver
        {
            public CodeLocation GetSourceLocationForMethod(string assemblyPath, int methodMetadataToken)
            {
                return CodeLocation.Unknown;
            }

            public void Dispose() { }
        }
    }
}
