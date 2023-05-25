#region License

// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#endregion
#region Supressions

using JetBrains.Annotations;

#pragma warning disable
// ReSharper disable CheckNamespace

#endregion

namespace MicroElements.Collections.Extensions.Execute
{
    using System;
    using System.Collections.Generic;
    using MicroElements.CodeContracts;

    /// <readme id="Execute">
    /// <![CDATA[
    /// ### Execute
    /// Execute extension allows to execute an action on each value while enumerating the source enumeration.
    ///
    /// This method is pure LINQ method, with postponed enumeration, also it can be chained.
    ///
    /// ```csharp
    /// Enumerable
    ///     .Range(1, 10)
    ///     .Execute(Console.WriteLine)
    ///     .Iterate();
    /// ```
    /// ]]>
    /// </readme>
    internal static partial class ExecuteExtensions
    {
        /// <summary>
        /// Execute extension allows to execute an action on each value while enumerating the source enumeration
        /// </summary>
        /// <param name="source"></param>
        /// <param name="action"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [LinqTunnel]
        public static IEnumerable<T> Execute<T>(this IEnumerable<T> source, Action<T> action)
        {
            source.AssertArgumentNotNull(nameof(source));
            action.AssertArgumentNotNull(nameof(action));

            if (source is IList<T> list)
            {
                return ExecuteListIterator(list, action);
            }

            return ExecuteIterator(source, action);
        }

        private static IEnumerable<T> ExecuteIterator<T>(IEnumerable<T> source, Action<T> action)
        {
            foreach (T value in source)
            {
                action(value);
                yield return value;
            }
        }

        private static IEnumerable<T> ExecuteListIterator<T>(IList<T> source, Action<T> action)
        {
            for (int i = 0, count = source.Count; i < count; i++)
            {
                action(source[i]);
            }

            return source;
        }
    }
}