using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using MicroElements.Collections.Extensions.Iterate;
using Xunit;

namespace MicroElements.Shared.Tests.Collections
{
    public class IterateTests
    {
        [Fact]
        public void Iterate_should_iterate_collection()
        {
            int i = 0;
            string[] array = {"Alex", "John"};
            array.Iterate(s => i++);
            i.Should().Be(2);
        }

        [Fact]
        public void Iterate_should_iterate_lazy_collection()
        {
            StringBuilder result = new StringBuilder();

            IEnumerable<string> LazyEnumeration()
            {
                result.Append("1");
                yield return "1";

                result.Append("2");
                yield return "2";
            }

            LazyEnumeration().Iterate();
            result.ToString().Should().Be("12");
        }
    }
}
