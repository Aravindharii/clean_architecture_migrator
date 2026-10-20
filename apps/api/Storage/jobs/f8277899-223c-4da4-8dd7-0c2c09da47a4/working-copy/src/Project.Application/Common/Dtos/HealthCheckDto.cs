using Project.Application.Validators;
namespace Project.Application.Common.Dtos;

public class HealthCheckDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "healthy";
    public long? LatencyMs { get; set; }
    public DateTime LastChecked { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SystemHealthDto
{
    public string OverallStatus { get; set; } = "healthy";
    public List<HealthCheckDto> Checks { get; set; } = new();
    public SystemInfoDto SystemInfo { get; set; } = new();
}

public class SystemInfoDto
{
    public string Environment { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string Runtime { get; set; } = string.Empty;
    public DateTime ServerTime { get; set; }
}
