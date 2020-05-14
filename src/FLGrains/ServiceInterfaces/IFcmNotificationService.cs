using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrains.ServiceInterfaces
{
    interface IFcmNotificationService
    {
        void SendMyTurnStarted(string token, string opponentName);
        void SendGameEnded(string token, string opponentName);

        public DateTime GetScheduledNotificationTime(DateTime suggestedTime, bool mustBeAfterSuggestedTime);
    }
}
