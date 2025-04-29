using System;
using System.Reflection;
using System.Runtime.Loader;

namespace TimeCardSystem.API
{
    /// <summary>
    /// Custom context for loading unmanaged libraries needed by DinkToPdf
    /// </summary>
    internal class CustomAssemblyLoadContext : AssemblyLoadContext
    {
        /// <summary>
        /// Creates a new instance of the CustomAssemblyLoadContext
        /// </summary>
        public CustomAssemblyLoadContext() : base(isCollectible: true)
        {
        }

        /// <summary>
        /// Loads an unmanaged library from the specified path
        /// </summary>
        /// <param name="absolutePath">The absolute path to the library file</param>
        public void LoadUnmanagedLibrary(string absolutePath)
        {
            LoadUnmanagedDll(absolutePath);
        }

        /// <summary>
        /// Loads an unmanaged DLL by name
        /// </summary>
        /// <param name="unmanagedDllName">The name of the DLL to load</param>
        /// <returns>The handle to the loaded library</returns>
        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            return LoadUnmanagedDllFromPath(unmanagedDllName);
        }

        /// <summary>
        /// Loads a managed assembly by name (not used in this context)
        /// </summary>
        /// <param name="assemblyName">The name of the assembly to load</param>
        /// <returns>The loaded assembly or null</returns>
        protected override Assembly Load(AssemblyName assemblyName)
        {
            return null;
        }
    }
}