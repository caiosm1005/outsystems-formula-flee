﻿using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;


namespace Flee.ExpressionElements.LogicalBitwise
{
    internal class NotElement : UnaryElement
    {
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            if (ReferenceEquals(MyChild.ResultType, typeof(bool)))
            {
                EmitLogical(ilg, services);
            }
            else
            {
                MyChild.Emit(ilg, services);
                ilg.Emit(OpCodes.Not);
            }
        }

        private void EmitLogical(FleeILGenerator ilg, IServiceProvider services)
        {
            MyChild.Emit(ilg, services);
            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Ceq);
        }

        protected override Type GetResultType(Type childType)
        {
            if (ReferenceEquals(childType, typeof(bool)))
            {
                return typeof(bool);
            }
            else if (Utility.IsIntegralType(childType) == true)
            {
                return childType;
            }
            else
            {
                return null;
            }
        }
    }
}
