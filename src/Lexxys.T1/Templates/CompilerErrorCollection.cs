using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.CodeDom.Compiler
{
    public class CompilerErrorCollection: List<CompilerError>
    {

        public bool HasErrors => this.Any(o => !o.IsWarning);
    }

    public class CompilerError
    {
        public string ErrorText { get; set; }

        public bool IsWarning { get; set; }
    }
}

namespace System.Runtime.Remoting.Messaging
{
    public static class CallContext
    {
        public static object LogicalGetData(string name) => null;
    }
}