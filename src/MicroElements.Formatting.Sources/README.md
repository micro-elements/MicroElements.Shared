# MicroElements.Formatting.Sources
MicroElements source only package: Formatting.

Source only package does not forces binary reference on it. Just add package and use as code.

 ## Usage
 ```csharp
new[] { 1, 2 }.FormatAsTuple().Should().Be("(1, 2)");
new[] { 1.1, 2.5 }.FormatAsTuple().Should().Be("(1.1, 2.5)");
new[]
{
    new DateTime(2021, 06, 22),
    new DateTime(2021, 06, 22, 13, 52, 49, 123)
}.FormatAsTuple().Should().Be("(2021-06-22, 2021-06-22T13:52:49)");
 ```