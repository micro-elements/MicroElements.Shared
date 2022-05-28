# MicroElements.Reflection.Sources

## Summary

MicroElements source only package: Reflection. Classes: TypeExtensions, TypeCheck, ObjectExtensions, Expressions, CodeCompiler, FriendlyName.

## Extensions

### FriendlyName
Gets friendly (human readable) name for the type.
            
Notes:
- Gets name for standard and primitive types like 'Int32' -> 'int'. Full list of standard aliases see at `FriendlyName.StandardTypeAliases`. You can replace standard aliases with 'typeAliases' param. 
- Gets name for generic types: List'1 -> List<int>
- Gets name for arrays: `Int32[]` -> `int[]`
- Uses recursion. Example: `List<Tuple<int, string>>`
- Uses cache: every name builds only once. You can use uncached `BuildFriendlyName`.
- ThreadSafe: true

## Other (Description TBD):
- TypeExtensions
- TypeCheck
- ObjectExtensions
- CodeCompiler
- Expressions
