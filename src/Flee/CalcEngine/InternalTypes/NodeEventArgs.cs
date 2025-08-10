namespace Flee.CalcEngine.InternalTypes
{
    public sealed class NodeEventArgs : EventArgs
    {

        private string _myName;

        private object _myResult;

        internal NodeEventArgs()
        {
        }

        internal void SetData(string name, object result)
        {
            _myName = name;
            _myResult = result;
        }

        public string Name => _myName;

        public object Result => _myResult;
    }
}