namespace Flee.PublicTypes
{
    public class ResolveVariableValueEventArgs : EventArgs
    {
        private readonly string _myName;
        private readonly Type _myType;

        private object? MyValue;
        internal ResolveVariableValueEventArgs(string name, Type t)
        {
            _myName = name;
            _myType = t;
        }

        public string VariableName
        {
            get { return _myName; }
        }

        public Type VariableType
        {
            get { return _myType; }
        }

        public object? VariableValue
        {
            get { return MyValue; }
            set { MyValue = value; }
        }
    }
}
