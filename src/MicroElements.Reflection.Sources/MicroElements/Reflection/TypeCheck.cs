using MicroElements.Reflection.TypeExtensions;

#pragma warning disable
// ReSharper disable once CheckNamespace

namespace MicroElements.Reflection.TypeCheck
{
    /// <summary>
    /// Represents cached type checks.
    /// </summary>
    /// <typeparam name="T">Type.</typeparam>
    internal static partial class TypeCheck<T>
    {
        /// <summary>
        /// Gets a value indicating whether the type specified by the generic argument is a reference type.
        /// </summary>
        public static bool IsReferenceType { get; }

        /// <summary>
        /// Gets a value indicating whether the type specified by the generic argument is a nullable struct.
        /// </summary>
        public static bool IsNullableStruct { get; }

        static TypeCheck()
        {
            IsNullableStruct = typeof(T).IsNullableStruct();
            IsReferenceType = typeof(T).IsReferenceType();
        }
    }
}