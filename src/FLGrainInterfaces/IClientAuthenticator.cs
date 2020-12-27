using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public interface IClientAuthenticator : IGrainWithIntegerKey
    {
        Task<Guid?> Authenticate(HandShakeMode mode, Guid? clientID, string? email, string? password, string? bazaarToken);
    }
}
