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
    //?? pagination
    class SuggestionService : ISuggestionService
    {
        public static async Task<SuggestionService> CreateInstance(ISystemSettingsProvider connectionStringProvider) =>
            new SuggestionService(await Queries.CreateInstance(await CassandraSessionFactory.CreateSession(connectionStringProvider.ConnectionString)));

        Queries queries;

        private SuggestionService(Queries queries) => this.queries = queries;

        public Task RegisterCategorySuggestion(Guid ownerID, string categoryName, IEnumerable<string> words) =>
            queries.Session.ExecuteAsync(queries["fl_UpsertSuggestedCategory"].Bind(new
            {
                name = categoryName,
                owner_id = ownerID,
                words = words
            }));

        public Task<IEnumerable<(string category, IEnumerable<string> words)>> GetCategorySuggestionsByUser(Guid ownerID) =>
            queries.Session.ExecuteAsync(queries["fl_ReadSuggestedCategoriesByUser"].Bind(new { owner_id = ownerID }))
            .ContinueWith(t => 
                t.Result.GetRows()
                .Select(r => ((string)r["name"], (IEnumerable<string>)r["words"])));

        public Task<IEnumerable<(Guid ownerID, string category, IEnumerable<string> words)>> GetAllCategorySuggestions() =>
            queries.Session.ExecuteAsync(queries["fl_ReadSuggestedCategories"].Bind())
            .ContinueWith(t =>
                t.Result.GetRows()
                .Select(r => ((Guid)r["owner_id"], (string)r["name"], (IEnumerable<string>)r["words"])));

        public Task RegisterWordSuggestion(Guid ownerID, string categoryName, IEnumerable<string> words) =>
            queries.Session.ExecuteAsync(queries["fl_UpsertSuggestedWords"].Bind(new
            {
                category_name = categoryName,
                owner_id = ownerID,
                words = words
            }));

        public Task<IEnumerable<(string category, IEnumerable<string> words)>> GetWordSuggestionsByUser(Guid ownerID) =>
            queries.Session.ExecuteAsync(queries["fl_ReadSuggestedWordsByUser"].Bind(new { owner_id = ownerID }))
            .ContinueWith(t =>
                t.Result.GetRows()
                .Select(r => ((string)r["category_name"], (IEnumerable<string>)r["words"])));

        public Task<IEnumerable<(Guid ownerID, string category, IEnumerable<string> words)>> GetAllWordSuggestions() =>
            queries.Session.ExecuteAsync(queries["fl_ReadSuggestedWords"].Bind())
            .ContinueWith(t =>
                t.Result.GetRows()
                .Select(r => ((Guid)r["owner_id"], (string)r["category_name"], (IEnumerable<string>)r["words"])));
    }
}
