namespace Flee.CalcEngine.PublicTypes
{
    public class CircularReferenceException : System.Exception
    {
        private readonly string _myCircularReferenceSource;

        internal CircularReferenceException()
        {
        }

        internal CircularReferenceException(string circularReferenceSource)
        {
            _myCircularReferenceSource = circularReferenceSource;
        }

        public override string Message
        {
            get
            {
                if (_myCircularReferenceSource == null)
                {
                    return "Circular reference detected in calculation engine";
                }
                else
                {
                    return $"Circular reference detected in calculation engine at '{_myCircularReferenceSource}'";
                }
            }
        }
    }
}