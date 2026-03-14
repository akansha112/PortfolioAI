namespace PortfolioAI.Models
{
    public class GeminiEmbeddingResponse
    {
        public EmbeddingData embedding { get; set; }
    }

    public class EmbeddingData
    {
        public float[] values { get; set; }
    }
}