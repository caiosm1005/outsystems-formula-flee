using System.Text;

namespace Flee.Parsing
{
    /**
      * A production pattern element. This class represents a reference to
      * either a token or a production. Each element also contains minimum
      * and maximum occurence counters, controlling the number of
      * repetitions allowed. A production pattern element is always
      * contained within a production pattern rule.
      */
    internal class ProductionPatternElement
    {
        private readonly bool _token;

        public ProductionPatternElement(bool isToken,
                                        int id,
                                        int min,
                                        int max)
        {

            _token = isToken;
            Id = id;
            if (min < 0)
            {
                min = 0;
            }
            MinCount = min;
            if (max <= 0)
            {
                max = Int32.MaxValue;
            }
            else if (max < min)
            {
                max = min;
            }
            MaxCount = max;
            LookAhead = null!;
        }

        public int Id { get; }

        public int GetId()
        {
            return Id;
        }

        public int MinCount { get; }

        public int GetMinCount()
        {
            return MinCount;
        }

        public int MaxCount { get; }

        public int GetMaxCount()
        {
            return MaxCount;
        }

        internal LookAheadSet LookAhead { get; set; } = null!;

        public bool IsToken()
        {
            return _token;
        }

        public bool IsProduction()
        {
            return !_token;
        }

        public bool IsMatch(Token? token)
        {
            return IsToken() && token != null && token.Id == Id;
        }

        public override bool Equals(object? obj)
        {
            return obj is ProductionPatternElement elem
                && _token == elem._token
                && Id == elem.Id
                && MinCount == elem.MinCount
                && MaxCount == elem.MaxCount;
        }

        public override int GetHashCode()
        {
            return Id * 37;
        }

        public override string ToString()
        {
            StringBuilder buffer = new();

            _ = buffer.Append(Id);
            _ = buffer.Append(_token ? "(Token)" : "(Production)");
            if (MinCount != 1 || MaxCount != 1)
            {
                _ = buffer.Append("{");
                _ = buffer.Append(MinCount);
                _ = buffer.Append(",");
                _ = buffer.Append(MaxCount);
                _ = buffer.Append("}");
            }
            return buffer.ToString();
        }
    }
}
