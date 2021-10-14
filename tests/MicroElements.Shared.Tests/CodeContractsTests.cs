﻿using System;
using FluentAssertions;
using MicroElements.CodeContracts;
using Xunit;

namespace MicroElements.Shared.Tests
{
    public class CodeContractsTests
    {
        [Fact]
        public void AssertArgumentNotNullTest()
        {
            static string ToUpper(string value)
            {
                value.AssertArgumentNotNull(nameof(value));
                return value.ToUpper();
            }

            ToUpper("value").Should().Be("VALUE");

            Action act = () => ToUpper(null);
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
