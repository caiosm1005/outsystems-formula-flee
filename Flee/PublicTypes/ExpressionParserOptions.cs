using System.Globalization;
using Flee.InternalTypes;

namespace Flee.PublicTypes
{
    /// <summary>
    /// Parser-level configuration for an <see cref="ExpressionContext"/> — separators, decimal
    /// handling, and date/time formatting used while reading expression source text.
    /// </summary>
    public class ExpressionParserOptions
    {
        private PropertyDictionary _myProperties;
        private readonly ExpressionContext _myOwner;
        private readonly CultureInfo _myParseCulture;

        /// <summary>
        /// The number-style flags used when parsing real-number literals.
        /// </summary>
        private readonly NumberStyles NumberStyles =
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.None;

        /// <summary>
        /// Initializes a new options instance owned by the given expression context.
        /// </summary>
        /// <param name="owner">The owning expression context.</param>
        internal ExpressionParserOptions(ExpressionContext owner)
        {
            _myOwner = owner;
            _myProperties = new PropertyDictionary();
            _myParseCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            InitializeProperties();
        }

        #region "Methods - Public"

        /// <summary>
        /// Forces the owning context to rebuild its parser, picking up any changes made to
        /// these options.
        /// </summary>
        public void RecreateParser()
        {
            _myOwner.RecreateParser();
        }

        #endregion

        #region "Methods - Internal"

        /// <summary>
        /// Creates a deep copy of these options. Used when an <see cref="ExpressionContext"/> is cloned.
        /// </summary>
        /// <returns>A new <see cref="ExpressionParserOptions"/> with the same values.</returns>
        internal ExpressionParserOptions Clone()
        {
            ExpressionParserOptions copy = (ExpressionParserOptions)MemberwiseClone();
            copy._myProperties = _myProperties.Clone();
            return copy;
        }

        /// <summary>
        /// Parses <paramref name="image"/> as a <see cref="double"/> using the configured culture.
        /// </summary>
        /// <param name="image">The literal text.</param>
        /// <returns>The parsed value.</returns>
        internal double ParseDouble(string image)
        {
            return double.Parse(image, NumberStyles, _myParseCulture);
        }

        /// <summary>
        /// Parses <paramref name="image"/> as a <see cref="float"/> using the configured culture.
        /// </summary>
        /// <param name="image">The literal text.</param>
        /// <returns>The parsed value.</returns>
        internal float ParseSingle(string image)
        {
            return float.Parse(image, NumberStyles, _myParseCulture);
        }

        /// <summary>
        /// Parses <paramref name="image"/> as a <see cref="decimal"/> using the configured culture.
        /// </summary>
        /// <param name="image">The literal text.</param>
        /// <returns>The parsed value.</returns>
        internal decimal ParseDecimal(string image)
        {
            return decimal.Parse(image, NumberStyles, _myParseCulture);
        }
        #endregion

        #region "Methods - Private"

        /// <summary>
        /// Sets the default values for all parser options.
        /// </summary>
        private void InitializeProperties()
        {
            DateTimeFormat = "dd/MM/yyyy";
            RequireDigitsBeforeDecimalPoint = false;
            DecimalSeparator = '.';
            FunctionArgumentSeparator = ',';
        }

        #endregion

        #region "Properties - Public"

        /// <summary>
        /// Gets or sets the format string used to parse date/time literals (e.g. <c>"dd/MM/yyyy"</c>).
        /// </summary>
        public string DateTimeFormat
        {
            get => _myProperties.GetValue<string>("DateTimeFormat");
            set => _myProperties.SetValue("DateTimeFormat", value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether real literals must include a digit
        /// before the decimal point (e.g. <c>0.5</c> required versus <c>.5</c>).
        /// </summary>
        public bool RequireDigitsBeforeDecimalPoint
        {
            get => _myProperties.GetValue<bool>("RequireDigitsBeforeDecimalPoint");
            set => _myProperties.SetValue("RequireDigitsBeforeDecimalPoint", value);
        }

        /// <summary>
        /// Gets or sets the decimal separator character used when parsing real literals.
        /// </summary>
        public char DecimalSeparator
        {
            get => _myProperties.GetValue<char>("DecimalSeparator");
            set
            {
                _myProperties.SetValue("DecimalSeparator", value);
                _myParseCulture.NumberFormat.NumberDecimalSeparator = value.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the character used to separate function arguments (e.g. <c>','</c> or <c>';'</c>).
        /// </summary>
        public char FunctionArgumentSeparator
        {
            get => _myProperties.GetValue<char>("FunctionArgumentSeparator");
            set => _myProperties.SetValue("FunctionArgumentSeparator", value);
        }

        #endregion
    }
}
