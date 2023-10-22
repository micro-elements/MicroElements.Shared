#region License
// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#endregion
#region Supressions
#pragma warning disable
// ReSharper disable CheckNamespace
#endregion

namespace MicroElements.Reflection.CodeCompiler
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using MicroElements.Reflection.FriendlyName;

    /// <summary>
    /// Creates compiled functions.
    /// </summary>
    internal static partial class CodeCompiler
    {
        #region Caches

        private readonly record struct FuncKey(Type Type, string Name);

        private static class Cache<Arg0, Result>
        {
            public static readonly ConcurrentDictionary<FuncKey, Func<Arg0, Result>> FuncCache = new ();
        }

        private static class Cache<Arg0, Arg1, Result>
        {
            public static readonly ConcurrentDictionary<FuncKey, Func<Arg0, Arg1, Result>> FuncCache = new ();
        }

        private static class Cache<Arg0, Arg1, Arg2, Result>
        {
            public static readonly ConcurrentDictionary<FuncKey, Func<Arg0, Arg1, Arg2, Result>> FuncCache = new ();
        }

        #endregion

        /// <summary>
        /// Marker type for generic function type.
        /// </summary>
        public class GenericType { }

        /// <summary>
        /// Creates compiled function from generic function.
        /// <paramref name="type"/> will be used instead <see cref="GenericType"/>.
        /// </summary>
        /// <typeparam name="Arg0">Input Arg0 type.</typeparam>
        /// <typeparam name="Result">Result type.</typeparam>
        /// <param name="type">Runtime type that will be used instead <see cref="GenericType"/> in <paramref name="genericMethodFunc"/>.</param>
        /// <param name="name">Function name to distinguish functions with the same arg types in cache.</param>
        /// <param name="genericMethodFunc">Generic function that will be compiled for each input <paramref name="type"/>.</param>
        /// <returns>Compiled function for <paramref name="type"/>.</returns>
        public static Func<Arg0, Result> CachedCompiledFunc<Arg0, Result>(Type type, string name, Func<Arg0, Result> genericMethodFunc)
        {
            if (!Cache<Arg0, Result>.FuncCache.TryGetValue(new FuncKey(type, name), out var cachedFunc))
            {
                cachedFunc = CompileGeneric(genericMethodFunc, type);
                Cache<Arg0, Result>.FuncCache.TryAdd(new FuncKey(type, name), cachedFunc);
            }

            return cachedFunc;
        }

        /// <summary>
        /// Creates compiled function from generic function.
        /// <paramref name="type"/> will be used instead <see cref="GenericType"/>.
        /// </summary>
        /// <typeparam name="Arg0">Input Arg0 type.</typeparam>
        /// <typeparam name="Arg1">Input Arg1 type.</typeparam>
        /// <typeparam name="Result">Result type.</typeparam>
        /// <param name="type">Runtime type that will be used instead <see cref="GenericType"/> in <paramref name="genericMethodFunc"/>.</param>
        /// <param name="name">Function name to distinguish functions with the same arg types in cache.</param>
        /// <param name="genericMethodFunc">Generic function that will be compiled for each input <paramref name="type"/>.</param>
        /// <returns>Compiled function for <paramref name="type"/>.</returns>
        public static Func<Arg0, Arg1, Result> CachedCompiledFunc<Arg0, Arg1, Result>(Type type, string name, Func<Arg0, Arg1, Result> genericMethodFunc)
        {
            if (!Cache<Arg0, Arg1, Result>.FuncCache.TryGetValue(new FuncKey(type, name), out var cachedFunc))
            {
                cachedFunc = CompileGeneric(genericMethodFunc, type);
                Cache<Arg0, Arg1, Result>.FuncCache.TryAdd(new FuncKey(type, name), cachedFunc);
            }

            return cachedFunc;
        }

        /// <summary>
        /// Creates compiled function from generic function.
        /// <paramref name="type"/> will be used instead <see cref="GenericType"/>.
        /// </summary>
        /// <typeparam name="Arg0">Input Arg0 type.</typeparam>
        /// <typeparam name="Arg1">Input Arg1 type.</typeparam>
        /// <typeparam name="Arg2">Input Arg2 type.</typeparam>
        /// <typeparam name="Result">Result type.</typeparam>
        /// <param name="type">Runtime type that will be used instead <see cref="GenericType"/> in <paramref name="genericMethodFunc"/>.</param>
        /// <param name="name">Function name to distinguish functions with the same arg types in cache.</param>
        /// <param name="genericMethodFunc">Generic function that will be compiled for each input <paramref name="type"/>.</param>
        /// <returns>Compiled function for <paramref name="type"/>.</returns>
        public static Func<Arg0, Arg1, Arg2, Result> CachedCompiledFunc<Arg0, Arg1, Arg2, Result>(Type type, string name, Func<Arg0, Arg1, Arg2, Result> genericMethodFunc)
        {
            if (!Cache<Arg0, Arg1, Arg2, Result>.FuncCache.TryGetValue(new FuncKey(type, name), out var cachedFunc))
            {
                cachedFunc = CompileGeneric(genericMethodFunc, type);
                Cache<Arg0, Arg1, Arg2, Result>.FuncCache.TryAdd(new FuncKey(type, name), cachedFunc);
            }

            return cachedFunc;
        }

        private static Func<Arg0, Result> CompileGeneric<Arg0, Result>(Func<Arg0, Result> genericMethodFunc, Type genericArg1)
        {
            MethodInfo genericMethod = genericMethodFunc.Method.GetGenericMethodDefinition().MakeGenericMethod(genericArg1);
            return Invoker.CompileMethod<Arg0, Result>(genericMethod);
        }

        private static Func<Arg0, Arg1, Result> CompileGeneric<Arg0, Arg1, Result>(Func<Arg0, Arg1, Result> genericMethodFunc, Type genericArg1)
        {
            MethodInfo genericMethod = genericMethodFunc.Method.GetGenericMethodDefinition().MakeGenericMethod(genericArg1);
            return Invoker.CompileMethod<Arg0, Arg1, Result>(genericMethod);
        }

        private static Func<Arg0, Arg1, Arg2, Result> CompileGeneric<Arg0, Arg1, Arg2, Result>(Func<Arg0, Arg1, Arg2, Result> genericMethodFunc, Type genericArg1)
        {
            MethodInfo genericMethod = genericMethodFunc.Method.GetGenericMethodDefinition().MakeGenericMethod(genericArg1);
            return Invoker.CompileMethod<Arg0, Arg1, Arg2, Result>(genericMethod);
        }
    }

    /// <summary>
    /// Invoker creates compiled cached delegates.
    /// </summary>
    internal static class Invoker
    {
        #region Caches

        private static class FuncCache<TMethodArg1, TResult>
        {
            internal static ConcurrentDictionary<(Type MethodOwner, string MethodName, Type? GenericArg1, Type? GenericArg2), Func<TMethodArg1, TResult>> Cache = new();
        }

        private static class FuncCache<TMethodArg1, TMethodArg2, TResult>
        {
            internal static ConcurrentDictionary<(Type MethodOwner, string MethodName, Type? GenericArg1, Type? GenericArg2), Func<TMethodArg1, TMethodArg2, TResult>> Cache = new();
        }

        private static class FuncCache<TMethodArg1, TMethodArg2, TMethodArg3, TResult>
        {
            internal static ConcurrentDictionary<(Type MethodOwner, string MethodName, Type? GenericArg1, Type? GenericArg2), Func<TMethodArg1, TMethodArg2, TMethodArg3, TResult>> Cache = new();
        }

        #endregion

        public static Func<TInstance, TMethodArg1, TResult> GetCompiledCachedMethod<TInstance, TMethodArg1, TResult>(
            string methodName, Type? genericArg1 = null, Type? genericArg2 = null)
        {
            var cacheKey = (typeof(TInstance), MethodName: methodName, GenericArg1: genericArg1, GenericArg2: genericArg2);
            return FuncCache<TInstance, TMethodArg1, TResult>.Cache.GetOrAdd(cacheKey,
                a => CompileMethod<TInstance, TMethodArg1, TResult>(a.MethodOwner, a.MethodName, a.GenericArg1, a.GenericArg2));
        }

        public static Func<TInstance, TMethodArg1, TMethodArg2, TResult> GetCompiledCachedMethod<TInstance, TMethodArg1, TMethodArg2, TResult>(
            string methodName, Type? genericArg1 = null, Type? genericArg2 = null)
        {
            var cacheKey = (typeof(TInstance), MethodName: methodName, GenericArg1: genericArg1, GenericArg2: genericArg2);
            return FuncCache<TInstance, TMethodArg1, TMethodArg2, TResult>.Cache.GetOrAdd(cacheKey,
                a => CompileMethod<TInstance, TMethodArg1, TMethodArg2, TResult>(a.MethodOwner, a.MethodName, a.GenericArg1, a.GenericArg2));
        }

        public static Func<TMethodArg1, TResult> GetCompiledCachedMethod<TMethodArg1, TResult>(
            this Type methodOwner, string methodName, Type? genericArg1 = null, Type? genericArg2 = null)
        {
            var cacheKey = (MethodOwner: methodOwner, MethodName: methodName, GenericArg1: genericArg1, GenericArg2: genericArg2);
            return FuncCache<TMethodArg1, TResult>.Cache.GetOrAdd(cacheKey,
                a => CompileMethod<TMethodArg1, TResult>(a.MethodOwner, a.MethodName, a.GenericArg1, a.GenericArg2));
        }

        public static Func<TMethodArg1, TMethodArg2, TResult> GetCompiledCachedMethod<TMethodArg1, TMethodArg2, TResult>(
            this Type methodOwner, string methodName, Type? genericArg1 = null, Type? genericArg2 = null)
        {
            var cacheKey = (MethodOwner: methodOwner, MethodName: methodName, GenericArg1: genericArg1, GenericArg2: genericArg2);
            return FuncCache<TMethodArg1, TMethodArg2, TResult>.Cache.GetOrAdd(cacheKey,
                a => CompileMethod<TMethodArg1, TMethodArg2, TResult>(a.MethodOwner, a.MethodName, a.GenericArg1, a.GenericArg2));
        }

        public static Func<TMethodArg1, TMethodArg2, TMethodArg3, TResult> GetCompiledCachedMethod<TMethodArg1, TMethodArg2, TMethodArg3, TResult>(
            this Type methodOwner, string methodName, Type? genericArg1 = null, Type? genericArg2 = null)
        {
            var cacheKey = (MethodOwner: methodOwner, MethodName: methodName, GenericArg1: genericArg1, GenericArg2: genericArg2);
            return FuncCache<TMethodArg1, TMethodArg2, TMethodArg3, TResult>.Cache.GetOrAdd(cacheKey,
                a => CompileMethod<TMethodArg1, TMethodArg2, TMethodArg3, TResult>(a.MethodOwner, a.MethodName, a.GenericArg1, a.GenericArg2));
        }

        public static Func<TMethodArg1, TResult> GetCompiledCachedMethod<TMethodArg1, TResult>(Func<TMethodArg1, TResult> genericMethodFunc, Type? genericArg1 = null, Type? genericArg2 = null)
        {
            var methodInfo = FromFunc(genericMethodFunc, genericArg1, genericArg2);
            var cacheKey = (MethodOwner: methodInfo.DeclaringType, MethodName: methodInfo.Name, GenericArg1: genericArg1, GenericArg2: genericArg2);
            return FuncCache<TMethodArg1, TResult>.Cache.GetOrAdd(cacheKey,
                a => CompileMethod<TMethodArg1, TResult>(a.MethodOwner, a.MethodName, a.GenericArg1, a.GenericArg2));
        }

        public static Func<TMethodArg1, TMethodArg2, TResult> GetCompiledCachedMethod<TMethodArg1, TMethodArg2, TResult>(Func<TMethodArg1, TMethodArg2, TResult> genericMethodFunc, Type? genericArg1 = null, Type? genericArg2 = null)
        {
            var methodInfo = FromFunc(genericMethodFunc, genericArg1, genericArg2);
            var cacheKey = (MethodOwner: methodInfo.DeclaringType, MethodName: methodInfo.Name, GenericArg1: genericArg1, GenericArg2: genericArg2);
            return FuncCache<TMethodArg1, TMethodArg2, TResult>.Cache.GetOrAdd(cacheKey,
                a => CompileMethod<TMethodArg1, TMethodArg2, TResult>(a.MethodOwner, a.MethodName, a.GenericArg1, a.GenericArg2));
        }

        public static Func<TMethodArg1, TMethodArg2, TMethodArg3, TResult> GetCompiledCachedMethod<TMethodArg1, TMethodArg2, TMethodArg3, TResult>(Func<TMethodArg1, TMethodArg2, TMethodArg3, TResult> genericMethodFunc, Type? genericArg1 = null, Type? genericArg2 = null)
        {
            var methodInfo = FromFunc(genericMethodFunc, genericArg1, genericArg2);
            var cacheKey = (MethodOwner: methodInfo.DeclaringType, MethodName: methodInfo.Name, GenericArg1: genericArg1, GenericArg2: genericArg2);
            return FuncCache<TMethodArg1, TMethodArg2, TMethodArg3, TResult>.Cache.GetOrAdd(cacheKey,
                a => CompileMethod<TMethodArg1, TMethodArg2, TMethodArg3, TResult>(a.MethodOwner, a.MethodName, a.GenericArg1, a.GenericArg2));
        }

        public static MethodInfo GetMethod(this Type methodOwner, string methodName, Type? genericArg1, Type? genericArg2)
        {
            BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var methodInfo = methodOwner.GetMethod(methodName, bindingFlags);
            if (methodInfo is null)
                throw new InvalidOperationException($"Type {methodOwner.GetFriendlyName()} has no method {methodName}");

            return MakeGenericMethodIfGeneric(methodInfo, genericArg1, genericArg2);
        }

        public static MethodInfo MakeGenericMethodIfGeneric(this MethodInfo methodInfo, Type? genericArg1, Type? genericArg2)
        {
            if (methodInfo.IsGenericMethodDefinition)
            {
                Type[] methodGenericArguments = GetArgs(genericArg1, genericArg2).ToArray();
                methodInfo = methodInfo.MakeGenericMethod(methodGenericArguments);
            }

            return methodInfo;

            static IEnumerable<Type> GetArgs(Type? genericArg1, Type? genericArg2)
            {
                if (genericArg1 != null) yield return genericArg1;
                if (genericArg2 != null) yield return genericArg2;
            }
        }

        public static MethodInfo EnsureIsGenericMethodDefinition(this MethodInfo methodInfo)
        {
            if (!methodInfo.IsGenericMethodDefinition)
                throw new InvalidOperationException("Method should be generic. Use method group instead of lambda.");
            return methodInfo;
        }

        public static Func<TMethodArg1, TResult> CompileMethod<TMethodArg1, TResult>(
            this Type methodOwner, string methodName, Type? genericArg1, Type? genericArg2)
        {
            var genericMethod = GetMethod(methodOwner, methodName, genericArg1, genericArg2);
            return CompileMethod<TMethodArg1, TResult>(genericMethod);
        }

        public static Func<TMethodArg1, TMethodArg2, TResult> CompileMethod<TMethodArg1, TMethodArg2, TResult>(
            this Type methodOwner, string methodName, Type? genericArg1, Type? genericArg2)
        {
            var genericMethod = GetMethod(methodOwner, methodName, genericArg1, genericArg2);
            return CompileMethod<TMethodArg1, TMethodArg2, TResult>(genericMethod);
        }

        public static Func<TMethodArg1, TMethodArg2, TMethodArg3, TResult> CompileMethod<TMethodArg1, TMethodArg2, TMethodArg3, TResult>(
            this Type methodOwner, string methodName, Type? genericArg1, Type? genericArg2)
        {
            var genericMethod = GetMethod(methodOwner, methodName, genericArg1, genericArg2);
            return CompileMethod<TMethodArg1, TMethodArg2, TMethodArg3, TResult>(genericMethod);
        }

        public static Func<TMethodArg1, TResult> CompileMethod<TMethodArg1, TResult>(this MethodInfo method)
        {
            var arg1 = Expression.Parameter(typeof(TMethodArg1), "arg1");

            MethodCallExpression callExpression = method.IsStatic ?
                Expression.Call(null, method, arg1) :
                Expression.Call(arg1, method);

            return Expression
                .Lambda<Func<TMethodArg1, TResult>>(callExpression, arg1)
                .Compile();
        }

        public static Func<TMethodArg1, TMethodArg2, TResult> CompileMethod<TMethodArg1, TMethodArg2, TResult>(this MethodInfo method)
        {
            var arg1 = Expression.Parameter(typeof(TMethodArg1), "arg1");
            var arg2 = Expression.Parameter(typeof(TMethodArg2), "arg2");

            MethodCallExpression callExpression = method.IsStatic ?
                Expression.Call(null, method, arg1, arg2) :
                Expression.Call(arg1, method, arg2);

            return Expression
                .Lambda<Func<TMethodArg1, TMethodArg2, TResult>>(callExpression, arg1, arg2)
                .Compile();
        }

        public static Func<TMethodArg1, TMethodArg2, TMethodArg3, TResult> CompileMethod<TMethodArg1, TMethodArg2, TMethodArg3, TResult>(this MethodInfo method)
        {
            var arg1 = Expression.Parameter(typeof(TMethodArg1), "arg1");
            var arg2 = Expression.Parameter(typeof(TMethodArg2), "arg2");
            var arg3 = Expression.Parameter(typeof(TMethodArg3), "arg3");

            MethodCallExpression callExpression = method.IsStatic ?
                Expression.Call(null, method, arg1, arg2, arg3) :
                Expression.Call(arg1, method, arg2, arg3);

            return Expression
                .Lambda<Func<TMethodArg1, TMethodArg2, TMethodArg3, TResult>>(callExpression, arg1, arg2, arg3)
                .Compile();
        }

        public static MethodInfo FromFunc<TMethodArg1, TResult>(Func<TMethodArg1, TResult> genericMethodGroup, Type? genericArg1 = null, Type? genericArg2 = null)
        {
            return genericMethodGroup.Method
                .GetGenericMethodDefinition()
                .MakeGenericMethodIfGeneric(genericArg1, genericArg2);
        }

        public static MethodInfo FromFunc<TMethodArg1, TMethodArg2, TResult>(Func<TMethodArg1, TMethodArg2, TResult> genericMethodGroup, Type? genericArg1 = null, Type? genericArg2 = null)
        {
            return genericMethodGroup.Method
                .GetGenericMethodDefinition()
                .MakeGenericMethodIfGeneric(genericArg1, genericArg2);
        }

        public static MethodInfo FromFunc<TMethodArg1, TMethodArg2, TMethodArg3, TResult>(Func<TMethodArg1, TMethodArg2, TMethodArg3, TResult> genericMethodGroup, Type? genericArg1 = null, Type? genericArg2 = null)
        {
            return genericMethodGroup.Method
                .GetGenericMethodDefinition()
                .MakeGenericMethodIfGeneric(genericArg1, genericArg2);
        }
    }
}
