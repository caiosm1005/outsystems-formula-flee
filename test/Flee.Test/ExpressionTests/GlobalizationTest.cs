using System;
using Flee.PublicTypes;
using NUnit.Framework;

namespace Flee.Test.ExpressionTests
{
    [TestFixture]
    public class GlobalizationTest
    {
        [Test]
        public void DateTimeFormat()
        {
            ExpressionContext context = new();

            context.ParserOptions.DateTimeFormats = new string[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss" };

            IDynamicExpression e1 = context.CompileDynamic("#2025-05-10#");
            IDynamicExpression e2 = context.CompileDynamic("#2025-05-10 12:12:12#");

            Assert.AreEqual(new DateTime(2025, 5, 10), e1.Evaluate());
            Assert.AreEqual(new DateTime(2025, 5, 10, 12, 12, 12), e2.Evaluate());
        }
    }
}
