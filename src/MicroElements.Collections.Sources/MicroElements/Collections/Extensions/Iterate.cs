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
using System.Runtime.CompilerServices;
using MicroElements.CodeContracts;

namespace MicroElements.Collections.Extensions.Iterate
{
    /// <readme id="Iterate">
    /// <![CDATA[
    /// ### Iterate
    /// Iterates values and executes action for each value.
    /// It's like `List.ForEach` but works with lazy enumerations and does not forces additional allocations.
    /// 
    /// ```csharp
    /// // Iterates values and outputs to console.
    /// Enumerable
    ///     .Range(1, 100_000)
    ///     .Iterate(Console.WriteLine);
    /// ```
    /// ]]>
    /// </readme>
    internal static class IterateExtensions
    {
        /// <summary>
        /// Iterates values.
        /// Can be used to replace <see cref="Enumerable.ToArray{TSource}"/> if no need to create array but only iterate.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="values">Enumeration.</param>
        public static void Iterate<T>(this IEnumerable<T> values)
        {
            values.Iterate(DoNothing);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void DoNothing(T value)
            {
            }
        }

        /// <summary>
        /// Iterates values and executes <paramref name="action"/> for each value.
        /// It's like <see cref="List{T}.ForEach"/> but works with lazy enumerations and does not forces additional allocations.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="values">Enumeration.</param>
        /// <param name="action">Action to execute.</param>
        public static void Iterate<T>(this IEnumerable<T> values, Action<T> action)
        {
            values.AssertArgumentNotNull(nameof(values));
            action.AssertArgumentNotNull(nameof(action));

            foreach (T value in values)
            {
                action(value);
            }
        }
    }
}