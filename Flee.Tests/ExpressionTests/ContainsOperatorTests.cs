using System.Collections.Generic;
using Flee.PublicTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flee.Tests.ExpressionTests
{
    /// <summary>
    /// Exercises the <c>CONTAINS</c> operator family: single-value, ANY/ALL with literal lists
    /// or runtime collections, and the regex variant.
    /// </summary>
    [TestClass]
    public class ContainsOperatorTests
    {
        private static ExpressionContext NewContextWithCollections()
        {
            var context = new ExpressionContext();
            context.Variables.Add("arr", new[] { 1, 5, 9 });
            context.Variables.Add("strs", new[] { "abc", "def", "abc123" });
            context.Variables.Add("other", new[] { 5, 99 });
            return context;
        }

        [TestMethod]
        public void Contains_SingleValue_PresentReturnsTrue()
        {
            var context = NewContextWithCollections();
            var e = context.CompileDynamic("arr CONTAINS 5");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void Contains_SingleValue_AbsentReturnsFalse()
        {
            var context = NewContextWithCollections();
            var e = context.CompileDynamic("arr CONTAINS 42");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void Contains_Any_LiteralList_OneMatchReturnsTrue()
        {
            var context = NewContextWithCollections();
            var e = context.CompileDynamic("arr CONTAINS ANY (5, 99, 100)");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void Contains_Any_LiteralList_NoMatchReturnsFalse()
        {
            var context = NewContextWithCollections();
            var e = context.CompileDynamic("arr CONTAINS ANY (42, 99, 100)");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void Contains_All_LiteralList_AllPresentReturnsTrue()
        {
            var context = NewContextWithCollections();
            var e = context.CompileDynamic("arr CONTAINS ALL (1, 5, 9)");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void Contains_All_LiteralList_OneMissingReturnsFalse()
        {
            var context = NewContextWithCollections();
            var e = context.CompileDynamic("arr CONTAINS ALL (1, 5, 99)");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void Contains_Any_VariableCollection_MatchesIfAnyOverlap()
        {
            var context = NewContextWithCollections();
            var e = context.CompileDynamic("arr CONTAINS ANY other");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void Contains_All_VariableCollection_RequiresEveryElement()
        {
            var context = NewContextWithCollections();
            // 'other' is {5, 99}; arr lacks 99
            var e = context.CompileDynamic("arr CONTAINS ALL other");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void Contains_Regex_AnyStringMatchReturnsTrue()
        {
            var context = NewContextWithCollections();
            var e = context.CompileDynamic("strs CONTAINS /\\d/");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void Contains_Regex_NoStringMatchReturnsFalse()
        {
            var context = NewContextWithCollections();
            var e = context.CompileDynamic("strs CONTAINS /^xyz/");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void NotContains_SingleValue_AbsentReturnsTrue()
        {
            var context = NewContextWithCollections();
            var e = context.CompileDynamic("arr NOT CONTAINS 42");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void NotContains_SingleValue_PresentReturnsFalse()
        {
            var context = NewContextWithCollections();
            var e = context.CompileDynamic("arr NOT CONTAINS 5");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void NotContainsAny_LiteralList_NoMatchReturnsTrue()
        {
            var context = NewContextWithCollections();
            var e = context.CompileDynamic("arr NOT CONTAINS ANY (42, 99, 100)");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void NotContainsAny_LiteralList_OneMatchReturnsFalse()
        {
            var context = NewContextWithCollections();
            var e = context.CompileDynamic("arr NOT CONTAINS ANY (5, 99, 100)");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void NotContainsAll_LiteralList_OneMissingReturnsTrue()
        {
            var context = NewContextWithCollections();
            var e = context.CompileDynamic("arr NOT CONTAINS ALL (1, 5, 99)");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void NotContainsAll_LiteralList_AllPresentReturnsFalse()
        {
            var context = NewContextWithCollections();
            var e = context.CompileDynamic("arr NOT CONTAINS ALL (1, 5, 9)");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void NotContainsAny_VariableCollection_NoOverlapReturnsTrue()
        {
            var context = new ExpressionContext();
            context.Variables.Add("arr", new[] { 1, 5, 9 });
            context.Variables.Add("disjoint", new[] { 2, 4, 8 });
            var e = context.CompileDynamic("arr NOT CONTAINS ANY disjoint");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void NotContainsRegex_NoStringMatchReturnsTrue()
        {
            var context = NewContextWithCollections();
            var e = context.CompileDynamic("strs NOT CONTAINS /^xyz/");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void NotContainsRegex_AnyStringMatchReturnsFalse()
        {
            var context = NewContextWithCollections();
            var e = context.CompileDynamic("strs NOT CONTAINS /\\d/");

            Assert.AreEqual(false, e.Evaluate());
        }
    }
}
