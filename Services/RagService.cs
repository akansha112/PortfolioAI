using PortfolioAI.Models;
using PortfolioAI.Repositories;

namespace PortfolioAI.Services
{
    public class RagService
    {
        private readonly IVectorRepository _vectorRepo;
        private readonly IAIService _ai;
        private readonly ChatHistoryRepository _historyRepo;


        public RagService(IVectorRepository vectorRepo, IAIService ai, ChatHistoryRepository historyRepo)
        {
            _vectorRepo = vectorRepo;
            _ai = ai;
            _historyRepo = historyRepo;
        }

        // -------------------------------
        // CHAT QUESTION ANSWERING
        // -------------------------------
        public async Task<string> AskAsync(string question)
        {
            // Handle simple greetings immediately
            if (new[] { "hi", "hello", "hey" }.Contains(question.ToLower()))
                return "Hello! I am Akansha's AI assistant. You can ask me about her work, projects, and skills.";

            // Step 1: Search vector DB (resume context)
            var docs = await _vectorRepo.SearchAsync(question);
            Console.WriteLine("[DEBUG] Retrieved context for AI prompt:");
            foreach(var doc in docs)
            {
                Console.WriteLine(doc.Content);
            }


            // Step 2: Build context from retrieved chunks
            var context = docs.Count > 0
                ? string.Join("\n", docs.Select((d, i) => $"[{i + 1}] {d.Content}"))
                : "";

            // Step 2b: Add fallback context for general questions
            context += "\n" + GetFallbackContext();

            // Step 3: Construct prompt for AI
            var prompt = $@"
You are a helpful and professional AI assistant for a developer portfolio.

The person described in the context is Akansha Saxena, a SOFTWARE DEVELOPER.

Use ONLY the information provided in the context below to answer the question in **a clear and detailed manner**. 
If possible, provide explanations, examples, or context to make your answer more informative.

If the answer cannot be found in the context, reply politely: 
'I don't have information about that.'

RESUME CONTEXT:
{context}

QUESTION:
{question}

ANSWER:
";

            // Step 4: Ask AI model
            var answer = await _ai.AskAsync(prompt);

            // Step 5: Save chat history
            var chat = new ChatHistory
            {
                Question = question,
                Answer = answer,
                CreatedAt = DateTime.UtcNow
            };
            await _historyRepo.SaveAsync(chat);

            return answer;
        }

        // ---------------------------------
        // RESUME INDEXING (NEW FEATURE)
        // ---------------------------------
        public async Task IndexResumeAsync(string resumeText)
        {
            if (string.IsNullOrWhiteSpace(resumeText))
                throw new Exception("Resume text is empty");

            // Step 1: Split resume into chunks
            var chunks = SplitIntoChunks(resumeText);

            Console.WriteLine($"Indexing {chunks.Count} resume chunks...");

            // Step 2: Store each chunk in vector DB
            foreach (var chunk in chunks)
            {
                await _vectorRepo.UpsertAsync(chunk);
            }
        }
        public async Task<List<ChatHistory>> GetHistoryAsync()
        {
            return await _historyRepo.GetAllAsync();
        }

        // ---------------------------------
        // HELPER: TEXT CHUNKING
        // ---------------------------------
        private string GetFallbackContext()
        {
            return @"
Akansha Saxena is a professional, friendly software developer.
She can answer questions about her work experience, skills, and projects.
You can greet her or ask basic conversational questions like 'Hello', 'How are you?'
";
        }
        private List<string> SplitIntoChunks(string text)
        {
            int chunkSize = 250; // smaller chunks improve search relevance
            var chunks = new List<string>();

            for (int i = 0; i < text.Length; i += chunkSize)
            {
                chunks.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
            }

            return chunks;
        }
    }
}