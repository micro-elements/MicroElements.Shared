using System.Linq;
using FluentAssertions;
using MicroElements.Collections.Extensions.WhereNotNull;
using Xunit;

namespace MicroElements.Shared.Tests.Collections;

public class WhereNotNullTests
{
    [Fact]
    public void WhereNotNull_Example()
    {
        string?[] namesWithNulls = {"Alex", null, "John"};
        string[] names = namesWithNulls.WhereNotNull().ToArray();
        names.Should().BeEquivalentTo("Alex", "John");
    }
}