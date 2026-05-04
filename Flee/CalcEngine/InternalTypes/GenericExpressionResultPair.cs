namespace Flee.CalcEngine.InternalTypes
{
    internal class GenericExpressionResultPair<T> : ExpressionResultPair
    {
        public T MyResult = default!;
        public GenericExpressionResultPair()
        {
        }

        public override void Recalculate()
        {
            MyResult = (T)MyExpression.Evaluate();
        }

        public T Result => MyResult;

        public override Type ResultType => typeof(T);

        public override object ResultAsObject
        {
            get => MyResult!; set => MyResult = (T)value;
        }
    }
}
