﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GrobExp.Compiler
{
    public static class ExpressionHashCalculator
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct Zuid
        {
            [FieldOffset(0)]
            public Guid guid;

            [FieldOffset(0)]
            public uint a;

            [FieldOffset(4)]
            public uint b;

            [FieldOffset(8)]
            public uint c;

            [FieldOffset(12)]
            public uint d;

            [FieldOffset(0)]
            public ulong lo;

            [FieldOffset(4)]
            public ulong b8;

            [FieldOffset(8)]
            public ulong hi;
        }

        public static int CalcHashCode(Expression node, bool strictly)
        {
            var hashCodes = new List<int>();
            CalcHashCode(node, new Context
            {
                Strictly = strictly,
                Parameters = new Dictionary<Type, Dictionary<ParameterExpression, int>>(),
                Labels = new Dictionary<LabelTarget, int>(),
                HashCodes = hashCodes,
                Hard =  false
            });
            const int x = 1084996987; //for sake of primality
            var result = 0;
            foreach(var hashCode in hashCodes)
            {
                unchecked
                {
                    result = result * x + hashCode;
                }
            }
            return result;
        }

        public static Guid CalcStrongHashCode(Expression node)
        {
            var hashCodes = new List<int>();
            CalcHashCode(node, new Context
            {
                Strictly = false,
                Parameters = new Dictionary<Type, Dictionary<ParameterExpression, int>>(),
                Labels = new Dictionary<LabelTarget, int>(),
                HashCodes = hashCodes,
                Hard = true
            });
            const uint x = 1084996987; //for sake of primality
            return Horner(x, hashCodes.ToArray()).guid;
        }

        private static Zuid Horner(uint x, int[] coeffs)
        {
            var result = default(Zuid);
            foreach (var hashCode in coeffs)
            {
                result = Add(Mul(result, x), ToZuid(hashCode));
            }
            return result;
        }

        private static Zuid ToZuid(int x)
        {
            var result = default(Zuid);
            result.a = unchecked((uint)x);
            return result;
        }

        private static Zuid Add(Zuid a, Zuid b)
        {
            Zuid result = default(Zuid);
            var c = ulong.MaxValue - a.lo > b.lo ? 1UL : 0UL;
            result.lo = unchecked (a.lo + b.lo);
            result.hi = unchecked (a.hi + b.hi + c);
            return result;
        }

        private static Zuid Mul(Zuid z, uint x)
        {
            var result = default(Zuid);
            result.lo = (ulong)z.a * x;
            var temp = default(Zuid);
            temp.b8 = (ulong)z.b * x;
            result = Add(result, temp);
            temp = default(Zuid);
            temp.hi = (ulong)z.c * x;
            result = Add(result, temp);
            temp = default(Zuid);
            temp.c = z.c * x;
            return Add(result, temp);
        }

        private static void CalcHashCode(IEnumerable<Expression> list, Context context)
        {
            foreach(var exp in list)
                CalcHashCode(exp, context);
        }

        private static void CalcHashCode(Expression node, Context context)
        {
            if(node == null)
            {
                context.HashCodes.Add(0);
                return;
            }
            CalcHashCode(node.NodeType, context);
            CalcHashCode(node.Type, context);
            switch(node.NodeType)
            {
            case ExpressionType.Add:
            case ExpressionType.AddAssign:
            case ExpressionType.AddAssignChecked:
            case ExpressionType.AddChecked:
            case ExpressionType.And:
            case ExpressionType.AndAlso:
            case ExpressionType.AndAssign:
            case ExpressionType.ArrayIndex:
            case ExpressionType.Assign:
            case ExpressionType.Coalesce:
            case ExpressionType.Divide:
            case ExpressionType.DivideAssign:
            case ExpressionType.Equal:
            case ExpressionType.ExclusiveOr:
            case ExpressionType.ExclusiveOrAssign:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LeftShift:
            case ExpressionType.LeftShiftAssign:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.Modulo:
            case ExpressionType.ModuloAssign:
            case ExpressionType.Multiply:
            case ExpressionType.MultiplyAssign:
            case ExpressionType.MultiplyAssignChecked:
            case ExpressionType.MultiplyChecked:
            case ExpressionType.NotEqual:
            case ExpressionType.Or:
            case ExpressionType.OrAssign:
            case ExpressionType.OrElse:
            case ExpressionType.Power:
            case ExpressionType.PowerAssign:
            case ExpressionType.RightShift:
            case ExpressionType.RightShiftAssign:
            case ExpressionType.Subtract:
            case ExpressionType.SubtractAssign:
            case ExpressionType.SubtractAssignChecked:
            case ExpressionType.SubtractChecked:
                CalcHashCodeBinary((BinaryExpression)node, context);
                break;
            case ExpressionType.ArrayLength:
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
            case ExpressionType.Decrement:
            case ExpressionType.Increment:
            case ExpressionType.IsFalse:
            case ExpressionType.IsTrue:
            case ExpressionType.Negate:
            case ExpressionType.NegateChecked:
            case ExpressionType.Not:
            case ExpressionType.OnesComplement:
            case ExpressionType.PostDecrementAssign:
            case ExpressionType.PostIncrementAssign:
            case ExpressionType.PreDecrementAssign:
            case ExpressionType.PreIncrementAssign:
            case ExpressionType.TypeAs:
            case ExpressionType.UnaryPlus:
            case ExpressionType.Unbox:
            case ExpressionType.Quote:
            case ExpressionType.Throw:
                CalcHashCodeUnary((UnaryExpression)node, context);
                break;
            case ExpressionType.Parameter:
                CalcHashCodeParameter((ParameterExpression)node, context);
                break;
            case ExpressionType.Block:
                CalcHashCodeBlock((BlockExpression)node, context);
                break;
            case ExpressionType.Call:
                CalcHashCodeCall((MethodCallExpression)node, context);
                break;
            case ExpressionType.Conditional:
                CalcHashCodeConditional((ConditionalExpression)node, context);
                break;
            case ExpressionType.Constant:
                CalcHashCodeConstant((ConstantExpression)node, context);
                break;
            case ExpressionType.DebugInfo:
                CalcHashCodeDebugInfo((DebugInfoExpression)node, context);
                break;
            case ExpressionType.Default:
                CalcHashCodeDefault((DefaultExpression)node, context);
                break;
            case ExpressionType.Dynamic:
                CalcHashCodeDynamic((DynamicExpression)node, context);
                break;
            case ExpressionType.Extension:
                CalcHashCodeExtension(node, context);
                break;
            case ExpressionType.Goto:
                CalcHashCodeGoto((GotoExpression)node, context);
                break;
            case ExpressionType.Index:
                CalcHashCodeIndex((IndexExpression)node, context);
                break;
            case ExpressionType.Invoke:
                CalcHashCodeInvoke((InvocationExpression)node, context);
                break;
            case ExpressionType.Label:
                CalcHashCodeLabel((LabelExpression)node, context);
                break;
            case ExpressionType.Lambda:
                CalcHashCodeLambda((LambdaExpression)node, context);
                break;
            case ExpressionType.ListInit:
                CalcHashCodeListInit((ListInitExpression)node, context);
                break;
            case ExpressionType.Loop:
                CalcHashCodeLoop((LoopExpression)node, context);
                break;
            case ExpressionType.MemberAccess:
                CalcHashCodeMemberAccess((MemberExpression)node, context);
                break;
            case ExpressionType.MemberInit:
                CalcHashCodeMemberInit((MemberInitExpression)node, context);
                break;
            case ExpressionType.New:
                CalcHashCodeNew((NewExpression)node, context);
                break;
            case ExpressionType.NewArrayBounds:
            case ExpressionType.NewArrayInit:
                CalcHashCodeNewArray((NewArrayExpression)node, context);
                break;
            case ExpressionType.RuntimeVariables:
                CalcHashCodeRuntimeVariables((RuntimeVariablesExpression)node, context);
                break;
            case ExpressionType.Switch:
                CalcHashCodeSwitch((SwitchExpression)node, context);
                break;
            case ExpressionType.Try:
                CalcHashCodeTry((TryExpression)node, context);
                break;
            case ExpressionType.TypeEqual:
            case ExpressionType.TypeIs:
                CalcHashCodeTypeBinary((TypeBinaryExpression)node, context);
                break;
            default:
                throw new NotSupportedException("Node type '" + node.NodeType + "' is not supported");
            }
        }

        private static void CalcHashCode(ExpressionType expressionType, Context context)
        {
            context.HashCodes.Add((int)expressionType);
        }

        private static void CalcHashCode(MemberBindingType bindingType, Context context)
        {
            context.HashCodes.Add((int)bindingType);
        }

        private static void CalcHashCode(MemberInfo member, Context context)
        {
            if(member == null)
            {
                context.HashCodes.Add(0);
                return;
            }
            context.HashCodes.Add(member.Module.MetadataToken);
            context.HashCodes.Add(member.MetadataToken);
        }

        private static void CalcHashCode(string str, Context context)
        {
            if(str == null)
            {
                context.HashCodes.Add(0);
                return;
            }
            if(!context.Hard)
                context.HashCodes.Add(str.GetHashCode());
            else
            {
                for(var i = 0; i < str.Length; i += 2)
                    context.HashCodes.Add((str[i] << 16) + (i + 1 == str.Length ? 0 : str[i + 1]));
            }
        }

        private static void CalcHashCode(int x, Context context)
        {
            context.HashCodes.Add(x);
        }

        private static void CalcHashCode(Type type, Context context)
        {
            context.HashCodes.Add(type.Module.MetadataToken);
            context.HashCodes.Add(type.MetadataToken);
        }

        private static void CalcHashCode(GotoExpressionKind kind, Context context)
        {
            if(context.Hard)
                CalcHashCode((int)kind, context);
            else
                context.HashCodes.Add((int)kind);
        }

        private static void CalcHashCodeObject(object obj, Context context)
        {
            if(obj == null)
            {
                context.HashCodes.Add(0);
                return;
            }
            if(!context.Hard)
                context.HashCodes.Add(obj.GetHashCode());
            else
            {
                var typecode = Type.GetTypeCode(obj.GetType());
                switch(typecode)
                {
                case TypeCode.Boolean:
                    CalcHashCode((bool)obj ? 1 : 0, context);
                    break;
                case TypeCode.Char:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                    CalcHashCode(unchecked((int)obj), context);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    CalcHashCode((int)((ulong)obj >> 32), context);
                    CalcHashCode((int)((ulong)obj % (1L << 32)), context);
                    break;
                case TypeCode.String:
                    CalcHashCode((string)obj, context);
                    break;
                default:
                    throw new NotSupportedException("Type is not supported by hard hashing");
                }
            }
        }

        private static void CalcHashCodeParameter(ParameterExpression node, Context context)
        {
            if(node == null)
            {
                context.HashCodes.Add(0);
                return;
            }

            var parameterType = node.IsByRef ? node.Type.MakeByRefType() : node.Type;
            CalcHashCode(parameterType, context);
            if(context.Strictly)
                CalcHashCode(node.Name, context);
            else
            {
                Dictionary<ParameterExpression, int> parameters;
                if(!context.Parameters.TryGetValue(parameterType, out parameters))
                    context.Parameters.Add(parameterType, parameters = new Dictionary<ParameterExpression, int>());
                int index;
                if(!parameters.TryGetValue(node, out index))
                    parameters.Add(node, index = parameters.Count);
                CalcHashCode(index, context);
            }
        }

        private static void CalcHashCodeUnary(UnaryExpression node, Context context)
        {
            CalcHashCode(node.Method, context);
            CalcHashCode(node.Operand, context);
        }

        private static void CalcHashCodeBinary(BinaryExpression node, Context context)
        {
            CalcHashCode(node.Method, context);
            CalcHashCode(node.Left, context);
            CalcHashCode(node.Right, context);
        }

        private static void CalcHashCodeBlock(BlockExpression node, Context context)
        {
            if(context.Strictly)
            {
                foreach(var variable in node.Variables)
                    CalcHashCodeParameter(variable, context);
            }

            if(!context.Strictly)
            {
                foreach(var variable in node.Variables)
                {
                    Dictionary<ParameterExpression, int> parameters;
                    if(!context.Parameters.TryGetValue(variable.Type, out parameters))
                        context.Parameters.Add(variable.Type, parameters = new Dictionary<ParameterExpression, int>());
                    parameters.Add(variable, parameters.Count);
                }
            }
            CalcHashCode(node.Expressions, context);
            if(!context.Strictly)
            {
                foreach(var variable in node.Variables)
                    context.Parameters[variable.Type].Remove(variable);
            }
        }

        private static void CalcHashCodeCall(MethodCallExpression node, Context context)
        {
            CalcHashCode(node.Method, context);
            CalcHashCode(node.Object, context);
            CalcHashCode(node.Arguments, context);
        }

        private static void CalcHashCodeConditional(ConditionalExpression node, Context context)
        {
            CalcHashCode(node.Test, context);
            CalcHashCode(node.IfTrue, context);
            CalcHashCode(node.IfFalse, context);
        }

        private static void CalcHashCodeConstant(ConstantExpression node, Context context)
        {
            CalcHashCodeObject(node.Value, context);
        }

        private static void CalcHashCodeDebugInfo(DebugInfoExpression node, Context context)
        {
            throw new NotSupportedException();
        }

        private static void CalcHashCodeDefault(DefaultExpression node, Context context)
        {
            //empty body
        }

        private static void CalcHashCodeDynamic(DynamicExpression node, Context context)
        {
            throw new NotSupportedException();
        }

        private static void CalcHashCodeExtension(Expression node, Context context)
        {
            throw new NotSupportedException();
        }

        private static void CalcHashCodeLabel(LabelTarget target, Context context)
        {
            if(target == null)
            {
                context.HashCodes.Add(0);
                return;
            }

            int labelId;
            if (!context.Labels.TryGetValue(target, out labelId))
                context.Labels.Add(target, labelId = context.Labels.Count);
            CalcHashCode(labelId, context);
        }

        private static void CalcHashCodeGoto(GotoExpression node, Context context)
        {
            CalcHashCode(node.Kind, context);
            CalcHashCodeLabel(node.Target, context);
            CalcHashCode(node.Value, context);
        }

        private static void CalcHashCodeIndex(IndexExpression node, Context context)
        {
            CalcHashCode(node.Object, context);
            CalcHashCode(node.Indexer, context);
            CalcHashCode(node.Arguments, context);
        }

        private static void CalcHashCodeInvoke(InvocationExpression node, Context context)
        {
            CalcHashCode(new[] {node.Expression}.Concat(node.Arguments), context);
        }

        private static void CalcHashCodeLabel(LabelExpression node, Context context)
        {
            int labelId;
            if (!context.Labels.TryGetValue(node.Target, out labelId))
            {
                labelId = context.Labels.Count;
                context.HashCodes.Add(labelId);
            }
            context.HashCodes.Add(labelId);
            CalcHashCode(node.DefaultValue, context);
        }

        private static void CalcHashCodeLambda(LambdaExpression node, Context context)
        {
            if(!context.Strictly)
            {
                foreach(var parameter in node.Parameters)
                {
                    Dictionary<ParameterExpression, int> parameters;
                    if(!context.Parameters.TryGetValue(parameter.Type, out parameters))
                        context.Parameters.Add(parameter.Type, parameters = new Dictionary<ParameterExpression, int>());
                    parameters.Add(parameter, parameters.Count);
                }
            }
            CalcHashCode(node.Body, context);
            if(!context.Strictly)
            {
                foreach(var parameter in node.Parameters)
                    context.Parameters[parameter.Type].Remove(parameter);
            }
        }

        private static void CalcHashCodeElemInits(IEnumerable<ElementInit> inits, Context context)
        {
            foreach(var init in inits)
            {
                CalcHashCode(init.AddMethod, context);
                CalcHashCode(init.Arguments, context);
            }
        }

        private static void CalcHashCodeListInit(ListInitExpression node, Context context)
        {
            CalcHashCode(node.NewExpression, context);
            CalcHashCodeElemInits(node.Initializers, context);
        }

        private static void CalcHashCodeLoop(LoopExpression node, Context context)
        {
            CalcHashCode(node.Body, context);
            CalcHashCodeLabel(node.ContinueLabel, context);
            CalcHashCodeLabel(node.BreakLabel, context);
        }

        private static void CalcHashCodeMemberAccess(MemberExpression node, Context context)
        {
            CalcHashCode(node.Member, context);
            CalcHashCode(node.Expression, context);
        }

        private static void CalcHashCodeMemberInit(MemberInitExpression node, Context context)
        {
            CalcHashCode(node.NewExpression, context);
            foreach(var memberBinding in node.Bindings)
            {
                var binding = (MemberAssignment)memberBinding;
                CalcHashCode(binding.BindingType, context);
                CalcHashCode(binding.Member, context);
                CalcHashCode(binding.Expression, context);
            }
        }

        private static void CalcHashCodeNew(NewExpression node, Context context)
        {
            CalcHashCode(node.Constructor, context);
            CalcHashCode(node.Arguments, context);

            if(node.Members != null)
            {
                var normalizedMembers = node.Members.ToList();
                if(!context.Strictly)
                {
                    normalizedMembers.Sort((first, second) =>
                    {
                        if(first.Module != second.Module)
                            return string.Compare(first.Module.FullyQualifiedName, second.Module.FullyQualifiedName, StringComparison.InvariantCulture);
                        return first.MetadataToken - second.MetadataToken;
                    });
                }
                foreach (var member in node.Members)
                    CalcHashCode(member, context);
            }
        }

        private static void CalcHashCodeNewArray(NewArrayExpression node, Context context)
        {
            CalcHashCode(node.Expressions, context);
        }

        private static void CalcHashCodeRuntimeVariables(RuntimeVariablesExpression node, Context context)
        {
            throw new NotSupportedException();
        }

        private static void CalcHashCodeCases(IEnumerable<SwitchCase> cases, Context context)
        {
            foreach(var oneCase in cases)
            {
                CalcHashCode(oneCase.Body, context);
                CalcHashCode(oneCase.TestValues, context);
            }
        }

        private static void CalcHashCodeSwitch(SwitchExpression node, Context context)
        {
            CalcHashCodeCases(node.Cases, context);
            CalcHashCode(node.Comparison, context);
            CalcHashCode(node.DefaultBody, context);
            CalcHashCode(node.SwitchValue, context);
        }

        private static void CalcHashCodeCatchBlocks(IEnumerable<CatchBlock> handlers, Context context)
        {
            foreach(var handler in handlers)
            {
                CalcHashCode(handler.Body, context);
                CalcHashCode(handler.Filter, context);
                CalcHashCode(handler.Test, context);
                CalcHashCodeParameter(handler.Variable, context);
            }
        }

        private static void CalcHashCodeTry(TryExpression node, Context context)
        {
            CalcHashCode(node.Body, context);
            CalcHashCode(node.Fault, context);
            CalcHashCode(node.Finally, context);
            CalcHashCodeCatchBlocks(node.Handlers, context);
        }

        private static void CalcHashCodeTypeBinary(TypeBinaryExpression node, Context context)
        {
            CalcHashCode(node.Expression, context);
            CalcHashCode(node.TypeOperand, context);
        }

        private class Context
        {
            public bool Strictly { get; set; }
            public Dictionary<Type, Dictionary<ParameterExpression, int>> Parameters { get; set; }
            public Dictionary<LabelTarget, int> Labels { get; set; }
            public List<int> HashCodes { get; set; }
            public bool Hard { get; set; }
        }
    }
}