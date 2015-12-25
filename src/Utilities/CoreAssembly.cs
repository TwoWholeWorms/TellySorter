using System;
using System.Reflection;

namespace TellySorter.Utilities
{
    
    public static class CoreAssembly
    {
        
        public static readonly Assembly Reference = typeof(CoreAssembly).Assembly;
        public static readonly Version Version = Reference.GetName().Version;

    }

}
