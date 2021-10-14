# MicroElements.Collections.Extensions.Sources
MicroElements source only package: Collection extensions.

Source only package does not forces binary reference on it. Just add package and use as code.

 ## Usage

 ### NotNull
 Returns not null enumeration if input is null.

 ```csharp
string? GetFirstName(IEnumerable<string>? names)
{
    return names
        .NotNull()
        .FirstOrDefault();
}
 ```