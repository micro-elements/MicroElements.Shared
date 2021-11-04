# MicroElements.Formatting.Sources
MicroElements source only package: Formatting.

Source only package does not forces binary reference on it. Just add package and use as code.

## Description
`FormatValue` formats numbers with invariant culture, date types in ISO formats, for collections uses `FormatAsTuple`

`FormatAsTuple` formats collections using arguments:
- `separator`: The value that uses to separate items. DefaultValue = `", "`
- `nullPlaceholder`: The value that renders if item is `null`. DefaultValue = `"null"` 
- `startSymbol`: Start symbol. DefaultValue = `'('`.
- `endSymbol`: End symbol. DefaultValue = `')'`.
- `formatValue`: Func that formats object value to string representation. By default uses `FormatValue`.
- `maxItems`: The max number of items that will be formatted. By default not limited.
- `maxTextLength`: Max result text length. Used to limit result text size. DefaultValue=`1024`
- `trimmedPlaceholder`: The value that replaces trimmed part of sequence. DefaultValue = `"..."` 

## Usage
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