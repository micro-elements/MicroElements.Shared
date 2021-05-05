// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MicroElements.CodeContracts;

namespace MicroElements.Reflection
{
    /// <summary>
    /// Reflection extensions.
    /// </summary>
    internal static partial class TypeExtensions
    {
        /// <summary>
        /// Returns <c>true</c> if <paramref name="sourceType"/> is assignable to <paramref name="targetType"/>.
        /// </summary>
        /// <param name="sourceType">Source type to check.</param>
        /// <param name="targetType">Target type.</param>
        /// <returns><c>true</c> if <paramref name="sourceType"/> is assignable to <paramref name="targetType"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAssignableTo(this Type sourceType, Type targetType)
        {
            return targetType.GetTypeInfo().IsAssignableFrom(sourceType.GetTypeInfo());
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="value"/> is assignable to <paramref name="targetType"/>.
        /// </summary>
        /// <param name="value">Value to check.</param>
        /// <param name="targetType">Target type.</param>
        /// <returns><c>true</c> if <paramref name="value"/> is assignable to <paramref name="targetType"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAssignableTo(this object value, Type targetType)
        {
            return value.GetType().IsAssignableTo(targetType);
        }

        /// <summary>
        /// Determines whether <paramref name="sourceType"/> is assignable to <typeparamref name="TTarget" />.
        /// </summary>
        /// <typeparam name="TTarget">Target type.</typeparam>
        /// <param name="sourceType">Source type to check.</param>
        /// <returns>True if type is assignable to references of type <typeparamref name="TTarget" />; otherwise, False.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAssignableTo<TTarget>(this Type sourceType)
        {
            return sourceType.IsAssignableTo(typeof(TTarget));
        }

        /// <summary>
        /// Determines whether <paramref name="sourceType"/> is concrete type: (not interface and not abstract).
        /// </summary>
        /// <param name="sourceType">Source type to check.</param>
        /// <returns>True if type is concrete.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsConcreteType(this Type sourceType)
        {
            return !sourceType.IsInterface && !sourceType.IsAbstract;
        }

        /// <summary>
        /// Determines whether <paramref name="sourceType"/> is concrete class and assignable to <typeparamref name="TTarget" />.
        /// </summary>
        /// <typeparam name="TTarget">Target type.</typeparam>
        /// <param name="sourceType">Source type to check.</param>
        /// <returns>True if type is assignable to references of type <typeparamref name="TTarget" />; otherwise, False.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsConcreteAndAssignableTo<TTarget>(this Type sourceType)
        {
            return sourceType.IsConcreteType() && sourceType.IsAssignableTo<TTarget>();
        }

        /// <summary>
        /// Returns a value indicating whether the type is a reference type.
        /// </summary>
        /// <param name="type">Source type.</param>
        /// <returns>True if argument is a reference type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsReferenceType(this Type type)
        {
            return !type.GetTypeInfo().IsValueType;
        }

        /// <summary>
        /// Returns a value indicating whether the type is a nullable struct.
        /// </summary>
        /// <param name="type">Source type.</param>
        /// <returns>True if argument is a nullable struct.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullableStruct(this Type type)
        {
            return type.GetTypeInfo().IsValueType && Nullable.GetUnderlyingType(type) != null;
        }

        /// <summary>
        /// Returns <c>true</c> if <c>null</c> can be assigned to type.
        /// </summary>
        /// <param name="targetType">Target type.</param>
        /// <returns><c>true</c> if <c>null</c> can be assigned to type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanAcceptNull(this Type targetType)
        {
            return targetType.IsReferenceType() || targetType.IsNullableStruct();
        }

        /// <summary>
        /// Returns <c>true</c> if <c>null</c> can not be assigned to type.
        /// </summary>
        /// <param name="targetType">Target type.</param>
        /// <returns><c>true</c> if <c>null</c> can not be assigned to type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanNotAcceptNull(this Type targetType)
        {
            return !targetType.CanAcceptNull();
        }

        /// <summary>
        /// Returns a value indicating whether the type is a numeric type.
        /// </summary>
        /// <param name="type">Source type.</param>
        /// <returns>True if argument is a numeric type.</returns>
        public static bool IsNumericType(this Type type)
        {
            type.AssertArgumentNotNull(nameof(type));

            //return TypeCache.NumericTypes.Contains(type);

            if (type == typeof(byte))
                return true;
            if (type == typeof(short))
                return true;
            if (type == typeof(int))
                return true;
            if (type == typeof(long))
                return true;
            if (type == typeof(float))
                return true;
            if (type == typeof(double))
                return true;
            if (type == typeof(decimal))
                return true;
            if (type == typeof(sbyte))
                return true;
            if (type == typeof(ushort))
                return true;
            if (type == typeof(uint))
                return true;
            if (type == typeof(ulong))
                return true;

            return false;
        }

        /// <summary>
        /// Returns a value indicating whether the type is a nullable numeric type.
        /// </summary>
        /// <param name="type">Source type.</param>
        /// <returns>True if argument is a nullable numeric type.</returns>
        public static bool IsNullableNumericType(this Type type)
        {
            type.AssertArgumentNotNull(nameof(type));

            if (!type.GetTypeInfo().IsValueType)
                return false;

            Type? underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType is null)
                return false;

            return underlyingType.IsNumericType();
        }

        /// <summary>
        /// Gets default value for type.
        /// </summary>
        /// <param name="type">Source type.</param>
        /// <returns>Default value.</returns>
        public static object? GetDefaultValue(this Type type)
        {
            type.AssertArgumentNotNull(nameof(type));

            if (type.IsValueType)
                return Activator.CreateInstance(type);

            // For reference types always returns null.
            return null;
        }
    }

    /// <summary>
    /// Represents cached type checks.
    /// </summary>
    /// <typeparam name="T">Type.</typeparam>
    internal static class TypeCheck<T>
    {
        /// <summary>
        /// Gets a value indicating whether the type specified by the generic argument is a reference type.
        /// </summary>
        public static readonly bool IsReferenceType;

        /// <summary>
        /// Gets a value indicating whether the type specified by the generic argument is a nullable struct.
        /// </summary>
        public static readonly bool IsNullableStruct;

        static TypeCheck()
        {
            IsNullableStruct = typeof(T).IsNullableStruct();
            IsReferenceType = typeof(T).IsReferenceType();
        }
    }
}
