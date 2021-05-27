using GrobExp.Mutators.AutoEvaluators;
using System;
using System.Linq.Expressions;

namespace GrobExp.Mutators
{
    public static class NewConverterConfiguratorExtensions
    {
        public static NewConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TTarget, TContext> Set<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TTarget, TContext>(
            this NewConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TTarget, TContext> configurator,
            Expression<Func<TSourceChild, TTarget>> value)
        {
            var pathToSourceChild = (Expression<Func<TSourceRoot, TSourceChild>>) configurator.PathToSourceChild.ReplaceEachWithCurrent();
            LambdaExpression valueFromRoot = pathToSourceChild.Merge(value);
            configurator.SetMutator(EqualsToConfiguration.Create(configurator.Root.ConfiguratorType, typeof(TDestRoot), valueFromRoot, null));
            return configurator;
        }

        public static NewConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TTarget, TContext> Set<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TTarget, TContext>(
            this NewConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TTarget, TContext> configurator,
            Expression<Func<TSourceChild, TContext, TTarget>> value)
        {            
            var rewrittenLambda = ContextRewriter.Rebuild<TSourceRoot, TContext>(value.Parameters[0], value.Body);

            var pathToSourceChild = (Expression<Func<TSourceRoot, TSourceChild>>) configurator.PathToSourceChild.ReplaceEachWithCurrent();
            LambdaExpression valueFromRoot = pathToSourceChild.Merge(rewrittenLambda);
            configurator.SetMutator(EqualsToConfiguration.Create(configurator.Root.ConfiguratorType, typeof(TDestRoot), valueFromRoot, null));
            return configurator;
        }
    }
}