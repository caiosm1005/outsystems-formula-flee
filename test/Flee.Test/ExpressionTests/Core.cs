using System;
using Flee.PublicTypes;

namespace Flee.Test.ExpressionTests
{
    public class Core
    {
        protected static IDynamicExpression CreateDynamicExpression(string expression, ExpressionContext context)
        {
            return context.CompileDynamic(expression);
        }

        protected static void WriteMessage(string msg, params object[] args)
        {
            msg = string.Format(msg, args);
            Console.WriteLine(msg);
        }
    }
}
