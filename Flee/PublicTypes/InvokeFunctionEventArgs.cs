namespace Flee.PublicTypes
{
    public class InvokeFunctionEventArgs : EventArgs
    {

        private readonly string _myName;
        private readonly object[] _myArguments;

        private object? _myFunctionResult;
        internal InvokeFunctionEventArgs(string name, object[] arguments)
        {
            _myName = name;
            _myArguments = arguments;
        }

        public string FunctionName
        {
            get { return _myName; }
        }

        public object[] Arguments
        {
            get { return _myArguments; }
        }

        public object? Result
        {
            get { return _myFunctionResult; }
            set { _myFunctionResult = value; }
        }
    }
}
