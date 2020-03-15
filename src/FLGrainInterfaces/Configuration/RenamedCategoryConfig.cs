using System.Collections.Generic;
using System.Linq;

namespace FLGrainInterfaces.Configuration
{
    public class RenamedCategoryConfig
    {
        public RenamedCategoryConfig(string oldName, string newName)
        {
            OldName = oldName;
            NewName = newName;
        }

        public string OldName { get; }
        public string NewName { get; }
    }
}
