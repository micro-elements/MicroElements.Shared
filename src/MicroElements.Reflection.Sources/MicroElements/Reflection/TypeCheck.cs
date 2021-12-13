﻿#region License
// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#endregion
#region Supressions
#pragma warning disable
// <auto-generated />
// ReSharper disable CheckNamespace
#endregion

namespace MicroElements.Reflection.TypeCheck
{
    using TypeExtensions;

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