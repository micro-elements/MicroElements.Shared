using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MicroElements.Collections.Extensions;
using Xunit;

namespace MicroElements.Shared.Tests
{
    public class CollectionExtensionsTests
    {
        [Fact]
        public void NotNull()
        {
            string[] array = {"Alex", "John"};
            array.NotNull().First().Should().Be("Alex");

            var enumerable = array as IEnumerable<string>;
            enumerable.NotNull().First().Should().Be("Alex");

            var readOnlyCollection = array as IReadOnlyCollection<string>;
            readOnlyCollection.NotNull().First().Should().Be("Alex");
        }
    }
}
