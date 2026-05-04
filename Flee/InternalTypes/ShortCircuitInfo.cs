using System.Collections;
using System.Reflection.Emit;

namespace Flee.InternalTypes
{
    internal class ShortCircuitInfo
    {

        public Stack Operands;
        public Stack Operators;
        private readonly Dictionary<object, Label> Labels;

        public ShortCircuitInfo()
        {
            Operands = new Stack();
            Operators = new Stack();
            Labels = [];
        }

        public void ClearTempState()
        {
            Operands.Clear();
            Operators.Clear();
        }

        public Label AddLabel(object key, Label lbl)
        {
            Labels.Add(key, lbl);
            return lbl;
        }

        public bool HasLabel(object key)
        {
            return Labels.ContainsKey(key);
        }

        public Label FindLabel(object key)
        {
            return Labels[key];
        }
    }
}
