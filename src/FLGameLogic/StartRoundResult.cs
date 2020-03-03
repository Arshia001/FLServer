using System;
using System.Collections.Generic;
using System.Text;

namespace FLGameLogic
{
    public enum StartRoundResult
    {
        Success,
        MustChooseCategory,
        Error_GameFinished,
        Error_PlayerAlreadyTookTurn,
        Error_NotThisPlayersTurn
    }

    public static class StartRoundResultExtensions
    {
        public static bool IsSuccess(this StartRoundResult value) => value == StartRoundResult.Success;
    }
}
