using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces.Configuration
{
    public interface IConfigReader
    {
        ReadOnlyConfigData Config { get; }
    }
}
