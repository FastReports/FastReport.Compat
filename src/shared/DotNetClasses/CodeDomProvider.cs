#if NETSTANDARD2_0 || NETSTANDARD2_1 || NETCOREAPP
using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Linq;
using System.Reflection.Metadata;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
#if NETCOREAPP
using System.Runtime.Loader;
#endif

namespace FastReport.Code.CodeDom.Compiler
{
    public abstract class CodeDomProvider : IDisposable
    {
        static readonly Dictionary<string, MetadataReference> cache = new Dictionary<string, MetadataReference>();

        public abstract void Dispose();
        public abstract CompilerResults CompileAssemblyFromSource(CompilerParameters cp, string v);

        /// <summary>
        /// Throws before compilation emit
        /// </summary>
        public event EventHandler<CompilationEventArgs> BeforeEmitCompilation;

        /// <summary>
        /// Manual resolve MetadataReference
        /// </summary>
        public static Func<AssemblyName, MetadataReference> ResolveMetadataReference { get; set; }

        /// <summary>
        /// For developers only
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static event EventHandler<string> Log;

#if NETCOREAPP
        /// <summary>
        /// If these assemblies were not found when 'trimmed', then skip them
        /// </summary>
        protected static readonly string[] SkippedAssemblies = new string[] {
                    "System",

                    "System.Core",

                    "System.Drawing",

                    //"System.Drawing.Primitives",

                    "System.Data",

                    "System.Xml",

                    "System.Private.CoreLib",
                };
#endif

        private static readonly string[] _additionalAssemblies = new[] {
                "mscorlib",
                "netstandard",
                "System.Core",
                "System.Collections.Concurrent",
                "System.Collections",
                "System.Collections.NonGeneric",
                "System.Collections.Specialized",
                "System.ComponentModel",
                "System.ComponentModel.Primitives",
                "System.Data.Common",
#if !SKIA
                "System.Drawing.Common",
#endif
                "System.Globalization",
                "System.IO",
                "System.Linq",
                "System.Linq.Expressions",
                "System.Linq.Parallel",
                "System.Linq.Queryable",
                "System.Numerics",
                "System.Runtime",
                "System.Text.Encoding",
                "System.Text.RegularExpressions"
            };

        [Conditional("DEBUG")]  // Comment for use log messages in Release configuration
        protected static void DebugMessage(string message)
        {
            Debug.WriteLine(message);
            Console.WriteLine(message);
            
            Log?.Invoke(null, message);
        }

        protected void AddReferences(CompilerParameters cp, List<MetadataReference> references)
        {
            foreach (string reference in cp.ReferencedAssemblies)
            {
                DebugMessage($"TRY ADD {reference}.");
#if NETCOREAPP
                try
                {
#endif
                    var metadata = GetReference(reference);
                    references.Add(metadata);
#if NETCOREAPP
                }
                catch (FileNotFoundException)
                {
                    DebugMessage($"{reference} FileNotFound");

                    string assemblyName = GetCorrectAssemblyName(reference);
                    if (SkippedAssemblies.Contains(assemblyName))
                    {
                        DebugMessage($"{reference} FileNotFound. SKIPPED");
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }
#endif

                DebugMessage($"{reference} ADDED");
            }
            DebugMessage("AFTER ADDING ReferencedAssemblies");

            AddExtraAssemblies(cp.ReferencedAssemblies, references);
        }

        protected void AddExtraAssemblies(StringCollection referencedAssemblies, List<MetadataReference> references)
        {
            DebugMessage("Add Extra Assemblies...");
            foreach(string assembly in _additionalAssemblies)
            {
                if (!referencedAssemblies.Contains(assembly))
                {
#if NETCOREAPP
                    try
                    {
#endif
                        var metadata = GetReference(assembly);
                        references.Add(metadata);
#if NETCOREAPP
                    }
                    // If user run 'dotnet publish' with Trimmed - dotnet cut some extra assemblies.
                    // We skip this error, because some assemblies in 'assemblies' array may not be needed
                    catch (FileNotFoundException)
                    {
                        DebugMessage($"{assembly} FILENOTFOUND. SKIPPED");
                        continue;
                    }
#endif
                }
            }
        }

        internal void OnBeforeEmitCompilation(Compilation compilation)
        {
            if (BeforeEmitCompilation != null)
            {
                var eventArgs = new CompilationEventArgs(compilation);
                BeforeEmitCompilation(this, eventArgs);
            }
        }

        public MetadataReference GetReference(string refDll)
        {
            if (cache.ContainsKey(refDll))
                return cache[refDll];

            MetadataReference result;
            string reference = GetCorrectAssemblyName(refDll);

            try
            {
                if (!refDll.Contains(Path.DirectorySeparatorChar))
                {
#if NETCOREAPP
                    // try find in AssemblyLoadContext
                    foreach (AssemblyLoadContext assemblyLoadContext in AssemblyLoadContext.All)
                    {
                        foreach (Assembly loadedAssembly in assemblyLoadContext.Assemblies)
                        {
                            if (loadedAssembly.GetName().Name == reference)
                            {
                                DebugMessage($"FIND {reference} IN AssemblyLoadContext");

                                result = ProcessAssembly(loadedAssembly);

                                AddToCache(refDll, result);
                                return result;
                            }
                        }
                    }
#else
                    foreach (Assembly currAssembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (string.Compare(currAssembly.GetName().Name, reference, true) == 0)
                        {
                            DebugMessage("FIND IN AppDomain");

                            // Found it, return the location as the full reference.
                            result = ProcessAssembly(currAssembly);
                            AddToCache(refDll, result);
                            return result;
                        }
                    }
#endif
                    // try find in ReferencedAssemblies
                    foreach (AssemblyName name in Assembly.GetExecutingAssembly().GetReferencedAssemblies())
                    {
                        if (name.Name == reference)
                        {
                            DebugMessage($"FIND {reference} IN ReferencedAssemblies");
#if NETCOREAPP
                            // try load Assembly in runtime (for user script with custom assembly)
                            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(name);
#else
                            var assembly = Assembly.Load(name);
#endif
                            result = ProcessAssembly(assembly);

                            AddToCache(refDll, result);
                            return result;
                        }
                    }
                }
                

                result = MetadataReference.CreateFromFile(refDll);
#if NETCOREAPP
                try
                {
                    // try load Assembly in runtime (for user script with custom assembly)
                    AssemblyLoadContext.Default.LoadFromAssemblyPath(refDll);
                }
                catch(ArgumentException) {
                    var fullpath = Path.Combine(Environment.CurrentDirectory, refDll);
                    try
                    {
                        AssemblyLoadContext.Default.LoadFromAssemblyPath(fullpath);
                    }
                    catch { }
                }
                catch { }
#endif
                AddToCache(refDll, result);

                return result;
            }
            catch
            {
                DebugMessage("IN AssemblyName");
                var assemblyName = new AssemblyName(reference);

                result = UserResolveMetadataReference(assemblyName);
                if(result != null)
                {
                    DebugMessage($"MetadataReference for assembly {reference} resolved by user");
                    AddToCache(refDll, result);
                    return result;
                }

#if NETCOREAPP
                // try load Assembly in runtime (for user script with custom assembly)
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
#else
                var assembly = Assembly.Load(assemblyName);
#endif
                DebugMessage("After LoadFromAssemblyName");

                result = ProcessAssembly(assembly);

                AddToCache(refDll, result);
                return result;
            }
        }

        private static MetadataReference UserResolveMetadataReference(AssemblyName assembly)
        {
            if (ResolveMetadataReference == null)
                return null;

            return ResolveMetadataReference(assembly);
        }

        private static MetadataReference ProcessAssembly(Assembly assembly)
        {
            MetadataReference result;
            DebugMessage($"Location: {assembly.Location}");

#if NETCOREAPP
            // In SFA location is empty
            // In WASM location is empty
            // In Android DEBUG location is correct (not empty)
            // In Android RELEASE (AOT) location is not empty but incorrect
            if (SpecialCondition(assembly))
            {
                DebugMessage("SpecialCondition is true");
                result = GetMetadataReferenceSpecialized(assembly);
                return result;
            }
#endif
            result = MetadataReference.CreateFromFile(assembly.Location);

            return result;
        }

#if NETCOREAPP

        private static bool SpecialCondition(Assembly assembly)
        {
            string location = assembly.Location;

            DebugMessage($"assemblyName Name {assembly.GetName().Name}");

            bool result = string.IsNullOrEmpty(location)
#if NET6_0_OR_GREATER   // ANDROID_BUILD || IOS_BUILD
                || location.StartsWith(assembly.GetName().Name)
#endif
                ;
            return result;
        }


        private static MetadataReference GetMetadataReferenceSpecialized(Assembly assembly)
        {
            MetadataReference result;
            try
            {
                result = GetMetadataReferenceInSingleFileApp(assembly);
            }
            catch (NotImplementedException)
            {
                DebugMessage("Not implemented assembly load from SFA");
#if AOT_COMPILE_FIX
                result = GetMetadataReferenceFromExternalSource(assembly.GetName());
#else
                throw;
#endif
            }
            return result;
        }

        private static unsafe MetadataReference GetMetadataReferenceInSingleFileApp(Assembly assembly)
        {
            DebugMessage($"TRY IN UNSAFE METHOD {assembly.GetName().Name}");
            assembly.TryGetRawMetadata(out byte* blob, out int length);
            var moduleMetadata = ModuleMetadata.CreateFromMetadata((IntPtr)blob, length);
            var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
            return assemblyMetadata.GetReference();
        }

#if AOT_COMPILE_FIX
        private static MetadataReference GetMetadataReferenceFromExternalSource(AssemblyName assemblyName)
        {
            // try load from external source
            var stream = KludgeLoader.GetResource(assemblyName);
            if(stream == null)
            {
                DebugMessage("KludgeLoader returned null");
                throw new FileNotFoundException(
                    message: "Assembly not found",
                    fileName: assemblyName.Name);
            }
            DebugMessage($"Resource has been received. {stream.Length} {stream.Position}");
            var metadata = AssemblyMetadata.CreateFromStream(stream);
            DebugMessage("Metadata has been got");
            return metadata.GetReference();
        }
#endif
#endif

        public static string TryFixReferenceInSingeFileApp(Assembly assembly)
        {
#if NETCOREAPP
            try
            {
                string assemblyName = assembly.GetName().Name;
                if (!cache.ContainsKey(assemblyName))
                {
                    MetadataReference metadataReference = GetMetadataReferenceSpecialized(assembly);
                    AddToCache(assemblyName, metadataReference);
                }
                return assemblyName;
            }
            catch (Exception ex)
            {
                DebugMessage(ex.ToString());
            }
#endif
            return null;
        }

        private static void AddToCache(string refDll, MetadataReference metadata)
        {
            cache[refDll] = metadata;
        }

        private static string GetCorrectAssemblyName(string reference)
        {
            string assemblyName = reference.EndsWith(".dll") || reference.EndsWith(".exe") ?
                reference.Substring(0, reference.Length - 4) : reference;
            return assemblyName;
        }
    }
}
#endif