using System;
using System.Collections.Generic;
using FluentAssertions;
using MicroElements.Reflection.FriendlyName;
using Xunit;

namespace MicroElements.Shared.Tests.Reflection;

public class FriendlyNameTests
{
    [Theory]
    [InlineData(typeof(string), "string")]
    [InlineData(typeof(object), "object")]
    [InlineData(typeof(bool), "bool")]
    [InlineData(typeof(byte), "byte")]
    [InlineData(typeof(char), "char")]
    [InlineData(typeof(decimal), "decimal")]
    [InlineData(typeof(double), "double")]
    [InlineData(typeof(short), "short")]
    [InlineData(typeof(int), "int")]
    [InlineData(typeof(long), "long")]
    [InlineData(typeof(sbyte), "sbyte")]
    [InlineData(typeof(float), "float")]
    [InlineData(typeof(ushort), "ushort")]
    [InlineData(typeof(uint), "uint")]
    [InlineData(typeof(void), "void")]
    
    [InlineData(typeof(int?), "int?")]
    [InlineData(typeof(int[]), "int[]")]
    [InlineData(typeof(int[][]), "int[][]")]
    
    [InlineData(typeof(KeyValuePair<int, string>), "KeyValuePair<int, string>")]
    [InlineData(typeof(Tuple<int, string>), "Tuple<int, string>")]
    [InlineData(typeof(Tuple<KeyValuePair<object, long>, string>), "Tuple<KeyValuePair<object, long>, string>")]
    [InlineData(typeof(List<Tuple<int, string>>), "List<Tuple<int, string>>")]
    [InlineData(typeof(Tuple<short[], string>), "Tuple<short[], string>")]
    [InlineData(typeof(List<int?>), "List<int?>")]
    [InlineData(typeof(List<ValueTuple<int?, string>?>), "List<ValueTuple<int?, string>?>")]
    [InlineData(typeof(ValueTuple<List<int?>, string>), "ValueTuple<List<int?>, string>")]
    public void friendly_name(Type type, string friendlyName)
    {
        var friendlyNameResult = type.GetFriendlyName();
        friendlyNameResult.Should().Be(friendlyName);
        var friendlyType = friendlyNameResult.ParseFriendlyName();
        friendlyType.Should().Be(type);
    }
}
