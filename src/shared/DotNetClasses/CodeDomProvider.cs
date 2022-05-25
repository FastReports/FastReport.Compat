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

        public event EventHandler<CompilationEventArgs> BeforeEmitCompilation;

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
                    "System.dll",

                    "System.Drawing.dll",

                    //"System.Drawing.Primitives",

                    "System.Data.dll",

                    "System.Xml.dll",
                };
#endif

        private static readonly string[] _additionalAssemblies = new[] {
                "mscorlib",
                "netstandard",
                "System.Collections.Concurrent",
                "System.Collections",
                "System.Collections.NonGeneric",
                "System.Collections.Specialized",
                "System.ComponentModel",
                "System.ComponentModel.Primitives",
                "System.Core",
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
                catch (FileNotFoundException e)
                {
                    DebugMessage($"{reference} FileNotFound");
                    if (SkippedAssemblies.Contains(reference))
                    {
                        DebugMessage($"{reference} FileNotFound. SKIPPED");
                        continue;
                    }
                    else
                        throw e;
                }
#endif

                DebugMessage($"{reference} ADDED");
            }
            DebugMessage("AFTER ADDING ReferencedAssemblies");

            AddExtraAssemblies(cp.ReferencedAssemblies, references);
        }


        protected void AddExtraAssemblies(StringCollection referencedAssemblies, List<MetadataReference> references)
        {

            foreach(string assembly in _additionalAssemblies)
            {
                if (!referencedAssemblies.Contains(assembly))
                {
#if NETCOREAPP
                    try
                    {
#endif
                        references.Add(GetReference(assembly));
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
                var eventArgs = new CompilationEventArgs
                    { Compilation = compilation };
                BeforeEmitCompilation(this, eventArgs);
            }
        }

        public MetadataReference GetReference(string refDll)
        {
            string reference = refDll;
            MetadataReference result;
            try
            {
                if (cache.ContainsKey(refDll))
                    return cache[refDll];

                reference = refDll.EndsWith(".dll") || refDll.EndsWith(".exe") ?
                    refDll.Substring(0, refDll.Length - 4) : refDll;                
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

                                string location = loadedAssembly.Location;
                                DebugMessage($"{reference} location: {location}");
                                if (string.IsNullOrEmpty(location))
                                {
                                    result = GetMetadataReferenceInSingleFileApp(loadedAssembly);
                                }
                                else
                                {
                                    try
                                    {
                                        result = MetadataReference.CreateFromFile(location);
                                    }
                                    catch (Exception ex)
                                    {
                                        DebugMessage(ex.ToString());
                                        throw;
                                    }
                                }

                                cache[refDll] = result;
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
                            result = MetadataReference.CreateFromFile(currAssembly.Location);
                            cache[refDll] = result;
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

                            string location = assembly.Location;

#if NETCOREAPP
                            if (string.IsNullOrEmpty(location))
                            {
                                result = GetMetadataReferenceInSingleFileApp(assembly);
                            }
                            else
#endif
                            {
                                result = MetadataReference.CreateFromFile(location);
                            }

                            cache[refDll] = result;
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
                cache[refDll] = result;
                
                return result;
            }
            catch
            {
                var assemblyName = new AssemblyName(reference);
                DebugMessage("IN AssemblyName");

#if NETCOREAPP
                // try load Assembly in runtime (for user script with custom assembly)
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
#else
                var assembly = Assembly.Load(assemblyName);
#endif
                string location = assembly.Location;
                DebugMessage($"Location after LoadFromAssemblyName: {location}");

#if NETCOREAPP
                if(string.IsNullOrEmpty(location))
                {
                    result = GetMetadataReferenceInSingleFileApp(assembly);
                }
                else
#endif
                    result = MetadataReference.CreateFromFile(location);
                cache[refDll] = result;
                return result;
            }
        }

#if NETCOREAPP
        private static unsafe MetadataReference GetMetadataReferenceInSingleFileApp(Assembly assembly)
        {
            DebugMessage($"TRY IN UNSAFE METHOD {assembly.GetName().Name}");
            assembly.TryGetRawMetadata(out byte* blob, out int length);
            var moduleMetadata = ModuleMetadata.CreateFromMetadata((IntPtr)blob, length);
            var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
            return assemblyMetadata.GetReference();
        }
#endif

        public static string TryFixReferenceInSingeFileApp(Assembly assembly)
        {
#if NETCOREAPP
            try
            {
                string assemblyName = assembly.GetName().Name;
                if(!cache.ContainsKey(assemblyName))
                {
                    MetadataReference metadataReference = GetMetadataReferenceInSingleFileApp(assembly);
                    cache[assemblyName] = metadataReference;
                }
                return assemblyName;
            }
            catch
            {

            }
#endif
            return null;
        }

        //public IEnumerable<MetadataReference> GetReferences(string[] refsDll)
        //{
        //    MetadataReference result;
        //    AssemblyName[] executingAssemblyReferences = Assembly.GetExecutingAssembly().GetReferencedAssemblies();

        //    foreach (string refDll in refsDll)
        //    {
        //        string reference = refDll;

        //        if (cache.ContainsKey(refDll))
        //            yield return MetadataReference.CreateFromFile(cache[refDll]);

        //        foreach (AssemblyName name in executingAssemblyReferences)
        //        {
        //            if (name.Name == reference
        //                || reference.ToLower().EndsWith(".dll")
        //                && name.Name == reference.Substring(0, reference.Length - 4))
        //            {
        //                result = MetadataReference.CreateFromFile(
        //                    Assembly.Load(name).Location);
        //                cache[refDll] = reference;
        //                yield return result;
        //            }

        //        }
        //    }

        //}
    }
}
#endif