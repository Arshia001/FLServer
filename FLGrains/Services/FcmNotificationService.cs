using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using FLGrains.ServiceInterfaces;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrains.Services
{
    class FcmNotificationService : IFcmNotificationService
    {
        private readonly ILogger<FcmNotificationService> logger;

        public FcmNotificationService(ISystemSettingsProvider settingsProvider, ILogger<FcmNotificationService> logger)
        {
            var options = new AppOptions
            {
                Credential = GoogleCredential.FromJson(settingsProvider.FcmServiceAccountKeys)
            };
            FirebaseApp.Create(options);
            this.logger = logger;
        }

        async void Send(string token, string title, string body, string collapseKey)
        {
            try
            {
                var message = new Message
                {
                    Android = new AndroidConfig
                    {
                        Notification = new AndroidNotification
                        {
                            Title = title,
                            Body = body,
                            Color = "#FFDF00"
                        },
                        CollapseKey = collapseKey
                    },
                    Token = token
                };
                await FirebaseMessaging.DefaultInstance.SendAsync(message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send FCM notification");
            }
        }

        public void SendGameEnded(string token, string opponentName) =>
            Send(token, "بازی تموم شد", $"بازیت با {opponentName} تموم شد. بیا نتیجه رو ببین!", "gameend");

        public void SendMyTurnStarted(string token, string opponentName) =>
            Send(token, "نوبتت شروع شد", $"حریفت {opponentName} نوبتشو بازی کرد، نوبت تو شروع شده!", "turnend");
    }
}
