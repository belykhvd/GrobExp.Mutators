using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace GrobExp.Mutators
{
    public static class ContextRewriter
    {
        public static LambdaExpression Rebuild<TRoot, TContext>(ParameterExpression rootParameter, ReadOnlyCollection<ParameterExpression> parameters, Expression lambdaBody)
        {
            var excludingContext = parameters.Where(x => x.Type != typeof(TContext)).ToArray();

            var rewriter = new RootContextRewriter<TRoot, TContext>(rootParameter, excludingContext);
            return rewriter.Rebuild(lambdaBody);
        }

        private sealed class RootContextRewriter<TRoot, TContext> : ExpressionVisitor
        {            
            private readonly MemberExpression rootContextAccess;
            private readonly ParameterExpression[] parameters;            

            public RootContextRewriter(ParameterExpression rootParameter, ParameterExpression[] parameters)
            {
                var contextMemberInfo = typeof(TRoot).GetMember("Context").FirstOrDefault();
                if (contextMemberInfo == null)
                    throw new ArgumentException($"Type {typeof(TRoot)} does not contain Context member");

                this.parameters = parameters;
                rootContextAccess = Expression.MakeMemberAccess(rootParameter, contextMemberInfo);
            }

            public LambdaExpression Rebuild(Expression lambdaBody)
            {
                var rewrittenBody = Visit(lambdaBody);
                return Expression.Lambda(rewrittenBody, parameters);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node.Type == typeof(TContext)
                    ? (Expression) rootContextAccess
                    : node;
            }
        }
    }
}