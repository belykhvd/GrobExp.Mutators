using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using GrEmit;

using GrobExp.ExpressionEmitters;

namespace GrobExp
{
    internal static class ExpressionEmittersCollection
    {
        public static bool Emit(Expression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, out Type resultType)
        {
            return Emit(node, context, returnDefaultValueLabel, ResultType.Value, false, out resultType);
        }

        public static void Emit(Expression node, EmittingContext context, out Type resultType)
        {
            Emit(node, context, null, ResultType.Value, false, out resultType);
        }

        public static bool Emit(Expression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            IExpressionEmitter emitter;
            if(!expressionEmitters.TryGetValue(node.NodeType, out emitter))
                throw new NotSupportedException("Node type '" + node.NodeType + "' is not supported");
            return emitter.Emit(node, context, returnDefaultValueLabel, whatReturn, extend, out resultType);
        }

        private static readonly Dictionary<ExpressionType, IExpressionEmitter> expressionEmitters = new Dictionary<ExpressionType, IExpressionEmitter>
            {
                {ExpressionType.Parameter, new ParameterExpressionEmitter()},
                {ExpressionType.MemberAccess, new MemberAccessExpressionEmitter()},
                {ExpressionType.Convert, new ConvertExpressionEmitter()},
                {ExpressionType.Constant, new ConstantExpressionEmitter()},
                {ExpressionType.ArrayIndex, new ArrayIndexExpressionEmitter()},
                {ExpressionType.Index, new IndexExpressionEmitter()},
                {ExpressionType.ArrayLength, new ArrayLengthExpressionEmitter()},
                {ExpressionType.Call, new CallExpressionEmitter()},
                {ExpressionType.Block, new BlockExpressionEmitter()},
                {ExpressionType.Assign, new AssignExpressionEmitter()},
                {ExpressionType.New, new NewExpressionEmitter()},
                {ExpressionType.Conditional, new ConditionalExpressionEmitter()},
                {ExpressionType.Equal, new EqualityExpressionEmitter()},
                {ExpressionType.NotEqual, new EqualityExpressionEmitter()},
                {ExpressionType.GreaterThan, new ComparisonExpressionEmitter()},
                {ExpressionType.LessThan, new ComparisonExpressionEmitter()},
                {ExpressionType.GreaterThanOrEqual, new ComparisonExpressionEmitter()},
                {ExpressionType.LessThanOrEqual, new ComparisonExpressionEmitter()},
                {ExpressionType.Add, new ArithmeticExpressionEmitter()},
                {ExpressionType.AddChecked, new ArithmeticExpressionEmitter()},
                {ExpressionType.Subtract, new ArithmeticExpressionEmitter()},
                {ExpressionType.SubtractChecked, new ArithmeticExpressionEmitter()},
                {ExpressionType.Multiply, new ArithmeticExpressionEmitter()},
                {ExpressionType.MultiplyChecked, new ArithmeticExpressionEmitter()},
                {ExpressionType.Divide, new ArithmeticExpressionEmitter()},
                {ExpressionType.Modulo, new ArithmeticExpressionEmitter()},
                {ExpressionType.LeftShift, new ArithmeticExpressionEmitter()},
                {ExpressionType.RightShift, new ArithmeticExpressionEmitter()},
                {ExpressionType.And, new ArithmeticExpressionEmitter()},
                {ExpressionType.Or, new ArithmeticExpressionEmitter()},
                {ExpressionType.ExclusiveOr, new ArithmeticExpressionEmitter()},
                {ExpressionType.OrElse, new LogicalExpressionEmitter()},
                {ExpressionType.AndAlso, new LogicalExpressionEmitter()},
                {ExpressionType.Not, new NotExpressionEmitter()},
                {ExpressionType.MemberInit, new MemberInitExpressionEmitter()},
                {ExpressionType.NewArrayInit, new NewArrayInitExpressionEmitter()},
                {ExpressionType.NewArrayBounds, new NewArrayBoundsExpressionEmitter()},
                {ExpressionType.Default, new DefaultExpressionEmitter()},
                {ExpressionType.Coalesce, new CoalesceExpressionEmitter()},
                {ExpressionType.Lambda, new LambdaExpressionEmitter()},
                {ExpressionType.Label, new LabelExpressionEmitter()},
                {ExpressionType.Goto, new GotoExpressionEmitter()},
            };
    }
}