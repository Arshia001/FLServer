using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace FLGrainInterfaces.Configuration
{
    public class SystemSettings
    {
        public class JsonValues
        {
            public JsonValues(string connectionString, uint latestVersion, uint minimumSupportedVersion,
                TimeSpan passwordRecoveryTokenExpirationInterval, string mailServerAddress, int mailServerPort,
                bool mailServerUseSsl, string forgotPasswordFromAddress, string forgotPasswordFromSenderName,
                string forgotPasswordSubject, string forgotPasswordTemplateFilePath, string forgotPasswordRecoveryUrlTemplate)
            {
                ConnectionString = connectionString;
                LatestVersion = latestVersion;
                MinimumSupportedVersion = minimumSupportedVersion;
                PasswordRecoveryTokenExpirationInterval = passwordRecoveryTokenExpirationInterval;
                MailServerAddress = mailServerAddress;
                MailServerPort = mailServerPort;
                MailServerUseSsl = mailServerUseSsl;
                ForgotPasswordFromAddress = forgotPasswordFromAddress;
                ForgotPasswordFromSenderName = forgotPasswordFromSenderName;
                ForgotPasswordSubject = forgotPasswordSubject;
                ForgotPasswordTemplateFilePath = forgotPasswordTemplateFilePath;
                ForgotPasswordRecoveryUrlTemplate = forgotPasswordRecoveryUrlTemplate;
            }

            public string ConnectionString { get; }

            public uint LatestVersion { get; }
            public uint MinimumSupportedVersion { get; }

            public TimeSpan PasswordRecoveryTokenExpirationInterval { get; }

            public string MailServerAddress { get; }
            public int MailServerPort { get; }
            public bool MailServerUseSsl { get; }

            public string ForgotPasswordFromAddress { get; }
            public string ForgotPasswordFromSenderName { get; }
            public string ForgotPasswordSubject { get; }
            public string ForgotPasswordTemplateFilePath { get; }
            public string ForgotPasswordRecoveryUrlTemplate { get; }
        }

        public SystemSettings(string jsonValues, string fcmServiceAccountKeys)
        {
            FcmServiceAccountKeys = fcmServiceAccountKeys;
            Values = JsonConvert.DeserializeObject<JsonValues>(jsonValues);
        }

        public string FcmServiceAccountKeys { get; }

        public JsonValues Values { get; }
    }
}
