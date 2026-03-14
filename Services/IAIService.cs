namespace PortfolioAI.Services
{
    public interface IAIService
    {
        Task<string> AskAsync(string prompt);
    }
}
