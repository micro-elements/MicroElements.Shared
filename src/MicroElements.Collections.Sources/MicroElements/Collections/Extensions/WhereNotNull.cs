#region License

// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#endregion
#region Supressions

#pragma warning disable
// ReSharper disable all

#endregion

namespace MicroElements.Collections.Extensions.WhereNotNull
{
    using JetBrains.Annotations;
    using MicroElements.CodeContracts;
    using System.Collections.Generic;
    using PureAttribute = System.Diagnostics.Contracts.PureAttribute;

    /// <readme id="WhereNotNull">
    /// <![CDATA[
    /// ### WhereNotNull
    /// Enumerates only not null values.
    ///
    /// ```csharp
    /// string?[] namesWithNulls = {"Alex", null, "John"};
    /// string[] names = namesWithNulls.WhereNotNull().ToArray();
    /// ```
    /// ]]>
    /// </readme>
    internal static class WhereNotNullExtensions
    {
        /// <summary>
        /// Enumerates only not null values.
        /// </summary>
        /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to filter.</param>
        /// <typeparam name="T">The type of the elements of <paramref name="source" />.</typeparam>
        /// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains only not null elements.</returns>
        [Pure, LinqTunnel]
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
        {
            source.AssertArgumentNotNull(nameof(source));

            return WhereNotNullIterator(source);
        }

        private static IEnumerable<TResult> WhereNotNullIterator<TResult>(IEnumerable<TResult?> source)
        {
            foreach (TResult? obj in source)
            {
                if (obj is { } result)
                {
                    yield return result;
                }
            }
        }
    }
}