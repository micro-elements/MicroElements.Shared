# MicroElements.Extensions.Collections.Sources
MicroElements source only package: Collection extensions.

Source only package does not forces binary reference on it. Just add package and use as code.

 ## Usage

 ```csharp
string? GetFirstName(IEnumerable<string>? names)
{
    return names
        .NotNull()
        .FirstOrDefault(name => name is not null)
}
 ```