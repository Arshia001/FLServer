using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public interface IConfigWriter
    {
        ConfigData Config { set; }
        int Version { get; }
    }
}
