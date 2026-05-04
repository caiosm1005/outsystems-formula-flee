namespace Flee.CalcEngine.PublicTypes
{

    public class CircularReferenceException : Exception
    {
        private readonly string? _myCircularReferenceSource;

        internal CircularReferenceException()
        {
        }

        internal CircularReferenceException(string circularReferenceSource)
        {
            _myCircularReferenceSource = circularReferenceSource;
        }

        public override string Message => _myCircularReferenceSource == null
            ? "Circular reference detected in calculation engine"
            : $"Circular reference detected in calculation engine at '{_myCircularReferenceSource}'";
    }
}
