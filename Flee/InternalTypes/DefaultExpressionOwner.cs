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
