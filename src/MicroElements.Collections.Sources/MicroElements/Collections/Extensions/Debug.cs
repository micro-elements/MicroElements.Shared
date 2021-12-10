#region License

// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#endregion
#region Supressions

#pragma warning disable
// ReSharper disable CheckNamespace

#endregion

using System.Collections.Generic;
using System.Linq;

namespace MicroElements.Collections.Extensions
{
    internal static partial class DebugExtensions
    {
        /// <summary>
        /// Shows source enumeration as array in DEBUG mode.
        /// Does not affect performance in Release build.
        /// </summary>
        public static IEnumerable<T> ToArrayDebug<T>(this IEnumerable<T> source)
        {
#if DEBUG
            return source.ToArray();
#else
            return source;
#endif
        }
    }
}
