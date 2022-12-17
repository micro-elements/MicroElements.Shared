#region License
// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#endregion
#region Supressions
#pragma warning disable
// ReSharper disable All
#endregion

namespace MicroElements.Reflection.FriendlyName
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using MicroElements.Collections.Extensions.NotNull;
    using MicroElements.Text.StringFormatter;
    using MicroElements.Reflection.TypeCaching;
    
    /// <summary>
    /// FriendlyName extensions.
    /// </summary>
    internal static partial class FriendlyName
    {
        /// <summary> Global type name cache. </summary>
        private static readonly ConcurrentDictionary<Type, string> _friendlyNameCache = new();

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

        /// <summary>
        /// Gets func that gets standard type alias.
        /// </summary>
        public static readonly Func<Type, string> GetStandardAliasFunc = type => StandardTypeAliases.GetValueOrDefault(type);

        /// <summary id="FriendlyName">
        /// <![CDATA[
        /// ### FriendlyName
        /// Gets friendly (human readable) name for the type.
        ///
        /// Usage
        /// ```
        /// // Without GetFriendlyName
        /// typeof(List<ValueTuple<int?, string>?>).Name.Should().Be("List`1");
        /// typeof(List<ValueTuple<int?, string>?>).FullName.Should().StartWith("System.Collections.Generic.List`1[[System.Nullable`1[[System.ValueTuple`2[[System.Nullable`1[[System.Int32, System.Private.CoreLib");
        /// // With GetFriendlyName
        /// typeof(List<(int Key, string Value)>).GetFriendlyName().Should().Be("List<ValueTuple<int, string>>");
        /// ```
        /// 
        /// Notes:
        /// - For for standard and primitive types uses aliases: `string, object, bool, byte, char, decimal, double, short, int, long, sbyte, float, ushort, uint, ulong, void`. Example: `Int32` -> `int`.
        /// - You can replace standard aliases with `typeAliases` param
        /// - For generic types uses angle brackets: `List'1` -> `List<int>`
        /// - For array types uses square brackets: `Int32[]` -> `int[]`
        /// - For `Nullable` value types adds `?` at the end
        /// - Uses recursion. Example: `List<Tuple<int, string>>`
        /// - Uses cache: every name builds only once. You can use uncached `BuildFriendlyName`.
        /// - ThreadSafe: true
        /// ]]>
        /// </summary>
        /// <param name="type">The type to search for.</param>
        /// <param name="getName">Optional func that gets type alias for the type.</param>
        /// <returns>Human readable type name.</returns>
        public static string GetFriendlyName(this Type type, Func<Type, string?>? getName = null)
            => _friendlyNameCache.GetOrAdd(type, BuildFriendlyName(type, getName));
        
        /// <summary>
        /// Builds friendly (human readable) name for the type.
        /// For full description see <see cref="GetFriendlyName"/>.
        /// </summary>
        /// <param name="type">The type to search for.</param>
        /// <param name="getName">Optional func that gets type alias for the type.</param>
        /// <returns>Human readable type name.</returns>
        public static string BuildFriendlyName(this Type type, Func<Type, string?>? getName = null)
        {
           getName ??= GetStandardAliasFunc;

            if (getName(type) is {} stdAlias)
                return stdAlias;

            var friendlyName = type.Name;
            if (type.IsGenericType)
            {
                var startSymbol = "<";
                var endSymbol = ">";

                if (type.GetTypeInfo().IsValueType && Nullable.GetUnderlyingType(type) != null)
                {
                    friendlyName = string.Empty;
                    startSymbol = string.Empty;
                    endSymbol = "?";
                }

                if (friendlyName.IndexOf('`') is var backtick and > 0)
                    friendlyName = friendlyName.Remove(backtick);

                friendlyName += startSymbol;

                Type[] typeParameters = type.GetGenericArguments();
                for (int i = 0; i < typeParameters.Length; i++)
                {
                    string typeParamName = typeParameters[i].GetFriendlyName(getName);
                    friendlyName += i == 0 ? typeParamName : ", " + typeParamName;
                }

                friendlyName += endSymbol;
            }

            if (type.IsArray && type.GetElementType() is {} elementType)
                return elementType.GetFriendlyName(getName) + "[]";
            
            return friendlyName;
        }
    }
    
    /// <summary>
    /// FriendlyName parser extensions.
    /// </summary>
    internal static class FriendlyNameParser
    {
        private static readonly ITypeCache FriendlyNameCache = new LazyTypeCache(() => new TypeCache(typeAliases: FriendlyName.StandardTypeAliases).WithParent(TypeCache.AppDomainTypesUpdatable));

        /// <summary>
        /// Gets type by friendly name.
        /// </summary>
        /// <param name="typeName">Friendly type name.</param>
        /// <param name="getType">Optional func that gets type by name.</param>
        /// <param name="addType">Optional func that adds type and it's name. It's for caching results.</param>
        /// <returns>Type for friendly name.</returns>
        internal static Type? ParseFriendlyName(this string typeName,
            Func<string, Type?>? getType = null,
            Action<Type, string>? addType = null)
        {
            getType ??= FriendlyNameCache.GetType;
            addType ??= FriendlyNameCache.AddType;
            
            var result = getType(typeName);
            if (result != null)
                return result;
            
            var typeNode = new TextNode(typeName);
            var parsed = typeNode.ParseType();
            var built = parsed.BuildType(getType);
            result = built.Type;

            if (result != null && addType != null)
                addType(result, typeName);

            return result;
        }
        
        #region Parsing

        internal interface ITypeNode
        {
            string TypeName { get; }
            
            Type? Type { get; }
            
            IReadOnlyCollection<ITypeNode>? Children { get; }

            void AddChild(ITypeNode node);

            void Build(Func<string, Type?> getType);
        }
        
        internal abstract class TypeNode : ITypeNode
        {
            public string TypeName { get; }

            protected TypeNode(string typeName) => TypeName = typeName;
            
            public override string ToString() => $"{(string?)GetType().Name.Replace("Node", string.Empty)}(\"{TypeName}\")".AppendChildren(Children);
            
            public Type? Type { get; protected set; }
            
            public IReadOnlyCollection<ITypeNode>? Children { get; private set; }
            
            public void AddChild(ITypeNode node) => Children = Children.NotNull().Append(node).ToArray();
            
            public virtual void Build(Func<string, Type?> getType)
            {
                Type = Children?.Where(node => node.Type != null).Select(node => node.Type).FirstOrDefault() ?? getType(TypeName);
            }
        }
        
        internal sealed class TextNode : TypeNode
        {
            public TextNode(string typeName) : base(typeName) { }
        }
        
        internal sealed class NullableNode : TypeNode
        {
            public NullableNode(string typeName) : base(typeName) { }
            
            public override void Build(Func<string, Type?> getType)
            {
                var typeToWrap = getType(TypeName);
                typeToWrap ??= Children.NotNull().Where(node => node.Type != null).Select(node => node.Type).FirstOrDefault();
                
                var nullable = (Type)typeof(Nullable<>).MakeGenericType(typeToWrap);
                Type = nullable;
            }
        }
        
        internal sealed class ArrayNode : TypeNode
        {
            public ArrayNode(string typeName) : base(typeName) { }
            
            public override void Build(Func<string, Type?> getType)
            {
                var typeToWrap = getType(TypeName);
                typeToWrap ??= Children.NotNull().Where(node => node.Type != null).Select(node => node.Type).FirstOrDefault();
                
                var arrayType = typeToWrap.MakeArrayType();
                Type = arrayType;
            }
        }
        
        internal sealed class GenericTypeNode : TypeNode
        {
            public GenericTypeNode(string genericTypeName) : base(genericTypeName) { }

            public override string ToString() => $"Generic({TypeName},{Children.NotNull().Select(node => node.TypeName).FormatAsTuple(startSymbol: string.Empty, endSymbol: string.Empty)})".AppendChildren(Children);

            public override void Build(Func<string, Type?> getType)
            {
                var argsLength = Children?.Count ?? 1;
                var genericTypeNameReal = $"{TypeName}`{argsLength}";
                var argTypes = Children.NotNull().Select(node => node.Type).ToArray();

                var openGenericType = getType(genericTypeNameReal);
                var genericType = openGenericType.MakeGenericType(argTypes);
               
                Type = genericType;
            }
        }
        
        internal static string AppendChildren(this string text, IReadOnlyCollection<ITypeNode>? children) 
            => children != null ? $"{text}->{children.FormatAsTuple()}" : text;
        
        internal static ITypeNode ParseType(this ITypeNode typeNode)
        {
            var typeName = typeNode.TypeName;

            #region Nullable

            if (typeName[^1] == '?')
            {
                var innerTypeName = typeName.Substring(0, typeName.Length - 1);
                var nullableNode = new NullableNode(innerTypeName).ParseType();
                typeNode.AddChild(nullableNode);
                return typeNode;
            }

            #endregion

            #region Array

            if (typeName[^2] == '[' && typeName[^1] == ']')
            {
                var innerTypeName = typeName.Substring(0, typeName.Length - 2);
                var arrayNode = new ArrayNode(innerTypeName).ParseType();
                typeNode.AddChild(arrayNode);
                return typeNode;
            }

            #endregion

            #region Generic

            bool isError = false;
            for (int i = 0; i < typeName.Length; i++)
            {
                var c = typeName[i];
                if (c == '<')
                {
                    int? iEnd = null;
                    for (int i1 = typeName.Length - 1; i1 > i; i1--)
                    {
                        if (typeName[i1] == '>')
                        {
                            iEnd = i1;
                            break;
                        }
                    }

                    if (iEnd == null)
                    {
                        isError = true;
                        break;
                    }

                    string genericTypeName = typeName.Substring(0, i);
                    var genericNode = new GenericTypeNode(genericTypeName);
                    
                    string typeArgsValue = typeName.Substring(i + 1, iEnd.Value - i - 1);
                    bool isGeneric = IsGeneric(typeArgsValue);
                    
                    if (isGeneric)
                    {
                        var argNode = new TextNode(typeArgsValue).ParseType();
                        
                        if (argNode.Children?.Count > 1)
                        {
                            foreach (var child in argNode.Children)
                            {
                                genericNode.AddChild(child);
                            }
                        }
                        else
                        {
                            genericNode.AddChild(argNode);
                        }
                    }
                    else
                    {
                        var typeArgs = typeArgsValue.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        
                        foreach (var typeArg in typeArgs)
                        {
                            var type = new TextNode(typeArg.Trim()).ParseType();
                            genericNode.AddChild(type);
                        }
                    }
                    
                    typeNode.AddChild(genericNode);

                    if (iEnd.Value+1 < typeName.Length)
                    {
                        int iNext = iEnd.Value + 1;
                        bool hasNext = false;
                        for (; iNext < typeName.Length; iNext++)
                        {
                            if (char.IsWhiteSpace(typeName[iNext]))
                                continue;
                            
                            if (typeName[iNext] == ',')
                            {
                                hasNext = true;
                            }

                            if (hasNext && char.IsLetter(typeName[iNext]))
                            {
                                break;
                            }
                        }

                        if (hasNext && iNext < typeName.Length)
                        {
                            var substring = typeName.Substring(iNext);
                            var next = new TextNode(substring).ParseType();
                            typeNode.AddChild(next);
                        }
                    }

                    return typeNode;
                }
            }

            static bool IsGeneric(string typeName)
            {
                foreach (var c in typeName)
                {
                    if (c == '<')
                        return true;
                    if (c == ',')
                        return false;
                }

                return false;
            }
            
            #endregion
            
            return typeNode;
        }
        
        internal static ITypeNode BuildType(this ITypeNode typeNode, Func<string, Type?> getType)
        {
            if (typeNode.Children != null)
            {
                foreach (var child in typeNode.Children)
                {
                    child.BuildType(getType);
                }
            }

            typeNode.Build(getType);

            return typeNode;
        }
        
        #endregion       
    }
    
    /// <summary>
    /// Interoperation with other types.
    /// </summary>
    internal static class TypeCacheExtensions
    {
        public static string GetFriendlyName(this Type type, IReadOnlyDictionary<Type, string>? typeAliases)
        {
            return type.GetFriendlyName(GetAliasForTypeFunc(typeAliases));

            static Func<Type, string?> GetAliasForTypeFunc(IReadOnlyDictionary<Type, string>? typeAliases)
            {
                return typeAliases == null ? FriendlyName.GetStandardAliasFunc : typeAliases.GetValueOrDefault;
            }
        }

        public static string GetFriendlyName(this Type type, ITypeCache typeCache)
            => type.GetFriendlyName(typeCache.GetName);

        internal static Type? ParseFriendlyName(this string typeName, ITypeCache typeCache)
            => typeName.ParseFriendlyName(getType: typeCache.GetType, addType: typeCache.AddType);
    }
}