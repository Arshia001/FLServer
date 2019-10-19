using System;
using System.Collections.Generic;
using System.Text;

namespace FLGameLogicServer
{
    public enum SetCategoryResult
    {
        Success,
        Error_AlreadySet,
        Error_IndexOutOfBounds,
        Error_PreviousCategoryNotSet
    }

    public static class SetCategoryResultExtensions
    {
        public static bool IsSuccess(this SetCategoryResult value) => value == SetCategoryResult.Success;
    }
}
