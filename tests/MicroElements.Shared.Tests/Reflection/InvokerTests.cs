using System;
using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions;
using MicroElements.Reflection.CodeCompiler;
using Xunit;

namespace MicroElements.Shared.Tests.Reflection;

public class InvokerTests
{
    public record GenericArg1(string Value);
    public record GenericArg2(string Value);

    public record Arg1(string Value);
    public record Arg2(string Value);

    public record Result(string Value, Type? GenericType1 = null, Type? GenericType2 = null);

    public class SomeClass
    {
        public Result InstancePublic(Arg1 arg1)
        {
            return new Result(arg1.Value, null);
        }

        public Result InstancePublicGeneric<TGenericArg1>(Arg1 arg1)
        {
            return new Result(arg1.Value, typeof(TGenericArg1));
        }

        public static Result StaticGeneric1<TGenericArg1>(Arg1 arg1)
        {
            return new Result(arg1.Value, typeof(TGenericArg1));
        }

        public static Result StaticGeneric2<TGenericArg1>(Arg1 arg1, Arg2 arg2)
        {
            return new Result($"{arg1.Value}_{arg2.Value}", typeof(TGenericArg1));
        }
    }
    [Fact]
    public void InstancePublic()
    {
        var func = Invoker.GetCompiledCachedMethod<SomeClass, Arg1, Result>(nameof(SomeClass.InstancePublic));
        var instance = new SomeClass();
        var result = func(instance, new Arg1("arg1"));
        result.Value.Should().Be("arg1");
        result.GenericType1.Should().Be(null);
    }

    [Fact]
    public void InstancePublicGeneric()
    {
        var func = Invoker.GetCompiledCachedMethod<SomeClass, Arg1, Result>(nameof(SomeClass.InstancePublicGeneric), typeof(GenericArg1));
        var instance = new SomeClass();
        var result = func(instance, new Arg1("arg1"));
        result.Value.Should().Be("arg1");
        result.GenericType1.Should().Be(typeof(GenericArg1));
    }

    [Fact]
    public void StaticGeneric()
    {
        var compileGeneric = Invoker.GetCompiledCachedMethod<Arg1, Result>(SomeClass.StaticGeneric1<object>, typeof(GenericArg1));
        var result = compileGeneric(new Arg1("arg1"));
        result.Value.Should().Be("arg1");
        result.GenericType1.Should().Be(typeof(GenericArg1));
    }

    [Fact]
    public void StaticGeneric2()
    {
        var compileGeneric = Invoker.GetCompiledCachedMethod<Arg1, Arg2, Result>(SomeClass.StaticGeneric2<object>, typeof(GenericArg1));
        var result = compileGeneric(new Arg1("arg1"), new Arg2("arg2"));
        result.Value.Should().Be("arg1_arg2");
        result.GenericType1.Should().Be(typeof(GenericArg1));
        result.GenericType2.Should().Be(null);
    }

    [Fact]
    public void CompileGenericLocal()
    {
        var compileGeneric = Invoker.GetCompiledCachedMethod<Arg1, Result>(LocalStaticGeneric<object>, typeof(GenericArg1));
        var result = compileGeneric(new Arg1("arg1"));
        result.Value.Should().Be("arg1");
        result.GenericType1.Should().Be(typeof(GenericArg1));

        static Result LocalStaticGeneric<TGenericArg1>(Arg1 arg1)
        {
            return new Result(arg1.Value, typeof(TGenericArg1));
        }
    }
}