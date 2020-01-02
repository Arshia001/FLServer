using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FLGrainInterfaces.Configuration;
using FLGrains.ServiceInterfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using RazorEngine.Templating;
using static RazorEngine.Engine;

namespace FLGrains.Services
{
    public class ForgotPasswordViewModel
    {
        public ForgotPasswordViewModel(string name, string recoveryUrl)
        {
            Name = name;
            RecoveryUrl = recoveryUrl;
        }

        public string Name { get; }
        public string RecoveryUrl { get; }
    }

    class EmailService : IEmailService
    {
        const string forgotPasswordTemplateName = "forgotPassword";

        readonly SystemSettings.JsonValues systemSettings;
        readonly ILogger<IEmailService> logger;

        public EmailService(ISystemSettingsProvider systemSettings, ILogger<IEmailService> logger)
        {
            this.systemSettings = systemSettings.Settings.Values;
            this.logger = logger;

            Razor.AddTemplate(forgotPasswordTemplateName, File.ReadAllText(this.systemSettings.ForgotPasswordTemplateFilePath));
            Razor.Compile(forgotPasswordTemplateName, typeof(ForgotPasswordViewModel));
        }

        async Task Send(string toAddress, string fromAddress, string fromName, string subject, string bodyHtml)
        {
            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(systemSettings.MailServerAddress, systemSettings.MailServerPort,
                    systemSettings.MailServerUseSsl ? MailKit.Security.SecureSocketOptions.SslOnConnect : MailKit.Security.SecureSocketOptions.None);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromAddress));
                message.To.Add(new MailboxAddress(toAddress));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = bodyHtml;
                message.Body = bodyBuilder.ToMessageBody();

                await client.SendAsync(message);
            }
        }

        public Task SendPasswordRecovery(string email, string name, string token)
        {
            var body = Razor.Run(forgotPasswordTemplateName,
                modelType: typeof(ForgotPasswordViewModel),
                model: new ForgotPasswordViewModel(name, string.Format(systemSettings.ForgotPasswordRecoveryUrlTemplate, token)));

            return Send(email, systemSettings.ForgotPasswordFromAddress, systemSettings.ForgotPasswordFromSenderName,
                systemSettings.ForgotPasswordSubject, body);
        }
    }
}
