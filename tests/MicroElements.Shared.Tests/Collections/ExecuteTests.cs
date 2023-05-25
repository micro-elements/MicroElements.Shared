using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using MicroElements.Collections.Extensions.Execute;
using MicroElements.Collections.Extensions.Iterate;
using Xunit;

namespace MicroElements.Shared.Tests.Collections;

public class ExecuteTests
{
    [Fact]
    public void Execute_should_iterate_lazy_collection()
    {
        StringBuilder result = new StringBuilder();

        IEnumerable<string> LazyEnumeration()
        {
            yield return "1";
            yield return "2";
        }

        LazyEnumeration()
            .Execute(s => result.Append((string?)s))
            .Iterate();

        result.ToString().Should().Be("12");
    }
}