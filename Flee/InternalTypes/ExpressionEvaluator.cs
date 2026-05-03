using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    internal delegate T ExpressionEvaluator<T>(object owner, ExpressionContext context, VariableCollection variables);
}
