using System.Reflection;
using Flee.PublicTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flee.Tests.ExpressionTests
{
    /// <summary>
    /// Exercises <see cref="ExpressionOptions"/> knobs: case sensitivity, integers-as-doubles,
    /// real literal data type, checked overflow, and owner-member access.
    /// </summary>
    [TestClass]
    public class ExpressionOptionsTests
    {
        // --- Case sensitivity ---

        [TestMethod]
        public void CaseSensitive_DefaultIsFalse_IdentifierMatchesAnyCase()
        {
            var context = new ExpressionContext();
            context.Variables.Add("Foo", 100);
            var e = context.CompileDynamic("foo + FOO");

            Assert.AreEqual(200, e.Evaluate());
        }

        [TestMethod]
        public void CaseSensitive_True_RequiresExactName()
        {
            var context = new ExpressionContext();
            context.Options.CaseSensitive = true;
            context.Variables.Add("Foo", 100);

            Assert.ThrowsException<ExpressionCompileException>(() => context.CompileDynamic("foo"));
        }

        [TestMethod]
        public void CaseSensitive_True_ExactMatchStillCompiles()
        {
            var context = new ExpressionContext();
            context.Options.CaseSensitive = true;
            context.Variables.Add("Foo", 100);
            var e = context.CompileDynamic("Foo");

            Assert.AreEqual(100, e.Evaluate());
        }

        // --- IntegersAsDoubles ---

        [TestMethod]
        public void IntegersAsDoubles_True_DivisionUsesFloatingPoint()
        {
            // Without the option: 1/2 = 0; with the option: 1/2 = 0.5.
            var context = new ExpressionContext();
            context.Options.IntegersAsDoubles = true;
            var e = context.CompileDynamic("1 / 2");

            Assert.AreEqual(0.5, e.Evaluate());
        }

        [TestMethod]
        public void IntegersAsDoubles_False_DivisionTruncates()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1 / 2");

            Assert.AreEqual(0, e.Evaluate());
        }

        // --- RealLiteralDataType ---

        [TestMethod]
        public void RealLiteralDataType_Single_ParsesUnsuffixedAsFloat()
        {
            var context = new ExpressionContext();
            context.Options.RealLiteralDataType = RealLiteralDataType.Single;
            var e = context.CompileDynamic("1.5");

            Assert.AreEqual(1.5f, e.Evaluate());
            Assert.AreEqual(typeof(float), e.Evaluate().GetType());
        }

        [TestMethod]
        public void RealLiteralDataType_Decimal_ParsesUnsuffixedAsDecimal()
        {
            var context = new ExpressionContext();
            context.Options.RealLiteralDataType = RealLiteralDataType.Decimal;
            var e = context.CompileDynamic("1.5");

            Assert.AreEqual(1.5M, e.Evaluate());
            Assert.AreEqual(typeof(decimal), e.Evaluate().GetType());
        }

        [TestMethod]
        public void RealLiteralDataType_Double_IsTheDefault()
        {
            var context = new ExpressionContext();
            // Confirm the default value is Double.
            Assert.AreEqual(RealLiteralDataType.Double, context.Options.RealLiteralDataType);

            var e = context.CompileDynamic("1.5");
            Assert.AreEqual(typeof(double), e.Evaluate().GetType());
        }

        // --- Checked overflow ---

        [TestMethod]
        public void Checked_True_OverflowingIntAddThrows()
        {
            var context = new ExpressionContext();
            context.Options.Checked = true;
            context.Variables.Add("a", int.MaxValue);
            context.Variables.Add("b", 1);
            var e = context.CompileDynamic("a + b");

            Assert.ThrowsException<System.OverflowException>(() => e.Evaluate());
        }

        [TestMethod]
        public void Checked_False_OverflowingIntAddWraps()
        {
            var context = new ExpressionContext();
            context.Variables.Add("a", int.MaxValue);
            context.Variables.Add("b", 1);
            var e = context.CompileDynamic("a + b");

            Assert.AreEqual(int.MinValue, e.Evaluate());
        }

        // --- StringComparison ---

        [TestMethod]
        public void StringComparison_OrdinalIgnoreCase_MakesStringEqualityIgnoreCase()
        {
            var context = new ExpressionContext();
            context.Options.StringComparison = System.StringComparison.OrdinalIgnoreCase;
            var e = context.CompileDynamic("\"abc\" = \"ABC\"");

            Assert.AreEqual(true, e.Evaluate());
        }

        // --- Owner member access ---

        public class OwnerWithPublicAndPrivate
        {
            public int PublicValue => 1;

            private int PrivateValue => 99;

            public int GetPrivate() => PrivateValue;
        }

        [TestMethod]
        public void OwnerMemberAccess_DefaultIsPublic_PrivateMembersHidden()
        {
            var context = new ExpressionContext(new OwnerWithPublicAndPrivate());

            Assert.ThrowsException<ExpressionCompileException>(() => context.CompileDynamic("PrivateValue"));
        }

        [TestMethod]
        public void OwnerMemberAccess_WithNonPublicFlag_ExposesPrivateMembers()
        {
            var context = new ExpressionContext(new OwnerWithPublicAndPrivate());
            context.Options.OwnerMemberAccess = BindingFlags.Public | BindingFlags.NonPublic;
            var e = context.CompileDynamic("PrivateValue");

            Assert.AreEqual(99, e.Evaluate());
        }

        [TestMethod]
        public void OwnerMemberAccess_PublicMemberAlwaysAvailable()
        {
            var context = new ExpressionContext(new OwnerWithPublicAndPrivate());
            var e = context.CompileDynamic("PublicValue");

            Assert.AreEqual(1, e.Evaluate());
        }

        // --- ExpressionOwnerMemberAccessAttribute ---

        public class OwnerWithExplicitAccessControl
        {
            public int OpenValue => 10;

            [ExpressionOwnerMemberAccess(false)]
            public int HiddenValue => 20;
        }

        [TestMethod]
        public void OwnerMember_HiddenByAttribute_FailsToCompile()
        {
            var context = new ExpressionContext(new OwnerWithExplicitAccessControl());

            Assert.ThrowsException<ExpressionCompileException>(() => context.CompileDynamic("HiddenValue"));
        }

        [TestMethod]
        public void OwnerMember_NotHidden_StillAccessible()
        {
            var context = new ExpressionContext(new OwnerWithExplicitAccessControl());
            var e = context.CompileDynamic("OpenValue");

            Assert.AreEqual(10, e.Evaluate());
        }

        // --- ParseCulture ---

        [TestMethod]
        public void ParseCulture_DeDe_AcceptsCommaDecimalAndSemiArgSeparator()
        {
            var context = new ExpressionContext();
            context.Options.ParseCulture = new System.Globalization.CultureInfo("de-DE");
            context.Imports.AddType(typeof(System.Math));
            var e = context.CompileDynamic("Max(1,5; 0,5)");

            Assert.AreEqual(1.5, e.Evaluate());
        }
    }
}
