using System.Diagnostics;
using System.Globalization;
using Flee.ExpressionElements.Literals.Integral;

namespace Flee.ExpressionElements.Base.Literals
{
    /// <summary>
    /// Base class for integer-literal elements (<see cref="Int32LiteralElement"/>,
    /// <see cref="UInt32LiteralElement"/>, etc.). Hosts the <see cref="Create"/> factory that
    /// picks the smallest CLR integer type capable of representing the source literal.
    /// </summary>
    internal abstract class IntegralLiteralElement : LiteralElement
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        protected IntegralLiteralElement()
        {
        }

        /// <summary>
        /// Attempt to find the first type of integer that a number can fit into. Mirrors the
        /// C# numeric-suffix rules: no suffix tries int → uint → long → ulong; <c>U</c> tries
        /// uint → ulong; <c>L</c> tries long → ulong; <c>UL</c> jumps straight to ulong. Falls
        /// back to a real-literal element when the source is decimal-style without a hex prefix.
        /// </summary>
        /// <param name="image">The literal source text.</param>
        /// <param name="isHex">Whether the literal was parsed as hexadecimal.</param>
        /// <param name="negated">Whether the literal was preceded by a unary minus.</param>
        /// <param name="services">The compile services.</param>
        /// <returns>The literal element representing the parsed value.</returns>
        public static LiteralElement Create(string image, bool isHex, bool negated, IServiceProvider services)
        {
            StringComparison comparison = StringComparison.OrdinalIgnoreCase;

            if (!isHex)
            {
                // Create a real element if required
                LiteralElement? realElement = RealLiteralElement.CreateFromInteger(image, services);

                if (realElement != null)
                {
                    return realElement;
                }
            }

            bool hasUSuffix = image.EndsWith("u", comparison) & !image.EndsWith("lu", comparison);
            bool hasLSuffix = image.EndsWith("l", comparison) & !image.EndsWith("ul", comparison);
            bool hasUlSuffix = image.EndsWith("ul", comparison) | image.EndsWith("lu", comparison);
            bool hasSuffix = hasUSuffix | hasLSuffix | hasUlSuffix;

            LiteralElement? constant;
            NumberStyles numStyles = NumberStyles.Integer;

            if (isHex)
            {
                numStyles = NumberStyles.AllowHexSpecifier;
                image = image.Remove(0, 2);
            }

            if (!hasSuffix)
            {
                // If the literal has no suffix, it has the first of these types in which its
                // value can be represented: int, uint, long, ulong.
                constant = Int32LiteralElement.TryCreate(image, isHex, negated);

                if (constant != null)
                {
                    return constant;
                }

                constant = UInt32LiteralElement.TryCreate(image, numStyles);

                if (constant != null)
                {
                    return constant;
                }

                constant = Int64LiteralElement.TryCreate(image, isHex, negated);

                return constant ?? new UInt64LiteralElement(image, numStyles);
            }
            else if (hasUSuffix)
            {
                image = image.Remove(image.Length - 1);
                // If the literal is suffixed by U or u, it has the first of these types in
                // which its value can be represented: uint, ulong.

                constant = UInt32LiteralElement.TryCreate(image, numStyles);

                return constant ?? new UInt64LiteralElement(image, numStyles);
            }
            else if (hasLSuffix)
            {
                // If the literal is suffixed by L or l, it has the first of these types in
                // which its value can be represented: long, ulong.
                image = image.Remove(image.Length - 1);

                constant = Int64LiteralElement.TryCreate(image, isHex, negated);

                return constant ?? new UInt64LiteralElement(image, numStyles);
            }
            else
            {
                // If the literal is suffixed by UL, Ul, uL, ul, LU, Lu, lU, or lu, it is of type ulong.
                Debug.Assert(hasUlSuffix, "expecting ul suffix");
                image = image.Remove(image.Length - 2);
                return new UInt64LiteralElement(image, numStyles);
            }
        }
    }
}
