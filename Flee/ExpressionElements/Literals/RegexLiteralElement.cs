using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements.Literals
{
    /// <summary>
    /// A regex literal of the form <c>/pattern/flags</c> (JavaScript-style). The grammar's
    /// contextual lexer emits this token only when it follows <c>MATCH</c> or <c>CONTAINS</c>.
    /// At runtime the regex is fetched from <see cref="RegexCache"/> so each literal compiles
    /// at most once per process.
    /// </summary>
    internal class RegexLiteralElement : LiteralElement
    {
        private static readonly MethodInfo _getMethod =
            typeof(RegexCache).GetMethod(nameof(RegexCache.Get), BindingFlags.Public | BindingFlags.Static)!;

        /// <summary>
        /// Gets the .NET regex pattern (without the leading/trailing <c>/</c> and flags).
        /// </summary>
        public string Pattern { get; }

        /// <summary>
        /// Gets the <see cref="RegexOptions"/> mapped from the source flag letters.
        /// </summary>
        public RegexOptions Options { get; }

        /// <summary>
        /// Initializes a new instance from the verbatim token image, stripping the leading and
        /// trailing <c>/</c> and translating any flag letters into <see cref="RegexOptions"/>.
        /// </summary>
        /// <param name="image">The token image, e.g. <c>/^foo$/i</c>.</param>
        public RegexLiteralElement(string image)
        {
            int closingSlash = image.LastIndexOf('/');
            Pattern = image.Substring(1, closingSlash - 1);
            string flags = image.Substring(closingSlash + 1);
            Options = ParseFlags(flags);
        }

        private RegexOptions ParseFlags(string flags)
        {
            RegexOptions options = RegexOptions.None;
            foreach (char c in flags)
            {
                switch (c)
                {
                    case 'i':
                    case 'I':
                        options |= RegexOptions.IgnoreCase;
                        break;
                    case 'm':
                    case 'M':
                        options |= RegexOptions.Multiline;
                        break;
                    case 's':
                    case 'S':
                        options |= RegexOptions.Singleline;
                        break;
                    case 'g':
                    case 'G':
                        // /g is meaningless for IsMatch — ignore silently to match user intent.
                        break;
                    default:
                        ThrowCompileException(
                            CompileErrorResourceKeys.CannotParseType,
                            CompileExceptionReason.InvalidFormat,
                            "Regex");
                        break;
                }
            }
            return options;
        }

        /// <summary>
        /// Emits a call into <see cref="RegexCache.Get(string, int)"/> with the literal
        /// pattern and option bitmask.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services (unused).</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            ilg.Emit(OpCodes.Ldstr, Pattern);
            LiteralElement.EmitLoad((int)Options, ilg);
            ilg.Emit(OpCodes.Call, _getMethod);
        }

        /// <summary>
        /// Gets <see cref="Regex"/>.
        /// </summary>
        public override Type ResultType => typeof(Regex);
    }
}
