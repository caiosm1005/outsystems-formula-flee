﻿using System.Globalization;
using Flee.InternalTypes;

namespace Flee.PublicTypes
{
    public class ExpressionParserOptions
    {
        private PropertyDictionary _myProperties;
        private readonly ExpressionContext _myOwner;
        private readonly CultureInfo _myParseCulture;
        private NumberStyles NumberStyles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.None;
        
        internal ExpressionParserOptions(ExpressionContext owner)
        {
            _myOwner = owner;
            _myProperties = new PropertyDictionary();
            _myParseCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            InitializeProperties();
        }

        #region "Methods - Public"

        public void RecreateParser()
        {
            _myOwner.RecreateParser();
        }

        #endregion

        #region "Methods - Internal"

        internal ExpressionParserOptions Clone()
        {
            ExpressionParserOptions copy = (ExpressionParserOptions)MemberwiseClone();
            copy._myProperties = _myProperties.Clone();
            return copy;
        }

        internal double ParseDouble(string image)
        {
            return double.Parse(image, NumberStyles, _myParseCulture);
        }

        internal float ParseSingle(string image)
        {
            return float.Parse(image, NumberStyles, _myParseCulture);
        }

        internal decimal ParseDecimal(string image)
        {
            return decimal.Parse(image, NumberStyles, _myParseCulture);
        }

        #endregion

        #region "Methods - Private"

        private void InitializeProperties()
        {
            DateTimeFormat = "dd/MM/yyyy";
            RequireDigitsBeforeDecimalPoint = false;
            DecimalSeparator = '.';
            FunctionArgumentSeparator = ',';
        }

        #endregion

        #region "Properties - Public"

        public string DateTimeFormat
        {
            get { return DateTimeFormats.FirstOrDefault(); }
            set { DateTimeFormats = new string[] { value }; }
        }

        public string[] DateTimeFormats
        {
            get { return _myProperties.GetValue<string[]>("DateTimeFormats"); }
            set { _myProperties.SetValue("DateTimeFormats", value); }
        }

        public bool RequireDigitsBeforeDecimalPoint
        {
            get { return _myProperties.GetValue<bool>("RequireDigitsBeforeDecimalPoint"); }
            set { _myProperties.SetValue("RequireDigitsBeforeDecimalPoint", value); }
        }

        public char DecimalSeparator
        {
            get { return _myProperties.GetValue<char>("DecimalSeparator"); }
            set
            {
                _myProperties.SetValue("DecimalSeparator", value);
                _myParseCulture.NumberFormat.NumberDecimalSeparator = value.ToString();
            }
        }

        public char FunctionArgumentSeparator
        {
            get { return _myProperties.GetValue<char>("FunctionArgumentSeparator"); }
            set { _myProperties.SetValue("FunctionArgumentSeparator", value); }
        }

        #endregion
    }
}
