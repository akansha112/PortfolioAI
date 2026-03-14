using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace PortfolioAI.Services
{
    public class GeminiService : IAIService
    {
        private readonly string _apiKey = "AIzaSyBHk9pH8En8nbKbpNM3SHq2H8GCKwsVMzc";

        public async Task<string> AskAsync(string prompt)
        {
            var client = new RestClient(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}");

            var request = new RestRequest("", Method.Post);

            var body = new
            {
                contents = new[]
                {
                new {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
            };

            request.AddJsonBody(body);

            var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                    throw new HttpRequestException($"API error {(int)response.StatusCode}: {response.Content}");

                if (string.IsNullOrWhiteSpace(response.Content))
                    throw new InvalidOperationException("Empty response from API.");

                var j = JObject.Parse(response.Content);
                var text = j.SelectToken("candidates[0].content.parts[0].text")?.ToString();

                if (string.IsNullOrEmpty(text))
                    throw new InvalidOperationException($"Unexpected API response shape: {response.Content}");

                return text;
        }
    }
}