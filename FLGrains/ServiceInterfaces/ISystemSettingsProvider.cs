using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrains.ServiceInterfaces
{
    public interface ISystemSettingsProvider
    {
        string ConnectionString { get; }
        string FcmServiceAccountKeys { get; }
    }
}
