// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace MicroElements.Reflection.ObjectExtensions
{
    /// <summary>
    /// Object extensions.
    /// </summary>
    internal static partial class ObjectExtensions
    {
        /// <summary>
        /// Returns true if the value is equal to this type's default value.
        /// </summary>
        /// <example>
        ///     0.IsDefault() == true
        ///     1.IsDefault() == false
        /// </example>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="value">Value to check.</param>
        /// <returns>True if the value is equal to this type's default value.</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDefault<T>(this T? value) =>
            EqualityComparer<T>.Default.Equals(value, default(T));

        /// <summary>
        /// Returns true if the value is null, and does so without boxing of any value-types.
        /// Value-types will always return false.
        /// </summary>
        /// <example>
        ///     int x = 0;
        ///     string y = null;
        ///
        ///     x.IsNull()  // false
        ///     y.IsNull()  // true
        /// </example>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="value">Value to check.</param>
        /// <returns>True if the value is null. Value-types will always return false.</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNull<T>([NotNullWhen(false)] this T? value) =>
            value is null;

        /// <summary>
        /// Returns true if value is not null.
        /// Value-types will always return true.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="value">Value to check.</param>
        /// <returns>True if value is not null. Value-types will always return true.</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNotNull<T>([NotNullWhen(true)] this T? value) =>
            !value.IsNull();

        /// <summary>
        /// Determines whether the value is not null or default.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="value">Value to check.</param>
        /// <returns>True if value is not null (for classes) and is not default (for value types).</returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNotNullOrDefault<T>([NotNullWhen(true)] this T? value) =>
            !value.IsNull() && !value.IsDefault();
    }
}