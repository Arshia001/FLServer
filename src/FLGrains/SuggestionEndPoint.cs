using FLGrainInterfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    class SuggestionEndPoint : SuggestionEndPointBase
    {
        readonly ISuggestionService suggestionService;

        public SuggestionEndPoint(ISuggestionService suggestionService) => this.suggestionService = suggestionService;

        protected override Task SuggestCategory(Guid clientID, string name, IReadOnlyList<string> words) =>
            suggestionService.RegisterCategorySuggestion(clientID, name, words);

        protected override Task SuggestWord(Guid clientID, string categoryName, IReadOnlyList<string> words) =>
            suggestionService.RegisterCategorySuggestion(clientID, categoryName, words);
    }
}
