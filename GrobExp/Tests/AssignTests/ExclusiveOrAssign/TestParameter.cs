﻿using System;
using System.Linq.Expressions;

using GrobExp.Compiler;

using NUnit.Framework;

namespace Tests.AssignTests.ExclusiveOrAssign
{
    [TestFixture]
    public class TestParameter
    {
        [Test]
        public void TestInt()
        {
            ParameterExpression a = Expression.Parameter(typeof(int), "a");
            ParameterExpression b = Expression.Parameter(typeof(int), "b");
            Expression<Func<int, int, int>> exp = Expression.Lambda<Func<int, int, int>>(Expression.ExclusiveOrAssign(a, b), a, b);
            var f = LambdaCompiler.Compile(exp, CompilerOptions.All);
            Assert.AreEqual(123, f(0, 123));
            Assert.AreEqual(6, f(3, 5));
            Assert.AreEqual(17235476 ^ 73172563, f(17235476, 73172563));

            exp = Expression.Lambda<Func<int, int, int>>(Expression.Block(typeof(int), Expression.ExclusiveOrAssign(a, b), a), a, b);
            f = LambdaCompiler.Compile(exp, CompilerOptions.All);
            Assert.AreEqual(123, f(0, 123));
            Assert.AreEqual(6, f(3, 5));
            Assert.AreEqual(17235476 ^ 73172563, f(17235476, 73172563));
        }

        [Test]
        public void TestNullableInt()
        {
            ParameterExpression a = Expression.Parameter(typeof(int?), "a");
            ParameterExpression b = Expression.Parameter(typeof(int?), "b");
            Expression<Func<int?, int?, int?>> exp = Expression.Lambda<Func<int?, int?, int?>>(Expression.ExclusiveOrAssign(a, b), a, b);
            var f = LambdaCompiler.Compile(exp, CompilerOptions.All);
            Assert.AreEqual(123, f(0, 123));
            Assert.AreEqual(6, f(3, 5));
            Assert.AreEqual(17235476 ^ 73172563, f(17235476, 73172563));
            Assert.IsNull(f(null, 1));
            Assert.IsNull(f(123, null));
            Assert.IsNull(f(null, null));

            exp = Expression.Lambda<Func<int?, int?, int?>>(Expression.Block(typeof(int?), Expression.ExclusiveOrAssign(a, b), a), a, b);
            f = LambdaCompiler.Compile(exp, CompilerOptions.All);
            Assert.AreEqual(123, f(0, 123));
            Assert.AreEqual(6, f(3, 5));
            Assert.AreEqual(17235476 ^ 73172563, f(17235476, 73172563));
            Assert.IsNull(f(null, 1));
            Assert.IsNull(f(123, null));
            Assert.IsNull(f(null, null));
        }
    }
}