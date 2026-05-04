namespace Flee.CalcEngine.InternalTypes
{
    public sealed class NodeEventArgs : EventArgs
    {
        internal NodeEventArgs()
        {
        }

        internal void SetData(string name, object result)
        {
            Name = name;
            Result = result;
        }

        public string Name { get; private set; } = string.Empty;

        public object Result { get; private set; } = null!;
    }
}
