﻿using Flee.PublicTypes;

namespace Flee.Parsing
{
    internal abstract class CustomTokenPattern : TokenPattern
    {
        protected CustomTokenPattern(int id, string name, PatternType type, string pattern) : base(id, name, type, pattern)
        {
        }

        public void Initialize(int id, string name, PatternType type, string pattern, ExpressionContext context)
        {
            ComputeToken(id, name, type, pattern, context);
        }

        protected abstract void ComputeToken(int id, string name, PatternType type, string pattern, ExpressionContext context);
    }

    internal class RealPattern : CustomTokenPattern
    {
        public RealPattern(int id, string name, PatternType type, string pattern) : base(id, name, type, pattern)
        {
        }

        protected override void ComputeToken(int id, string name, PatternType type, string pattern, ExpressionContext context)
        {
            ExpressionParserOptions options = context.ParserOptions;

            char digitsBeforePattern = options.RequireDigitsBeforeDecimalPoint ? '+' : '*';

            pattern = string.Format(pattern, digitsBeforePattern, options.DecimalSeparator);

            SetData(id, name, type, pattern);
        }
    }

    internal class ArgumentSeparatorPattern : CustomTokenPattern
    {
        public ArgumentSeparatorPattern(int id, string name, PatternType type, string pattern) : base(id, name, type, pattern)
        {
        }

        protected override void ComputeToken(int id, string name, PatternType type, string pattern, ExpressionContext context)
        {
            ExpressionParserOptions options = context.ParserOptions;
            SetData(id, name, type, options.FunctionArgumentSeparator.ToString());
        }
    }
}
