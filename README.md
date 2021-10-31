# MicroElements.Shared
Provides microelements shared components as sources. The purpose of this lib is provide most used shared utilitiy code without additional dependencies.

## Statuses
[![License](https://img.shields.io/github/license/micro-elements/MicroElements.Shared.svg)](https://raw.githubusercontent.com/micro-elements/MicroElements.Shared/master/LICENSE)
[![NuGetVersion](https://img.shields.io/nuget/v/MicroElements.Shared.svg)](https://www.nuget.org/packages/MicroElements.Shared)
![NuGetDownloads](https://img.shields.io/nuget/dt/MicroElements.Shared.svg)
[![MyGetVersion](https://img.shields.io/myget/micro-elements/v/MicroElements.Shared.svg)](https://www.myget.org/feed/micro-elements/package/nuget/MicroElements.Shared)

[![Travis](https://img.shields.io/travis/micro-elements/MicroElements.Shared/master.svg?logo=travis)](https://travis-ci.org/micro-elements/MicroElements.Shared)

[![Gitter](https://img.shields.io/gitter/room/micro-elements/MicroElements.Shared.svg)](https://gitter.im/micro-elements/MicroElements.Shared)


## License
This project is licensed under the MIT license. See the [LICENSE] file for more info.

[LICENSE]: https://raw.githubusercontent.com/micro-elements/MicroElements.Shared/master/LICENSE

## Components

- Trimmed version of JetBrains annotations used in MicroElements packages.
- CodeContracts
- IsExternalInit
- Reflection
- Collections


## Design concepts

- Designed for .Net Core so most libs uses .NetStandard 2.1
- MicroElements uses modern language features (C#9) to reduce code size and improve readability.
- Granular namespaces to reduce collision possibility. You can control usages till class level.
- All source packages stores its code in one root `MicroElements` that keeps project clean.
- MicroElements source code packages can reduce your `common libs` dependencies
- Debugging is easy because you have full sources

