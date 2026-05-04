using System.Resources;

namespace Flee.Resources
{
    /// <summary>
    /// Singleton facade over the three Flee resource bundles (compile errors, element names,
    /// general errors). Lazily creates a <see cref="ResourceManager"/> per bundle and caches
    /// it for subsequent lookups.
    /// </summary>
    internal class FleeResourceManager
    {
        private readonly Dictionary<string, ResourceManager> MyResourceManagers;

        /// <summary>
        /// Initializes the singleton. Private to enforce the singleton pattern.
        /// </summary>
        private FleeResourceManager()
        {
            MyResourceManagers = new Dictionary<string, ResourceManager>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns the cached <see cref="ResourceManager"/> for <paramref name="resourceFile"/>,
        /// constructing it on first access.
        /// </summary>
        /// <param name="resourceFile">The resource bundle's base name (without extension).</param>
        /// <returns>The resource manager.</returns>
        private ResourceManager GetResourceManager(string resourceFile)
        {
            lock (this)
            {
                if (!MyResourceManagers.TryGetValue(resourceFile, out ResourceManager? rm))
                {
                    Type t = typeof(FleeResourceManager);
                    rm = new ResourceManager(string.Format("{0}.{1}", t.Namespace, resourceFile), t.Assembly);
                    MyResourceManagers.Add(resourceFile, rm);
                }
                return rm;
            }
        }

        /// <summary>
        /// Returns the localized string for <paramref name="key"/> in <paramref name="resourceFile"/>,
        /// or <see langword="null"/> when the key is missing.
        /// </summary>
        /// <param name="resourceFile">The resource bundle's base name.</param>
        /// <param name="key">The resource key.</param>
        /// <returns>The localized string, or <see langword="null"/>.</returns>
        private string? GetResourceString(string resourceFile, string key)
        {
            ResourceManager rm = GetResourceManager(resourceFile);
            return rm.GetString(key);
        }

        /// <summary>
        /// Returns a compile-error message string by key.
        /// </summary>
        /// <param name="key">The compile-error resource key.</param>
        /// <returns>The localized message, or <see langword="null"/>.</returns>
        public string? GetCompileErrorString(string key)
        {
            return GetResourceString("CompileErrors", key);
        }

        /// <summary>
        /// Returns an element-name string by key, used for diagnostic prefixes.
        /// </summary>
        /// <param name="key">The element-name resource key.</param>
        /// <returns>The localized name, or <see langword="null"/>.</returns>
        public string? GetElementNameString(string key)
        {
            return GetResourceString("ElementNames", key);
        }

        /// <summary>
        /// Returns a general-error message string by key.
        /// </summary>
        /// <param name="key">The general-error resource key.</param>
        /// <returns>The localized message, or <see langword="null"/>.</returns>
        public string? GetGeneralErrorString(string key)
        {
            return GetResourceString("GeneralErrors", key);
        }

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static FleeResourceManager Instance { get; } = new FleeResourceManager();
    }
}
