using GrobExp.Mutators.ModelConfiguration;
using System;
using System.Linq.Expressions;

namespace GrobExp.Mutators
{
    public class NewConverterConfigurator<TSource, TDest, TContext>
    {
        public NewConverterConfigurator(ModelConfigurationNode root)
            : this(null, root, null)
        {
        }

        internal NewConverterConfigurator(ConfiguratorReporter reporter, ModelConfigurationNode root, LambdaExpression condition = null)
        {
            this.reporter = reporter;
            Condition = condition;
            Root = root;
        }

        public void SetMutator(Expression pathToTarget, MutatorConfiguration mutator)
        {
            var pathToTraverse = pathToTarget.ResolveInterfaceMembers();
            var mutatorToAdd = Condition == null ? mutator : mutator.If(Condition);
            reporter?.Report(null, pathToTraverse, mutatorToAdd);
            Root.Traverse(pathToTraverse, true).AddMutator(mutatorToAdd);
        }

        public NewConverterConfigurator<TSource, TDest, TContext> WithoutCondition()
        {
            return new NewConverterConfigurator<TSource, TDest, TContext>(reporter, Root);
        }

        public NewConverterConfigurator<TSource, TSource, TDest, TDest, TValue, TContext> Target<TValue>(Expression<Func<TDest, TValue>> pathToValue)
        {
            return new NewConverterConfigurator<TSource, TSource, TDest, TDest, TValue, TContext>(reporter, Root, source => source, dest => dest, pathToValue, Condition);
        }

        public NewConverterConfigurator<TSource, TSource, TDest, TChild, TChild, TContext> GoTo<TChild>(Expression<Func<TDest, TChild>> pathToChild)
        {
            return new NewConverterConfigurator<TSource, TSource, TDest, TChild, TChild, TContext>(reporter, Root, source => source, pathToChild, pathToChild, Condition);
        }

        public NewConverterConfigurator<TSource, TSourceChild, TDest, TDestChild, TDestChild, TContext> GoTo<TDestChild, TSourceChild>(Expression<Func<TDest, TDestChild>> pathToDestChild, Expression<Func<TSource, TSourceChild>> pathToSourceChild)
        {
            return new NewConverterConfigurator<TSource, TSourceChild, TDest, TDestChild, TDestChild, TContext>(reporter, Root, pathToSourceChild, pathToDestChild, pathToDestChild, Condition);
        }

        public NewConverterConfigurator<TSource, TDest, TContext> If(LambdaExpression condition)
        {
            return new NewConverterConfigurator<TSource, TDest, TContext>(reporter, Root, Condition.AndAlso((LambdaExpression) condition.ReplaceEachWithCurrent()));
        }

        public NewConverterConfigurator<TSource, TDest, TContext> If(Expression<Func<TSource, bool?>> condition) => If((LambdaExpression) condition);

        public NewConverterConfigurator<TSource, TDest, TContext> If(Expression<Func<TSource, TDest, bool?>> condition) => If((LambdaExpression) condition);

        public LambdaExpression Condition { get; }
        internal ModelConfigurationNode Root { get; }
        private readonly ConfiguratorReporter reporter;
    }

    public class NewConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue, TContext>
    {
        public NewConverterConfigurator(ModelConfigurationNode root,
                                     Expression<Func<TSourceRoot, TSourceChild>> pathToSourceChild,
                                     Expression<Func<TDestRoot, TDestChild>> pathToChild,
                                     Expression<Func<TDestRoot, TDestValue>> pathToValue,
                                     LambdaExpression condition)
            : this(null, root, pathToSourceChild, pathToChild, pathToValue, condition)
        {
        }

        internal NewConverterConfigurator(ConfiguratorReporter reporter,
                                       ModelConfigurationNode root,
                                       Expression<Func<TSourceRoot, TSourceChild>> pathToSourceChild,
                                       Expression<Func<TDestRoot, TDestChild>> pathToChild,
                                       Expression<Func<TDestRoot, TDestValue>> pathToValue,
                                       LambdaExpression condition)
        {
            this.reporter = reporter;
            Root = root;
            PathToSourceChild = pathToSourceChild;
            PathToChild = pathToChild;
            PathToValue = pathToValue;
            Condition = condition;
        }

        public void SetMutator(MutatorConfiguration mutator)
        {
            if (PathToValue != null)
            {
                var rootMutator = GetRootMutator(mutator);
                var mutatorToAdd = Condition == null ? rootMutator : rootMutator.If(Condition);
                Root.AddMutatorSmart(PathToValue.ResolveInterfaceMembers(), mutatorToAdd, reporter);
            }
        }

        /// <summary>
        ///     В случае, когда нахерачили всяких GoTo, пути в конфигурации могут идти не от рута. Здесь это фиксится.
        /// </summary>
        private MutatorConfiguration GetRootMutator(MutatorConfiguration mutator)
        {
            if (mutator.Type == typeof(TDestRoot))
                return mutator;

            var pathToChild = PathToChild.ReplaceEachWithCurrent().ResolveInterfaceMembers();
            return mutator.ToRoot((Expression<Func<TDestRoot, TDestChild>>)pathToChild);
        }

        public NewConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue, TContext> WithoutCondition()
        {
            return new NewConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue, TContext>(reporter, Root, PathToSourceChild, PathToChild, PathToValue, null);
        }

        public NewConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, T, TContext> Target<T>(Expression<Func<TDestValue, T>> pathToValue)
        {
            return new NewConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, T, TContext>(reporter, Root, PathToSourceChild, PathToChild, PathToValue.Merge(pathToValue), Condition);
        }

        public NewConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, T, T, TContext> GoTo<T>(Expression<Func<TDestChild, T>> pathToChild)
        {
            var path = PathToChild.Merge(pathToChild);
            return new NewConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, T, T, TContext>(reporter, Root, PathToSourceChild, path, path, Condition);
        }

        public NewConverterConfigurator<TSourceRoot, T2, TDestRoot, T1, T1, TContext> GoTo<T1, T2>(Expression<Func<TDestChild, T1>> pathToDestChild, Expression<Func<TSourceChild, T2>> pathToSourceChild)
        {
            var path = PathToChild.Merge(pathToDestChild);
            return new NewConverterConfigurator<TSourceRoot, T2, TDestRoot, T1, T1, TContext>(reporter, Root, PathToSourceChild.Merge(pathToSourceChild), path, path, Condition);
        }

        public NewConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue, TContext> If(Expression<Func<TSourceChild, bool?>> condition)
        {
            return new NewConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue, TContext>(reporter, Root, PathToSourceChild, PathToChild, PathToValue, Condition.AndAlso((LambdaExpression) PathToSourceChild.Merge(condition).ReplaceEachWithCurrent()));
        }

        public NewConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue, TContext> If(Expression<Func<TSourceChild, TContext, bool?>> condition)
        {            
            var rewrittenLambda = ContextRewriter.Rebuild<TSourceRoot, TContext>(condition.Parameters[0], condition.Body);

            return new NewConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue, TContext>(reporter, Root, PathToSourceChild, PathToChild, PathToValue, Condition.AndAlso((LambdaExpression) PathToSourceChild.Merge(rewrittenLambda).ReplaceEachWithCurrent()));
        }

        public NewConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue, TContext> If(Expression<Func<TSourceChild, TDestChild, bool?>> condition)
        {
            return new NewConverterConfigurator<TSourceRoot, TSourceChild, TDestRoot, TDestChild, TDestValue, TContext>(reporter, Root, PathToSourceChild, PathToChild, PathToValue, Condition.AndAlso((LambdaExpression)condition.MergeFrom2Roots(PathToSourceChild, PathToChild).ReplaceEachWithCurrent()));
        }

        public NewConverterConfigurator<TSourceRoot, TDestRoot, TContext> ToRoot()
        {
            return new NewConverterConfigurator<TSourceRoot, TDestRoot, TContext>(reporter, Root, Condition);
        }

        public Expression<Func<TSourceRoot, TSourceChild>> PathToSourceChild { get; }
        public Expression<Func<TDestRoot, TDestChild>> PathToChild { get; }
        public Expression<Func<TDestRoot, TDestValue>> PathToValue { get; }
        public LambdaExpression Condition { get; }
        internal ModelConfigurationNode Root { get; }
        private readonly ConfiguratorReporter reporter;
    }
}