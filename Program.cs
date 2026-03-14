using PortfolioAI.Data;
using PortfolioAI.Repositories;
using PortfolioAI.Services;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------
// Add services to the container
// -----------------------------
builder.Services.AddControllers();

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

// applies the above policy

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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Apply CORS
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();