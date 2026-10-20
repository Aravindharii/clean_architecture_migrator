using LibraryManagement.Api.Common.Responses;
using LibraryManagement.Api.DTOs;
using LibraryManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IHealthService _healthService;

    public HealthController(IHealthService healthService)
    {
        _healthService = healthService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSystemHealth()
    {
        var health = await _healthService.GetSystemHealthAsync();

        return Ok(new ApiResponse<SystemHealthDto>
        {
            Success = true,
            Message = "System health retrieved successfully.",
            Data = health
        });
    }

}
