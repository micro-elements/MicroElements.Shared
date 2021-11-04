// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace MicroElements.CodeContracts
{
    /// <summary>
    /// CodeContracts Assertions.
    /// </summary>
    internal static partial class Assertions
    {
        /// <summary>
        /// Checks that argument of an operation is not null.
        /// </summary>
        /// <typeparam name="T">Argument type.</typeparam>
        /// <param name="arg">The argument.</param>
        /// <param name="name">The argument name.</param>
        /// <returns>The same input argument.</returns>
        /// <exception cref="ArgumentNullException">The argument is null.</exception>
        /// <example><code>
        /// void Foo(string arg) {
        ///   arg.AssertArgumentNotNull(nameof(arg));
        /// }
        /// </code></example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T AssertArgumentNotNull<T>([NoEnumeration] this T? arg, [InvokerParameterName] string name)
        {
            if (arg is null)
                throw new ArgumentNullException(name);
            return arg;
        }
    }
}
