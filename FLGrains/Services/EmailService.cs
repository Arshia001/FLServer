using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FLGrains.ServiceInterfaces;
using Microsoft.Extensions.Logging;

namespace FLGrains.Services
{
    class EmailService : IEmailService
    {
        private readonly ISystemSettingsProvider systemSettings;
        private readonly ILogger<IEmailService> logger;

        public EmailService(ISystemSettingsProvider systemSettings, ILogger<IEmailService> logger)
        {
            this.systemSettings = systemSettings;
            this.logger = logger;
        }

        public Task SendPasswordRecovery(string email, string name, string token)
        {
            logger.LogInformation($"Player {name} with email {email} gets recovery token {token}");
            return Task.CompletedTask; //?? configure and connect a mail server!
        }
    }
}
