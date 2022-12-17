# Changelog

## [1.8.0] - 2022-12-17
- Changed: `MicroElements.Reflection.TypeCache` renamed to `MicroElements.Reflection.TypeCaching`
- Added: `LazyTypeCache` 
- Added: `TypeCache.AppDomainTypes` and `TypeCache.AppDomainTypesUpdatable`
- Changed: `FriendlyName` switched to lazy type cache with updatable app domain types

## [1.7.0] - 2022-11-15
- Changed: FriendlyName become more functional. All main methods now use functions as extension points.
- Changed: Added GetName to ITypeCache that allow to use it without TypeCache fields knowledge. Also it can be used in FriendlyName methods.

## [1.6.0] - 2022-11-11
- Changed: TypeLoader extensions simplified
- Added: FriendlyName: added ParseFriendlyName extension
- Added: TypeCache

## [1.5.0] - 2022-10-09
- Changed: FriendlyName nullable types support, more documentation and tests

## [1.4.0] - 2022-05-28
- Added: FriendlyName that allows to get friendly (human readable) name for the type.

## [1.3.1] - 2022-01-12
- Changed: MicroElements.Collections.Sources bumped to 1.3.0

## [1.3.0] - 2022-01-09
- Changed: Package reference MicroElements.Formatting.Sources replaced with MicroElements.Text.Sources

## [1.2.0] - 2021-12-10
- Added: TypeLoader
- Added: AndAlso ExpressionExtensions

## [1.1.0] - 2021-11-08
- Added: Expressions GetPropertySetter, GetPropertyGetterAndSetter, Mutate

## [1.0.0] - 2021-10-17
- CodeCompiler, ObjectExtensions, TypeCheck, TypeExtensions extracted from MicroElements.Functional and other MicroElements projects

Full release notes can be found at: https://github.com/micro-elements/MicroElements.Shared/blob/master/src/MicroElements.Reflection.Sources/CHANGELOG.md
