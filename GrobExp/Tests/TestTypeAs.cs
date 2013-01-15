﻿using System;
using System.Linq.Expressions;

using GrobExp;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class TestTypeAs
    {
        [Test]
        public void Test1()
        {
            var parameter = Expression.Parameter(typeof(int));
            var exp = Expression.Lambda<Func<int, double?>>(Expression.TypeAs(parameter, typeof(double?)), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.IsNull(f(5));
        }

        [Test]
        public void Test2()
        {
            var parameter = Expression.Parameter(typeof(int));
            var exp = Expression.Lambda<Func<int, int?>>(Expression.TypeAs(parameter, typeof(int?)), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(5, f(5));
        }

        [Test]
        public void Test3()
        {
            var parameter = Expression.Parameter(typeof(int));
            var exp = Expression.Lambda<Func<int, object>>(Expression.TypeAs(parameter, typeof(object)), parameter);
            var f = LambdaCompiler.Compile(exp);
            object res = f(5);
            Assert.AreEqual(5, res);
        }

        [Test]
        public void Test4()
        {
            var parameter = Expression.Parameter(typeof(TestClassB));
            var exp = Expression.Lambda<Func<TestClassB, TestClassA>>(Expression.TypeAs(parameter, typeof(TestClassA)), parameter);
            var f = LambdaCompiler.Compile(exp);
            var b = new TestClassB();
            TestClassA a = f(b);
            Assert.AreEqual(b, a);
        }

        [Test]
        public void Test5()
        {
            var parameter = Expression.Parameter(typeof(TestClassA));
            var exp = Expression.Lambda<Func<TestClassA, TestClassB>>(Expression.TypeAs(parameter, typeof(TestClassB)), parameter);
            var f = LambdaCompiler.Compile(exp);
            var a = new TestClassA();
            TestClassB b = f(a);
            Assert.IsNull(b);
            b = new TestClassB();
            var bb = f(b);
            Assert.AreEqual(b, bb);
        }

        [Test]
        public void Test6()
        {
            var parameter = Expression.Parameter(typeof(object));
            var exp = Expression.Lambda<Func<object, int?>>(Expression.TypeAs(parameter, typeof(int?)), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(5, f(5));
            Assert.IsNull(f(5.5));
        }

        [Test]
        public void Test7()
        {
            var parameter = Expression.Parameter(typeof(object));
            var exp = Expression.Lambda<Func<object, TestEnum?>>(Expression.TypeAs(parameter, typeof(TestEnum?)), parameter);
            var f = LambdaCompiler.Compile(exp);
            Assert.AreEqual(TestEnum.One, f(TestEnum.One));
            Assert.IsNull(f(5.5));
        }

        [Test]
        public void Test8()
        {
            var parameter = Expression.Parameter(typeof(TestEnum));
            var exp = Expression.Lambda<Func<TestEnum, Enum>>(Expression.TypeAs(parameter, typeof(Enum)), parameter);
            var f = LambdaCompiler.Compile(exp);
            Enum actual = f(TestEnum.One);
            Assert.AreEqual(TestEnum.One, actual);
        }

        private enum TestEnum
        {
            One,
            Two
        }

        private class TestClassA
        {
            
        }

        private class TestClassB: TestClassA
        {
            
        }
    }
}