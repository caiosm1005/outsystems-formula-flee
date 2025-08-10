namespace Flee.PublicTypes
{
    public class ResolveFunctionEventArgs : EventArgs
    {

        private readonly string MyName;
        private readonly Type[] MyArgumentTypes;

        private Type _myReturnType;
        internal ResolveFunctionEventArgs(string name, Type[] argumentTypes)
        {
            MyName = name;
            MyArgumentTypes = argumentTypes;
        }

        public string FunctionName
        {
            get { return MyName; }
        }

        public Type[] ArgumentTypes
        {
            get { return MyArgumentTypes; }
        }

        public Type ReturnType
        {
            get { return _myReturnType; }
            set { _myReturnType = value; }
        }
    }
}
