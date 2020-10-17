using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrains.ServiceInterfaces
{
    interface IFcmNotificationService
    {
        void SendMyTurnStarted(Guid playerID, string token, string opponentName);
        void SendGameEnded(Guid playerID, string token, string opponentName);
        
        void SendInactivityReminder(Guid playerID, string token);
        void SendRoundWinRewardAvailableReminder(Guid playerID, string token);
        void SendCoinRewardVideoReadyReminder(Guid playerID, string token);
    }
}
