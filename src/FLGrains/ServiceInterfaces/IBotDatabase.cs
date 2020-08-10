using FLGrainInterfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrains.ServiceInterfaces
{
    interface IBotDatabase
    {
        PlayerInfoDTO GetRandom();
        PlayerInfoDTO? GetByID(Guid id);
        bool IsBotID(Guid id);
    }
}
