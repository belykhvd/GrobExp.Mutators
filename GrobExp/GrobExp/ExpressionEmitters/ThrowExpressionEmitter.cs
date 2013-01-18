﻿using System;
using System.Linq.Expressions;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class ThrowExpressionEmitter : ExpressionEmitter<UnaryExpression>
    {
        protected override bool Emit(UnaryExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            Type operandType;
            var result = ExpressionEmittersCollection.Emit(node.Operand, context, returnDefaultValueLabel, out operandType);
            context.Il.Throw();
            resultType = typeof(void);
            return result;
        }
    }
}