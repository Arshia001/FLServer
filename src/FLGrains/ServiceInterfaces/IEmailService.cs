using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains.ServiceInterfaces
{
    interface IEmailService
    {
        Task SendPasswordRecovery(string email, string name, string token);
    }
}
