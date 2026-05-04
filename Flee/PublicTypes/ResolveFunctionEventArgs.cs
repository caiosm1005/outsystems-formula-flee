namespace Flee.PublicTypes
{
    public class ResolveFunctionEventArgs : EventArgs
    {
        internal ResolveFunctionEventArgs(string name, Type[] argumentTypes)
        {
            FunctionName = name;
            ArgumentTypes = argumentTypes;
        }

        public string FunctionName { get; }

        public Type[] ArgumentTypes { get; }

        public Type? ReturnType { get; set; }
    }
}
