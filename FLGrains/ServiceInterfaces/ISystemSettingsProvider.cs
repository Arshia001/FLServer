using FLGrainInterfaces.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrains.ServiceInterfaces
{
    public interface ISystemSettingsProvider
    {
        SystemSettings Settings { get; }
    }
}
