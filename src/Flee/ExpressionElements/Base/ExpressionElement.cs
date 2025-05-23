using System.Diagnostics;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements.Base
{
    internal abstract class ExpressionElement
    {
        internal ExpressionElement()
        {
        }

        /// <summary>
        /// // All expression elements must be able to emit their IL
        /// </summary>
        /// <param name="ilg"></param>
        /// <param name="services"></param>
        public abstract void Emit(FleeILGenerator ilg, IServiceProvider services);
        /// <summary>
        /// All expression elements must expose the Type they evaluate to
        /// </summary>
        public abstract Type ResultType { get; }

        public override string ToString()
        {
            return Name;
        }

        protected string Name
        {
            get
            {
                string key = GetType().Name;
                string value = FleeResourceManager.Instance.GetElementNameString(key);
                Debug.Assert(value != null, $"Element name for '{key}' not in resource file");
                return value;
            }
        }
    }
}
