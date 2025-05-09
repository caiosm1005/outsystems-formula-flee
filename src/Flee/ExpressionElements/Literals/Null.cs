﻿using System.Reflection.Emit;
using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.Literals
{
    internal class NullLiteralElement : LiteralElement
    {
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            ilg.Emit(OpCodes.Ldnull);
        }

        public override Type ResultType => typeof(Null);
    }
}
