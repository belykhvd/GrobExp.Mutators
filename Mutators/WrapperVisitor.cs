using System.Linq;
using System.Linq.Expressions;

namespace GrobExp.Mutators
{
    public sealed class WrapperVisitor<TSource, TContext> : ExpressionVisitor
    {
        private readonly ParameterExpression wrapperParameter;
        private readonly MemberExpression sourceMemberParameter;
        private readonly MemberExpression contextMemberParameter;

        public WrapperVisitor(ParameterExpression wrapperParameter)
        {
            var sourceMemberInfo = typeof(Wrapper<TSource, TContext>).GetMember(nameof(Wrapper<TSource, TContext>.Source)).First();
            var contextMemberInfo = typeof(Wrapper<TSource, TContext>).GetMember(nameof(Wrapper<TSource, TContext>.Context)).First();

            sourceMemberParameter = Expression.MakeMemberAccess(wrapperParameter, sourceMemberInfo);
            contextMemberParameter = Expression.MakeMemberAccess(wrapperParameter, contextMemberInfo);

            this.wrapperParameter = wrapperParameter;
        }

        public Expression Rebuild(Expression lambdaBody)
        {
            return Visit(lambdaBody);
        }

        protected override Expression VisitInvocation(InvocationExpression node)
        {
            if (node == null)
                return base.VisitInvocation(node);

            var newExpression = base.Visit(node.Expression);

            var newArguments = node.Arguments.ToArray();
            for (var i = 0; i < newArguments.Length; i++)
            {
                if (newArguments[i].Type == typeof(TSource))
                    newArguments[i] = wrapperParameter;
            }
            
            var newInvocation = Expression.Invoke(newExpression, newArguments);
            return newInvocation;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == null)
                return base.VisitParameter(node);

            if (node.Type == typeof(TSource))
                return sourceMemberParameter;

            if (node.Type == typeof(TContext))
                return contextMemberParameter;

            return base.VisitParameter(node);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (node == null || node.Parameters.All(x => x.Type != typeof(TSource)))
                return base.VisitLambda(node);

            var parameters = node.Parameters.ToArray();
            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Type == typeof(TSource))
                    parameters[i] = wrapperParameter;
            }

            var body = base.Visit(node.Body);

            return Expression.Lambda(body, parameters);
        }
    }
}