using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public interface ISuggestionService
    {
        Task RegisterCategorySuggestion(Guid ownerID, string categoryName, IEnumerable<string> words);
        Task<IEnumerable<(string category, IEnumerable<string> words)>> GetCategorySuggestionsByUser(Guid ownerID);
        Task<IEnumerable<(Guid ownerID, string category, IEnumerable<string> words)>> GetAllCategorySuggestions();

        Task RegisterWordSuggestion(Guid ownerID, string categoryName, IEnumerable<string> words);
        Task<IEnumerable<(string category, IEnumerable<string> words)>> GetWordSuggestionsByUser(Guid ownerID);
        Task<IEnumerable<(Guid ownerID, string category, IEnumerable<string> words)>> GetAllWordSuggestions();
    }
}
