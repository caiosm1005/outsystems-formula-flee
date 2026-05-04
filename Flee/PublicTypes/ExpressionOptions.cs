using System.Reflection;
using System.Globalization;
using Flee.InternalTypes;

namespace Flee.PublicTypes
{
    /// <summary>
    /// Compile-time and evaluation-time options for an <see cref="ExpressionContext"/> —
    /// type policies, case sensitivity, owner-member access, and so on.
    /// </summary>
    public sealed class ExpressionOptions
    {
        private PropertyDictionary _myProperties;
        private readonly ExpressionContext _myOwner;

        /// <summary>
        /// Raised when <see cref="CaseSensitive"/> changes so that dependent collections
        /// (e.g. variables) can rebuild with a different comparer.
        /// </summary>
        internal event EventHandler? CaseSensitiveChanged;

        /// <summary>
        /// Initializes a new options instance owned by the given expression context.
        /// </summary>
        /// <param name="owner">The owning expression context.</param>
        internal ExpressionOptions(ExpressionContext owner)
        {
            _myOwner = owner;
            _myProperties = new PropertyDictionary();

            InitializeProperties();
        }

        #region "Methods - Private"

        /// <summary>
        /// Sets the default values for all options.
        /// </summary>
        private void InitializeProperties()
        {
            StringComparison = StringComparison.Ordinal;
            OwnerMemberAccess = BindingFlags.Public;

            _myProperties.SetToDefault<bool>("CaseSensitive");
            _myProperties.SetToDefault<bool>("Checked");
            _myProperties.SetToDefault<bool>("EmitToAssembly");
            _myProperties.SetToDefault<Type>("ResultType");
            _myProperties.SetToDefault<bool>("IsGeneric");
            _myProperties.SetToDefault<bool>("IntegersAsDoubles");
            _myProperties.SetValue("ParseCulture", CultureInfo.CurrentCulture);
            SetParseCulture(ParseCulture);
            _myProperties.SetValue("RealLiteralDataType", RealLiteralDataType.Double);
        }

        /// <summary>
        /// Synchronizes the parser-level options (separators, date format) with
        /// <paramref name="ci"/>'s culture-specific settings.
        /// </summary>
        /// <param name="ci">The culture to take separators and formats from.</param>
        private void SetParseCulture(CultureInfo ci)
        {
            ExpressionParserOptions po = _myOwner.ParserOptions;
            po.DecimalSeparator = Convert.ToChar(ci.NumberFormat.NumberDecimalSeparator);
            po.FunctionArgumentSeparator = Convert.ToChar(ci.TextInfo.ListSeparator);
            po.DateTimeFormat = ci.DateTimeFormat.ShortDatePattern;
        }

        #endregion

        #region "Methods - Internal"

        /// <summary>
        /// Creates a deep copy of these options. Used when an <see cref="ExpressionContext"/> is cloned.
        /// </summary>
        /// <returns>A new <see cref="ExpressionOptions"/> with the same values.</returns>
        internal ExpressionOptions Clone()
        {
            ExpressionOptions clonedOptions = (ExpressionOptions)MemberwiseClone();
            clonedOptions._myProperties = _myProperties.Clone();
            return clonedOptions;
        }

        /// <summary>
        /// Returns whether <paramref name="t"/> is assignable from the configured owner type.
        /// </summary>
        /// <param name="t">The candidate type.</param>
        /// <returns><see langword="true"/> when <paramref name="t"/> is the owner type or a subclass.</returns>
        internal bool IsOwnerType(Type t)
        {
            return OwnerType.IsAssignableFrom(t);
        }

        /// <summary>
        /// Records the runtime type of the expression's owner instance so the compiler can
        /// resolve member access against it.
        /// </summary>
        /// <param name="ownerType">The owner's CLR type.</param>
        internal void SetOwnerType(Type ownerType)
        {
            OwnerType = ownerType;
        }

        #endregion

        #region "Properties - Public"

        /// <summary>
        /// Gets or sets the type the expression is required to evaluate to. Used by
        /// <see cref="ExpressionContext.CompileGeneric{T}"/>.
        /// </summary>
        public Type ResultType
        {
            get => _myProperties.GetValue<Type>("ResultType");
            set
            {
                Utility.AssertNotNull(value, "value");
                _myProperties.SetValue("ResultType", value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether arithmetic emits checked overflow opcodes.
        /// </summary>
        public bool Checked
        {
            get => _myProperties.GetValue<bool>("Checked");
            set => _myProperties.SetValue("Checked", value);
        }

        /// <summary>
        /// Gets or sets the <see cref="System.StringComparison"/> used by string operations
        /// emitted by the compiler.
        /// </summary>
        public StringComparison StringComparison
        {
            get => _myProperties.GetValue<StringComparison>("StringComparison");
            set => _myProperties.SetValue("StringComparison", value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the compiled IL should be emitted into a
        /// persistable assembly (used for diagnostics/inspection).
        /// </summary>
        public bool EmitToAssembly
        {
            get => _myProperties.GetValue<bool>("EmitToAssembly");
            set => _myProperties.SetValue("EmitToAssembly", value);
        }

        /// <summary>
        /// Gets or sets the <see cref="BindingFlags"/> applied when looking up members on the
        /// expression owner. Defaults to <see cref="BindingFlags.Public"/>.
        /// </summary>
        public BindingFlags OwnerMemberAccess
        {
            get => _myProperties.GetValue<BindingFlags>("OwnerMemberAccess");
            set => _myProperties.SetValue("OwnerMemberAccess", value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether identifier lookup is case-sensitive.
        /// Changing this rebuilds dependent collections via <see cref="CaseSensitiveChanged"/>.
        /// </summary>
        public bool CaseSensitive
        {
            get => _myProperties.GetValue<bool>("CaseSensitive");
            set
            {
                if (CaseSensitive != value)
                {
                    _myProperties.SetValue("CaseSensitive", value);
                    CaseSensitiveChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether integer literals are promoted to <see cref="double"/>
        /// during arithmetic.
        /// </summary>
        public bool IntegersAsDoubles
        {
            get => _myProperties.GetValue<bool>("IntegersAsDoubles");
            set => _myProperties.SetValue("IntegersAsDoubles", value);
        }

        /// <summary>
        /// Gets or sets the <see cref="CultureInfo"/> used to interpret numeric and date/time
        /// literals at parse time. Setting a new culture also propagates separators to
        /// <see cref="ExpressionParserOptions"/>.
        /// </summary>
        public CultureInfo ParseCulture
        {
            get => _myProperties.GetValue<CultureInfo>("ParseCulture");
            set
            {
                Utility.AssertNotNull(value, "ParseCulture");
                if (value.LCID != ParseCulture.LCID)
                {
                    _myProperties.SetValue("ParseCulture", value);
                    SetParseCulture(value);
                    _myOwner.ParserOptions.RecreateParser();
                }
            }
        }

        /// <summary>
        /// Gets or sets the CLR type used for real-number literals. See <see cref="PublicTypes.RealLiteralDataType"/>.
        /// </summary>
        public RealLiteralDataType RealLiteralDataType
        {
            get => _myProperties.GetValue<RealLiteralDataType>("RealLiteralDataType");
            set => _myProperties.SetValue("RealLiteralDataType", value);
        }
        #endregion

        #region "Properties - Non Public"

        /// <summary>
        /// Gets the <see cref="IEqualityComparer{T}"/> for identifier-name comparisons,
        /// chosen based on <see cref="CaseSensitive"/>.
        /// </summary>
        internal IEqualityComparer<string> StringComparer => CaseSensitive
            ? System.StringComparer.Ordinal
            : System.StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// Gets the <see cref="System.Reflection.MemberFilter"/> used during reflection lookups,
        /// chosen based on <see cref="CaseSensitive"/>.
        /// </summary>
        internal MemberFilter MemberFilter => CaseSensitive ? Type.FilterName : Type.FilterNameIgnoreCase;

        /// <summary>
        /// Gets the <see cref="StringComparison"/> for member-name comparisons,
        /// chosen based on <see cref="CaseSensitive"/>.
        /// </summary>
        internal StringComparison MemberStringComparison => CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        /// <summary>
        /// Gets the runtime type of the expression's owner instance.
        /// </summary>
        internal Type OwnerType { get; private set; } = null!;

        /// <summary>
        /// Gets or sets a value indicating whether the expression is being compiled with
        /// a strongly typed result via <see cref="ExpressionContext.CompileGeneric{T}"/>.
        /// </summary>
        internal bool IsGeneric
        {
            get => _myProperties.GetValue<bool>("IsGeneric");
            set => _myProperties.SetValue("IsGeneric", value);
        }
        #endregion
    }
}
