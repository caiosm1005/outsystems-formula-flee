using System.ComponentModel.Design;
using System.Reflection.Emit;
using System.Reflection;
using Flee.ExpressionElements;
using Flee.ExpressionElements.Base;
using Flee.PublicTypes;
using Flee.Resources;
using IDynamicExpression = Flee.PublicTypes.IDynamicExpression;

namespace Flee.InternalTypes
{
    /// <summary>
    /// The compiled expression. Parses an expression source string, builds an
    /// <see cref="ExpressionElement"/> tree, emits IL into a <see cref="DynamicMethod"/>, and
    /// exposes both <see cref="IDynamicExpression"/> (untyped) and <see cref="IGenericExpression{T}"/>
    /// (strongly typed) evaluation entry points.
    /// </summary>
    /// <typeparam name="T">The expression's result type.</typeparam>
    internal class Expression<T> : IExpression, IDynamicExpression, IGenericExpression<T>
    {
        private ExpressionOptions _myOptions = null!;
        private ExpressionEvaluator<T> _myEvaluator = null!;

        private object _myOwner;

        /// <summary>
        /// Assembly name used when <see cref="ExpressionOptions.EmitToAssembly"/> is enabled.
        /// </summary>
        private const string EmitAssemblyName = "FleeExpression";

        /// <summary>
        /// Display name of the dynamic method holding the compiled IL.
        /// </summary>
        private const string DynamicMethodName = "Flee Expression";

        /// <summary>
        /// Compiles <paramref name="expression"/> against <paramref name="context"/>.
        /// </summary>
        /// <param name="expression">The expression source text.</param>
        /// <param name="context">
        /// The compilation context. Cloned unless <see cref="ExpressionContext.NoClone"/> is set.
        /// </param>
        /// <param name="isGeneric">
        /// When <see langword="true"/>, the result type is fixed to <typeparamref name="T"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="expression"/> is <see langword="null"/>.
        /// </exception>
        public Expression(string expression, ExpressionContext context, bool isGeneric)
        {
            Utility.AssertNotNull(expression, "expression");
            Text = expression;
            _myOwner = context.ExpressionOwner;

            Context = context;

            if (!context.NoClone)
            {
                Context = context.CloneInternal(false);
            }

            Info1 = new ExpressionInfo();

            SetupOptions(Context.Options, isGeneric);

            Context.Imports.ImportOwner(_myOptions.OwnerType);

            ValidateOwner(_myOwner!);

            Compile(expression, _myOptions);

            Context.CalculationEngine?.FixTemporaryHead(this, Context, _myOptions.ResultType);
        }

        /// <summary>
        /// Configures options carried over from the compile context (generic flag, result type,
        /// owner type).
        /// </summary>
        /// <param name="options">The options to mutate.</param>
        /// <param name="isGeneric">Whether the expression has a strongly typed result.</param>
        private void SetupOptions(ExpressionOptions options, bool isGeneric)
        {
            // Make sure we clone the options
            _myOptions = options;
            _myOptions.IsGeneric = isGeneric;

            if (isGeneric)
            {
                _myOptions.ResultType = typeof(T);
            }

            _myOptions.SetOwnerType(_myOwner.GetType());
        }

        /// <summary>
        /// Parses <paramref name="expression"/>, walks the resulting element tree and emits IL
        /// into a <see cref="DynamicMethod"/>. Performs a second emit pass when long branches
        /// require it.
        /// </summary>
        /// <param name="expression">The expression source text.</param>
        /// <param name="options">The compile options.</param>
        private void Compile(string expression, ExpressionOptions options)
        {
            // Add the services that will be used by elements during the compile
            IServiceContainer services = new ServiceContainer();
            AddServices(services);

            // Parse and get the root element of the parse tree
            ExpressionElement topElement = Context.Parse(expression, services);

            if (options.ResultType == null)
            {
                options.ResultType = topElement.ResultType;
            }

            RootExpressionElement rootElement = new(topElement, options.ResultType);

            DynamicMethod dm = CreateDynamicMethod();

            FleeILGenerator ilg = new(dm.GetILGenerator());

            // Emit the IL
            rootElement.Emit(ilg, services);
            if (ilg.NeedsSecondPass())
            {
                // second pass required due to long branches.
                dm = CreateDynamicMethod();
                ilg.PrepareSecondPass(dm.GetILGenerator());
                rootElement.Emit(ilg, services);
            }

            ilg.ValidateLength();

            // Emit to an assembly if required
            if (options.EmitToAssembly)
            {
                EmitToAssembly(ilg, rootElement, services);
            }

            Type delegateType = typeof(ExpressionEvaluator<>).MakeGenericType(typeof(T));
            _myEvaluator = (ExpressionEvaluator<T>)dm.CreateDelegate(delegateType);
        }

        /// <summary>
        /// Creates a fresh <see cref="DynamicMethod"/> matching the evaluator delegate signature.
        /// </summary>
        /// <returns>The new dynamic method.</returns>
        private DynamicMethod CreateDynamicMethod()
        {
            // Create the dynamic method
            Type[] parameterTypes = [
            typeof(object),
            typeof(ExpressionContext),
            typeof(VariableCollection)
        ];
            DynamicMethod dm;

            dm = new DynamicMethod(DynamicMethodName, typeof(T), parameterTypes, _myOptions.OwnerType);

            return dm;
        }

        /// <summary>
        /// Registers the per-compile services consumed by the expression elements.
        /// </summary>
        /// <param name="dest">The service container to populate.</param>
        private void AddServices(IServiceContainer dest)
        {
            dest.AddService(typeof(ExpressionOptions), _myOptions);
            dest.AddService(typeof(ExpressionParserOptions), Context.ParserOptions);
            dest.AddService(typeof(ExpressionContext), Context);
            dest.AddService(typeof(IExpression), this);
            dest.AddService(typeof(ExpressionInfo), Info1);
        }

        /// <summary>
        /// Emit to an assembly. We've already computed long branches at this point, so we emit
        /// as a second pass.
        /// </summary>
        /// <param name="ilg">The IL generator that already completed the first pass.</param>
        /// <param name="rootElement">The element tree to emit.</param>
        /// <param name="services">The compile services.</param>
        private static void EmitToAssembly(
            FleeILGenerator ilg,
            ExpressionElement rootElement,
            IServiceContainer services)
        {
            AssemblyName assemblyName = new(EmitAssemblyName);

            string assemblyFileName = string.Format("{0}.dll", EmitAssemblyName);

            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                assemblyName,
                AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyFileName);

            MethodBuilder mb = moduleBuilder.DefineGlobalMethod(
                "Evaluate",
                MethodAttributes.Public | MethodAttributes.Static,
                typeof(T),
                [typeof(object), typeof(ExpressionContext), typeof(VariableCollection)]);
            // already emitted once for local use,
            ilg.PrepareSecondPass(mb.GetILGenerator());

            rootElement.Emit(ilg, services);

            moduleBuilder.CreateGlobalFunctions();
            //assemblyBuilder.Save(assemblyFileName);
            _ = assemblyBuilder.CreateInstance(assemblyFileName);
        }

        /// <summary>
        /// Throws when <paramref name="owner"/> isn't compatible with the configured owner type.
        /// </summary>
        /// <param name="owner">The owner instance to check.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="owner"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when the type isn't assignable.</exception>
        private void ValidateOwner(object owner)
        {
            Utility.AssertNotNull(owner, "owner");
            if (!_myOptions.OwnerType.IsAssignableFrom(owner.GetType()))
            {
                string msg = Utility.GetGeneralErrorMessage(
                    GeneralErrorResourceKeys.NewOwnerTypeNotAssignableToCurrentOwner);
                throw new ArgumentException(msg);
            }
        }

        /// <summary>
        /// Evaluates the expression and returns the result boxed as <see cref="object"/>.
        /// </summary>
        /// <returns>The boxed evaluation result.</returns>
        public object Evaluate()
        {
            return _myEvaluator(_myOwner, Context, Context.Variables)!;
        }

        /// <summary>
        /// Evaluates the expression and returns its strongly typed result.
        /// </summary>
        /// <returns>The evaluation result.</returns>
        public T EvaluateGeneric()
        {
            return _myEvaluator(_myOwner, Context, Context.Variables);
        }

        /// <summary>
        /// Explicit <see cref="IGenericExpression{T}.Evaluate"/> implementation.
        /// </summary>
        /// <returns>The evaluation result.</returns>
        T IGenericExpression<T>.Evaluate()
        {
            return EvaluateGeneric();
        }

        /// <summary>
        /// Creates a deep copy of this expression. Cloning forks the context (and its variables)
        /// so the copy can be evaluated independently.
        /// </summary>
        /// <returns>The cloned expression.</returns>
        public IExpression Clone()
        {
            Expression<T> copy = (Expression<T>)MemberwiseClone();
            copy.Context = Context.CloneInternal(true);
            copy._myOptions = copy.Context.Options;
            return copy;
        }

        /// <summary>
        /// Returns the original source text of the expression.
        /// </summary>
        /// <returns>The source text.</returns>
        public override string ToString()
        {
            return Text;
        }

        /// <summary>
        /// Gets the configured result type. Internal because public callers see this through
        /// the strongly typed <typeparamref name="T"/> on <see cref="IGenericExpression{T}"/>.
        /// </summary>
        internal Type ResultType => _myOptions.ResultType;

        /// <summary>
        /// Gets the original source text of the expression.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the metadata collected during compilation.
        /// </summary>
        public ExpressionInfo Info1 { get; }

        /// <summary>
        /// Explicit <see cref="IExpression.Info"/> implementation routing through
        /// <see cref="Info1"/>.
        /// </summary>
        ExpressionInfo IExpression.Info => Info1;

        /// <summary>
        /// Gets or sets the owner instance whose members are exposed to the expression.
        /// Setting validates assignment against the configured owner type.
        /// </summary>
        public object? Owner
        {
            get => _myOwner;
            set
            {
                ValidateOwner(value!);
                _myOwner = value!;
            }
        }

        /// <summary>
        /// Gets the compilation context the expression was compiled against.
        /// </summary>
        public ExpressionContext Context { get; private set; }
    }
}
