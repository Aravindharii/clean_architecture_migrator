using System.Text;
using System.Threading.Channels;
using FluentValidation;
using LibraryManagement.Api.Authentication;
using LibraryManagement.Api.Common.ETag;
using LibraryManagement.Api.Common.Exceptions;
using LibraryManagement.Api.Common.Responses;
using LibraryManagement.Api.Common.Webhooks;
using LibraryManagement.Api.Data;
using LibraryManagement.Api.Filters;
using LibraryManagement.Api.Repositories;
using LibraryManagement.Api.Repositories.Interfaces;
using LibraryManagement.Api.Services;
using LibraryManagement.Api.Services.Interfaces;
using LibraryManagement.Api.Services.Webhooks;
using LibraryManagement.Api.Settings;
using LibraryManagement.Api.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// JWT Settings
builder.Services.AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .Validate(static settings =>
            !string.IsNullOrWhiteSpace(settings.SecretKey) && settings.SecretKey.Length >= 32,
        "Jwt:SecretKey must be configured and be at least 32 characters long.")
    .Validate(static settings => !string.IsNullOrWhiteSpace(settings.Issuer),
        "Jwt:Issuer must be configured.")
    .Validate(static settings => !string.IsNullOrWhiteSpace(settings.Audience),
        "Jwt:Audience must be configured.")
    .ValidateOnStart();

builder.Services.AddOptions<CorsSettings>()
    .Bind(builder.Configuration.GetSection("Cors"))
    .Validate(static settings => settings.Origins is { Length: > 0 },
        "At least one origin must be configured in 'Cors:Origins'. " +
        "Set 'Cors:Origins:0' or the 'Cors__Origins__0' environment variable to your frontend URL.")
    .ValidateOnStart();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "The database connection string 'ConnectionStrings:DefaultConnection' is not configured. " +
        "Set it via appsettings.{Environment}.json or the " +
        "'ConnectionStrings__DefaultConnection' environment variable.");
}

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("The 'Jwt' configuration section is missing.");

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<AuthorCreateRequestValidator>();
builder.Services.AddScoped<ValidationFilter>();

// Add Controllers with global validation filter and custom model state response
builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
        options.Filters.Add<ETagResultFilter>();
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    e => char.ToLowerInvariant(e.Key[0]) + e.Key[1..],
                    e => e.Value!.Errors.Select(err => err.ErrorMessage).ToArray());

            return new BadRequestObjectResult(new ValidationErrorResponse
            {
                Success = false,
                Message = "Validation failed.",
                Errors = errors
            });
        };
    });

// Global Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Library Management API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.OperationFilter<JwtSecurityRequirementsOperationFilter>();
});

// JWT Authentication
var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Authentication required.",
                    Data = null
                });
            }
        },
        OnForbidden = async context =>
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(new ApiResponse<object>
                {
                    Success = false,
                    Message = "You do not have permission to access this resource.",
                    Data = null
                });
            }
        }
    };
});

// CORS
var corsSettings = builder.Configuration.GetSection("Cors").Get<CorsSettings>()
    ?? new CorsSettings { Origins = [] };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsSettings.Origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// PostgreSQL DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Repositories
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IAuthorRepository, AuthorRepository>();
builder.Services.AddScoped<ICopyRepository, CopyRepository>();
builder.Services.AddScoped<ILoanRepository, LoanRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWebhookSubscriptionRepository, WebhookSubscriptionRepository>();

// Services
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IAuthorService, AuthorService>();
builder.Services.AddScoped<ICopyService, CopyService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Webhook services
builder.Services.AddScoped<IWebhookSubscriptionService, WebhookSubscriptionService>();
builder.Services.AddScoped<IWebhookSignatureService, WebhookSignatureService>();
builder.Services.AddScoped<IWebhookPayloadBuilder, WebhookPayloadBuilder>();
builder.Services.AddScoped<IWebhookDispatcherService, WebhookDispatcherService>();

// ETag service
builder.Services.AddScoped<IETagService, ETagService>();

// Webhook event channel (in-process queue for asynchronous dispatch)
var webhookChannel = Channel.CreateBounded<WebhookEvent>(
    new BoundedChannelOptions(1000)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true
    });
builder.Services.AddSingleton(webhookChannel);
builder.Services.AddSingleton(webhookChannel.Reader);
builder.Services.AddSingleton(webhookChannel.Writer);
builder.Services.AddSingleton<IWebhookEventPublisher, WebhookEventPublisher>();
builder.Services.AddHostedService<WebhookEventBackgroundService>();

builder.Services.AddHttpClient<IWebhookDispatcherService, WebhookDispatcherService>();

// JWT Service
builder.Services.AddSingleton<JwtService>();

// HttpContext accessor for cookie operations
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Apply migrations and seed development data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    context.Database.Migrate();

    if (app.Environment.IsDevelopment())
    {
        SeedDevelopmentData(context);
    }
}

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
    });
}

app.UseExceptionHandler();

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

static void SeedDevelopmentData(AppDbContext context)
{
    if (!context.Users.Any())
    {
        var users = SeedData.GetSeedUsers();
        context.Users.AddRange(users);
        context.SaveChanges();
    }

    if (!context.Books.Any())
    {
        var authors = SeedData.GetSampleAuthors();
        context.Authors.AddRange(authors);
        context.SaveChanges();

        var authorLookup = authors.ToDictionary(a => a.Name, a => a.Id);
        var books = SeedData.GetSampleBooks(authorLookup);
        context.Books.AddRange(books);
        context.SaveChanges();

        var bookLookup = books.ToDictionary(b => b.Title, b => b.Id);
        var copies = SeedData.GetSampleCopies(bookLookup);
        context.Copies.AddRange(copies);
        context.SaveChanges();
    }
}
