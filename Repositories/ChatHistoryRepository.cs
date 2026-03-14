using MongoDB.Driver;
using PortfolioAI.Models;

namespace PortfolioAI.Repositories
{
    public class ChatHistoryRepository
    {
        private readonly IMongoCollection<ChatHistory> _collection;

        public ChatHistoryRepository(string connectionString)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("PortfolioAI");
            _collection = database.GetCollection<ChatHistory>("ChatHistory");
        }

        public async Task SaveAsync(ChatHistory chat)
        {
            await _collection.InsertOneAsync(chat);
        }

        public async Task<List<ChatHistory>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }
    }
}