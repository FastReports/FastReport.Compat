#if NETSTANDARD2_0 || NETSTANDARD2_1 || NETCOREAPP
using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Linq;
using System.Reflection.Metadata;
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


        protected void AddExtraAssemblies(StringCollection referencedAssemblies, List<MetadataReference> references)
        {

            string[] assemblies = new[] {
                "mscorlib",
                "netstandard",
                "System.Collections.Concurrent",
                "System.Collections",
                "System.Collections.NonGeneric",
                "System.Collections.Specialized",
                "System.ComponentModel.Primitives",
                "System.Core",
                "System.Data.Common",
                "System.Drawing.Common",
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

            foreach(string assembly in assemblies)
            {
                if (!referencedAssemblies.Contains(assembly))
                    references.Add(GetReference(assembly));
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
                                string location = loadedAssembly.Location;
                                if (string.IsNullOrEmpty(location))
                                {
                                    result = GetMetadataReferenceInSingleFileApp(loadedAssembly);
                                }
                                else
                                {
                                    result = MetadataReference.CreateFromFile(location);
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
#if NETCOREAPP
                // try load Assembly in runtime (for user script with custom assembly)
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
#else
                var assembly = Assembly.Load(assemblyName);
#endif

                string location = assembly.Location;

                result = MetadataReference.CreateFromFile(location);
                cache[refDll] = result;
                return result;
            }
        }

#if NETCOREAPP
        private static unsafe MetadataReference GetMetadataReferenceInSingleFileApp(Assembly assembly)
        {
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