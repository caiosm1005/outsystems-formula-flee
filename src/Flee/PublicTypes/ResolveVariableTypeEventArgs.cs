namespace Flee.PublicTypes
{
    public class ResolveVariableTypeEventArgs : EventArgs
    {
        private readonly string _myName;
        private Type _myType;
        internal ResolveVariableTypeEventArgs(string name)
        {
            this._myName = name;
        }

        public string VariableName => _myName;

        public Type VariableType
        {
            get { return _myType; }
            set { _myType = value; }
        }
    }
}
