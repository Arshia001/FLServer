using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains.Utility
{
    public static class TaskEx
    {
        public static Task<bool> True { get; } = Task.FromResult(true);
        
        public static Task<bool> False { get; } = Task.FromResult(false);
    }
}
