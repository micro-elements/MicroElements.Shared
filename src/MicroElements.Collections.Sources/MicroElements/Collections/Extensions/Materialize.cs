#region License

// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#endregion
#region Supressions

#pragma warning disable
// ReSharper disable CheckNamespace

#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace MicroElements.Collections.Extensions.Materialize
{
    /// <readme id="Materialize">
    /// <![CDATA[
    /// ### Materialize
    /// Materializes source enumeration and allows to see intermediate results without changing execute chain.
    /// MaterializeDebug is the same as Materialize but works only in Debug mode and does not affect performance for Release builds.
    /// 
    /// ```csharp
    /// Enumerable
    ///     .Range(1, 10)
    ///     .Materialize(values => { /*set breakpoint here*/ })
    ///     .Iterate(Console.WriteLine);
    /// ```
    /// ]]>
    /// </readme>
    internal static partial class MaterializeExtensions
    {
        /// <summary>
        /// Materializes <paramref name="source"/> as <see cref="IReadOnlyList{T}"/> and invokes <paramref name="action"/>.
        /// If <paramref name="action"/> is null then no materialization occurs.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="source">Source enumeration.</param>
        /// <param name="action">Optional action with source snapshot as argument.</param>
        /// <returns>The same enumeration if action is null or materialized enumeration.</returns>
        public static IEnumerable<T> Materialize<T>(this IEnumerable<T> source, Action<IReadOnlyList<T>>? action)
        {
            if (action == null)
                return source;

            var materializedItems = source.ToArray();
            action(materializedItems);
            return materializedItems;
        }

        /// <summary>
        /// Materializes source enumeration and allows to see intermediate results without changing execute chain.
        /// The same as <see cref="Materialize{T}"/> but only for DEBUG mode. Does not affect performance in Release build.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="source">Source enumeration.</param>
        /// <param name="action">Optional action with source snapshot as argument.</param>
        /// <returns>The same enumeration if action is null or materialized enumeration.</returns>
        public static IEnumerable<T> MaterializeDebug<T>(this IEnumerable<T> source, Action<IReadOnlyList<T>> action)
        {
#if DEBUG
            return Materialize(source, action);
#else
            return source;
#endif
        }
    }
}