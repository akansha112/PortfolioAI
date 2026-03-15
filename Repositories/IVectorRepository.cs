using PortfolioAI.Models;

namespace PortfolioAI.Repositories
{
    public interface IVectorRepository
    {
        // Search similar resume chunks
        Task<List<DocumentChunk>> SearchAsync(string query);

        // Insert / update resume chunks in vector DB
        Task UpsertAsync(string content);

        // NEW: Clear all existing context before uploading a new resume
        Task ClearNamespaceAsync();
    }
}