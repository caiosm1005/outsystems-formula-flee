using Flee.PublicTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flee.Tests.ExpressionTests
{
    /// <summary>
    /// Exercises identifier resolution with the new SQL-style <c>@</c> and <c>@@</c> prefixes.
    /// The prefix is part of the variable name lookup key — pure pass-through to
    /// <see cref="VariableCollection"/>.
    /// </summary>
    [TestClass]
    public class IdentifierPrefixTests
    {
        [TestMethod]
        public void AtPrefix_VariableLookup_Succeeds()
        {
            var context = new ExpressionContext();
            context.Variables.Add("@status", "active");
            var e = context.CompileDynamic("@status = 'active'");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void DoubleAtPrefix_VariableLookup_Succeeds()
        {
            var context = new ExpressionContext();
            context.Variables.Add("@@status", "active");
            var e = context.CompileDynamic("@@status = 'active'");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void NoPrefix_StillWorks()
        {
            var context = new ExpressionContext();
            context.Variables.Add("status", "active");
            var e = context.CompileDynamic("status = 'active'");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void AtAndDoubleAt_AreDistinctNames()
        {
            var context = new ExpressionContext();
            context.Variables.Add("@status", "session");
            context.Variables.Add("@@status", "global");
            var e1 = context.CompileDynamic("@status");
            var e2 = context.CompileDynamic("@@status");

            Assert.AreEqual("session", e1.Evaluate());
            Assert.AreEqual("global", e2.Evaluate());
        }
    }
}
