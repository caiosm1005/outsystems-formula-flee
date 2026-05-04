using System.Collections.Generic;
using Flee.PublicTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flee.Tests.ExpressionTests
{
    /// <summary>
    /// Exercises the <c>IF(...)</c> conditional and the <c>IN</c> membership operator in both
    /// inline-list mode and collection mode (arrays, lists, dictionaries).
    /// </summary>
    [TestClass]
    public class ConditionalAndInOperatorTests
    {
        // --- IF: basic branching ---

        [TestMethod]
        public void If_TrueCondition_ReturnsThenBranch()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("if(true, 100, 200)");

            Assert.AreEqual(100, e.Evaluate());
        }

        [TestMethod]
        public void If_FalseCondition_ReturnsElseBranch()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("if(false, 100, 200)");

            Assert.AreEqual(200, e.Evaluate());
        }

        [TestMethod]
        public void If_StringBranches_PicksMatchingArm()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("if(1 < 2, \"yes\", \"no\")");

            Assert.AreEqual("yes", e.Evaluate());
        }

        [TestMethod]
        public void If_DifferentNumericTypeArms_PromotesToCommon()
        {
            // One arm is double, the other is int — result should be double.
            var context = new ExpressionContext();
            var e = context.CompileDynamic("if(true, 1.5, 2)");

            Assert.AreEqual(1.5, e.Evaluate());
            Assert.AreEqual(typeof(double), e.Evaluate().GetType());
        }

        // --- IF: lazy evaluation ---

        public sealed class IfTracker
        {
            public int ThenCalls { get; set; }

            public int ElseCalls { get; set; }

            public int Then()
            {
                ThenCalls++;
                return 100;
            }

            public int Else()
            {
                ElseCalls++;
                return 200;
            }
        }

        [TestMethod]
        public void If_TrueCondition_DoesNotEvaluateElse()
        {
            var owner = new IfTracker();
            var context = new ExpressionContext(owner);
            var e = context.CompileDynamic("if(true, Then(), Else())");

            Assert.AreEqual(100, e.Evaluate());
            Assert.AreEqual(1, owner.ThenCalls);
            Assert.AreEqual(0, owner.ElseCalls);
        }

        [TestMethod]
        public void If_FalseCondition_DoesNotEvaluateThen()
        {
            var owner = new IfTracker();
            var context = new ExpressionContext(owner);
            var e = context.CompileDynamic("if(false, Then(), Else())");

            Assert.AreEqual(200, e.Evaluate());
            Assert.AreEqual(0, owner.ThenCalls);
            Assert.AreEqual(1, owner.ElseCalls);
        }

        // --- IF: nested ---

        [TestMethod]
        public void If_Nested_PicksDeepestMatchingArm()
        {
            var context = new ExpressionContext();
            context.Variables.Add("x", 5);
            var e = context.CompileDynamic("if(x > 0, if(x > 10, \"big\", \"small\"), \"neg\")");

            Assert.AreEqual("small", e.Evaluate());
        }

        // --- IN operator: list mode ---

        [TestMethod]
        public void In_ListContainsValue_ReturnsTrue()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("3 IN (1, 2, 3, 4)");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void In_ListMissingValue_ReturnsFalse()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("99 IN (1, 2, 3, 4)");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void NotIn_PrefixForm_ListMissingValue_ReturnsTrue()
        {
            // Legacy `NOT <expr> IN (...)` form — unary NOT applied to the bool result of IN.
            var context = new ExpressionContext();
            var e = context.CompileDynamic("NOT 99 IN (1, 2, 3, 4)");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void NotIn_PostfixForm_ListMissingValue_ReturnsTrue()
        {
            // SQL-style `<expr> NOT IN (...)` form — the NOT is part of the SQL operator.
            var context = new ExpressionContext();
            var e = context.CompileDynamic("99 NOT IN (1, 2, 3, 4)");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void NotIn_PostfixForm_ListContainsValue_ReturnsFalse()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("3 NOT IN (1, 2, 3, 4)");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void NotIn_PostfixForm_StringList_MatchesByValue()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("\"foo\" NOT IN (\"a\", \"b\", \"c\")");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void NotIn_PostfixForm_VariableCollection_FindsValue()
        {
            var context = new ExpressionContext();
            context.Variables.Add("arr", new[] { 1, 5, 9 });
            var e = context.CompileDynamic("5 NOT IN arr");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void In_StringList_MatchesByValue()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("\"abc\" IN (\"x\", \"abc\", \"y\")");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void In_ListWithNumericConversion_HandlesTypePromotion()
        {
            // 100 (int) compared against doubles — the conversion is applied implicitly.
            var context = new ExpressionContext();
            var e = context.CompileDynamic("100 IN (10.0, 50.0, 100.0, 200.0)");

            Assert.AreEqual(true, e.Evaluate());
        }

        // --- IN operator: collection mode ---

        [TestMethod]
        public void In_VariableArray_ChecksMembership()
        {
            var context = new ExpressionContext();
            context.Variables.Add("arr", new[] { 10, 20, 30 });
            var e = context.CompileDynamic("20 IN arr");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void In_VariableArray_MissingValueReturnsFalse()
        {
            var context = new ExpressionContext();
            context.Variables.Add("arr", new[] { 10, 20, 30 });
            var e = context.CompileDynamic("99 IN arr");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void In_GenericList_ChecksMembership()
        {
            var context = new ExpressionContext();
            context.Variables.Add("list", new List<string> { "a", "b", "c" });
            var e = context.CompileDynamic("\"b\" IN list");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void In_Dictionary_ChecksKeys()
        {
            var context = new ExpressionContext();
            var dict = new Dictionary<string, int> { ["foo"] = 1, ["bar"] = 2 };
            context.Variables.Add("d", dict);
            var e = context.CompileDynamic("\"foo\" IN d");

            Assert.AreEqual(true, e.Evaluate());
        }

        // --- Member access: methods, properties, indexers ---

        [TestMethod]
        public void Method_StringLength_ReturnsLength()
        {
            var context = new ExpressionContext();
            context.Variables.Add("s", "hello");
            var e = context.CompileDynamic("s.Length");

            Assert.AreEqual(5, e.Evaluate());
        }

        [TestMethod]
        public void Method_StringSubstring_CallsRegularMethod()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("\"hello\".Substring(1, 3)");

            Assert.AreEqual("ell", e.Evaluate());
        }

        [TestMethod]
        public void Method_StringReplace_CallsWithCharArgs()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("\"abc\".Replace('b', 'X')");

            Assert.AreEqual("aXc", e.Evaluate());
        }

        [TestMethod]
        public void Indexer_OnString_ReturnsCharAtPosition()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("\"hello\"[1]");

            Assert.AreEqual('e', e.Evaluate());
        }

        [TestMethod]
        public void Method_MathStaticCall_ReturnsResult()
        {
            var context = new ExpressionContext();
            context.Imports.AddType(typeof(System.Math));
            var e = context.CompileDynamic("Abs(-100)");

            Assert.AreEqual(100, e.Evaluate());
        }

        [TestMethod]
        public void Method_MathStaticCallChained_ReturnsResult()
        {
            var context = new ExpressionContext();
            context.Imports.AddType(typeof(System.Math), "Math");
            var e = context.CompileDynamic("Math.Max(Math.Abs(-3), 2)");

            Assert.AreEqual(3, e.Evaluate());
        }

        // --- Owner method ---

        public sealed class CalcOwner
        {
            public int Triple(int x) => x * 3;

            public int Constant => 42;
        }

        [TestMethod]
        public void OwnerMethod_CalledWithoutQualifier()
        {
            var owner = new CalcOwner();
            var context = new ExpressionContext(owner);
            var e = context.CompileDynamic("Triple(5)");

            Assert.AreEqual(15, e.Evaluate());
        }

        [TestMethod]
        public void OwnerProperty_AccessedWithoutQualifier()
        {
            var owner = new CalcOwner();
            var context = new ExpressionContext(owner);
            var e = context.CompileDynamic("Constant + 8");

            Assert.AreEqual(50, e.Evaluate());
        }
    }
}
