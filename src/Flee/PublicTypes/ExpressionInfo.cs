namespace Flee.PublicTypes
{
    public sealed class ExpressionInfo
    {


        private readonly IDictionary<string, object> _myData;
        internal ExpressionInfo()
        {
            _myData = new Dictionary<string, object>
            {
                {"ReferencedVariables", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)}
            };
        }

        internal void AddReferencedVariable(string name)
        {
            IDictionary<string, string> dict = (IDictionary<string, string>)_myData["ReferencedVariables"];
            dict[name] = name;
        }

        public string[] GetReferencedVariables()
        {
            IDictionary<string, string> dict = (IDictionary<string, string>)_myData["ReferencedVariables"];
            string[] arr = new string[dict.Count];
            dict.Keys.CopyTo(arr, 0);
            return arr;
        }
    }
}
