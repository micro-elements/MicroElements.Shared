using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MicroElements.Collections.Extensions.NotNull;
using Xunit;

namespace MicroElements.Shared.Tests.Collections
{
    public class NotNullTests
    {
        [Fact]
        public void NotNull_should_return_empty_collection()
        {
            string[]? array = null;
            array.NotNull().Should().BeEmpty();

            IEnumerable<string>? enumerable = null;
            enumerable.NotNull().Should().BeEmpty();

            IReadOnlyCollection<string>? readOnlyCollection = null;
            readOnlyCollection.NotNull().Should().BeEmpty();
        }

        [Fact]
        public void NotNull_Example()
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
