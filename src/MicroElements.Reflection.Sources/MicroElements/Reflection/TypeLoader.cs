#region License
// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#endregion
#region Supressions
#pragma warning disable
// ReSharper disable CheckNamespace
#endregion

namespace MicroElements.Reflection
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using MicroElements.CodeContracts;

    /// <summary>
    /// Reflection utils.
    /// </summary>
    internal static partial class TypeLoader
    {
        /// <summary>
        /// Gets types according <see cref="AssemblySource"/> and <see cref="TypeFilters"/>.
        /// For more detailed information see <see cref="LoadAssemblies"/> and <see cref="GetTypes"/>
        /// </summary>
        /// <param name="assemblySource">The assembly source.</param>
        /// <param name="typeFilters">The type filters.</param>
        /// <param name="messages">Collection for output messages.</param>
        /// <returns></returns>
        public static IReadOnlyCollection<Type> LoadTypes(
            this AssemblySource assemblySource,
            TypeFilters typeFilters,
            ICollection<string>? messages = null)
        {
            var types = assemblySource
                .LoadAssemblies(messages)
                .GetTypes(typeFilters, messages);
            return types;
        }
        
        /// <summary>
        /// Loads assemblies according <paramref name="assemblySource"/>.
        /// 1. Gets all assemblies from <see cref="AppDomain.CurrentDomain"/> if <see cref="AssemblySource.LoadFromDomain"/> is true.
        /// 2. Applies filters <see cref="AssemblyFilters.IncludePatterns"/> and <see cref="AssemblyFilters.ExcludePatterns"/>.
        /// 3. Optionally loads assemblies from <see cref="AssemblySource.LoadFromDirectory"/> with the same filters.
        /// </summary>
        /// <param name="assemblySource">Filters for getting and filtering assembly list.</param>
        /// <param name="messages">Message list for diagnostic messages.</param>
        /// <returns>Assemblies.</returns>
        public static IEnumerable<Assembly> LoadAssemblies(
            this AssemblySource assemblySource,
            ICollection<string>? messages = null)
        {
            assemblySource.AssertArgumentNotNull(nameof(assemblySource));

            IEnumerable<Assembly> assemblies = Array.Empty<Assembly>();
            
            if (assemblySource.LoadFromDomain)
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            if (assemblySource.LoadFromDirectory != null)
            {
                if (!Directory.Exists(assemblySource.LoadFromDirectory))
                    throw new DirectoryNotFoundException($"Assembly ScanDirectory {assemblySource.LoadFromDirectory} is not exists.");

                var searchPatterns = assemblySource.SearchPatterns ?? new[] { "*.dll" };
                var assembliesFromDirectory =
                    searchPatterns
                        .SelectMany(filePattern => Directory.EnumerateFiles(assemblySource.LoadFromDirectory, filePattern, SearchOption.TopDirectoryOnly))
                        .IncludeByPatterns(fileName => fileName, assemblySource.IncludePatterns)
                        .ExcludeByPatterns(fileName => fileName, assemblySource.ExcludePatterns)
                        .Select(assemblyFile => TryLoadAssemblyFrom(assemblyFile, messages)!)
                        .Where(assembly => assembly != null);

                assemblies = assemblies.Concat(assembliesFromDirectory);
            }

            if (assemblySource.Assemblies is { Count: > 0 })
            {
                assemblies = assemblies.Concat(assemblySource.Assemblies);
            }
            
            assemblies = assemblies
                .IncludeByPatterns(assembly => assembly.FullName, assemblySource.IncludePatterns)
                .ExcludeByPatterns(assembly => assembly.FullName, assemblySource.ExcludePatterns);

            assemblies = assemblies.Distinct();

            return assemblies;
        }
        
        /// <summary>
        /// Gets types from assembly list according type filters.
        /// </summary>
        /// <param name="assemblies">Assembly list.</param>
        /// <param name="typeFilters">Type filters.</param>
        /// <param name="messages">Message list for diagnostic messages.</param>
        /// <returns>Types that matches filters.</returns>
        public static IReadOnlyCollection<Type> GetTypes(
            this IEnumerable<Assembly> assemblies,
            TypeFilters typeFilters,
            ICollection<string>? messages = null)
        {
            assemblies.AssertArgumentNotNull(nameof(assemblies));

            var types = assemblies
                .SelectMany(assembly => assembly.GetDefinedTypesSafe(messages))
                .Where(type => type.FullName != null)
                .Where(type => type.IsPublic == typeFilters.IsPublic)
                .IncludeByPatterns(type => type.FullName, typeFilters.FullNameIncludes)
                .ExcludeByPatterns(type => type.FullName, typeFilters.FullNameExcludes)
                .ToArray();

            return types;
        }

        /// <summary>
        /// Safely returns the set of loadable types from an assembly.
        /// </summary>
        /// <param name="assembly">The <see cref="T:System.Reflection.Assembly" /> from which to load types.</param>
        /// <param name="messages">Message list for diagnostic messages.</param>
        /// <returns>
        /// The set of types from the <paramref name="assembly" />, or the subset
        /// of types that could be loaded if there was any error.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// Thrown if <paramref name="assembly" /> is <see langword="null" />.
        /// </exception>
        public static IEnumerable<Type> GetDefinedTypesSafe(this Assembly assembly, ICollection<string>? messages = null)
        {
            assembly.AssertArgumentNotNull(nameof(assembly));

            try
            {
                return assembly.DefinedTypes.Select(t => t.AsType());
            }
            catch (ReflectionTypeLoadException ex)
            {
                if (messages != null)
                {
                    foreach (Exception loaderException in ex.LoaderExceptions)
                    {
                        messages.Add(loaderException.Message);
                    }
                }

                return ex.Types.Where(t => t != null);
            }
        }

        /// <summary>
        /// Tries to load assembly from file.
        /// </summary>
        /// <param name="assemblyFile">The name or path of the file that contains the manifest of the assembly.</param>
        /// <param name="messages">Message list for diagnostic messages.</param>
        /// <returns>Assembly or null if error occurred.</returns>
        public static Assembly? TryLoadAssemblyFrom(string assemblyFile, ICollection<string>? messages = null)
        {
            try
            {
                return Assembly.LoadFrom(assemblyFile);
            }
            catch (Exception e)
            {
                messages?.Add($"Error on load assembly {assemblyFile}. Message: {e.Message}");
                return null;
            }
        }
    }
    
    /// <summary>
    /// Assembly source.
    /// </summary>
    internal class AssemblySource
    {
        /// <summary>
        /// Gets an empty assembly source. No assemblies, no filters.
        /// </summary>
        public static AssemblySource Empty { get; } = new (
            loadFromDomain: false,
            loadFromDirectory: null);

        /// <summary>
        /// All assemblies from AppDomain.
        /// </summary>
        public static AssemblySource AppDomain { get; } = new (
            loadFromDomain: true,
            loadFromDirectory: null,
            filterByTypeFilters: true);

        /// <summary> Load assemblies from <see cref="System.AppDomain.CurrentDomain"/>. </summary>
        public bool LoadFromDomain { get; set; }

        /// <summary> Optional load assemblies from provided directory. </summary>
        public string? LoadFromDirectory { get; set; }

        /// <summary>
        /// Optional file patterns for loading from directory.
        /// </summary>
        public IReadOnlyCollection<string>? SearchPatterns { get; set; }

        /// <summary>
        /// <see cref="Assembly.FullName"/> wildcard include patterns.
        /// <example>MyCompany.*</example>
        /// </summary>
        public IReadOnlyCollection<string>? IncludePatterns { get; set; } = null;

        /// <summary>
        /// <see cref="Assembly.FullName"/> wildcard exclude patterns.
        /// <example>System.*</example>
        /// </summary>
        public IReadOnlyCollection<string>? ExcludePatterns { get; set; } = null;

        /// <summary>
        /// Take user provided assemblies.
        /// </summary>
        public IReadOnlyCollection<Assembly>? Assemblies { get; set; }

        /// <summary>
        /// Filter assemblies after type filtering and take only assemblies that owns filtered types.
        /// </summary>
        public bool FilterByTypeFilters { get; set; } = true;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblySource"/> class.
        /// </summary>
        /// <param name="loadFromDomain">Optional load assemblies from <see cref="System.AppDomain.CurrentDomain"/>.</param>
        /// <param name="loadFromDirectory">Optional load assemblies from provided directory.</param>
        /// <param name="searchPatterns">Optional file patterns for loading from directory.</param>
        /// <param name="assemblyFilters">Optional assembly filters.</param>
        /// <param name="assemblies">User provided assemblies.</param>
        /// <param name="filterByTypeFilters">Filter assemblies after type filtering and take only assemblies that owns filtered types.</param>
        public AssemblySource(
            bool loadFromDomain = false,
            string? loadFromDirectory = null,
            IReadOnlyCollection<string>? searchPatterns = null,
            IReadOnlyCollection<Assembly>? assemblies = null,
            bool filterByTypeFilters = true)
        {
            LoadFromDomain = loadFromDomain;
            
            LoadFromDirectory = loadFromDirectory;
            SearchPatterns = searchPatterns;
            
            Assemblies = assemblies;
            FilterByTypeFilters = filterByTypeFilters;
        }
    }

    /// <summary>
    /// Type filters.
    /// </summary>
    internal class TypeFilters
    {
        /// <summary>
        /// All public types excluding anonymous.
        /// </summary>
        public static TypeFilters AllPublicTypes { get; } = new ()
        {
            IsPublic = true,
            FullNameExcludes = new[] { "<*" }
        };
        
        /// <summary> Include only public types. </summary>
        public bool IsPublic { get; set; }

        /// <summary> Include types that <see cref="Type.FullName"/> matches filters. </summary>
        public IReadOnlyCollection<string>? FullNameIncludes { get; set; }

        /// <summary> Exclude types that <see cref="Type.FullName"/> matches filters. </summary>
        public IReadOnlyCollection<string>? FullNameExcludes { get; set; }
    }
    
    /// <summary>
    /// Provides methods for filtering.
    /// </summary>
    internal static class Filtering
    {
        internal static string WildcardToRegex(string pat) => "^" + Regex.Escape(pat).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";

        internal static bool FileNameMatchesPattern(string filename, string pattern) => Regex.IsMatch(Path.GetFileName(filename) ?? string.Empty, WildcardToRegex(pattern));

        internal static IEnumerable<T> IncludeByPatterns<T>(this IEnumerable<T> values, Func<T, string> filterComponent, IReadOnlyCollection<string>? includePatterns = null)
        {
            if (includePatterns == null)
                return values;
            return values.Where(value => includePatterns.Any(pattern => FileNameMatchesPattern(filterComponent(value), pattern)));
        }

        internal static IEnumerable<T> ExcludeByPatterns<T>(this IEnumerable<T> values, Func<T, string> filterComponent, IReadOnlyCollection<string>? excludePatterns = null)
        {
            if (excludePatterns == null)
                return values;
            return values.Where(value => excludePatterns.Any(excludePattern => !FileNameMatchesPattern(filterComponent(value), excludePattern)));
        }
    }
}
