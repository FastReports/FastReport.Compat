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
using System.Runtime.InteropServices;

namespace FastReport.Code.CSharp
{
    public class CSharpCodeProvider : CodeDomProvider
    {

        public override CompilerResults CompileAssemblyFromSource(CompilerParameters cp, string code)
        {
            DebugMessage(typeof(SyntaxTree).Assembly.FullName);

#if NET6_0_OR_GREATER
            DebugMessage($"rid: {RuntimeInformation.RuntimeIdentifier} " +
                $"arch: {RuntimeInformation.ProcessArchitecture} " +
                $"os-arch: {RuntimeInformation.OSArchitecture} " +
                $"os: {RuntimeInformation.OSDescription}");
#endif

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

            DebugMessage($"References count: {references.Count}");
            //foreach (var reference in references)
            //    DebugMessage($"{reference.Display}");

            Compilation compilation = CSharpCompilation.Create(
                "_" + Guid.NewGuid().ToString("D"), new SyntaxTree[] { codeTree },
                references: references, options: options
                );


            OnBeforeEmitCompilation(compilation);

            using (MemoryStream ms = new MemoryStream())
            {
                DebugMessage("Emit...");
                //DebugMessage(code);
                EmitResult results = compilation.Emit(ms);
                if (results.Success)
                {
#if DEBUG
                    foreach (Diagnostic d in results.Diagnostics)
                        if(d.Severity > DiagnosticSeverity.Hidden)
                            DebugMessage($"Compiler {d.Severity}: {d.GetMessage()}. Line: {d.Location}");
#endif

                    var compiledAssembly = Assembly.Load(ms.ToArray());
                    return new CompilerResults(compiledAssembly);
                }
                else
                {
                    DebugMessage($"results not success, {ms.Length}");
                    CompilerResults result = new CompilerResults();
                    foreach (Diagnostic d in results.Diagnostics)
                    {
                        if (d.Severity == DiagnosticSeverity.Error)
                        {
                            var position = d.Location.GetLineSpan().StartLinePosition;
                            result.Errors.Add(new CompilerError()
                            {
                                ErrorText = d.GetMessage(),
                                ErrorNumber = d.Id,
                                Line = position.Line,
                                Column = position.Character,
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