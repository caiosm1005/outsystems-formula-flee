﻿using System.Collections;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements.Base
{
    /// <summary>
    /// Base class for expression elements that operate on two child elements
    /// </summary>
    internal abstract class BinaryExpressionElement : ExpressionElement
    {

        protected ExpressionElement MyLeftChild;
        protected ExpressionElement MyRightChild;
        private Type _myResultType;

        protected BinaryExpressionElement()
        {
        }

        /// <summary>
        /// Converts a list of binary elements into a binary tree
        /// </summary>
        /// <param name="childValues"></param>
        /// <param name="elementType"></param>
        /// <returns></returns>
        public static BinaryExpressionElement CreateElement(IList childValues, Type elementType)
        {
            BinaryExpressionElement firstElement = (BinaryExpressionElement)Activator.CreateInstance(elementType);
            firstElement.Configure((ExpressionElement)childValues[0], (ExpressionElement)childValues[2], childValues[1]);

            BinaryExpressionElement lastElement = firstElement;

            for (int i = 3; i <= childValues.Count - 1; i += 2)
            {
                BinaryExpressionElement element = (BinaryExpressionElement)Activator.CreateInstance(elementType);
                element.Configure(lastElement, (ExpressionElement)childValues[i + 1], childValues[i]);
                lastElement = element;
            }

            return lastElement;
        }

        protected abstract void GetOperation(object operation);

        protected void ValidateInternal(object op)
        {
            _myResultType = GetResultType(MyLeftChild.ResultType, MyRightChild.ResultType);

            if (_myResultType == null)
            {
                ThrowOperandTypeMismatch(op, MyLeftChild.ResultType, MyRightChild.ResultType);
            }
        }

        protected MethodInfo GetOverloadedBinaryOperator(string name, object operation)
        {
            Type leftType = MyLeftChild.ResultType;
            Type rightType = MyRightChild.ResultType;
            BinaryOperatorBinder binder = new(leftType, rightType);

            // If both arguments are of the same type, pick either as the owner type
            if (ReferenceEquals(leftType, rightType))
            {
                return Utility.GetOverloadedOperator(name, leftType, binder, leftType, rightType);
            }
            // Get the operator for both types
            MethodInfo leftMethod = Utility.GetOverloadedOperator(name, leftType, binder, leftType, rightType);
            MethodInfo rightMethod = Utility.GetOverloadedOperator(name, rightType, binder, leftType, rightType);

            // Pick the right one
            if (leftMethod == null & rightMethod == null)
            {
                // No operator defined for either
                return null;
            }
            else if (leftMethod == null)
            {
                return rightMethod;
            }
            else if (rightMethod == null)
            {
                return leftMethod;
            }
            else if (ReferenceEquals(leftMethod, rightMethod))
            {
                // same operator for both (most likely defined in a common base class)
                return leftMethod;
            }
            else
            {
                // Ambiguous call
                ThrowAmbiguousCallException(leftType, rightType, operation);
                return null;
            }
        }

        protected void EmitOverloadedOperatorCall(MethodInfo method, FleeILGenerator ilg, IServiceProvider services)
        {
            ParameterInfo[] @params = method.GetParameters();
            ParameterInfo pLeft = @params[0];
            ParameterInfo pRight = @params[1];

            EmitChildWithConvert(MyLeftChild, pLeft.ParameterType, ilg, services);
            EmitChildWithConvert(MyRightChild, pRight.ParameterType, ilg, services);
            ilg.Emit(OpCodes.Call, method);
        }

        protected void ThrowOperandTypeMismatch(object operation, Type leftType, Type rightType)
        {
            ThrowCompileException(CompileErrorResourceKeys.OperationNotDefinedForTypes, CompileExceptionReason.TypeMismatch, operation, leftType.Name, rightType.Name);
        }

        protected abstract Type GetResultType(Type leftType, Type rightType);

        protected static void EmitChildWithConvert(ExpressionElement child, Type resultType, FleeILGenerator ilg, IServiceProvider services)
        {
            child.Emit(ilg, services);
            bool converted = ImplicitConverter.EmitImplicitConvert(child.ResultType, resultType, ilg);
            Debug.Assert(converted, "convert failed");
        }

        protected bool AreBothChildrenOfType(Type target)
        {
            return IsChildOfType(MyLeftChild, target) & IsChildOfType(MyRightChild, target);
        }

        protected bool IsEitherChildOfType(Type target)
        {
            return IsChildOfType(MyLeftChild, target) || IsChildOfType(MyRightChild, target);
        }

        protected static bool IsChildOfType(ExpressionElement child, Type t)
        {
            return ReferenceEquals(child.ResultType, t);
        }

        /// <summary>
        /// Set the left and right operands, get the operation, and get the result type
        /// </summary>
        /// <param name="leftChild"></param>
        /// <param name="rightChild"></param>
        /// <param name="op"></param>
        private void Configure(ExpressionElement leftChild, ExpressionElement rightChild, object op)
        {
            MyLeftChild = leftChild;
            MyRightChild = rightChild;
            GetOperation(op);

            ValidateInternal(op);
        }

        public sealed override Type ResultType => _myResultType;
    }
}
