using Flee.PublicTypes;

namespace Flee.CalcEngine.InternalTypes
{
    internal abstract class ExpressionResultPair
    {
        protected IDynamicExpression MyExpression = null!;

        protected ExpressionResultPair()
        {
        }

        public abstract void Recalculate();

        public void SetExpression(IDynamicExpression e)
        {
            MyExpression = e;
        }

        public void SetName(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public string Name { get; private set; } = string.Empty;

        public abstract Type ResultType { get; }
        public abstract object ResultAsObject { get; set; }

        public IDynamicExpression Expression => MyExpression;
    }
}
