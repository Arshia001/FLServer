using System;
using System.Collections.Generic;
using System.Text;

namespace FLGameLogic
{
    public enum PlayWordResult
    {
        Success,
        Error_TurnOver
    }

    public static class PlayWordResultExtensions
    {
        public static bool IsSuccess(this PlayWordResult value) => value == PlayWordResult.Success;
    }
}
