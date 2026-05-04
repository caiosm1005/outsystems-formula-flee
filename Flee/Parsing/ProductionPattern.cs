using System.Collections;
using System.Text;


namespace Flee.Parsing
{

    /**
     * A production pattern. This class represents a set of production
     * alternatives that together forms a single production. A
     * production pattern is identified by an integer id and a name,
     * both provided upon creation. The pattern id is used for
     * referencing the production pattern from production pattern
     * elements.
     */
    internal class ProductionPattern(int id, string name)
    {
        private readonly ArrayList _alternatives = [];
        private int _defaultAlt = -1;

        public int Id { get; } = id;

        public int GetId()
        {
            return Id;
        }

        public string Name { get; } = name;

        public string GetName()
        {
            return Name;
        }

        public bool Synthetic { get; set; }

        public bool IsSyntetic()
        {
            return Synthetic;
        }

        public void SetSyntetic(bool synthetic)
        {
            Synthetic = synthetic;
        }

        internal LookAheadSet LookAhead { get; set; } = null!;

        internal ProductionPatternAlternative? DefaultAlternative
        {
            get
            {
                if (_defaultAlt >= 0)
                {
                    object obj = _alternatives[_defaultAlt]!;
                    return (ProductionPatternAlternative)obj;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                _defaultAlt = 0;
                for (int i = 0; i < _alternatives.Count; i++)
                {
                    if (_alternatives[i] == value)
                    {
                        _defaultAlt = i;
                    }
                }
            }
        }

        public int Count => _alternatives.Count;

        public int GetAlternativeCount()
        {
            return Count;
        }

        public ProductionPatternAlternative this[int index] => (ProductionPatternAlternative)_alternatives[index]!;

        public ProductionPatternAlternative GetAlternative(int pos)
        {
            return this[pos];
        }

        public bool IsLeftRecursive()
        {
            ProductionPatternAlternative alt;

            for (int i = 0; i < _alternatives.Count; i++)
            {
                alt = (ProductionPatternAlternative)_alternatives[i]!;
                if (alt.IsLeftRecursive())
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsRightRecursive()
        {
            ProductionPatternAlternative alt;

            for (int i = 0; i < _alternatives.Count; i++)
            {
                alt = (ProductionPatternAlternative)_alternatives[i]!;
                if (alt.IsRightRecursive())
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsMatchingEmpty()
        {
            ProductionPatternAlternative alt;

            for (int i = 0; i < _alternatives.Count; i++)
            {
                alt = (ProductionPatternAlternative)_alternatives[i]!;
                if (alt.IsMatchingEmpty())
                {
                    return true;
                }
            }
            return false;
        }

        public void AddAlternative(ProductionPatternAlternative alt)
        {
            if (_alternatives.Contains(alt))
            {
                throw new ParserCreationException(
                    ParserCreationException.ErrorType.INVALID_PRODUCTION,
                    Name,
                    "two identical alternatives exist");
            }
            alt.SetPattern(this);
            _ = _alternatives.Add(alt);
        }

        public override string ToString()
        {
            StringBuilder buffer = new();
            StringBuilder indent = new();
            int i;

            _ = buffer.Append(Name);
            _ = buffer.Append("(");
            _ = buffer.Append(Id);
            _ = buffer.Append(") ");
            for (i = 0; i < buffer.Length; i++)
            {
                _ = indent.Append(" ");
            }
            for (i = 0; i < _alternatives.Count; i++)
            {
                if (i == 0)
                {
                    _ = buffer.Append("= ");
                }
                else
                {
                    _ = buffer.Append("\n");
                    _ = buffer.Append(indent);
                    _ = buffer.Append("| ");
                }
                _ = buffer.Append(_alternatives[i]);
            }
            return buffer.ToString();
        }
    }
}
