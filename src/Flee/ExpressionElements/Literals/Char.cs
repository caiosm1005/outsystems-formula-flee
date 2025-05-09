﻿using Flee.ExpressionElements.Base.Literals;

using Flee.InternalTypes;


namespace Flee.ExpressionElements.Literals
{
    internal class CharLiteralElement : LiteralElement
    {
        private readonly char _myValue;
        public CharLiteralElement(char value)
        {
            _myValue = value;
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            int intValue = Convert.ToInt32(_myValue);
            EmitLoad(intValue, ilg);
        }

        public override Type ResultType => typeof(char);
    }
}
