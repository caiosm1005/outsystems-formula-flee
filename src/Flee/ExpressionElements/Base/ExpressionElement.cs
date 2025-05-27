using Flee.InternalTypes;
using Flee.Resources;

namespace Flee.ExpressionElements.Base
{
    /// <summary>
    /// Represents the base class for all expression elements in the Flee expression engine.
    /// </summary>
    internal abstract class ExpressionElement
    {
        /// <summary>
        /// Emits the IL code necessary to evaluate this expression element.
        /// </summary>
        /// <param name="ilg">The <see cref="FleeILGenerator"/> used to emit IL instructions.</param>
        /// <param name="services">A service provider for resolving dependencies during emission.</param>
        public abstract void Emit(FleeILGenerator ilg, IServiceProvider services);

        /// <summary>
        /// Gets the <see cref="Type"/> that this expression element evaluates to.
        /// </summary>
        public abstract Type ResultType { get; }

        /// <summary>
        /// Returns the localized name of this expression element, as defined in the resource file.
        /// </summary>
        /// <returns>The localized name string.</returns>
        public override string ToString() => Name;

        /// <summary>
        /// Gets the localized name of this expression element from the resource manager.
        /// Throws an <see cref="InvalidOperationException"/> if the name is not found.
        /// </summary>
        protected string Name
        {
            get
            {
                string key = GetType().Name;
                string value = FleeResourceManager.Instance.GetElementNameString(key);

                if (string.IsNullOrEmpty(value))
                {
                    throw new InvalidOperationException($"Element name for '{key}' not found in resource file.");
                }

                return value;
            }
        }
    }
}
