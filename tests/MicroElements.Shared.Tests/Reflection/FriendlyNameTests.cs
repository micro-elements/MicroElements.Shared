using System;
using System.Collections.Generic;
using FluentAssertions;
using MicroElements.Reflection.FriendlyName;
using Xunit;

namespace MicroElements.Shared.Tests.Reflection;

public class FriendlyNameTests
{
    [Fact]
    public void friendly_name()
    {
        typeof(List<ValueTuple<int?, string>?>).Name.Should().Be("List`1");
        typeof(List<ValueTuple<int?, string>?>).FullName.Should().StartWith("System.Collections.Generic.List`1[[System.Nullable`1[[System.ValueTuple`2[[System.Nullable`1[[System.Int32, System.Private.CoreLib");

        typeof(string).GetFriendlyName().Should().Be("string");
        typeof(object).GetFriendlyName().Should().Be("object");
        typeof(bool).GetFriendlyName().Should().Be("bool");
        typeof(byte).GetFriendlyName().Should().Be("byte");
        typeof(char).GetFriendlyName().Should().Be("char");
        typeof(decimal).GetFriendlyName().Should().Be("decimal");
        typeof(double).GetFriendlyName().Should().Be("double");
        typeof(short).GetFriendlyName().Should().Be("short");
        typeof(int).GetFriendlyName().Should().Be("int");
        typeof(int?).GetFriendlyName().Should().Be("int?");
        typeof(long).GetFriendlyName().Should().Be("long");
        typeof(sbyte).GetFriendlyName().Should().Be("sbyte");
        typeof(float).GetFriendlyName().Should().Be("float");
        typeof(ushort).GetFriendlyName().Should().Be("ushort");
        typeof(uint).GetFriendlyName().Should().Be("uint");
        typeof(ulong).GetFriendlyName().Should().Be("ulong");
        typeof(void).GetFriendlyName().Should().Be("void");

        typeof(int[]).GetFriendlyName().Should().Be("int[]");
        typeof(int[][]).GetFriendlyName().Should().Be("int[][]");
        typeof(KeyValuePair<int, string>).GetFriendlyName().Should().Be("KeyValuePair<int, string>");
        typeof(Tuple<int, string>).GetFriendlyName().Should().Be("Tuple<int, string>");
        typeof(Tuple<KeyValuePair<object, long>, string>).GetFriendlyName().Should().Be("Tuple<KeyValuePair<object, long>, string>");
        typeof(List<Tuple<int, string>>).GetFriendlyName().Should().Be("List<Tuple<int, string>>");
        typeof(Tuple<short[], string>).GetFriendlyName().Should().Be("Tuple<short[], string>");
        typeof(List<int?>).GetFriendlyName().Should().Be("List<int?>");
        typeof(List<ValueTuple<int?, string>?>).GetFriendlyName().Should().Be("List<ValueTuple<int?, string>?>");
        typeof(List<(int Key, string Value)>).GetFriendlyName().Should().Be("List<ValueTuple<int, string>>");
    }
}