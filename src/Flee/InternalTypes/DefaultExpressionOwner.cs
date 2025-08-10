using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    internal class DefaultExpressionOwner
    {


        private static readonly DefaultExpressionOwner OurInstance = new DefaultExpressionOwner();

        private DefaultExpressionOwner()
        {
        }

        public static object Instance => OurInstance;
    }
}