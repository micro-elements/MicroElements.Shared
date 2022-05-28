#region License
// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#endregion
#region Supressions

//#pragma warning disable
// ReSharper disable CheckNamespace
#endregion

namespace MicroElements.Reflection.FriendlyName
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    internal static partial class FriendlyName
    {
        /// <summary> Global type name cache. </summary>
        private static readonly ConcurrentDictionary<Type, string> _friendlyName = new();

        /// <summary>
        /// Gets standard type aliases: string, object, bool, byte, char, decimal, double, short, int, long, sbyte, float, ushort, uint, ulong, void.
        /// </summary>
        public static readonly IReadOnlyDictionary<Type, string> StandardTypeAliases = new Dictionary<Type, string>()
        {
            { typeof(string), "string" },
            { typeof(object), "object" },
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(char), "char" },
            { typeof(decimal), "decimal" },
            { typeof(double), "double" },
            { typeof(short), "short" },
            { typeof(int), "int" },
            { typeof(long), "long" },
            { typeof(sbyte), "sbyte" },
            { typeof(float), "float" },
            { typeof(ushort), "ushort" },
            { typeof(uint), "uint" },
            { typeof(ulong), "ulong" },
            { typeof(void), "void" }
        };

        /// <summary id="FriendlyName">
        /// <![CDATA[
        /// ### FriendlyName
        /// Gets friendly (human readable) name for the type.
        ///
        /// Notes:
        /// - Gets name for standard and primitive types like 'Int32' -> 'int'. Full list of standard aliases see at `FriendlyName.StandardTypeAliases`. You can replace standard aliases with 'typeAliases' param. 
        /// - Gets name for generic types: List'1 -> List<int>
        /// - Gets name for arrays: `Int32[]` -> `int[]`
        /// - Uses recursion. Example: `List<Tuple<int, string>>`
        /// - Uses cache: every name builds only once. You can use uncached `BuildFriendlyName`.
        /// - ThreadSafe: true
        /// ]]>
        /// </summary>
        /// <param name="type">The type to search for.</param>
        /// <param name="typeAliases">Optional type aliases that replaces default type aliases.</param>
        /// <returns>Human readable type name.</returns>
        public static string GetFriendlyName(this Type type, IReadOnlyDictionary<Type, string>? typeAliases = null) =>
            _friendlyName.GetOrAdd(type, BuildFriendlyName(type, typeAliases));

        /// <summary>
        /// Builds friendly (human readable) name for the type.
        /// </summary>
        /// <param name="type">The type to search for.</param>
        /// <param name="typeAliases">Optional type aliases that replaces default type aliases.</param>
        /// <returns>Human readable type name.</returns>
        public static string BuildFriendlyName(this Type type, IReadOnlyDictionary<Type, string>? typeAliases = null)
        {
            typeAliases ??= StandardTypeAliases;

            if (typeAliases.TryGetValue(type, out var stdAlias))
                return stdAlias;

            var friendlyName = type.Name;
            if (type.IsGenericType)
            {
                if (friendlyName.IndexOf('`') is var backtick and > 0)
                    friendlyName = friendlyName.Remove(backtick);
                
                friendlyName += "<";

                Type[] typeParameters = type.GetGenericArguments();
                for (int i = 0; i < typeParameters.Length; i++)
                {
                    string typeParamName = typeParameters[i].GetFriendlyName(typeAliases);
                    friendlyName += (i == 0 ? typeParamName : ", " + typeParamName);
                }

                friendlyName += ">";
            }

            if (type.IsArray && type.GetElementType() is {} elementType)
                return elementType.GetFriendlyName(typeAliases) + "[]";
            
            return friendlyName;
        }
    }
}