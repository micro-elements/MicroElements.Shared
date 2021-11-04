# MicroElements.Shared
Provides microelements shared components as sources. The purpose of this lib is provide most used shared utilitiy code without additional dependencies.

## Statuses
[![License](https://img.shields.io/github/license/micro-elements/MicroElements.Shared.svg)](https://raw.githubusercontent.com/micro-elements/MicroElements.Shared/master/LICENSE)
[![Gitter](https://img.shields.io/gitter/room/micro-elements/MicroElements.Shared.svg)](https://gitter.im/micro-elements/MicroElements.Shared)

### MicroElements.IsExternalInit
[![NuGetVersion](https://img.shields.io/nuget/v/MicroElements.IsExternalInit.svg)](https://www.nuget.org/packages/MicroElements.IsExternalInit)
![NuGetDownloads](https://img.shields.io/nuget/dt/MicroElements.IsExternalInit.svg)

### MicroElements.JetBrains.Sources
[![NuGetVersion](https://img.shields.io/nuget/v/MicroElements.JetBrains.Sources.svg)](https://www.nuget.org/packages/MicroElements.JetBrains.Sources)
![NuGetDownloads](https://img.shields.io/nuget/dt/MicroElements.JetBrains.Sources.svg)

### MicroElements.Formatting.Sources
[![NuGetVersion](https://img.shields.io/nuget/v/MicroElements.Formatting.Sources.svg)](https://www.nuget.org/packages/MicroElements.Formatting.Sources)
![NuGetDownloads](https://img.shields.io/nuget/dt/MicroElements.Formatting.Sources.svg)

### MicroElements.Reflection.Sources
[![NuGetVersion](https://img.shields.io/nuget/v/MicroElements.Reflection.Sources.svg)](https://www.nuget.org/packages/MicroElements.Reflection.Sources)
![NuGetDownloads](https://img.shields.io/nuget/dt/MicroElements.Reflection.Sources.svg)

### MicroElements.CodeContracts.Sources
[![NuGetVersion](https://img.shields.io/nuget/v/MicroElements.CodeContracts.Sources.svg)](https://www.nuget.org/packages/MicroElements.CodeContracts.Sources)
![NuGetDownloads](https://img.shields.io/nuget/dt/MicroElements.CodeContracts.Sources.svg)

### MicroElements.Collections.Sources
[![NuGetVersion](https://img.shields.io/nuget/v/MicroElements.Collections.Sources.svg)](https://www.nuget.org/packages/MicroElements.Collections.Sources)
![NuGetDownloads](https://img.shields.io/nuget/dt/MicroElements.Collections.Sources.svg)

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

[MicroElements.Collections.Sources](https://github.com/micro-elements/MicroElements.Shared/blob/master/src/MicroElements.Collections.Sources/README.md)


## License
This project is licensed under the MIT license. See the [LICENSE] file for more info.

[LICENSE]: https://raw.githubusercontent.com/micro-elements/MicroElements.Shared/master/LICENSE