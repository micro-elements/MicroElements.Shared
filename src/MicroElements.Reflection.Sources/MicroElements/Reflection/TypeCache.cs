#region License
// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#endregion
#region Supressions
#pragma warning disable
// ReSharper disable All
#endregion

namespace MicroElements.Reflection.TypeCache
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using MicroElements.CodeContracts;
    using MicroElements.Collections.Extensions.Iterate;
    using MicroElements.Collections.Extensions.NotNull;

    /// <summary> Represents type cache abstraction. </summary>
    internal interface ITypeCache
    {
        Type? GetType(string typeName);
        string? GetName(Type type);
        void AddType(Type type, string typeName);
    }

    /// <summary> Type cache with ability to set type aliases. </summary>
    internal class TypeCache : ITypeCache
    {
        private readonly List<Type> _types;
        
        public Lazy<IReadOnlyDictionary<string, Type>> TypesByFullName { get; }
        
        public Lazy<IReadOnlyDictionary<string, Type>> TypesByAlias { get; }
        
        public Lazy<IReadOnlyDictionary<Type, string>> AliasForType { get; }

        public TypeCache(IReadOnlyCollection<Type>? types = null, IReadOnlyDictionary<Type, string>? typeAliases = null)
        {
            IEnumerable<Type> allTypes = types.NotNull();
            
            if (typeAliases != null)
            {
                allTypes = allTypes
                    .Concat(typeAliases.Select(pair => pair.Key))
                    .Distinct();
            }
            
            _types = new List<Type>(allTypes);
            
            TypesByFullName = new Lazy<IReadOnlyDictionary<string, Type>>(() =>
            {
                var typesByFullName = _types
                    .GroupBy(type => type.FullName)
                    .Select(grouping => (FullName: grouping.Key, Type: grouping.First()))
                    .ToDictionary(tuple => tuple.FullName, tuple => tuple.Type);
                return new ConcurrentDictionary<string, Type>(typesByFullName);
            });
            
            TypesByAlias = new Lazy<IReadOnlyDictionary<string, Type>>(() =>
            {
                var typesByAlias = _types
                    .GroupBy(type => type.Name)
                    .Select(grouping => (Alias: grouping.Key, Type: grouping.First()))
                    .ToDictionary(tuple => tuple.Alias, tuple => tuple.Type);
                
                typeAliases
                    .NotNull()
                    .Iterate(pair => typesByAlias[pair.Value] = pair.Key);
                
                return new ConcurrentDictionary<string, Type>(typesByAlias);
            });
            
            AliasForType = new Lazy<IReadOnlyDictionary<Type, string>>(() =>
            {
                var aliasForType = TypesByAlias.Value
                    .Select(pair => (Type: pair.Value, ShortName: pair.Key))
                    .GroupBy(pair => pair.Type)
                    .Select(group => (Type: group.Key, ShortName: group.First().ShortName))
                    .ToDictionary(pair => pair.Type, pair => pair.ShortName);
                
                typeAliases
                    .NotNull()
                    .Iterate(pair => aliasForType[pair.Key] = pair.Value);
                
                return new ConcurrentDictionary<Type, string>(aliasForType);
            });
        }

        public Type? GetType(string typeName)
        {
            if (TypesByAlias.Value.TryGetValue(typeName, out var byAlias))
                return byAlias;
            
            if (TypesByFullName.Value.TryGetValue(typeName, out var byFullName))
                return byFullName;
            
            return null;
        }
        
        public string? GetName(Type type)
        {
            if (AliasForType.Value.TryGetValue(type, out var alias))
                return alias;
            
            return null;
        }

        public void AddType(Type type, string typeName)
        {
            lock (_types)
            {
                if (!_types.Contains(type))
                    _types.Add(type);
            
                var typesByFullName = (ConcurrentDictionary<string, Type>)TypesByFullName.Value;
                typesByFullName[type.FullName] = type;
            
                var typesByAlias = (ConcurrentDictionary<string, Type>)TypesByAlias.Value;
                typesByAlias[typeName] = type;
            
                var aliasForType = (ConcurrentDictionary<Type, string>)AliasForType.Value;
                aliasForType[type] = typeName;
            }
        }
    }

    /// <summary> Type cache that gets value from parent if it was not found in current cache. </summary>
    internal class HierarchicalTypeCache : ITypeCache
    {
        private readonly ITypeCache _parent;
        private readonly ITypeCache _typeCache;

        public HierarchicalTypeCache(ITypeCache parent, ITypeCache typeCache)
        {
            _parent = parent.AssertArgumentNotNull(nameof(parent));
            _typeCache = typeCache.AssertArgumentNotNull(nameof(typeCache));
        }
        
        public Type? GetType(string typeName) => _typeCache.GetType(typeName) ?? _parent.GetType(typeName);
        
        public string? GetName(Type type) => _typeCache.GetName(type) ?? _parent.GetName(type);
        
        public void AddType(Type type, string typeName) => _typeCache.AddType(type, typeName);
    }

    internal static class TypeCacheExtensions
    {
        public static ITypeCache WithParent(this ITypeCache typeCache, ITypeCache parent) =>
            new HierarchicalTypeCache(parent, typeCache);
    }
}