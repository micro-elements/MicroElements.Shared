# MicroElements.Shared
Provides microelements shared components as sources. The purpose of this lib is provide most used shared utilitiy code without additional dependencies.

## Statuses
[![License](https://img.shields.io/github/license/micro-elements/MicroElements.Shared.svg)](https://raw.githubusercontent.com/micro-elements/MicroElements.Shared/master/LICENSE)
[![Gitter](https://img.shields.io/gitter/room/micro-elements/MicroElements.Shared.svg)](https://gitter.im/micro-elements/MicroElements.Shared)

## Components

### MicroElements.CodeContracts.Sources

MicroElements source code only package: CodeContracts. Main methods: AssertArgumentNotNull.

[MicroElements.CodeContracts.Sources](https://github.com/micro-elements/MicroElements.Shared/blob/master/src/MicroElements.CodeContracts.Sources/README.md)

[![NuGetVersion](https://img.shields.io/nuget/v/MicroElements.CodeContracts.Sources.svg)](https://www.nuget.org/packages/MicroElements.CodeContracts.Sources)
![NuGetDownloads](https://img.shields.io/nuget/dt/MicroElements.CodeContracts.Sources.svg)

### MicroElements.Collections.Sources

MicroElements source only package: Collection extensions: NotNull, Iterate. Special collections: TwoLayerCache.

[MicroElements.Collections.Sources](https://github.com/micro-elements/MicroElements.Shared/blob/master/src/MicroElements.Collections.Sources/README.md)

[![NuGetVersion](https://img.shields.io/nuget/v/MicroElements.Collections.Sources.svg)](https://www.nuget.org/packages/MicroElements.Collections.Sources)
![NuGetDownloads](https://img.shields.io/nuget/dt/MicroElements.Collections.Sources.svg)

### MicroElements.Formatting.Sources

MicroElements source only package: Formatting. Main methods: FormatValue, FormatAsTuple.

[MicroElements.Formatting.Sources](https://github.com/micro-elements/MicroElements.Shared/blob/master/src/MicroElements.Formatting.Sources/README.md)

[![NuGetVersion](https://img.shields.io/nuget/v/MicroElements.Formatting.Sources.svg)](https://www.nuget.org/packages/MicroElements.Formatting.Sources)
![NuGetDownloads](https://img.shields.io/nuget/dt/MicroElements.Formatting.Sources.svg)

### MicroElements.IsExternalInit

MicroElements source code only package: IsExternalInit. Record support for dotnet versions before .NET 5.0.

[MicroElements.IsExternalInit](https://github.com/micro-elements/MicroElements.Shared/blob/master/src/MicroElements.IsExternalInit/README.md)

[![NuGetVersion](https://img.shields.io/nuget/v/MicroElements.IsExternalInit.svg)](https://www.nuget.org/packages/MicroElements.IsExternalInit)
![NuGetDownloads](https://img.shields.io/nuget/dt/MicroElements.IsExternalInit.svg)

### MicroElements.Reflection.Sources

MicroElements source only package: Reflection. Classes: TypeExtensions, TypeCheck, ObjectExtensions, Expressions, CodeCompiler.

[MicroElements.Reflection.Sources](https://github.com/micro-elements/MicroElements.Shared/blob/master/src/MicroElements.Reflection.Sources/README.md)

[![NuGetVersion](https://img.shields.io/nuget/v/MicroElements.Reflection.Sources.svg)](https://www.nuget.org/packages/MicroElements.Reflection.Sources)
![NuGetDownloads](https://img.shields.io/nuget/dt/MicroElements.Reflection.Sources.svg)


## Design concepts

- Designed for .Net Core so most libs uses .NetStandard 2.1
- MicroElements uses modern language features (C#9) to reduce code size and improve readability.
- Granular namespaces to reduce collision possibility. You can control usages till class level.
- All source packages stores its code in one root `MicroElements` that keeps project clean.
- MicroElements source code packages can reduce your `common libs` dependencies
- Debugging is easy because you have full sources

## Components

- Trimmed version of JetBrains annotations used in MicroElements packages.
- CodeContracts
- IsExternalInit
- Reflection
- Collections

## License
This project is licensed under the MIT license. See the [LICENSE] file for more info.

[LICENSE]: https://raw.githubusercontent.com/micro-elements/MicroElements.Shared/master/LICENSE