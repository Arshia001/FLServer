using Cassandra;
using FLGrainInterfaces;
using FLGrains.ServiceInterfaces;
using OrleansCassandraUtils.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FLGrains
{
    //!! pagination - use cassandra's paging capabilities, i.e. RowSet.PagingState and friends
    class SuggestionService : ISuggestionService
    {
        public static async Task<SuggestionService> CreateInstance(ISystemSettingsProvider connectionStringProvider) =>
            new SuggestionService(await Queries.CreateInstance(await CassandraSessionFactory.CreateSession(connectionStringProvider.Settings.Values.ConnectionString)));

        readonly Queries queries;

        private SuggestionService(Queries queries) => this.queries = queries;

        public Task RegisterCategorySuggestion(Guid ownerID, string categoryName, string words) =>
            queries.Session.ExecuteAsync(queries["fl_UpsertSuggestedCategory"].Bind(new
            {
                name = categoryName,
                owner_id = ownerID,
                words = words
            }));

        public async Task<IEnumerable<(string category, string words)>> GetCategorySuggestionsByUser(Guid ownerID)
        {
            var rows = await queries.Session.ExecuteAsync(queries["fl_ReadSuggestedCategoriesByUser"].Bind(new { owner_id = ownerID }));
            return rows.Select(r => ((string)r["name"], (string)r["words"]));
        }

        public async Task<IEnumerable<(Guid ownerID, string category, string words)>> GetAllCategorySuggestions()
        {
            var rows = await queries.Session.ExecuteAsync(queries["fl_ReadSuggestedCategories"].Bind());
            return rows.Select(r => ((Guid)r["owner_id"], (string)r["name"], (string)r["words"]));
        }

        public Task RegisterWordSuggestion(Guid ownerID, string categoryName, IEnumerable<string> words) =>
            queries.Session.ExecuteAsync(queries["fl_UpsertSuggestedWords"].Bind(new
            {
                category_name = categoryName,
                owner_id = ownerID,
                words = words
            }));

        public async Task<IEnumerable<(string category, IEnumerable<string> words)>> GetWordSuggestionsByUser(Guid ownerID)
        {
            var rows = await queries.Session.ExecuteAsync(queries["fl_ReadSuggestedWordsByUser"].Bind(new { owner_id = ownerID }));
            return rows.Select(r => ((string)r["category_name"], (IEnumerable<string>)r["words"]));
        }

        public async Task<IEnumerable<(Guid ownerID, string category, IEnumerable<string> words)>> GetAllWordSuggestions()
        {
            var rows = await queries.Session.ExecuteAsync(queries["fl_ReadSuggestedWords"].Bind());
            return rows.Select(r => ((Guid)r["owner_id"], (string)r["category_name"], (IEnumerable<string>)r["words"]));
        }
    }
}
