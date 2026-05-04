namespace Flee.PublicTypes
{
    public class ResolveVariableTypeEventArgs : EventArgs
    {
        internal ResolveVariableTypeEventArgs(string name)
        {
            VariableName = name;
        }

        public string VariableName { get; }

        public Type? VariableType { get; set; }
    }
}
