using Cassandra;
using FLGrainInterfaces;
using FLGrains.ServiceInterfaces;
using Orleans;
using OrleansCassandraUtils.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    class SuggestionService : ISuggestionService
    {
        public static async Task<SuggestionService> CreateInstance(IConnectionStringProvider connectionStringProvider) => 
            new SuggestionService
            {
                queries = await Queries.CreateInstance(await CassandraSessionFactory.CreateSession(connectionStringProvider.ConnectionString))
            };


        Queries queries;


        private SuggestionService() { }

        public Task RegisterCategorySuggestion(Guid ownerID, string categoryName, IEnumerable<string> words) =>
            queries.Session.ExecuteAsync(queries["fl_UpsertSuggestedCategory"].Bind(new
            {
                name = categoryName,
                owner_id = ownerID,
                words = words
            }));

        public Task RegisterWordSuggestion(Guid ownerID, string categoryName, IEnumerable<string> words) =>
            queries.Session.ExecuteAsync(queries["fl_UpsertSuggestedWords"].Bind(new
            {
                category_name = categoryName,
                owner_id = ownerID,
                words = words
            }));
    }
}
