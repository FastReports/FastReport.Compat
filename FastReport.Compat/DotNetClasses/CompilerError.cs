#if NETSTANDARD2_0 || NETSTANDARD2_1 || NETCOREAPP
using System;
using System.Collections.Generic;
using System.Text;

namespace FastReport.Code.CodeDom.Compiler
{
    public class CompilerError
    {
        public int Line { get; internal set; }
        public int Column { get; internal set; }
        public string ErrorText { get; internal set; }
        public string ErrorNumber { get; internal set; }

    }
}

#endif