// Copyright 2005-2010 Gallio Project - http://www.gallio.org/
// Licensed under the Apache License, Version 2.0 (the "License").

// .NET 8 replacement for ReflectorResolveUtils.cs:
// - UnresolvedCodeElementFactory.Instance.Wrap(x) calls replaced with null.
// - Reflector.IsUnresolved() guards replaced with explicit null checks.
// - IUnresolvedCodeElement guards replaced with null checks.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Gallio.Common.Collections;
using Gallio.Common.Reflection;

namespace Gallio.Common.Reflection.Impl
{
    /// <summary>
    /// Provides helpers for resolving abstract reflection objects to obtain
    /// native ones based on the structural properties of the reflected code elements.
    /// </summary>
    public class ReflectorResolveUtils
    {
        private static readonly LazyCache<string, Assembly> resolvedAssemblyCache
            = new LazyCache<string, Assembly>(PopulateResolvedAssembly);
        private static readonly LazyCache<string, Assembly> resolvedAssemblyCacheWithFallbackOnPartialName
            = new LazyCache<string, Assembly>(PopulateResolvedAssemblyWithFallbackOnPartialName);

        /// <summary>Resolves a reflected assembly to its native <see cref="Assembly" /> object.</summary>
        public static Assembly ResolveAssembly(IAssemblyInfo assembly, bool fallbackOnPartialName, bool throwOnError)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            string fullName = assembly.FullName;
            Assembly resolvedAssembly = fallbackOnPartialName
                ? resolvedAssemblyCacheWithFallbackOnPartialName[fullName]
                : resolvedAssemblyCache[fullName];

            if (throwOnError && resolvedAssembly == null)
                throw new ReflectionResolveException(assembly);

            return resolvedAssembly; // null when not found; callers guard with null-check
        }

        private static Assembly PopulateResolvedAssembly(string fullName)
        {
            try   { return Assembly.Load(fullName); }
            catch (FileNotFoundException)   { }
            catch (BadImageFormatException) { }
            return null;
        }

        private static Assembly PopulateResolvedAssemblyWithFallbackOnPartialName(string fullName)
        {
            Assembly resolved = resolvedAssemblyCache[fullName];
            if (resolved == null)
            {
                string partialName = new AssemblyName(fullName).Name;
                if (fullName != partialName)
                {
                    try   { resolved = Assembly.Load(partialName); }
                    catch (FileNotFoundException)   { }
                    catch (BadImageFormatException) { }
                }
            }
            return resolved;
        }

        /// <summary>Resolves a reflected type to its native <see cref="Type" /> object.</summary>
        public static Type ResolveType(IResolvableTypeInfo type, MethodInfo methodContext, bool throwOnError)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            try
            {
                ITypeInfo elementType = type.ElementType;
                if (elementType != null)
                {
                    Type resolvedElementType = ResolveTypeWithMethodContext(elementType, methodContext);
                    if (type.IsArray)
                    {
                        int rank = type.ArrayRank;
                        return rank == 1 ? resolvedElementType.MakeArrayType() : resolvedElementType.MakeArrayType(rank);
                    }
                    if (type.IsByRef)    return resolvedElementType.MakeByRefType();
                    if (type.IsPointer)  return resolvedElementType.MakePointerType();
                }

                if (type.IsGenericParameter)
                {
                    Type resolvedType = ResolveGenericParameter((IGenericParameterInfo)type, methodContext);
                    if (resolvedType != null) return resolvedType;
                }
                else
                {
                    ITypeInfo simpleType = type.GenericTypeDefinition ?? type;
                    Assembly resolvedAssembly = simpleType.Assembly.Resolve(throwOnError);
                    if (resolvedAssembly != null)
                    {
                        Type resolvedType = resolvedAssembly.GetType(simpleType.FullName);
                        if (resolvedType != null)
                        {
                            if (type.IsGenericType && !type.IsGenericTypeDefinition)
                            {
                                Type[] resolvedTypeArguments = ResolveTypesWithMethodContext(type.GenericArguments, methodContext);
                                resolvedType = resolvedType.MakeGenericType(resolvedTypeArguments);
                            }
                            return resolvedType;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (throwOnError) throw new ReflectionResolveException(type, ex);
            }

            if (throwOnError) throw new ReflectionResolveException(type);
            return null; // unresolved
        }

        private static Type ResolveGenericParameter(IGenericParameterInfo genericParameter, MethodInfo methodContext)
        {
            IMethodInfo declaringMethod = genericParameter.DeclaringMethod;
            if (declaringMethod != null)
            {
                if (methodContext == null)
                    methodContext = declaringMethod.Resolve(true);
                return methodContext.GetGenericArguments()[genericParameter.Position];
            }
            return genericParameter.DeclaringType.Resolve(true).GetGenericArguments()[genericParameter.Position];
        }

        /// <summary>Resolves a reflected field to its native <see cref="FieldInfo" /> object.</summary>
        public static FieldInfo ResolveField(IFieldInfo field, bool throwOnError)
        {
            if (field == null) throw new ArgumentNullException("field");
            try
            {
                Type resolvedType = field.DeclaringType.Resolve(throwOnError);
                if (resolvedType != null)
                {
                    FieldInfo resolvedField = resolvedType.GetField(field.Name,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    if (resolvedField != null) return resolvedField;
                }
            }
            catch (Exception ex) { if (throwOnError) throw new ReflectionResolveException(field, ex); }

            if (throwOnError) throw new ReflectionResolveException(field);
            return null;
        }

        /// <summary>Resolves a reflected property to its native <see cref="PropertyInfo" /> object.</summary>
        public static PropertyInfo ResolveProperty(IPropertyInfo property, bool throwOnError)
        {
            if (property == null) throw new ArgumentNullException("property");
            try
            {
                Type resolvedType = property.DeclaringType.Resolve(throwOnError);
                if (resolvedType != null)
                {
                    Type returnType         = property.ValueType.Resolve(true);
                    Type[] parameterTypes   = ResolveParameterTypes(property.IndexParameters);
                    PropertyInfo resolved   = resolvedType.GetProperty(property.Name,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
                        null, returnType, parameterTypes, null);
                    if (resolved != null) return resolved;
                }
            }
            catch (Exception ex) { if (throwOnError) throw new ReflectionResolveException(property, ex); }

            if (throwOnError) throw new ReflectionResolveException(property);
            return null;
        }

        /// <summary>Resolves a reflected event to its native <see cref="EventInfo" /> object.</summary>
        public static EventInfo ResolveEvent(IEventInfo @event, bool throwOnError)
        {
            if (@event == null) throw new ArgumentNullException("event");
            try
            {
                Type resolvedType = @event.DeclaringType.Resolve(throwOnError);
                if (resolvedType != null)
                {
                    EventInfo resolved = resolvedType.GetEvent(@event.Name,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    if (resolved != null) return resolved;
                }
            }
            catch (Exception ex) { if (throwOnError) throw new ReflectionResolveException(@event, ex); }

            if (throwOnError) throw new ReflectionResolveException(@event);
            return null;
        }

        /// <summary>Resolves a reflected constructor to its native <see cref="ConstructorInfo" /> object.</summary>
        public static ConstructorInfo ResolveConstructor(IConstructorInfo constructor, bool throwOnError)
        {
            if (constructor == null) throw new ArgumentNullException("constructor");
            try
            {
                Type resolvedType = constructor.DeclaringType.Resolve(throwOnError);
                if (resolvedType != null)
                {
                    BindingFlags flags = (constructor.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic)
                                       | (constructor.IsStatic ? BindingFlags.Static : BindingFlags.Instance);
                    Type[] paramTypes  = ResolveParameterTypes(constructor.Parameters);
                    ConstructorInfo resolved = resolvedType.GetConstructor(flags, null, paramTypes, null);
                    if (resolved != null) return resolved;
                }
            }
            catch (Exception ex) { if (throwOnError) throw new ReflectionResolveException(constructor, ex); }

            if (throwOnError) throw new ReflectionResolveException(constructor);
            return null;
        }

        /// <summary>Resolves a reflected method to its native <see cref="MethodInfo" /> object.</summary>
        public static MethodInfo ResolveMethod(IMethodInfo method, bool throwOnError)
        {
            if (method == null) throw new ArgumentNullException("method");
            try
            {
                Type resolvedType = method.DeclaringType.Resolve(throwOnError);
                if (resolvedType != null)
                {
                    BindingFlags flags = BindingFlags.DeclaredOnly
                                       | (method.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic)
                                       | (method.IsStatic ? BindingFlags.Static : BindingFlags.Instance);
                    string name        = method.Name;
                    MethodInfo resolved = method.IsGenericMethod
                        ? ResolveGenericMethod(resolvedType, name, flags, method.GenericMethodDefinition.Parameters, method.GenericArguments)
                        : ResolveNonGenericMethod(resolvedType, name, flags, method.Parameters);
                    if (resolved != null) return resolved;
                }
            }
            catch (Exception ex) { if (throwOnError) throw new ReflectionResolveException(method, ex); }

            if (throwOnError) throw new ReflectionResolveException(method);
            return null;
        }

        private static MethodInfo ResolveNonGenericMethod(Type resolvedType, string methodName, BindingFlags flags,
            ICollection<IParameterInfo> parameters)
        {
            Type[] paramTypes = ResolveParameterTypes(parameters);
            return resolvedType.GetMethod(methodName, flags, null, paramTypes, null);
        }

        private static MethodInfo ResolveGenericMethod(Type resolvedType, string methodName, BindingFlags flags,
            IList<IParameterInfo> parameters, ICollection<ITypeInfo> genericArguments)
        {
            foreach (MethodInfo m in resolvedType.GetMethods(flags))
            {
                if (m.Name != methodName || !m.IsGenericMethod || m.GetGenericArguments().Length != genericArguments.Count)
                    continue;
                if (HasSameParameters(m, parameters))
                {
                    Type[] resolvedArgs = ResolveTypesWithMethodContext(genericArguments, m);
                    return m.MakeGenericMethod(resolvedArgs);
                }
            }
            return null;
        }

        private static bool HasSameParameters(MethodInfo method, IList<IParameterInfo> parameters)
        {
            ParameterInfo[] mp = method.GetParameters();
            if (mp.Length != parameters.Count) return false;
            for (int i = 0; i < mp.Length; i++)
            {
                ITypeInfo valueType = parameters[i].ValueType;
                try
                {
                    Type resolved = ResolveTypeWithMethodContext(valueType, method);
                    if (!resolved.Equals(mp[i].ParameterType)) return false;
                }
                catch (ReflectionResolveException) { return false; }
            }
            return true;
        }

        /// <summary>Resolves a reflected parameter to its native <see cref="ParameterInfo" /> object.</summary>
        public static ParameterInfo ResolveParameter(IParameterInfo parameter, bool throwOnError)
        {
            if (parameter == null) throw new ArgumentNullException("parameter");
            try
            {
                MemberInfo resolvedMember = parameter.Member.Resolve(throwOnError);
                if (resolvedMember != null && !(resolvedMember is IUnresolvedCodeElement))
                {
                    int parameterIndex = parameter.Position;
                    ParameterInfo[] resolvedParameters;
                    MethodBase resolvedMethod = resolvedMember as MethodBase;
                    if (resolvedMethod != null)
                    {
                        if (parameterIndex == -1) return ((MethodInfo)resolvedMethod).ReturnParameter;
                        resolvedParameters = resolvedMethod.GetParameters();
                    }
                    else
                    {
                        resolvedParameters = ((PropertyInfo)resolvedMember).GetIndexParameters();
                    }
                    if (parameterIndex < resolvedParameters.Length) return resolvedParameters[parameterIndex];
                }
            }
            catch (Exception ex) { if (throwOnError) throw new ReflectionResolveException(parameter, ex); }

            if (throwOnError) throw new ReflectionResolveException(parameter);
            return null;
        }

        private static Type[] ResolveParameterTypes(ICollection<IParameterInfo> parameters)
        {
            return GenericCollectionUtils.ConvertAllToArray<IParameterInfo, Type>(parameters,
                delegate(IParameterInfo p) { return p.ValueType.Resolve(true); });
        }

        private static Type[] ResolveTypesWithMethodContext(ICollection<ITypeInfo> types, MethodInfo methodContext)
        {
            return GenericCollectionUtils.ConvertAllToArray<ITypeInfo, Type>(types,
                delegate(ITypeInfo t) { return ResolveTypeWithMethodContext(t, methodContext); });
        }

        private static Type ResolveTypeWithMethodContext(ITypeInfo type, MethodInfo methodContext)
        {
            IResolvableTypeInfo resolvableType = type as IResolvableTypeInfo;
            return resolvableType != null ? resolvableType.Resolve(methodContext, true) : type.Resolve(true);
        }
    }
}
