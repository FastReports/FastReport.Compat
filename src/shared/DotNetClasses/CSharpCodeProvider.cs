#if NETSTANDARD2_0 || NETSTANDARD2_1 || NETCOREAPP
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.CSharp;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Text;
using FastReport.Code.CodeDom.Compiler;

namespace FastReport.Code.CSharp
{
    public class CSharpCodeProvider : CodeDomProvider
    {


        public override CompilerResults CompileAssemblyFromSource(CompilerParameters cp, string code)
        {
            DebugMessage(typeof(SyntaxTree).Assembly.FullName);

            DebugMessage("FR.Compat: " +
#if NETSTANDARD
                "NETSTANDARD"
#elif NETCOREAPP
                "NETCOREAPP"
#endif
                );

            SyntaxTree codeTree = CSharpSyntaxTree.ParseText(code);
            CSharpCompilationOptions options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                generalDiagnosticOption: ReportDiagnostic.Default,
                reportSuppressedDiagnostics: true);

            List<MetadataReference> references = new List<MetadataReference>();

            AddReferences(cp, references);


            Compilation compilation = CSharpCompilation.Create(
                "_" + Guid.NewGuid().ToString("D"), new SyntaxTree[] { codeTree },
                references: references, options: options
                );


            OnBeforeEmitCompilation(compilation);

            using (MemoryStream ms = new MemoryStream())
            {
                EmitResult results = compilation.Emit(ms);
                if (results.Success)
                {
                    return new CompilerResults()
                    {
                        CompiledAssembly = Assembly.Load(ms.ToArray())
                    };
                }
                else
                {
                    CompilerResults result = new CompilerResults();
                    foreach (Diagnostic d in results.Diagnostics)
                    {
                        if (d.Severity == DiagnosticSeverity.Error)
                        {
                            result.Errors.Add(new CompilerError()
                            {
                                ErrorText = d.GetMessage(),
                                ErrorNumber = d.Id,
                                Line = d.Location.GetLineSpan().StartLinePosition.Line,
                                Column = d.Location.GetLineSpan().StartLinePosition.Character,

                            });
                        }
                    }
                    return result;
                }
            }

        }

        public override void Dispose()
        {
            
        }

    }
}
#endif