using Newtonsoft.Json;
using PortfolioAI.Models;
using RestSharp;

namespace PortfolioAI.Data
{
    public class ResumeDataSeeder
    {
        private readonly string _apiKey = Environment.GetEnvironmentVariable("PINECONE_KEY");
        private readonly string _indexUrl =Environment.GetEnvironmentVariable("PINECONE_INDEX_URL");
        private readonly string _geminiKey = Environment.GetEnvironmentVariable("GOOGLE_GEMINI_KEY");

        public async Task SeedAsync(string resumeText)
        {
            if (string.IsNullOrWhiteSpace(resumeText))
                throw new Exception("Resume text is empty");

            Console.WriteLine("Starting Resume Data Seeding...\n");

            // Step 1: Split resume into chunks dynamically
            var resumeChunks = SplitIntoChunks(resumeText);

            foreach (var chunk in resumeChunks)
            {
                Console.WriteLine($"Processing chunk:\n{chunk}\n");

                var embedding = await GenerateEmbedding(chunk);

                Console.WriteLine($"Embedding generated. Vector size: {embedding.Length}");

                await UpsertVector(chunk, embedding);

                Console.WriteLine("Vector inserted successfully.\n");
            }

            Console.WriteLine("Resume data seeding completed.");
        }

        private List<string> SplitIntoChunks(string text)
        {
            int chunkSize = 500; // you can tweak for more relevance
            var chunks = new List<string>();
            for (int i = 0; i < text.Length; i += chunkSize)
            {
                chunks.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
            }
            return chunks;
        }

    //// GenerateEmbedding() and UpsertVector() remain the same

    //    private List<string> GetResumeChunks()
    //    {
    //        return new List<string>
    //        {
    //            "Akansha Saxena is a .NET full stack developer with experience in ASP.NET Core, C#, SQL Server and REST APIs.",

    //            "She has experience building scalable web APIs using .NET 8 and implementing repository pattern and clean architecture.",

    //            "She has experience with frontend technologies including React.js, JavaScript, HTML and CSS.",

    //            "Akansha has worked with Generative AI concepts including RAG architecture, vector search and LLM integration.",

    //            "She has built projects integrating AI assistants with .NET backend and React frontend.",

    //            "Her skills include C#, .NET Core, SQL Server, React.js, REST APIs, Git, and AI integration.",

    //            "Akansha is passionate about learning new technologies and building modern AI powered applications."
    //        };
    //    }

        private async Task<float[]> GenerateEmbedding(string text)
        {
            Console.WriteLine("Generating embedding from Gemini...");

            var client = new RestClient(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent?key={_geminiKey}");

            var request = new RestRequest("", Method.Post);

            var body = new
            {
                content = new
                {
                    parts = new[]
                    {
                new { text = text }
            }
                }
            };

            request.AddJsonBody(body);

            var response = await client.ExecuteAsync(request);

            Console.WriteLine("Gemini response:");
            Console.WriteLine(response.Content);

            var result = JsonConvert.DeserializeObject<GeminiEmbeddingResponse>(response.Content);

            var fullEmbedding = result.embedding.values;

            Console.WriteLine($"Original embedding size: {fullEmbedding.Length}");

            // Reduce to 768
            var reducedEmbedding = fullEmbedding.Take(768).ToArray();

            Console.WriteLine($"Reduced embedding size: {reducedEmbedding.Length}");

            return reducedEmbedding;
        }

        private async Task UpsertVector(string text, float[] embedding)
        {
            Console.WriteLine("Uploading vector to Pinecone...");

            var client = new RestClient(_indexUrl);

            var request = new RestRequest("/vectors/upsert", Method.Post);

            request.AddHeader("Api-Key", _apiKey);
            request.AddHeader("Content-Type", "application/json");

            var body = new
            {
                vectors = new[]
                {
                    new
                    {
                        id = Guid.NewGuid().ToString(),
                        values = embedding,
                        metadata = new
                        {
                            text = text
                        }
                    }
                }
            };

            request.AddJsonBody(body);

            var response = await client.ExecuteAsync(request);

            Console.WriteLine("Pinecone response:");
            Console.WriteLine(response.Content);

            if (!response.IsSuccessful)
                throw new Exception("Pinecone upsert failed: " + response.Content);
        }
    }
}