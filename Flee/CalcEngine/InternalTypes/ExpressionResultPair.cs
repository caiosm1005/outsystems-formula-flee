using Flee.PublicTypes;

namespace Flee.CalcEngine.InternalTypes
{
    internal abstract class ExpressionResultPair
    {

        private string _myName = string.Empty;

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
            _myName = name;
        }

        public override string ToString()
        {
            return _myName;
        }

        public string Name => _myName;

        public abstract Type ResultType { get; }
        public abstract object ResultAsObject { get; set; }

        public IDynamicExpression Expression => MyExpression;
    }
}
