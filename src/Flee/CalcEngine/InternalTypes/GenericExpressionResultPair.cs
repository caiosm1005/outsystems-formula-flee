namespace Flee.CalcEngine.InternalTypes
{
    internal class GenericExpressionResultPair<T> : ExpressionResultPair
    {
        public T MyResult;
        public GenericExpressionResultPair()
        {
        }

        public override void Recalculate()
        {
            MyResult = (T)MyExpression.Evaluate();
        }

        public T Result => MyResult;

        public override System.Type ResultType => typeof(T);

        public override object ResultAsObject
        {
            get { return MyResult; }
            set { MyResult = (T)value; }
        }
    }
}