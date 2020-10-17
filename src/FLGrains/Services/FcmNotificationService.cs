using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using FLGrains.ServiceInterfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrains.Services
{
    class FcmNotificationService : IFcmNotificationService
    {
        readonly ILogger<FcmNotificationService> logger;
        readonly IGrainFactory grainFactory;

        public FcmNotificationService(ISystemSettingsProvider settingsProvider, ILogger<FcmNotificationService> logger, IGrainFactory grainFactory)
        {
            var options = new AppOptions
            {
                Credential = GoogleCredential.FromJson(settingsProvider.Settings.FcmServiceAccountKeys)
            };
            FirebaseApp.Create(options);

            this.logger = logger;
            this.grainFactory = grainFactory;
        }

        async void Send(Guid playerID, string token, string title, string body, string collapseKey, string analyticsCategory)
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
                    Token = token,
                    Data = new Dictionary<string, string>
                    {
                        { "analytics_category", analyticsCategory }
                    }
                };
                await FirebaseMessaging.DefaultInstance.SendAsync(message);
            }
            catch (FirebaseMessagingException fmex) when (
                fmex.MessagingErrorCode == MessagingErrorCode.SenderIdMismatch ||
                fmex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
            {
                try
                {
                    await grainFactory.GetGrain<IPlayer>(playerID).UnsetFcmToken();
                }
                catch (Exception ex2)
                {
                    logger.LogError(ex2, $"Failed to unset FCM token for player {playerID} after error with code {fmex.MessagingErrorCode}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send FCM notification");
            }
        }

        public void SendGameEnded(Guid playerID, string token, string opponentName) =>
            Send(playerID, token, "بازی تموم شد", $"بازیت با {opponentName} تموم شد. بیا نتیجه رو ببین!", "gameend", "gameend");

        public void SendMyTurnStarted(Guid playerID, string token, string opponentName) =>
            Send(playerID, token, "نوبتت شروع شد", $"حریفت {opponentName} نوبتشو بازی کرد، نوبت تو شروع شده!", "turnend", "turnend");

        public void SendInactivityReminder(Guid playerID, string token) =>
            Send(playerID, token, "خیلی وقته بهمون سر نزدی!", "همه دارن سر امتیاز با هم می‌جنگن، تو هم بیا رتبه‌ت رو ببر بالا که ازشون عقب نمونی!", "day4", "day4");

        public void SendRoundWinRewardAvailableReminder(Guid playerID, string token) =>
            Send(playerID, token, "چالش روزانه آماده شد", "جعبه چالش روزانه آماده شده. بیا بازی کن و جایزه‌ت رو بگیر!", "roundwinreward", "roundwinreward");

        public void SendCoinRewardVideoReadyReminder(Guid playerID, string token) =>
            Send(playerID, token, "هدیه آماده شد", "هدیه‌ی بعدی آماده‌ست، بیا تو بازی تا دریافتش کنی!", "coinrewardvideo", "coinrewardvideo");
    }
}
