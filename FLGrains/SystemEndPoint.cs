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
    [StatelessWorker(128), EndPointName("game")]
    class SystemEndPoint : EndPointGrain, ISystemEndPoint
    {
        ISuggestionService suggestionService;


        public SystemEndPoint(ISuggestionService suggestionService)
        {
            this.suggestionService = suggestionService;
        }


        [MethodName("csug")]
        public async Task<EndPointFunctionResult> SuggestCategory(EndPointFunctionParams args)
        {
            await suggestionService.RegisterCategorySuggestion(args.ClientID, args.Args[0].AsString, args.Args[1].AsArray.Select(p => p.AsString));
            return Success();
        }

        [MethodName("wsug")]
        public async Task<EndPointFunctionResult> SuggestWord(EndPointFunctionParams args)
        {
            await suggestionService.RegisterCategorySuggestion(args.ClientID, args.Args[0].AsString, args.Args[1].AsArray.Select(p => p.AsString));
            return Success();
        }
    }
}
