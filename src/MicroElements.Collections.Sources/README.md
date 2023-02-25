# MicroElements.Collections.Sources

## Summary

MicroElements source only package:
      Collection extensions: NotNull, Iterate, Materialize, IncludeByWildcardPatterns, ExcludeByWildcardPatterns.
      Special collections: TwoLayerCache.

## Extensions

### NotNull
Returns not null (empty) enumeration if input is null.
            
```csharp
string? GetFirstValue(IEnumerable<string>? optionalValues) =>
    optionalValues
        .NotNull()
        .FirstOrDefault();
```

### Iterate
Iterates values and executes action for each value.
It's like `List.ForEach` but works with lazy enumerations and does not forces additional allocations.
            
```csharp
// Iterates values and outputs to console.
Enumerable
    .Range(1, 100_000)
    .Iterate(Console.WriteLine);
```

### Materialize
Materializes source enumeration and allows to see intermediate results without changing execute chain.
MaterializeDebug is the same as Materialize but works only in Debug mode and does not affect performance for Release builds.
            
```csharp
Enumerable
    .Range(1, 10)
    .Materialize(values => { /*set breakpoint here*/ })
    .Iterate(Console.WriteLine);
```

### WildCard
Provides methods for wildcard or glob filtering.
            
See also: https://en.wikipedia.org/wiki/Glob_(programming).
             
```csharp
[Fact]
public void WildcardInclude()
{
    string[] values = { "Microsoft.Extension.Logging", "Microsoft.AspNetCore.Hosting", "FluentAssertions", "System.Collections.Generic" };
    string[] includePatterns = { "Microsoft.AspNetCore.*", "*.Collections.*" };
    string[] result = { "Microsoft.AspNetCore.Hosting", "System.Collections.Generic" };
    values.IncludeByWildcardPatterns(includePatterns).Should().BeEquivalentTo(result);
}
            
[Fact]
public void WildcardExclude()
{
    string[] values = { "Microsoft.Extension.Logging", "Microsoft.AspNetCore.Hosting", "FluentAssertions", "System.Collections.Generic" };
    string[] excludePatterns =  { "Microsoft.AspNetCore.*", "*.Collections.*" };
    string[] result = { "Microsoft.Extension.Logging", "FluentAssertions" };
    values.ExcludeByWildcardPatterns(excludePatterns).Should().BeEquivalentTo(result);
}
```

## Collections

### TwoLayerCache
Represents ThreadSafe cache that holds only limited number of items. Can be used as drop in replacement for `ConcurrentDictionary`.
            
Use it when you need simple cache but with memory limit.
            
Notes:
            
- Cache organized in two layers: hot and cold.
- Items first added to cold cache.
- GetValue first checks hot cache. If value not found in hot cache than cold cache uses for search.
- If value exists in cold cache than item moves to hot cache.
- If hot cache exceeds item limit then hot cache became cold cache and new hot cache creates.

### Cache
Global ambient cache extensions.
Represents `ConcurrentDictionary` of some type that is accessed by it's name.
            
Reason: Use cache from any place of your code without declaring cache (that's so boring and noisy).
Best suited for global caches of immutable objects that never changes in application lifecycle.

#### Usage

```csharp
// Static named cache
var value = Cache
    .Instance<string, string>("Example")
    .GetOrAdd("key1", k => VeryLongFetch(k));
```

```csharp
// Cache attached to instance
var value = instance
    .GetWeakCache<string, string>(/*optional name*/)
    .GetOrAdd("key1", k => VeryLongFetch(k));
```

#### Notes
- Cache instance is global so use proper cache instance names.
- For static cache use some const name for example class name or method name
- Can cause to memory leak if cache grows permanently. For such cases use caches that clears by time or size for example `TwoLayerCache`
- Adds one more operation to find cache instance
- It can be treated as some kind of an "AMBIENT CONTEXT" that becomes an anti-pattern. See: https://freecontent.manning.com/the-ambient-context-anti-pattern/
