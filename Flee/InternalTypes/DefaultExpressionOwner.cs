namespace Flee.InternalTypes
{
    internal class DefaultExpressionOwner
    {


        private static readonly DefaultExpressionOwner OurInstance = new();

        private DefaultExpressionOwner()
        {
        }

        public static object Instance => OurInstance;
    }
}
