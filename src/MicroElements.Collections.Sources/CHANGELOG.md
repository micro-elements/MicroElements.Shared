# Changelog

## [1.8.0] - 2023-05-25
- Added: WhereNotNull extension that enumerates only not null values 
- Added: Execute extension that allows to execute an action on each value while enumerating the source enumeration

## [1.7.0] - 2023-02-25
- Added: Cache.GetWeakCache - special cache that attaches to instance with weak reference and can be released by GC after the host object release

## [1.6.0] - 2022-10-09
- Added: MicroElements.Collections.Extensions.WildCard. Methods: IsMatchesWildcard, IncludeByWildcardPatterns, ExcludeByWildcardPatterns
- Changed: Cache.Instance and TwoLayerCache.Instance methods unified and become typesafe. 

## [1.5.0] - 2022-05-28
- Added: Cache.Instance - global static cache instance

## [1.4.0] - 2022-05-15
- Added: TwoLayerCache.Instance - global static cache instance

## [1.3.0] - 2022-01-12
- Changed: Fixed PureAttribute ambiguity in NotNull extension when JetBrains.Annotations referenced

## [1.2.0] - 2021-12-10
- Added: Materialize extensions
- Changed: TwoLayerCache: Interlocked increments for cache metrics
- Changed: MicroElements.CodeContracts.Sources bumped to 1.1.0

## [1.1.0] - 2021-10-17
- Added: Iterate extensions
- Added: TwoLayerCache
- Changed: Namespaces granularity on file level.

## [1.0.0] - 2021-10-14
- Added: NotNull extensions

Full release notes can be found at: https://github.com/micro-elements/MicroElements.Shared/blob/master/src/MicroElements.Collections.Sources/CHANGELOG.md
