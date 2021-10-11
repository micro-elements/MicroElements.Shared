using System;
using System.Collections.Generic;

namespace MicroElements.Extensions
{
    /// <summary>
    /// <see cref="IReadOnlyCollection{T}"/> extensions.
    /// </summary>
    internal static partial class ReadOnlyCollectionExtensions
    {
        /// <summary>
        /// Returns not null <see cref="IReadOnlyCollection{T}"/>.
        /// </summary>
        /// <typeparam name="T">Item type.</typeparam>
        /// <param name="items"><see cref="IReadOnlyCollection{T}"/> or null.</param>
        /// <returns>The same items or empty collection.</returns>
        public static IReadOnlyCollection<T> NotNull<T>(this IReadOnlyCollection<T>? items)
            => items ?? Array.Empty<T>();
    }
}