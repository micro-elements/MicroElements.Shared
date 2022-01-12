#region License

// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#endregion
#region Supressions

#pragma warning disable
// ReSharper disable CheckNamespace

#endregion

namespace MicroElements.Collections.Extensions.NotNull
{
    using System;
    using System.Collections.Generic;
    using JetBrains.Annotations;
    using PureAttribute = System.Diagnostics.Contracts.PureAttribute;

    /// <readme id="NotNull">
    /// <![CDATA[
    /// ### NotNull
    /// Returns not null (empty) enumeration if input is null.
    /// 
    /// ```csharp
    /// string? GetFirstValue(IEnumerable<string>? optionalValues) =>
    ///     optionalValues
    ///         .NotNull()
    ///         .FirstOrDefault();
    /// ```
    /// ]]>
    /// </readme>
    internal static partial class NotNullExtensions
    {
        /// <summary>
        /// Returns not null <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">Item type.</typeparam>
        /// <param name="items"><see cref="IEnumerable{T}"/> or null.</param>
        /// <returns>The same items or empty enumeration.</returns>
        /// <example>
        /// <code>
        /// string? GetFirstName(IEnumerable&lt;string&gt;? names)
        /// {
        ///     return names
        ///         .NotNull()
        ///         .FirstOrDefault();
        /// }
        /// </code>
        /// </example>
        [Pure, LinqTunnel]
        public static IEnumerable<T> NotNull<T>([NoEnumeration] this IEnumerable<T>? items) =>
            items ?? Array.Empty<T>();

        /// <summary>
        /// Returns not null <see cref="IReadOnlyCollection{T}"/>.
        /// </summary>
        /// <typeparam name="T">Item type.</typeparam>
        /// <param name="items"><see cref="IReadOnlyCollection{T}"/> or null.</param>
        /// <returns>The same items or empty collection.</returns>
        [Pure]
        public static IReadOnlyCollection<T> NotNull<T>(this IReadOnlyCollection<T>? items)
            => items ?? Array.Empty<T>();

        /// <summary>
        /// Returns not null array.
        /// </summary>
        /// <typeparam name="T">Item type.</typeparam>
        /// <param name="items"><see cref="IReadOnlyCollection{T}"/> or null.</param>
        /// <returns>The same items or empty collection.</returns>
        [Pure]
        public static T[] NotNull<T>(this T[]? items)
            => items ?? Array.Empty<T>();
    }
}
