# MicroElements.Formatting.Sources
MicroElements source only package: Formatting.

Source only package does not forces binary reference on it. Just add package and use as code.

 ## Usage
 ```csharp
void Foo(string arg)
{
    // Method arguments checks
    arg.AssertArgumentNotNull(nameof(arg));

    // Argument usage
    UseArg(arg);
}

void Bar(string arg)
{
    // Can be used as inplace replacement of UseArg(arg);
    UseArg(arg.AssertArgumentNotNull(nameof(arg)));
}
 ```