namespace Flee.PublicTypes
{
    public class ResolveVariableValueEventArgs : EventArgs
    {
        internal ResolveVariableValueEventArgs(string name, Type t)
        {
            VariableName = name;
            VariableType = t;
        }

        public string VariableName { get; }

        public Type VariableType { get; }

        public object? VariableValue { get; set; }
    }
}
