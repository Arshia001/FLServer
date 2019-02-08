using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public interface ISuggestionService
    {
        Task RegisterCategorySuggestion(Guid ownerID, string categoryName, IEnumerable<string> words);
        Task RegisterWordSuggestion(Guid ownerID, string categoryName, IEnumerable<string> words);
    }
}
