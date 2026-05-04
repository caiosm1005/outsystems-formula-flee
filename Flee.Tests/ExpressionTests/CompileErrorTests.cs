using Flee.PublicTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flee.Tests.ExpressionTests
{
    /// <summary>
    /// Exercises compile-time errors. Each test asserts both the exception type and the
    /// <see cref="ExpressionCompileException.Reason"/> categorization.
    /// </summary>
    [TestClass]
    public class CompileErrorTests
    {
        // --- Syntax errors ---

        [TestMethod]
        public void Syntax_UnclosedString_IsSyntaxError()
        {
            var context = new ExpressionContext();
            var ex = Assert.ThrowsException<ExpressionCompileException>(
                () => context.CompileDynamic("\"unterminated"));

            Assert.AreEqual(CompileExceptionReason.SyntaxError, ex.Reason);
        }

        [TestMethod]
        public void Syntax_DanglingPlus_IsSyntaxError()
        {
            var context = new ExpressionContext();
            var ex = Assert.ThrowsException<ExpressionCompileException>(
                () => context.CompileDynamic("1 +"));

            Assert.AreEqual(CompileExceptionReason.SyntaxError, ex.Reason);
        }

        [TestMethod]
        public void Syntax_MissingParen_IsSyntaxError()
        {
            var context = new ExpressionContext();
            var ex = Assert.ThrowsException<ExpressionCompileException>(
                () => context.CompileDynamic("(1 + 2"));

            Assert.AreEqual(CompileExceptionReason.SyntaxError, ex.Reason);
        }

        [TestMethod]
        public void Syntax_EmptyString_IsSyntaxError()
        {
            var context = new ExpressionContext();
            var ex = Assert.ThrowsException<ExpressionCompileException>(
                () => context.CompileDynamic(""));

            Assert.AreEqual(CompileExceptionReason.SyntaxError, ex.Reason);
        }

        // --- Undefined names ---

        [TestMethod]
        public void Undefined_VariableName_IsUndefinedName()
        {
            var context = new ExpressionContext();
            var ex = Assert.ThrowsException<ExpressionCompileException>(
                () => context.CompileDynamic("missingVar + 1"));

            Assert.AreEqual(CompileExceptionReason.UndefinedName, ex.Reason);
        }

        [TestMethod]
        public void Undefined_FunctionName_IsUndefinedName()
        {
            var context = new ExpressionContext();
            var ex = Assert.ThrowsException<ExpressionCompileException>(
                () => context.CompileDynamic("undefinedFunc(1)"));

            Assert.AreEqual(CompileExceptionReason.UndefinedName, ex.Reason);
        }

        [TestMethod]
        public void Undefined_MemberOnString_IsUndefinedName()
        {
            var context = new ExpressionContext();
            var ex = Assert.ThrowsException<ExpressionCompileException>(
                () => context.CompileDynamic("\"abc\".NotAMember"));

            Assert.AreEqual(CompileExceptionReason.UndefinedName, ex.Reason);
        }

        // --- Type mismatches ---

        [TestMethod]
        public void TypeMismatch_StringMinusInt_IsTypeMismatch()
        {
            var context = new ExpressionContext();
            var ex = Assert.ThrowsException<ExpressionCompileException>(
                () => context.CompileDynamic("\"abc\" - 1"));

            Assert.AreEqual(CompileExceptionReason.TypeMismatch, ex.Reason);
        }

        [TestMethod]
        public void TypeMismatch_BoolPlusInt_IsTypeMismatch()
        {
            var context = new ExpressionContext();
            var ex = Assert.ThrowsException<ExpressionCompileException>(
                () => context.CompileDynamic("true + 1"));

            Assert.AreEqual(CompileExceptionReason.TypeMismatch, ex.Reason);
        }

        // --- Constant overflow ---

        [TestMethod]
        public void ConstantOverflow_TooLargeIntLiteral_IsConstantOverflow()
        {
            var context = new ExpressionContext();
            var ex = Assert.ThrowsException<ExpressionCompileException>(
                () => context.CompileDynamic("99999999999999999999999"));

            Assert.AreEqual(CompileExceptionReason.ConstantOverflow, ex.Reason);
        }

        // --- Invalid format (date) ---

        [TestMethod]
        public void InvalidFormat_BadDateLiteral_IsInvalidFormat()
        {
            var context = new ExpressionContext();
            context.ParserOptions.DateTimeFormat = "dd/MM/yyyy";
            context.ParserOptions.RecreateParser();

            var ex = Assert.ThrowsException<ExpressionCompileException>(
                () => context.CompileDynamic("#not-a-date#"));

            Assert.AreEqual(CompileExceptionReason.InvalidFormat, ex.Reason);
        }

        // --- Generic compile result-type mismatch ---

        [TestMethod]
        public void Generic_StringExpressionAsInt_FailsToCompile()
        {
            var context = new ExpressionContext();

            Assert.ThrowsException<ExpressionCompileException>(
                () => context.CompileGeneric<int>("\"abc\""));
        }

        // --- Message wraps inner parser message ---

        [TestMethod]
        public void Syntax_Message_ContainsInnerDetail()
        {
            var context = new ExpressionContext();
            var ex = Assert.ThrowsException<ExpressionCompileException>(
                () => context.CompileDynamic("@@@@"));

            Assert.IsNotNull(ex.Message);
            Assert.IsTrue(ex.Message.Length > 0);
        }
    }
}
