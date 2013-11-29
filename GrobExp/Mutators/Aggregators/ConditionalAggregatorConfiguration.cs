﻿using System;
using System.Linq;
using System.Linq.Expressions;

using GrobExp.Mutators.Visitors;

namespace GrobExp.Mutators.Aggregators
{
    public class ConditionalAggregatorConfiguration : AggregatorConfiguration
    {
        public ConditionalAggregatorConfiguration(Type type, LambdaExpression condition, string name)
            : base(type)
        {
            Name = name;
            Condition = condition;
        }

        public static ConditionalAggregatorConfiguration Create<TData>(Expression<Func<TData, bool?>> condition, string name)
        {
            return new ConditionalAggregatorConfiguration(typeof(TData), Prepare(condition), name);
        }

        public override MutatorConfiguration If(LambdaExpression condition)
        {
            return new ConditionalAggregatorConfiguration(Type, Prepare(condition).AndAlso(Condition), Name);
        }

        public override void GetArrays(ArraysExtractor arraysExtractor)
        {
            arraysExtractor.GetArrays(Condition);
        }

        public override MutatorConfiguration ToRoot(LambdaExpression path)
        {
            return new ConditionalAggregatorConfiguration(path.Parameters.Single().Type, path.Merge(Condition), Name);
        }

        public override MutatorConfiguration Mutate(Type to, Expression path, CompositionPerformer performer)
        {
            return new ConditionalAggregatorConfiguration(to, Resolve(path, performer, Condition), Name);
        }

        public LambdaExpression Condition { get; private set; }
        public string Name { get; private set; }

        protected override LambdaExpression[] GetDependencies()
        {
            return Condition == null ? new LambdaExpression[0] : Condition.ExtractDependencies(Condition.Parameters.Where(parameter => parameter.Type == Type));
        }

        protected override Expression GetLCP()
        {
            return Condition == null ? null : Condition.Body.CutToChains(false, false).FindLCP();
        }
    }
}