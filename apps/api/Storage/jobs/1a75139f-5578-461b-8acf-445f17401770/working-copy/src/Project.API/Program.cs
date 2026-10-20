using System.Text.Json;
using System.Text;
using FluentValidation;
using Project.Infrastructure.Persistence;
using Project.Application.Common.Dtos;
using Project.API.Filters;
using LibraryManagement.API.Middleware;
using Project.Application.Services;
using Project.Application.Validators;
using Project.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Project.Application.Common.Interfaces;
using Project.Application.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Register controllers and JSON options
builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            JsonNamingPolicy.CamelCase;
    });

// Register CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactClient", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Register Swagger
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] =
    new List<string>()
        });
});

// Register Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Register Repository Layer
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IAuthorRepository, AuthorRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICopyRepository, CopyRepository>();

// Register Service Layer
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IAuthorService, AuthorService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ICopyService, CopyService>();

// Auth Service
builder.Services.AddScoped<IAuthService, AuthService>();

// Register Password Hasher
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Register Validation Filter
builder.Services.AddScoped<ValidationFilter>();

// Register Validators
builder.Services.AddScoped<IValidator<BookCreateDto>, BookCreateDtoValidator>();
builder.Services.AddScoped<IValidator<BookUpdateDto>, BookUpdateDtoValidator>();
builder.Services.AddScoped<IValidator<BookPatchDto>, BookPatchDtoValidator>();

// Author Validators
builder.Services.AddScoped<IValidator<AuthorCreateDto>, AuthorCreateValidator>();
builder.Services.AddScoped<IValidator<AuthorUpdateDto>, AuthorUpdateValidator>();

// Register Auth Validators
builder.Services.AddScoped<IValidator<RegisterDto>, RegisterValidator>();
builder.Services.AddScoped<IValidator<LoginDto>, LoginValidator>();

// Register JWT Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("Jwt");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    jwtSettings["Key"]!))
        };
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<AppDbContext>();
    var passwordHasher = services.GetRequiredService<IPasswordHasher<User>>();

    await DbSeeder.SeedAsync(context, passwordHasher);
}

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable React frontend communication
app.UseCors("ReactClient");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();

app.Run();