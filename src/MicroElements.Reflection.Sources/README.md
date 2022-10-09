# MicroElements.Reflection.Sources

## Summary

MicroElements source only package: Reflection. Classes: TypeExtensions, TypeCheck, ObjectExtensions, Expressions, CodeCompiler, FriendlyName.

## Extensions

### FriendlyName
Gets friendly (human readable) name for the type.
            
Usage
```
// Without GetFriendlyName
typeof(List<ValueTuple<int?, string>?>).Name.Should().Be("List`1");
typeof(List<ValueTuple<int?, string>?>).FullName.Should().StartWith("System.Collections.Generic.List`1[[System.Nullable`1[[System.ValueTuple`2[[System.Nullable`1[[System.Int32, System.Private.CoreLib");
// With GetFriendlyName
typeof(List<(int Key, string Value)>).GetFriendlyName().Should().Be("List<ValueTuple<int, string>>");
```
             
Notes:
- For for standard and primitive types uses aliases: `string, object, bool, byte, char, decimal, double, short, int, long, sbyte, float, ushort, uint, ulong, void`. Example: `Int32` -> `int`.
- You can replace standard aliases with `typeAliases` param
- For generic types uses angle brackets: `List'1` -> `List<int>`
- For array types uses square brackets: `Int32[]` -> `int[]`
- For `Nullable` value types adds `?` at the end
- Uses recursion. Example: `List<Tuple<int, string>>`
- Uses cache: every name builds only once. You can use uncached `BuildFriendlyName`.
- ThreadSafe: true

## Other (Description TBD):
- TypeExtensions
- TypeCheck
- ObjectExtensions
- CodeCompiler
- Expressions
