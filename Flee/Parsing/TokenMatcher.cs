using System.Text;

namespace Flee.Parsing
{
    internal abstract class TokenMatcher
    {
        protected TokenPattern[] Patterns = new TokenPattern[0];

        protected bool IgnoreCase = false;

        protected TokenMatcher(bool ignoreCase)
        {
            IgnoreCase = ignoreCase;
        }

        public abstract void Match(ReaderBuffer buffer, TokenMatch match);

        public TokenPattern GetPattern(int id)
        {
            for (int i = 0; i < Patterns.Length; i++)
            {
                if (Patterns[i].Id == id)
                {
                    return Patterns[i];
                }
            }
            return null;
        }

        public virtual void AddPattern(TokenPattern pattern)
        {
            Array.Resize(ref Patterns, Patterns.Length + 1);
            Patterns[Patterns.Length - 1] = pattern;
        }
        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();

            for (int i = 0; i < Patterns.Length; i++)
            {
                buffer.Append(Patterns[i]);
                buffer.Append("\n\n");
            }
            return buffer.ToString();
        }
    }
}
