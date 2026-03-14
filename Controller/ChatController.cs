using Microsoft.AspNetCore.Mvc;
using PortfolioAI.DTOs;
using PortfolioAI.Services;

namespace PortfolioAI.Controller
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly RagService _rag;

        public ChatController(RagService rag)
        {
            _rag = rag;
        }

        // -----------------------------------
        // ASK QUESTION
        // -----------------------------------
        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Question))
            {
                return BadRequest("Question cannot be empty.");
            }

            try
            {
                var answer = await _rag.AskAsync(dto.Question);

                return Ok(new ChatResponseDto
                {
                    Answer = answer
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                return StatusCode(500, "Something went wrong while processing the question.");
            }
        }


        // -----------------------------------
        // SUGGESTED QUESTIONS (FOR UI)
        // -----------------------------------
        [HttpGet("suggestions")]
        public IActionResult GetSuggestions()
        {
            var suggestions = new List<string>
            {
                "What are your technical skills?",
                "Tell me about your projects.",
                "Explain your AI assistant project.",
                "What technologies do you work with?",
                "Do you have experience with .NET and React?"
            };

            return Ok(suggestions);
        }
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var history = await _rag.GetHistoryAsync();
            return Ok(history);
        }
    }
}