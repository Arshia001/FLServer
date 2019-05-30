using FLGameLogic;
using FLGrainInterfaces;
using LightMessage.Common.Messages;
using LightMessage.OrleansUtils.GrainInterfaces;
using LightMessage.OrleansUtils.Grains;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    class SystemEndPoint : SystemEndPointBase
    {
        ISuggestionService suggestionService;


        public SystemEndPoint(ISuggestionService suggestionService)
        {
            this.suggestionService = suggestionService;
        }

        protected override Task<OwnPlayerInfo> GetStartupInfo(Guid clientID) =>
            GrainFactory.GetGrain<IPlayer>(clientID).PerformStartupTasksAndGetInfo().UnwrapImmutable();

        protected override Task SuggestCategory(Guid clientID, string name, IReadOnlyList<string> words) =>
            suggestionService.RegisterCategorySuggestion(clientID, name, words);

        protected override Task SuggestWord(Guid clientID, string categoryName, IReadOnlyList<string> words) =>
            suggestionService.RegisterCategorySuggestion(clientID, categoryName, words);
    }
}
