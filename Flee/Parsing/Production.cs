using System.Collections;

namespace Flee.Parsing
{

    /**
    * A production node. This class represents a grammar production
    * (i.e. a list of child nodes) in a parse tree. The productions
    * are created by a parser, that adds children a according to a
    * set of production patterns (i.e. grammar rules).
    */
    internal class Production(ProductionPattern pattern) : Node
    {
        private readonly ArrayList _children = [];

        public override int Id => Pattern.Id;

        public override string Name => Pattern.Name;

        public override int Count => _children.Count;

        public override Node this[int index] => index < 0 || index >= _children.Count ? null! : (Node)_children[index]!;

        public void AddChild(Node child)
        {
            if (child != null)
            {
                child.SetParent(this);
                _ = _children.Add(child);
            }
        }

        public ProductionPattern Pattern { get; } = pattern;

        public ProductionPattern GetPattern()
        {
            return Pattern;
        }

        internal override bool IsHidden()
        {
            return Pattern.Synthetic;
        }

        public override string ToString()
        {
            return Pattern.Name + '(' + Pattern.Id + ')';
        }
    }
}
