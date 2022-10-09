#region License

// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#endregion
#region Supressions

#pragma warning disable
// ReSharper disable CheckNamespace

#endregion

namespace MicroElements.Collections.Extensions.WildCard
{
    using JetBrains.Annotations;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using TwoLayerCache;
    
    /// <readme id="WildCard">
    /// <![CDATA[
    /// ### WildCard
    /// Provides methods for wildcard or glob filtering.
    ///
    /// See also: https://en.wikipedia.org/wiki/Glob_(programming).
    /// 
    /// ```csharp
    /// [Fact]
    /// public void WildcardInclude()
    /// {
    ///     string[] values = { "Microsoft.Extension.Logging", "Microsoft.AspNetCore.Hosting", "FluentAssertions", "System.Collections.Generic" };
    ///     string[] includePatterns = { "Microsoft.AspNetCore.*", "*.Collections.*" };
    ///     string[] result = { "Microsoft.AspNetCore.Hosting", "System.Collections.Generic" };
    ///     values.IncludeByWildcardPatterns(includePatterns).Should().BeEquivalentTo(result);
    /// }
    ///
    /// [Fact]
    /// public void WildcardExclude()
    /// {
    ///     string[] values = { "Microsoft.Extension.Logging", "Microsoft.AspNetCore.Hosting", "FluentAssertions", "System.Collections.Generic" };
    ///     string[] excludePatterns =  { "Microsoft.AspNetCore.*", "*.Collections.*" };
    ///     string[] result = { "Microsoft.Extension.Logging", "FluentAssertions" };
    ///     values.ExcludeByWildcardPatterns(excludePatterns).Should().BeEquivalentTo(result);
    /// }
    /// ```
    /// ]]>
    /// </readme>
    internal static class WildCardExtensions
    {
        private static readonly TwoLayerCache<string, WildcardInfo> _wildcardInfo = TwoLayerCache.Instance<string, WildcardInfo>();
        
        /// <summary>
        /// Converts given string containing wildcards (* or ?) to its corresponding regular expression.
        /// </summary>
        /// <param name="pattern">wildcard pattern.</param>
        /// <returns>Regex pattern.</returns>
        public static string WildcardToRegex(this string pattern) => "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";

        /// <summary> Contains regex pattern and wildcard symbols count. </summary>
        public record WildcardInfo(string RegexPattern, int WildcardCount);
        
        /// <summary> Gets <see cref="WildcardInfo"/> for <paramref name="pattern"/>. </summary>
        public static WildcardInfo GetWildcardInfo(this string pattern)
        {
            var wildcardCount = pattern.Count(x => x is '*' or '?');
            if (wildcardCount > 0) 
                return new(pattern.WildcardToRegex(), wildcardCount);
            return new WildcardInfo(pattern, wildcardCount);
        }

        /// <summary> Gets <see cref="WildcardInfo"/> for <paramref name="pattern"/>. Uses internal cache. </summary>
        public static WildcardInfo GetWildcardInfoCached(this string pattern) => _wildcardInfo.GetOrAdd(pattern, p => p.GetWildcardInfo());
        
        /// <summary>
        /// Returns true if the <paramref name="value"/> is matches <paramref name="pattern"/>.
        /// </summary>
        /// <param name="value">The value to check against the pattern.</param>
        /// <param name="pattern">Pattern to check.</param>
        /// <returns><see langword="true"/> if matches.</returns>
        public static bool IsMatchesWildcard(this string value, string pattern)
        {
            if (value == pattern)
                return true;

            // Get cached regex pattern and additional info
            var wildcardInfo = pattern.GetWildcardInfoCached();

            // The most ordinary case when pattern ends with '*'. Example: 'Microsoft.AspNet.*'
            if (wildcardInfo.WildcardCount == 1 && pattern[^1] == '*')
                return value.AsSpan().StartsWith(pattern.AsSpan(0, pattern.Length - 1));
            
            // Note: Regex.IsMatch uses internal RegexCache for pattern
            return Regex.IsMatch(value, wildcardInfo.RegexPattern);
        }

        /// <summary>
        /// Filters a sequence of values based on include wildcard patterns.
        /// <para>Includes values that matches at least one include pattern.</para>
        /// </summary>
        /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to filter.</param>
        /// <param name="includePatterns">Include patterns in wildcard form.</param>
        /// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains elements from the input sequence that satisfy the include patterns.</returns>
        [LinqTunnel]
        public static IEnumerable<string> IncludeByWildcardPatterns(this IEnumerable<string> source, IReadOnlyCollection<string>? includePatterns = null)
        {
            if (includePatterns == null)
                return source;
            return source.Where(value => includePatterns.Any(includePattern => value.IsMatchesWildcard(includePattern)));
        }

        /// <summary>
        /// Filters a sequence of values based on exclude wildcard patterns.
        /// <para>Excludes values that matches at least one exclude pattern.</para>
        /// </summary>
        /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to filter.</param>
        /// <param name="excludePatterns">Exclude patterns in wildcard form.</param>
        /// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains elements from the input sequence that satisfy the include patterns.</returns>
        [LinqTunnel]
        public static IEnumerable<string> ExcludeByWildcardPatterns(this IEnumerable<string> values, IReadOnlyCollection<string>? excludePatterns = null)
        {
            if (excludePatterns == null)
                return values;
            return values.Where(value => excludePatterns.All(excludePattern => !value.IsMatchesWildcard(excludePattern)));
        }
    }
}