// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace MicroElements.Reflection
{
    internal static partial class ExpressionUtils
    {
        /// <summary>
        /// Convert a lambda expression for a getter into a setter.
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
        /// </summary>
        public static Action<object, TProperty> GetPropertySetter<TProperty>(Type instanceType, string propertyName)
        {
            var propertyInfo = instanceType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo? setMethod = propertyInfo?.GetSetMethod(nonPublic: true);
            if (setMethod == null)
                throw new InvalidOperationException($"Type {instanceType} should have writable property {propertyName}.");

            var parameterTProperty = Expression.Parameter(typeof(TProperty), "valueT");

            var objectArg = Expression.Parameter(typeof(object), "objInst");
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
    }
}
