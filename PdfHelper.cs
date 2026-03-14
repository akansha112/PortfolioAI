using System.Text;
using UglyToad.PdfPig;
using Microsoft.AspNetCore.Http;

namespace PortfolioAI
{
      public static class PdfHelper
    {
        public static string ExtractText(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var pdf = PdfDocument.Open(stream);

            var textBuilder = new StringBuilder();

            foreach (var page in pdf.GetPages())
            {
                textBuilder.AppendLine(page.Text);
            }

            return textBuilder.ToString();
        }
    }
}
