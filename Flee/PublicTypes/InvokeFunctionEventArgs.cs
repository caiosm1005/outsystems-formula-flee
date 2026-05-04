namespace Flee.PublicTypes
{
    public class InvokeFunctionEventArgs : EventArgs
    {
        internal InvokeFunctionEventArgs(string name, object[] arguments)
        {
            FunctionName = name;
            Arguments = arguments;
        }

        public string FunctionName { get; }

        public object[] Arguments { get; }

        public object? Result { get; set; }
    }
}
