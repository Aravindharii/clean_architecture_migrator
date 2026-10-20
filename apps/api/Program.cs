using System.Text.Json.Serialization;
using CleanArchitectureMigration.Api.Data;
using CleanArchitectureMigration.Api.Services;
using CleanArchitectureMigration.ArchitectureRules;
using CleanArchitectureMigration.DotNetWorker;
using CleanArchitectureMigration.JavaWorker;
using CleanArchitectureMigration.Orchestrator;
using CleanArchitectureMigration.Orchestrator.AI;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers & JSON configuration
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Clean Architecture Migration Platform API",
        Version = "v1",
        Description = "Automated AI-assisted Clean Architecture migration for .NET and Java systems"
    });
});

// Database configuration: PostgreSQL with SQLite fallback
var pgConnectionString = builder.Configuration.GetConnectionString("PostgreSQL");
if (!string.IsNullOrWhiteSpace(pgConnectionString))
{
    builder.Services.AddDbContext<MigrationDbContext>(options =>
        options.UseNpgsql(pgConnectionString));
}
else
{
    var sqliteConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=migration_platform.db";
    builder.Services.AddDbContext<MigrationDbContext>(options =>
        options.UseSqlite(sqliteConnection));
}

// Storage & Extraction Services
builder.Services.AddSingleton<ISafeArchiveExtractor, SafeArchiveExtractor>();
builder.Services.AddSingleton<IArtifactStorageService, LocalArtifactStorageService>();

// Language Workers
builder.Services.AddSingleton<IDotNetWorker, DotNetWorker>();
builder.Services.AddSingleton<IJavaWorker, JavaWorker>();

// Architecture Rules Engine
builder.Services.AddSingleton<ArchitectureRulesEngine>();

// AI LLM Planner
builder.Services.AddSingleton<ILLMService, RuleAssistedPlannerService>();

// Migration Orchestrator
builder.Services.AddScoped<IMigrationOrchestrator, MigrationOrchestrator>();

// Background Job Queue & Worker
builder.Services.AddSingleton<IJobQueue, ChannelJobQueue>();
builder.Services.AddHostedService<JobProcessorBackgroundService>();

// CORS configuration for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebUI", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Ensure database is initialized
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MigrationDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Clean Architecture Migration API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowWebUI");

app.UseAuthorization();

app.MapControllers();

app.Run();
