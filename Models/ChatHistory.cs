using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PortfolioAI.Models
{
    public class ChatHistory
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } // leave it null, MongoDB will auto-generate

        public string Question { get; set; }

        public string Answer { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
