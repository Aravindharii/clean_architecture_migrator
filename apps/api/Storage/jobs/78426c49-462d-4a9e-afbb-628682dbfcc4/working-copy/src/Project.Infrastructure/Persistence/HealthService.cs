using System.Diagnostics;
using System.Reflection;
using Project.Infrastructure.Persistence;
using Project.Application.Validators;
using Project.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Project.Application.Common.Dtos;
using Project.Application.Services;

namespace Project.Infrastructure.Persistence;

public class HealthService : IHealthService
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public HealthService(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<SystemHealthDto> GetSystemHealthAsync()
    {
        var checks = new List<HealthCheckDto>
        {
            await CheckDatabaseAsync(),
            await CheckApiServerAsync(),
            await CheckAuthenticationAsync(),
        };

        var overallStatus = "healthy";
        if (checks.Any(c => c.Status == "down"))
            overallStatus = "down";
        else if (checks.Any(c => c.Status == "degraded"))
            overallStatus = "degraded";

        return new SystemHealthDto
        {
            OverallStatus = overallStatus,
            Checks = checks,
            SystemInfo = GetSystemInfo()
        };
    }

    private async Task<HealthCheckDto> CheckDatabaseAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _context.Database.ExecuteSqlRawAsync("SELECT 1");
            stopwatch.Stop();
            return new HealthCheckDto
            {
                Id = "database",
                Name = "PostgreSQL Database",
                Status = "healthy",
                LatencyMs = stopwatch.ElapsedMilliseconds,
                LastChecked = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HealthCheckDto
            {
                Id = "database",
                Name = "PostgreSQL Database",
                Status = "down",
                LatencyMs = stopwatch.ElapsedMilliseconds,
                LastChecked = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<HealthCheckDto> CheckApiServerAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var canConnect = await _context.Books.AnyAsync();
            stopwatch.Stop();
            return new HealthCheckDto
            {
                Id = "api",
                Name = "API Server",
                Status = "healthy",
                LatencyMs = stopwatch.ElapsedMilliseconds,
                LastChecked = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HealthCheckDto
            {
                Id = "api",
                Name = "API Server",
                Status = "down",
                LatencyMs = stopwatch.ElapsedMilliseconds,
                LastChecked = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<HealthCheckDto> CheckAuthenticationAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _context.Users.AnyAsync();
            stopwatch.Stop();
            return new HealthCheckDto
            {
                Id = "auth",
                Name = "Authentication Service",
                Status = "healthy",
                LatencyMs = stopwatch.ElapsedMilliseconds,
                LastChecked = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HealthCheckDto
            {
                Id = "auth",
                Name = "Authentication Service",
                Status = "down",
                LatencyMs = stopwatch.ElapsedMilliseconds,
                LastChecked = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }

    private SystemInfoDto GetSystemInfo()
    {
        var runtimeVersion = Environment.Version.ToString();
        return new SystemInfoDto
        {
            Environment = _environment.EnvironmentName,
            ApiVersion = "v1.0.0",
            Database = "PostgreSQL 15",
            Runtime = $".NET {runtimeVersion}",
            ServerTime = DateTime.UtcNow
        };
    }
}
