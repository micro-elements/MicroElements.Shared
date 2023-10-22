# MicroElements.Shared
Provides microelements shared components as sources. The purpose of this lib is provide most used shared utilitiy code without additional dependencies.

## Statuses
[![License](https://img.shields.io/github/license/micro-elements/MicroElements.Shared.svg)](https://raw.githubusercontent.com/micro-elements/MicroElements.Shared/master/LICENSE)
[![Gitter](https://img.shields.io/gitter/room/micro-elements/MicroElements.Shared.svg)](https://gitter.im/micro-elements/MicroElements.Shared)

## Design concepts

- Designed for .Net Core so most libs uses .NetStandard 2.1
- MicroElements uses modern language features (C#9) to reduce code size and improve readability.
- Granular namespaces to reduce collision possibility. You can control usages till class level.
- All source packages stores its code in one root `MicroElements` that keeps project clean.
- MicroElements source code packages can reduce your `common libs` dependencies
- Debugging is easy because you have full sources

## Components
___
### MicroElements.CodeContracts.Sources
|   |   |
--- | ---
Name | MicroElements.CodeContracts.Sources
Description | MicroElements source code only package: CodeContracts. Main methods: AssertArgumentNotNull.
Github | [https://github.com/micro-elements/MicroElements.Shared/tree/master/src/MicroElements.CodeContracts.Sources](https://github.com/micro-elements/MicroElements.Shared/tree/master/src/MicroElements.CodeContracts.Sources)
Status | [![NuGetVersion](https://img.shields.io/nuget/v/MicroElements.CodeContracts.Sources.svg)](https://www.nuget.org/packages/MicroElements.CodeContracts.Sources) ![NuGetDownloads](https://img.shields.io/nuget/dt/MicroElements.CodeContracts.Sources.svg)

___
### MicroElements.IsExternalInit
|   |   |
--- | ---
Name | MicroElements.IsExternalInit
Description | MicroElements source code only package: IsExternalInit. Record support for dotnet versions before .NET 5.0.
Github | [https://github.com/micro-elements/MicroElements.Shared/tree/master/src/MicroElements.IsExternalInit](https://github.com/micro-elements/MicroElements.Shared/tree/master/src/MicroElements.IsExternalInit)
Status | [![NuGetVersion](https://img.shields.io/nuget/v/MicroElements.IsExternalInit.svg)](https://www.nuget.org/packages/MicroElements.IsExternalInit) ![NuGetDownloads](https://img.shields.io/nuget/dt/MicroElements.IsExternalInit.svg)

___
### MicroElements.Collections.Sources
|   |   |
--- | ---
Name | MicroElements.Collections.Sources
Description | MicroElements source only package:
      Collection extensions: NotNull, Iterate, Execute, WhereNotNull, Materialize, IncludeByWildcardPatterns, ExcludeByWildcardPatterns.
      Special collections: Cache, TwoLayerCache, PollingCache.
Github | [https://github.com/micro-elements/MicroElements.Shared/tree/master/src/MicroElements.Collections.Sources](https://github.com/micro-elements/MicroElements.Shared/tree/master/src/MicroElements.Collections.Sources)
Status | [![NuGetVersion](https://img.shields.io/nuget/v/MicroElements.Collections.Sources.svg)](https://www.nuget.org/packages/MicroElements.Collections.Sources) ![NuGetDownloads](https://img.shields.io/nuget/dt/MicroElements.Collections.Sources.svg)

___
### MicroElements.Formatting.Sources
|   |   |
--- | ---
Name | MicroElements.Formatting.Sources
Description | MicroElements source only package: Formatting. Main methods: FormatValue, FormatAsTuple.
Github | [https://github.com/micro-elements/MicroElements.Shared/tree/master/src/MicroElements.Formatting.Sources](https://github.com/micro-elements/MicroElements.Shared/tree/master/src/MicroElements.Formatting.Sources)
Status | [![NuGetVersion](https://img.shields.io/nuget/v/MicroElements.Formatting.Sources.svg)](https://www.nuget.org/packages/MicroElements.Formatting.Sources) ![NuGetDownloads](https://img.shields.io/nuget/dt/MicroElements.Formatting.Sources.svg)

___
### MicroElements.Reflection.Sources
|   |   |
--- | ---
Name | MicroElements.Reflection.Sources
Description | MicroElements source only package: Reflection. Classes: TypeExtensions, TypeCheck, ObjectExtensions, Expressions, CodeCompiler, FriendlyName.
Github | [https://github.com/micro-elements/MicroElements.Shared/tree/master/src/MicroElements.Reflection.Sources](https://github.com/micro-elements/MicroElements.Shared/tree/master/src/MicroElements.Reflection.Sources)
Status | [![NuGetVersion](https://img.shields.io/nuget/v/MicroElements.Reflection.Sources.svg)](https://www.nuget.org/packages/MicroElements.Reflection.Sources) ![NuGetDownloads](https://img.shields.io/nuget/dt/MicroElements.Reflection.Sources.svg)

## License
This project is licensed under the MIT license. See the [LICENSE] file for more info.

[LICENSE]: https://raw.githubusercontent.com/micro-elements/MicroElements.Shared/master/LICENSE