#if NETSTANDARD2_0 || NETSTANDARD2_1 || NETCOREAPP
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;

namespace FastReport.Code.CodeDom.Compiler
{
    public abstract class CodeDomProvider : IDisposable
    {
        static readonly Dictionary<string, string> cache = new Dictionary<string, string>();

        public abstract void Dispose();
        public abstract CompilerResults CompileAssemblyFromSource(CompilerParameters cp, string v);

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


        public MetadataReference GetReference(string refDll)
        {
            string reference = refDll;
            try
            {
                if (cache.ContainsKey(refDll))
                    return MetadataReference.CreateFromFile(cache[refDll]);
                MetadataReference result;
                foreach (AssemblyName name in Assembly.GetExecutingAssembly().GetReferencedAssemblies())
                {
                    if (name.Name == reference
                        || reference.ToLower().EndsWith(".dll")
                        && name.Name == reference.Substring(0, reference.Length - 4))
                    {
                        result = MetadataReference.CreateFromFile(
                            Assembly.Load(name).Location);
                        cache[refDll] = reference;
                        return result;
                    }
                }

                result = MetadataReference.CreateFromFile(reference);
                cache[refDll] = reference;
                return result;
            }
            catch
            {
                string ass = reference;
                if (reference.ToLower().EndsWith(".dll"))
                    ass = reference.Substring(0, reference.Length - 4);
                string location = Assembly.Load(new AssemblyName(ass)).Location;
                cache[refDll] = location;
                return MetadataReference.CreateFromFile(location);
            }

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