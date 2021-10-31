// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace MicroElements.Reflection.Expressions
{
    /// <summary>Represents compiled property getter and setter.</summary>
    internal record PropertyReflection<T, TProperty>(Func<T, TProperty> Getter, Action<T, TProperty> Setter);

    /// <summary>Expression helper methods.</summary>
    internal static partial class Expressions
    {
        /// <summary>
        /// Convert property expression to property setter action.
        /// </summary>
        public static Action<T, TProperty> GetPropertySetter<T, TProperty>(Expression<Func<T, TProperty>> expression)
        {
            var memberExpression = (MemberExpression)expression.Body;
            var property = (PropertyInfo)memberExpression.Member;
            var setMethod = property.GetSetMethod(nonPublic: true);
            if (setMethod == null)
                throw new InvalidOperationException($"Type {typeof(T)} should have writable property {property.Name}.");

            var parameterT = Expression.Parameter(typeof(T), "x");
            var parameterTProperty = Expression.Parameter(typeof(TProperty), "y");

            var callExpression = Expression.Call(parameterT, setMethod, parameterTProperty);
            var setExpression =
                Expression.Lambda<Action<T, TProperty>>(
                    callExpression,
                    parameterT,
                    parameterTProperty
                );

            return setExpression.Compile();
        }

        /// <summary>
        /// Gets property setter.
        /// Creates Action `(instance, propertyValue) => Convert(instance, TestClass).set_Value(propertyValue)`
        /// </summary>
        public static Action<object, TProperty> GetPropertySetter<TProperty>(Type instanceType, string propertyName)
        {
            var propertyInfo = instanceType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo? setMethod = propertyInfo?.GetSetMethod(nonPublic: true);
            if (setMethod == null)
                throw new InvalidOperationException($"Type {instanceType} should have writable property {propertyName}.");

            var parameterTProperty = Expression.Parameter(typeof(TProperty), "propertyValue");

            var objectArg = Expression.Parameter(typeof(object), "instance");
            UnaryExpression objectArgAsT = Expression.Convert(objectArg, instanceType);

            var callExpression = Expression.Call(objectArgAsT, setMethod, parameterTProperty);
            var setExpression =
                Expression.Lambda<Action<object, TProperty>>(
                    callExpression,
                    objectArg,
                    parameterTProperty
                );

            return setExpression.Compile();
        }

        /// <summary>
        /// Gets property getter and setter from property expression.
        /// </summary>
        public static PropertyReflection<T, TProperty> GetPropertyGetterAndSetter<T, TProperty>(Expression<Func<T, TProperty>> expression)
        {
            return new (expression.Compile(), GetPropertySetter(expression));
        }

        /// <summary>
        /// Mutates target object using cached property expression.
        /// </summary>
        /// <returns>The same object.</returns>
        public static T Mutate<T, TProperty>(this T target, Expression<Func<T, TProperty>> expression, TProperty value)
        {
            Cached.GetPropertyGetterAndSetter(expression).Setter(target, value);
            return target;
        }
    }

    internal static partial class Expressions
    {
        /// <summary> Cached expressions. </summary>
        public static class Cached
        {
            private static class ExpressionCache<T, TProperty>
            {
                internal static readonly ConcurrentDictionary<string, PropertyReflection<T, TProperty>> Cache = new();
            }

            /// <summary>
            /// Gets property getter and setter from property expression.
            /// </summary>
            public static PropertyReflection<T, TProperty> GetPropertyGetterAndSetter<T, TProperty>(Expression<Func<T, TProperty>> expression)
            {
                string expressionKey = expression.Body is MemberExpression { Member: PropertyInfo propertyInfo } ? propertyInfo.Name : expression.ToString();

                return ExpressionCache<T, TProperty>.Cache.GetOrAdd(expressionKey, (_, expr) => Expressions.GetPropertyGetterAndSetter(expr), expression);
            }
        }
    }
}
