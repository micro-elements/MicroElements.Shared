# MicroElements.Text.Sources

## Summary

MicroElements source code only package. Provides extensions for working with text: formatting, hashing, encoding. Commonly Used Extensions: FormatValue, FormatAsTuple, EncodeBase58.

## Extensions

### FormatValue
Provides format function for most used types.
            
Rules:
- formats numeric types with invariant culture
- formats date types in ISO formats
- recursively formats collections with `FormatAsTuple` as [value1, value2, ... valueN]
- formats `KeyValuePair<string, object>` and `ValueTuple<string, object>` as `(key: value)`
- for `null` returns provided placeholder

### FormatAsTuple
Formats enumeration of values as tuple: (value1, value2, ...).

## Base58

Base58 encoding provides fast encoding for small amount of data for example hashes.
            
Benefits over Base64 encoding:
- Human readable because excludes similar characters 0OIl that looks the same in some fonts and could be used to create visually identical looking data.
- Does not have line-breaks and special symbols so can be typed easy.
- Double-clicking selects the whole string as one word if it's all alphanumeric.

