using PortfolioAI.Data;
using PortfolioAI.Repositories;
using PortfolioAI.Services;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------
// Add services to the container
// -----------------------------
builder.Services.AddControllers();

// Swagger / API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// -----------------------------
// CORS (for React frontend)
// -----------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()   // allow any frontend for now
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// -----------------------------
// Dependency Injection
// -----------------------------
builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddScoped<IVectorRepository, PineconeRepository>();
builder.Services.AddScoped<IAIService, GeminiService>();
builder.Services.AddSingleton(new ChatHistoryRepository(
    "mongodb+srv://saxenaakansha014_db_user:Ee9fzrZVhUr23IoF@portfolioai.awha7rv.mongodb.net/PortfolioAI?retryWrites=true&w=majority"
));
builder.Services.AddScoped<RagService>();
builder.Services.AddSingleton<ResumeDataSeeder>(); // Seeder injected

var app = builder.Build();

// -----------------------------
// HTTP request pipeline
// -----------------------------

// Enable Swagger for all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PortfolioAI API V1");
    c.RoutePrefix = "swagger"; // Swagger available at /swagger/index.html
});

// Apply HTTPS redirection only locally
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")) &&
    Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development")
{
    app.UseHttpsRedirection();
}

// Apply CORS
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// -----------------------------
// Bind to Render dynamic PORT if set
// -----------------------------
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    app.Urls.Add($"http://*:{port}");
}

app.Run();