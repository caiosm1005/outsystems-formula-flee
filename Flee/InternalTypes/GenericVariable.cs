namespace Flee.InternalTypes
{
    internal class GenericVariable<T> : IVariable, IGenericVariable<T>
    {


        public object MyValue = null!;
        public IVariable Clone()
        {
            GenericVariable<T> copy = new() { MyValue = MyValue };
            return copy;
        }

        public object GetValue()
        {
            return MyValue;
        }

        public Type VariableType => typeof(T);

        public object ValueAsObject
        {
            get => MyValue;
            set => MyValue = value ?? default(T)!;
        }
    }
}
