using Microsoft.AspNetCore.Mvc;
using PortfolioAI.Services;
using PortfolioAI.Data; // Make sure ResumeDataSeeder is in this namespace

namespace PortfolioAI.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly RagService _ragService;
        private readonly ResumeDataSeeder _resumeSeeder; // Add Seeder

        // Inject both RagService and ResumeDataSeeder
        public AdminController(RagService ragService, ResumeDataSeeder resumeSeeder)
        {
            _ragService = ragService;
            _resumeSeeder = resumeSeeder;
        }

        [HttpPost("resume")]
        public async Task<IActionResult> UploadResume(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Resume file missing");

            // Step 1: Extract text from PDF
            var resumeText = PdfHelper.ExtractText(file);

            if (string.IsNullOrWhiteSpace(resumeText))
                return BadRequest("Could not extract text from PDF");

            // Step 2: Index resume dynamically using ResumeDataSeeder
            await _resumeSeeder.SeedAsync(resumeText);

            // Optional: Also update RAG service (if needed)
            await _ragService.IndexResumeAsync(resumeText);

            return Ok(new { message = "Resume indexed successfully" });
        }
    }
}