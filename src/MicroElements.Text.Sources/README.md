# MicroElements.Text.Sources

## Summary

MicroElements source code only package. Provides extensions for working with text: formatting, hashing, encoding. Commonly Used Extensions: FormatValue, FormatAsTuple, EncodeBase58.

## Extensions

### FormatValue
Provides format function for most used types.
            
#### Rules:
- formats numeric types with invariant culture
- formats date types in ISO formats
- recursively formats collections with `FormatAsTuple` as [value1, value2, ... valueN]
- formats `KeyValuePair<string, object>` and `ValueTuple<string, object>` as `(key: value)`
- for `null` returns provided placeholder

### FormatAsTuple
Formats enumeration of values as tuple: (value1, value2, ...).
             
#### Rules:
- formats numeric types with invariant culture
- formats date types in ISO formats
- recursively formats collections with `FormatAsTuple` as [value1, value2, ... valueN]
- formats `KeyValuePair<string, object>` and `ValueTuple<string, object>` as `(key: value)`
- for `null` returns provided placeholder
             
 #### Arguments
- `separator`: The value that uses to separate items. DefaultValue = `", "`
- `nullPlaceholder`: The value that renders if item is `null`. DefaultValue = `"null"` 
- `startSymbol`: Start symbol. DefaultValue = `'('`.
- `endSymbol`: End symbol. DefaultValue = `')'`.
- `formatValue`: Func that formats object value to string representation. By default uses `FormatValue`.
- `maxItems`: The max number of items that will be formatted. By default not limited.
- `maxTextLength`: Max result text length. Used to limit result text size. DefaultValue=`1024`
- `trimmedPlaceholder`: The value that replaces trimmed part of sequence. DefaultValue = `"..."`
            
#### Usage
 ```csharp
new[] { 1, 2 }.FormatAsTuple().Should().Be("(1, 2)");
new[] { 1.1, 2.5 }.FormatAsTuple().Should().Be("(1.1, 2.5)");
new[] { new DateTime(2021, 06, 22), new DateTime(2021, 06, 22, 13, 52, 49, 123)}
    .FormatAsTuple().Should().Be("(2021-06-22, 2021-06-22T13:52:49)");
Enumerable.Range(1, 100)
    .FormatAsTuple(maxItems: 2)
    .Should().Be("(1, 2, ...)");
Enumerable.Repeat("abcde", 100)
    .FormatAsTuple(maxTextLength: 14)
    .Should().Be("(abcde, ab...)")
    .And.Subject.Length.Should().Be(14);
 ```

## Base58

Base58 encoding provides fast encoding for small amount of data for example hashes.
            
Benefits over Base64 encoding:
- Human readable because excludes similar characters 0OIl that looks the same in some fonts and could be used to create visually identical looking data.
- Does not have line-breaks and special symbols so can be typed easy.
- Double-clicking selects the whole string as one word if it's all alphanumeric.

