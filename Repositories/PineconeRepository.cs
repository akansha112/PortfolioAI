using Azure.Core;
using Newtonsoft.Json;
using PortfolioAI.Models;
using RestSharp;

namespace PortfolioAI.Repositories
{
    public class PineconeRepository : IVectorRepository
    {
        private readonly string _apiKey = "pcsk_5H7Xge_6VthomV68zx9a4erp2Ep8Wbp3gnvfXGVSuBmRSLH8C9YidY5HBFgGWjcXoamCaR";
        private readonly string _indexUrl = "https://ai-portfolio-hk303f0.svc.aped-4627-b74a.pinecone.io";
        private readonly string _geminiKey = "AIzaSyBHk9pH8En8nbKbpNM3SHq2H8GCKwsVMzc";
        private readonly string _namespace = "resume-akansha"; // consistent namespace

        // -----------------------------
        // SEARCH VECTOR DB
        // -----------------------------
        public async Task<List<DocumentChunk>> SearchAsync(string query)
        {
            Console.WriteLine($"[DEBUG] Searching vector DB for query: '{query}'");

            var embedding = await GenerateEmbedding(query);
            Console.WriteLine($"[DEBUG] Embedding size used for search: {embedding.Length}");

            var client = new RestClient(_indexUrl);
            var request = new RestRequest("/query", Method.Post);
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

            Console.WriteLine("[DEBUG] Pinecone query raw response:");
            Console.WriteLine(response.Content);

            dynamic result = JsonConvert.DeserializeObject(response.Content);
            var docs = new List<DocumentChunk>();

            if (result?.matches != null)
            {
                foreach (var match in result.matches)
                {
                    Console.WriteLine($"[DEBUG] Match ID: {match.id}, Text: {match.metadata.text}");
                    docs.Add(new DocumentChunk
                    {
                        Id = match.id,
                        Content = match.metadata.text
                    });
                }
            }

            Console.WriteLine($"[DEBUG] Documents retrieved: {docs.Count}");
            return docs;
        }

        // -----------------------------
        // UPSERT DOCUMENT CHUNK
        // -----------------------------
        public async Task UpsertAsync(string content)
        {
            Console.WriteLine($"[DEBUG] Indexing chunk: {content}");

            var embedding = await GenerateEmbedding(content);

            if (embedding.All(x => x == 0f))
            {
                Console.WriteLine("[WARNING] Skipping empty vector for content.");
                return;
            }

            Console.WriteLine($"[DEBUG] Embedding length: {embedding.Length}");
            Console.WriteLine($"[DEBUG] First 5 values: {string.Join(", ", embedding.Take(5))}");

            var client = new RestClient(_indexUrl);
            var request = new RestRequest("/vectors/upsert", Method.Post);
            request.AddHeader("Api-Key", _apiKey);
            request.AddHeader("Content-Type", "application/json");

            var vector = new
            {
                id = Guid.NewGuid().ToString(),
                values = embedding,
                metadata = new
                {
                    text = content
                }
            };

            var body = new
            {
                @namespace = _namespace,
                vectors = new[] { vector }
            };

            request.AddJsonBody(body);
            var response = await client.ExecuteAsync(request);

            Console.WriteLine("[DEBUG] Upsert response:");
            Console.WriteLine(response.Content);

            if (!response.IsSuccessful)
                throw new Exception("Pinecone upsert failed: " + response.Content);
        }

        // -----------------------------
        // GENERATE EMBEDDING
        // -----------------------------
        private async Task<float[]> GenerateEmbedding(string text)
        {
            var client = new RestClient(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent?key={_geminiKey}");

            var request = new RestRequest("", Method.Post);
            request.AddJsonBody(new
            {
                content = new { parts = new[] { new { text = text } } }
            });

            var response = await client.ExecuteAsync(request);
            var result = JsonConvert.DeserializeObject<GeminiEmbeddingResponse>(response.Content);

            var fullEmbedding = result.embedding.values;

            // Ensure embedding has 768 dimensions
            var embedding768 = fullEmbedding.Take(768).ToList();
            if (embedding768.Count < 768)
                embedding768.AddRange(Enumerable.Repeat(0f, 768 - embedding768.Count));

            Console.WriteLine($"[DEBUG] Generated embedding length: {embedding768.Count}");
            Console.WriteLine($"[DEBUG] First 5 embedding values: {string.Join(", ", embedding768.Take(5))}");

            return embedding768.ToArray();
        }
    }
}