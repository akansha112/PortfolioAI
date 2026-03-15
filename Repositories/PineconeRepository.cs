using Azure.Core;
using Newtonsoft.Json;
using PortfolioAI.Models;
using RestSharp;

namespace PortfolioAI.Repositories
{
    public class PineconeRepository : IVectorRepository
    {
        private readonly string _apiKey = Environment.GetEnvironmentVariable("PINECONE_KEY");
        private readonly string _indexUrl = Environment.GetEnvironmentVariable("PINECONE_INDEX_URL");
        private readonly string _geminiKey = Environment.GetEnvironmentVariable("GOOGLE_GEMINI_KEY");
        private readonly string _namespace = Environment.GetEnvironmentVariable("PINECONE_NAMESPACE") ?? "resume-akansha";

        // -----------------------------
        // DELETE/CLEAR NAMESPACE (New Method)
        // -----------------------------
        public async Task ClearNamespaceAsync()
        {
            Console.WriteLine($"[DEBUG] Clearing all data in namespace: {_namespace}");

            var client = new RestClient(_indexUrl.TrimEnd('/'));
            // Serverless endpoint to delete vectors
            var request = new RestRequest("vectors/delete", Method.Post);

            request.AddHeader("Api-Key", _apiKey);
            request.AddHeader("Content-Type", "application/json");

            var body = new
            {
                @namespace = _namespace,
                deleteAll = true
            };

            request.AddJsonBody(body);
            var response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                Console.WriteLine($"[ERROR] Clear Namespace Failed: {response.StatusCode} - {response.Content}");
            }
            else
            {
                Console.WriteLine("[DEBUG] Namespace cleared successfully. Ready for new resume data.");
            }
        }

        // -----------------------------
        // SEARCH VECTOR DB
        // -----------------------------
        public async Task<List<DocumentChunk>> SearchAsync(string query)
        {
            Console.WriteLine($"[DEBUG] Searching vector DB for query: '{query}'");
            var embedding = await GenerateEmbedding(query);

            var client = new RestClient(_indexUrl.TrimEnd('/'));
            var request = new RestRequest("query", Method.Post); // Absolute path

            request.AddHeader("Api-Key", _apiKey);
            request.AddHeader("Content-Type", "application/json");

            var body = new
            {
                vector = embedding,
                topK = 5,
                includeMetadata = true,
                @namespace = _namespace
            };

            request.AddJsonBody(body);
            var response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                Console.WriteLine($"[ERROR] Search Failed: {response.StatusCode} - {response.Content}");
                return new List<DocumentChunk>();
            }

            dynamic result = JsonConvert.DeserializeObject(response.Content);
            var docs = new List<DocumentChunk>();

            if (result?.matches != null)
            {
                foreach (var match in result.matches)
                {
                    docs.Add(new DocumentChunk
                    {
                        Id = match.id,
                        Content = match.metadata?.text ?? "No content found"
                    });
                }
            }
            return docs;
        }

        // -----------------------------
        // UPSERT DOCUMENT CHUNK
        // -----------------------------
        public async Task UpsertAsync(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;
            var embedding = await GenerateEmbedding(content);

            var client = new RestClient(_indexUrl.TrimEnd('/'));
            // Serverless usually requires /vectors/upsert
            var request = new RestRequest("vectors/upsert", Method.Post);

            request.AddHeader("Api-Key", _apiKey);
            request.AddHeader("Content-Type", "application/json");

            var vector = new
            {
                id = Guid.NewGuid().ToString(),
                values = embedding,
                metadata = new { text = content }
            };

            var body = new
            {
                @namespace = _namespace,
                vectors = new[] { vector }
            };

            request.AddJsonBody(body);
            var response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                throw new Exception($"Pinecone upsert failed: {response.StatusCode} - {response.Content}");
            }
            Console.WriteLine("[DEBUG] Upsert successful!");
        }

        // -----------------------------
        // GENERATE EMBEDDING
        // -----------------------------
        private async Task<float[]> GenerateEmbedding(string text)
        {
            var client = new RestClient($"https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent?key={_geminiKey}");
            var request = new RestRequest("", Method.Post);
            request.AddJsonBody(new { content = new { parts = new[] { new { text = text } } } });

            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful) throw new Exception("Gemini Embedding failed.");

            var result = JsonConvert.DeserializeObject<GeminiEmbeddingResponse>(response.Content);
            var fullEmbedding = result.embedding.values;

            var embedding768 = fullEmbedding.Take(768).ToList();
            while (embedding768.Count < 768) embedding768.Add(0f);

            return embedding768.ToArray();
        }
    }
}