using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrains.ServiceInterfaces
{
    public interface IConnectionStringProvider
    {
        string ConnectionString { get; }
    }
}
