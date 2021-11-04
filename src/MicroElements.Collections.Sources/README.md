# MicroElements.Collections.Sources

## Summary
MicroElements source only package: Collection extensions: NotNull, Iterate. Special collections: TwoLayerCache.

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
