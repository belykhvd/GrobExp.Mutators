﻿using System;
using System.Linq.Expressions;

using GrobExp.Compiler;

using NUnit.Framework;

namespace Tests.AssignTests.PreIncrementAssign
{
    [TestFixture]
    public class TestStaticMember
    {
        [Test]
        public void TestIntProp()
        {
            Expression<Func<int>> exp = Expression.Lambda<Func<int>>(Expression.PreIncrementAssign(Expression.MakeMemberAccess(null, typeof(TestClassA).GetProperty("IntProp"))));
            var f = LambdaCompiler.Compile(exp, CompilerOptions.CheckNullReferences);
            TestClassA.IntProp = 0;
            Assert.AreEqual(1, f());
            Assert.AreEqual(1, TestClassA.IntProp);
            TestClassA.IntProp = -1;
            Assert.AreEqual(0, f());
            Assert.AreEqual(0, TestClassA.IntProp);
            TestClassA.IntProp = int.MaxValue;
            Assert.AreEqual(int.MinValue, f());
            Assert.AreEqual(int.MinValue, TestClassA.IntProp);
        }

        [Test]
        public void TestDoubleField()
        {
            Expression<Func<double>> exp = Expression.Lambda<Func<double>>(Expression.PreIncrementAssign(Expression.MakeMemberAccess(null, typeof(TestClassA).GetField("DoubleField"))));
            var f = LambdaCompiler.Compile(exp, CompilerOptions.CheckNullReferences);
            TestClassA.DoubleField = 0;
            Assert.AreEqual(1, f());
            Assert.AreEqual(1, TestClassA.DoubleField);
            TestClassA.DoubleField = -1;
            Assert.AreEqual(0, f());
            Assert.AreEqual(0, TestClassA.DoubleField);
            TestClassA.DoubleField = -0.5;
            Assert.AreEqual(0.5, f());
            Assert.AreEqual(0.5, TestClassA.DoubleField);
        }

        [Test]
        public void TestNullable()
        {
            Expression<Func<int?>> exp = Expression.Lambda<Func<int?>>(Expression.PreIncrementAssign(Expression.MakeMemberAccess(null, typeof(TestClassA).GetProperty("NullableIntProp"))));
            var f = LambdaCompiler.Compile(exp, CompilerOptions.All);
            TestClassA.NullableIntProp = 0;
            Assert.AreEqual(1, f());
            Assert.AreEqual(1, TestClassA.NullableIntProp);
            TestClassA.NullableIntProp = -1;
            Assert.AreEqual(0, f());
            Assert.AreEqual(0, TestClassA.NullableIntProp);
            TestClassA.NullableIntProp = int.MaxValue;
            Assert.AreEqual(int.MinValue, f());
            Assert.AreEqual(int.MinValue, TestClassA.NullableIntProp);
            TestClassA.NullableIntProp = null;
            Assert.IsNull(f());
            Assert.IsNull(TestClassA.NullableIntProp);
        }

        private class TestClassA
        {
            public static int IntProp { get; set; }
            public static int? NullableIntProp { get; set; }
            public static double DoubleField;
        }
    }
}