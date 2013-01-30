using System;
using System.Linq.Expressions;
using System.Reflection;

using GrEmit;

namespace GrobExp.ExpressionEmitters
{
    internal class ParameterExpressionEmitter : ExpressionEmitter<ParameterExpression>
    {
        protected override bool Emit(ParameterExpression node, EmittingContext context, GroboIL.Label returnDefaultValueLabel, ResultType whatReturn, bool extend, out Type resultType)
        {
            if(whatReturn == ResultType.Void)
            {
                resultType = typeof(void);
                return false;
            }
            ConstructorInfo constructor = node.Type.GetConstructor(Type.EmptyTypes);
            extend &= node != context.ClosureParameter && node != context.ConstantsParameter && ((node.Type.IsClass && constructor != null) || node.Type.IsArray);
            int index = Array.IndexOf(context.Parameters, node);
            if(index >= 0)
            {
                if(extend)
                {
                    context.Il.Ldarg(index);
                    var parameterIsNotNullLabel = context.Il.DefineLabel("parameterIsNotNull");
                    context.Il.Brtrue(parameterIsNotNullLabel);
                    context.Il.Ldarga(index);
                    if(!node.Type.IsArray)
                        context.Il.Newobj(constructor);
                    else
                    {
                        context.Il.Ldc_I4(0);
                        context.Il.Newarr(node.Type.GetElementType());
                    }
                    context.Il.Stind(node.Type);
                    context.Il.MarkLabel(parameterIsNotNullLabel);
                }

                switch(whatReturn)
                {
                case ResultType.Value:
                    context.Il.Ldarg(index); // stack: [parameter]
                    resultType = node.Type;
                    break;
                case ResultType.ByRefAll:
                    context.Il.Ldarga(index); // stack: [&parameter]
                    resultType = node.Type.MakeByRefType();
                    break;
                case ResultType.ByRefValueTypesOnly:
                    if(node.Type.IsValueType)
                    {
                        context.Il.Ldarga(index); // stack: [&parameter]
                        resultType = node.Type.MakeByRefType();
                    }
                    else
                    {
                        context.Il.Ldarg(index); // stack: [parameter]
                        resultType = node.Type;
                    }
                    break;
                default:
                    throw new NotSupportedException("Result type '" + whatReturn + "' is not supported");
                }
                return false;
            }
            EmittingContext.LocalHolder variable;
            if(context.VariablesToLocals.TryGetValue(node, out variable))
            {
                if(extend)
                {
                    context.Il.Ldloc(variable);
                    var parameterIsNotNullLabel = context.Il.DefineLabel("parameterIsNotNull");
                    context.Il.Brtrue(parameterIsNotNullLabel);
                    if(!node.Type.IsArray)
                        context.Il.Newobj(constructor);
                    else
                    {
                        context.Il.Ldc_I4(0);
                        context.Il.Newarr(node.Type.GetElementType());
                    }
                    context.Il.Stloc(variable);
                    context.Il.MarkLabel(parameterIsNotNullLabel);
                }
                switch(whatReturn)
                {
                case ResultType.Value:
                    context.Il.Ldloc(variable); // stack: [variable]
                    resultType = node.Type;
                    break;
                case ResultType.ByRefAll:
                    context.Il.Ldloca(variable); // stack: [&variable]
                    resultType = node.Type.MakeByRefType();
                    break;
                case ResultType.ByRefValueTypesOnly:
                    if(node.Type.IsValueType)
                    {
                        context.Il.Ldloca(variable); // stack: [&variable]
                        resultType = node.Type.MakeByRefType();
                    }
                    else
                    {
                        context.Il.Ldloc(variable); // stack: [variable]
                        resultType = node.Type;
                    }
                    break;
                default:
                    throw new NotSupportedException("Result type '" + whatReturn + "' is not supported");
                }
                return false;
            }
            throw new InvalidOperationException("Unknown parameter " + node);
        }
    }
}