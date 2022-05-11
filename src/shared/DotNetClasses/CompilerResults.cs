#if NETSTANDARD2_0 || NETSTANDARD2_1 || NETCOREAPP
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace FastReport.Code.CodeDom.Compiler
{
    public class CompilerResults
    {
        public List<CompilerError> Errors { get; } = new List<CompilerError>();
        public Assembly CompiledAssembly { get; internal set; } = null;
    }
}

#endif