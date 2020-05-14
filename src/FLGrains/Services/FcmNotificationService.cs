﻿using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using FLGrainInterfaces.Configuration;
using FLGrains.ServiceInterfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrains.Services
{
    class FcmNotificationService : IFcmNotificationService
    {
        private readonly IConfigReader configReader;
        private readonly ILogger<FcmNotificationService> logger;

        public FcmNotificationService(ISystemSettingsProvider settingsProvider, IConfigReader configReader, ILogger<FcmNotificationService> logger)
        {
            var options = new AppOptions
            {
                Credential = GoogleCredential.FromJson(settingsProvider.Settings.FcmServiceAccountKeys)
            };
            FirebaseApp.Create(options);

            this.configReader = configReader;
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

        public void SendDay4Reminder(string token) =>
            Send(token, "خیلی وقته بهمون سر نزدی!", "همه دارن سر امتیاز با هم می‌جنگن، تو هم بیا رتبه‌ت رو ببر بالا که ازشون عقب نمونی!", "day4");

        public void SendRoundWinRewardAvailableReminder(string token) =>
            Send(token, "چالش روزانه آماده شد", "جعبه چالش روزانه آماده شده. بیا بازی کن و جایزه‌ت رو بگیر!", "roundwinreward");
    }
}
