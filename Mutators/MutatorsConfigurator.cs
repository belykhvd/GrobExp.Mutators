using System;
using System.Linq.Expressions;

using GrobExp.Mutators.ModelConfiguration;
using GrobExp.Mutators.MultiLanguages;

namespace GrobExp.Mutators
{
    public class MutatorsConfigurator<TRoot, TContext>
    {
        public MutatorsConfigurator(ModelConfigurationNode root, LambdaExpression condition = null)
        {
            this.root = root;
            Condition = condition;
        }

        public void SetMutator<TValue>(Expression<Func<TRoot, TValue>> pathToValue, MutatorConfiguration mutator)
        {
            SetMutator((LambdaExpression)pathToValue, mutator);
        }

        public void SetMutator(LambdaExpression pathToValue, MutatorConfiguration mutator)
        {
            root.AddMutatorSmart(pathToValue.ResolveInterfaceMembers(), mutator.If(Condition));
        }

        public MutatorsConfigurator<TRoot, TContext> WithoutCondition()
        {
            return new MutatorsConfigurator<TRoot, TContext>(root);
        }

        public MutatorsConfigurator<TRoot, TRoot, TValue, TContext> Target<TValue>(Expression<Func<TRoot, TValue>> pathToValue, MultiLanguageTextBase title = null)
        {
            return new MutatorsConfigurator<TRoot, TRoot, TValue, TContext>(root, data => data, pathToValue, Condition, title);
        }

        public MutatorsConfigurator<TRoot, TChild, TChild, TContext> GoTo<TChild>(Expression<Func<TRoot, TChild>> pathToChild)
        {
            return new MutatorsConfigurator<TRoot, TChild, TChild, TContext>(root, pathToChild, pathToChild, Condition, null);
        }

        public MutatorsConfigurator<TRoot, TContext> If(LambdaExpression condition)
        {
            return new MutatorsConfigurator<TRoot, TContext>(root, Condition.AndAlso((LambdaExpression)condition.ReplaceEachWithCurrent()));
        }

        public MutatorsConfigurator<TRoot, TContext> If(Expression<Func<TRoot, bool?>> condition)
        {
            return new MutatorsConfigurator<TRoot, TContext>(root, Condition.AndAlso((LambdaExpression)condition.ReplaceEachWithCurrent()));
        }

        public LambdaExpression Condition { get; }

        protected readonly ModelConfigurationNode root;
    }

    public class MutatorsConfigurator<TRoot, TChild, TValue, TContext>
    {
        public MutatorsConfigurator(ModelConfigurationNode root, Expression<Func<TRoot, TChild>> pathToChild, Expression<Func<TRoot, TValue>> pathToValue, LambdaExpression condition, MultiLanguageTextBase title)
        {
            this.Root = root;
            Title = title;
            PathToChild = pathToChild;
            PathToValue = pathToValue;
            Condition = condition;
        }

        public void SetMutator(MutatorConfiguration mutator)
        {
            MutatorConfiguration rootMutator;
            if (mutator.Type == typeof(TRoot))
                rootMutator = mutator;
            else
            {
                var pathToChild = PathToChild.ReplaceEachWithCurrent().ResolveInterfaceMembers();
                rootMutator = mutator.ToRoot((Expression<Func<TRoot, TChild>>)pathToChild);
            }

            if (PathToValue != null)
                Root.AddMutatorSmart(PathToValue.ResolveInterfaceMembers(), rootMutator.If(Condition));
        }

        public void SetMutator(LambdaExpression pathToTarget, MutatorConfiguration mutator)
        {
            Root.AddMutatorSmart(pathToTarget.ResolveInterfaceMembers(), mutator.If(Condition));
        }

        public void SetMutator(Expression pathToNode, Expression pathToTarget, MutatorConfiguration mutator)
        {
            Root.Traverse(pathToNode.ResolveInterfaceMembers(), true).AddMutator(pathToTarget, mutator.If(Condition));
        }

        public MutatorsConfigurator<TRoot, TChild, TValue, TContext> WithoutCondition()
        {
            return new MutatorsConfigurator<TRoot, TChild, TValue, TContext>(Root, PathToChild, PathToValue, null, Title);
        }

        public MutatorsConfigurator<TRoot, T, T, TContext> GoTo<T>(Expression<Func<TChild, T>> path)
        {
            var pathToChild = PathToChild.Merge(path);
            return new MutatorsConfigurator<TRoot, T, T, TContext>(Root, pathToChild, pathToChild, Condition, Title);
        }

        public MutatorsConfigurator<TRoot, TChild, T, TContext> Target<T>(Expression<Func<TValue, T>> path, MultiLanguageTextBase title = null)
        {
            return new MutatorsConfigurator<TRoot, TChild, T, TContext>(Root, PathToChild, PathToValue.Merge(path), Condition, title);
        }

        public MutatorsConfigurator<TRoot, TChild, TValue, TContext> If(Expression<Func<TChild, bool?>> condition)
        {
            return new MutatorsConfigurator<TRoot, TChild, TValue, TContext>(Root, PathToChild, PathToValue, Condition.AndAlso((LambdaExpression)PathToChild.Merge(condition).ReplaceEachWithCurrent()), Title);
        }

        public MutatorsConfigurator<TRoot, TChild, TValue, TContext> IfFromRoot(Expression<Func<TRoot, bool?>> condition)
        {
            return new MutatorsConfigurator<TRoot, TChild, TValue, TContext>(Root, PathToChild, PathToValue, Condition.AndAlso((LambdaExpression)condition.ReplaceEachWithCurrent()), Title);
        }

        public Expression<Func<TRoot, TChild>> PathToChild { get; }

        public Expression<Func<TRoot, TValue>> PathToValue { get; }
        public LambdaExpression Condition { get; }
        public MultiLanguageTextBase Title { get; }
        internal ModelConfigurationNode Root { get; }
    }

    public class MutatorsConfigurator<TRoot>
    {
        public MutatorsConfigurator(ModelConfigurationNode root, LambdaExpression condition = null)
        {
            this.root = root;
            Condition = condition;
        }

        public void SetMutator<TValue>(Expression<Func<TRoot, TValue>> pathToValue, MutatorConfiguration mutator)
        {
            SetMutator((LambdaExpression)pathToValue, mutator);
        }

        public void SetMutator(LambdaExpression pathToValue, MutatorConfiguration mutator)
        {
            root.AddMutatorSmart(pathToValue.ResolveInterfaceMembers(), mutator.If(Condition));
        }

        public MutatorsConfigurator<TRoot> WithoutCondition()
        {
            return new MutatorsConfigurator<TRoot>(root);
        }

        public MutatorsConfigurator<TRoot, TRoot, TValue> Target<TValue>(Expression<Func<TRoot, TValue>> pathToValue, MultiLanguageTextBase title = null)
        {
            return new MutatorsConfigurator<TRoot, TRoot, TValue>(root, data => data, pathToValue, Condition, title);
        }

        public MutatorsConfigurator<TRoot, TChild, TChild> GoTo<TChild>(Expression<Func<TRoot, TChild>> pathToChild)
        {
            return new MutatorsConfigurator<TRoot, TChild, TChild>(root, pathToChild, pathToChild, Condition, null);
        }

        public MutatorsConfigurator<TRoot> If(LambdaExpression condition)
        {
            return new MutatorsConfigurator<TRoot>(root, Condition.AndAlso((LambdaExpression)condition.ReplaceEachWithCurrent()));
        }

        public MutatorsConfigurator<TRoot> If(Expression<Func<TRoot, bool?>> condition)
        {
            return new MutatorsConfigurator<TRoot>(root, Condition.AndAlso((LambdaExpression)condition.ReplaceEachWithCurrent()));
        }

        public LambdaExpression Condition { get; }

        protected readonly ModelConfigurationNode root;
    }

    public class MutatorsConfigurator<TRoot, TChild, TValue>
    {
        public MutatorsConfigurator(ModelConfigurationNode root, Expression<Func<TRoot, TChild>> pathToChild, Expression<Func<TRoot, TValue>> pathToValue, LambdaExpression condition, MultiLanguageTextBase title)
        {
            this.Root = root;
            Title = title;
            PathToChild = pathToChild;
            PathToValue = pathToValue;
            Condition = condition;
        }

        public void SetMutator(MutatorConfiguration mutator)
        {
            MutatorConfiguration rootMutator;
            if (mutator.Type == typeof(TRoot))
                rootMutator = mutator;
            else
            {
                var pathToChild = PathToChild.ReplaceEachWithCurrent().ResolveInterfaceMembers();
                rootMutator = mutator.ToRoot((Expression<Func<TRoot, TChild>>)pathToChild);
            }

            if (PathToValue != null)
                Root.AddMutatorSmart(PathToValue.ResolveInterfaceMembers(), rootMutator.If(Condition));
        }

        public void SetMutator(LambdaExpression pathToTarget, MutatorConfiguration mutator)
        {
            Root.AddMutatorSmart(pathToTarget.ResolveInterfaceMembers(), mutator.If(Condition));
        }

        public void SetMutator(Expression pathToNode, Expression pathToTarget, MutatorConfiguration mutator)
        {
            Root.Traverse(pathToNode.ResolveInterfaceMembers(), true).AddMutator(pathToTarget, mutator.If(Condition));
        }

        public MutatorsConfigurator<TRoot, TChild, TValue> WithoutCondition()
        {
            return new MutatorsConfigurator<TRoot, TChild, TValue>(Root, PathToChild, PathToValue, null, Title);
        }

        public MutatorsConfigurator<TRoot, T, T> GoTo<T>(Expression<Func<TChild, T>> path)
        {
            var pathToChild = PathToChild.Merge(path);
            return new MutatorsConfigurator<TRoot, T, T>(Root, pathToChild, pathToChild, Condition, Title);
        }

        public MutatorsConfigurator<TRoot, TChild, T> Target<T>(Expression<Func<TValue, T>> path, MultiLanguageTextBase title = null)
        {
            return new MutatorsConfigurator<TRoot, TChild, T>(Root, PathToChild, PathToValue.Merge(path), Condition, title);
        }

        public MutatorsConfigurator<TRoot, TChild, TValue> If(Expression<Func<TChild, bool?>> condition)
        {
            return new MutatorsConfigurator<TRoot, TChild, TValue>(Root, PathToChild, PathToValue, Condition.AndAlso((LambdaExpression)PathToChild.Merge(condition).ReplaceEachWithCurrent()), Title);
        }

        public MutatorsConfigurator<TRoot, TChild, TValue> IfFromRoot(Expression<Func<TRoot, bool?>> condition)
        {
            return new MutatorsConfigurator<TRoot, TChild, TValue>(Root, PathToChild, PathToValue, Condition.AndAlso((LambdaExpression)condition.ReplaceEachWithCurrent()), Title);
        }

        public Expression<Func<TRoot, TChild>> PathToChild { get; }

        public Expression<Func<TRoot, TValue>> PathToValue { get; }
        public LambdaExpression Condition { get; }
        public MultiLanguageTextBase Title { get; }
        internal ModelConfigurationNode Root { get; }
    }
}