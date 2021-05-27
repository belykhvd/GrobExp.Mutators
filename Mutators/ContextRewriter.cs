using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GrobExp.Mutators
{
    public static class ContextRewriter
    {
        public static LambdaExpression Rebuild<TSource, TContext>(ParameterExpression rootParameter, Expression lambdaBody)
        {
            var rewriter = new RootContextRewriter<TSource, TContext>(rootParameter);
            return rewriter.Rebuild(lambdaBody);
        }

        private sealed class RootContextRewriter<TSource, TContext> : ExpressionVisitor
        {
            private readonly MemberInfo rootContextMemberInfo = typeof(TSource).GetMember("Context").First();
            private readonly ParameterExpression rootParameter;
            private readonly MemberExpression rootContextAccess;

            public RootContextRewriter(ParameterExpression rootParameter)
            {
                this.rootParameter = rootParameter;
                rootContextAccess = Expression.MakeMemberAccess(rootParameter, rootContextMemberInfo);
            }

            public LambdaExpression Rebuild(Expression lambdaBody)
            {
                var rewrittenBody = Visit(lambdaBody);
                return Expression.Lambda(rewrittenBody, rootParameter);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node.Type == typeof(TContext)
                    ? (Expression)rootContextAccess
                    : node;
            }
        }
    }
}