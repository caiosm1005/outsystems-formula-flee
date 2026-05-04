using System.Diagnostics;
using Flee.ExpressionElements.Literals.Real;
using Flee.PublicTypes;

namespace Flee.ExpressionElements.Base.Literals
{
    /// <summary>
    /// Base class for real-number literal elements (<see cref="DoubleLiteralElement"/>,
    /// <see cref="SingleLiteralElement"/>, <see cref="DecimalLiteralElement"/>). Hosts factory
    /// methods that pick the concrete CLR type based on suffix and configuration.
    /// </summary>
    internal abstract class RealLiteralElement : LiteralElement
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        protected RealLiteralElement()
        {
        }

        /// <summary>
        /// Tries to convert an integer literal source <paramref name="image"/> into a
        /// real-typed literal — either via a suffix (<c>f</c>, <c>m</c>) or because
        /// <see cref="ExpressionOptions.IntegersAsDoubles"/> is enabled.
        /// </summary>
        /// <param name="image">The literal source text.</param>
        /// <param name="services">The compile services.</param>
        /// <returns>A real literal element when one applies, otherwise <see langword="null"/>.</returns>
        public static LiteralElement? CreateFromInteger(string image, IServiceProvider services)
        {
            LiteralElement? element = CreateSingle(image, services);

            if (element != null)
            {
                return element;
            }

            element = CreateDecimal(image, services);

            if (element != null)
            {
                return element;
            }

            ExpressionOptions options = (ExpressionOptions)services.GetService(typeof(ExpressionOptions))!;

            // Convert to a double if option is set
            return options.IntegersAsDoubles
                ? DoubleLiteralElement.Parse(image, services)
                : (LiteralElement?)null;
        }

        /// <summary>
        /// Creates a real-literal element from a real-style source <paramref name="image"/>,
        /// honoring suffix selectors first and falling back to
        /// <see cref="ExpressionOptions.RealLiteralDataType"/>.
        /// </summary>
        /// <param name="image">The literal source text.</param>
        /// <param name="services">The compile services.</param>
        /// <returns>The created literal element.</returns>
        public static LiteralElement Create(string image, IServiceProvider services)
        {
            LiteralElement? element = CreateSingle(image, services);

            if (element != null)
            {
                return element;
            }

            element = CreateDecimal(image, services);

            if (element != null)
            {
                return element;
            }

            element = CreateDouble(image, services);

            if (element != null)
            {
                return element;
            }

            element = CreateImplicitReal(image, services);

            return element!;
        }

        /// <summary>
        /// Creates a literal of the implicitly configured real type when the source has no
        /// explicit type suffix.
        /// </summary>
        /// <param name="image">The literal source text.</param>
        /// <param name="services">The compile services.</param>
        /// <returns>The created literal element.</returns>
        private static LiteralElement? CreateImplicitReal(string image, IServiceProvider services)
        {
            ExpressionOptions options = (ExpressionOptions)services.GetService(typeof(ExpressionOptions))!;
            RealLiteralDataType realType = options.RealLiteralDataType;

            switch (realType)
            {
                case RealLiteralDataType.Double:
                    return DoubleLiteralElement.Parse(image, services);
                case RealLiteralDataType.Single:
                    return SingleLiteralElement.Parse(image, services);
                case RealLiteralDataType.Decimal:
                    return DecimalLiteralElement.Parse(image, services);
                default:
                    Debug.Fail("Unknown value");
                    return null;
            }
        }

        /// <summary>
        /// Creates a <see cref="DoubleLiteralElement"/> when <paramref name="image"/> has a
        /// <c>d</c> suffix.
        /// </summary>
        /// <param name="image">The literal source text.</param>
        /// <param name="services">The compile services.</param>
        /// <returns>The created element, or <see langword="null"/> when no suffix.</returns>
        private static DoubleLiteralElement? CreateDouble(string image, IServiceProvider services)
        {
            if (image.EndsWith("d", StringComparison.OrdinalIgnoreCase))
            {
                image = image.Remove(image.Length - 1);
                return DoubleLiteralElement.Parse(image, services);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a <see cref="SingleLiteralElement"/> when <paramref name="image"/> has an
        /// <c>f</c> suffix.
        /// </summary>
        /// <param name="image">The literal source text.</param>
        /// <param name="services">The compile services.</param>
        /// <returns>The created element, or <see langword="null"/> when no suffix.</returns>
        private static SingleLiteralElement? CreateSingle(string image, IServiceProvider services)
        {
            if (image.EndsWith("f", StringComparison.OrdinalIgnoreCase))
            {
                image = image.Remove(image.Length - 1);
                return SingleLiteralElement.Parse(image, services);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a <see cref="DecimalLiteralElement"/> when <paramref name="image"/> has an
        /// <c>m</c> suffix.
        /// </summary>
        /// <param name="image">The literal source text.</param>
        /// <param name="services">The compile services.</param>
        /// <returns>The created element, or <see langword="null"/> when no suffix.</returns>
        private static DecimalLiteralElement? CreateDecimal(string image, IServiceProvider services)
        {
            if (image.EndsWith("m", StringComparison.OrdinalIgnoreCase))
            {
                image = image.Remove(image.Length - 1);
                return DecimalLiteralElement.Parse(image, services);
            }
            else
            {
                return null;
            }
        }
    }
}
