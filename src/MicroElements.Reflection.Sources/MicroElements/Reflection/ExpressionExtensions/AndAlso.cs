#region License

// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#endregion
#region Supressions

#pragma warning disable
// ReSharper disable CheckNamespace

#endregion

using System;
using System.Linq.Expressions;

namespace MicroElements.Reflection.ExpressionExtensions.AndAlso
{
    internal static partial class AndAlsoExtensions
    {
        internal static Expression<Func<T, bool>> AndAlso<T>(
            this Expression<Func<T, bool>>? expr1,
            Expression<Func<T, bool>> expr2)
        {
            if (expr1 is null)
                return expr2;

            var parameter = Expression.Parameter(typeof(T), "p");

            var leftVisitor = new ReplaceExpressionVisitor(expr1.Parameters[0], parameter);
            var left = leftVisitor.Visit(expr1.Body);

            var rightVisitor = new ReplaceExpressionVisitor(expr2.Parameters[0], parameter);
            var right = rightVisitor.Visit(expr2.Body);

            return Expression.Lambda<Func<T, bool>>(
                Expression.AndAlso(left, right), parameter);
        }

        private class ReplaceExpressionVisitor : ExpressionVisitor
        {
            private readonly Expression _oldValue;
            private readonly Expression _newValue;

            public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
            {
                _oldValue = oldValue;
                _newValue = newValue;
            }

            public override Expression Visit(Expression node)
            {
                if (node == _oldValue)
                    return _newValue;
                return base.Visit(node);
            }
        }
    }
}
