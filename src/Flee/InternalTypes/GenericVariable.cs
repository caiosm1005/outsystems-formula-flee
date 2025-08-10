namespace Flee.InternalTypes
{
    internal class GenericVariable<T> : IVariable, IGenericVariable<T>
    {
        public object MyValue;
        public IVariable Clone()
        {
            GenericVariable<T> copy = new GenericVariable<T> { MyValue = MyValue };
            return copy;
        }

        public object GetValue()
        {
            return MyValue;
        }

        public System.Type VariableType => typeof(T);

        public object ValueAsObject
        {
            get { return MyValue; }
            set
            {
                if (value == null)
                {
                    MyValue = default(T);
                }
                else
                {
                    MyValue = value;
                }
            }
        }
    }
}
