﻿using System;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators
{
    public class ExpressionCanonicalForm
    {
        public Expression Source { get; private set; }
        public Expression CanonicalForm { get; private set; }
        public ParameterExpression ParameterAccessor { get; set; }
        private Expression[] ExtractedExpressions { get; set; }

        public ExpressionCanonicalForm(Expression source, params ParameterExpression[] parametersToExtract)
        {
            Source = source;
            ParameterAccessor = Expression.Parameter(typeof(object[]));
            Expression[] parameters;
            CanonicalForm = new ExpressionCanonizer(ParameterAccessor, parametersToExtract).Canonize(Source, out parameters);
            ExtractedExpressions = parameters;
        }

        public Expression ConstructInvokation(LambdaExpression lambda)
        {
            var array = Expression.Parameter(typeof(object[]));
            var newArray = Expression.NewArrayBounds(typeof(object), Expression.Constant(ExtractedExpressions.Length, typeof(int)));
            var arrayInit = ExtractedExpressions
                .Select(exp => Expression.Convert(exp, typeof(object)))
                .Select((exp, i) => Expression.Assign(Expression.ArrayAccess(array, Expression.Constant(i, typeof(int))), exp));
            return Expression.Block(new []{ array }, 
                Expression.Assign(array, newArray), 
                Expression.Block(arrayInit), 
                Expression.Invoke(lambda, array)
            );
        }
    }
}
