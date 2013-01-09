﻿using System;
using System.Linq.Expressions;

using GrobExp;

using NUnit.Framework;

namespace Tests.ArithmeticTests
{
    [TestFixture]
    public class TestUnaryPlusMinus
    {
        [Test]
        public void TestNegate1()
        {
            Expression<Func<int, int>> exp = x => -x;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0));
            Assert.AreEqual(-1, f(1));
            Assert.AreEqual(1, f(-1));
            Assert.AreEqual(int.MinValue, f(int.MinValue));
        }

        [Test]
        public void TestNegate2()
        {
            Expression<Func<int?, int?>> exp = x => -x;
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0));
            Assert.AreEqual(-1, f(1));
            Assert.AreEqual(1, f(-1));
            Assert.IsNull(f(null));
            Assert.AreEqual(int.MinValue, f(int.MinValue));
        }

        [Test]
        public void TestNegate3()
        {
            ParameterExpression parameter = Expression.Parameter(typeof(int));
            Expression<Func<int, int>> exp = Expression.Lambda<Func<int, int>>(Expression.NegateChecked(parameter), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0));
            Assert.AreEqual(-1, f(1));
            Assert.AreEqual(1, f(-1));
            Assert.Throws<OverflowException>(() => f(int.MinValue));
        }

        [Test]
        public void TestNegate4()
        {
            ParameterExpression parameter = Expression.Parameter(typeof(int?));
            Expression<Func<int?, int?>> exp = Expression.Lambda<Func<int?, int?>>(Expression.NegateChecked(parameter), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0));
            Assert.AreEqual(-1, f(1));
            Assert.AreEqual(1, f(-1));
            Assert.IsNull(f(null));
            Assert.Throws<OverflowException>(() => f(int.MinValue));
        }

        [Test]
        public void TestUnaryPlus1()
        {
            ParameterExpression parameter = Expression.Parameter(typeof(int));
            Expression<Func<int, int>> exp = Expression.Lambda<Func<int, int>>(Expression.UnaryPlus(parameter), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0));
            Assert.AreEqual(1, f(1));
            Assert.AreEqual(-1, f(-1));
            Assert.AreEqual(int.MinValue, f(int.MinValue));
        }

        [Test]
        public void TestUnaryPlus2()
        {
            ParameterExpression parameter = Expression.Parameter(typeof(int?));
            Expression<Func<int?, int?>> exp = Expression.Lambda<Func<int?, int?>>(Expression.UnaryPlus(parameter), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(0, f(0));
            Assert.AreEqual(1, f(1));
            Assert.AreEqual(-1, f(-1));
            Assert.IsNull(f(null));
            Assert.AreEqual(int.MinValue, f(int.MinValue));
        }
    }
}