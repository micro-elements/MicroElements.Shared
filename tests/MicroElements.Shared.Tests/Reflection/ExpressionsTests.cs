using System;
using System.Linq.Expressions;
using FluentAssertions;
using MicroElements.Reflection.Expressions;
using Xunit;

namespace MicroElements.Shared.Tests.Reflection
{
    public class ExpressionsTests
    {
        public class Person
        {
            public int Age { get; set; }
        }

        [Fact]
        public void GetPropertySetterTests()
        {
            Person person = new Person();
            person.Age.Should().Be(0);

            var propertySetter = Expressions.GetPropertySetter<int>(typeof(Person), nameof(Person.Age));
            propertySetter(person, 42);

            person.Age.Should().Be(42);
        }

        [Fact]
        public void GetPropertyGetterSetterTests()
        {
            Person person = new Person();
            person.Age.Should().Be(0);

            Expression<Func<Person, int>> expression = valueOwner => valueOwner.Age;

            var getterAndSetter = Expressions.GetPropertyGetterAndSetter<Person, int>(expression);
            getterAndSetter.Setter(person, 42);
            getterAndSetter.Getter(person).Should().Be(42);

            person.Age.Should().Be(42);
        }

        [Fact]
        public void MutateTests()
        {
            Person person = new Person();
            person.Age.Should().Be(0);
            person.Mutate(p => p.Age, 42);
            person.Age.Should().Be(42);
        }
    }
}
