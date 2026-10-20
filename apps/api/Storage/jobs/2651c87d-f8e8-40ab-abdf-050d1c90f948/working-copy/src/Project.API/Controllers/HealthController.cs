using Project.Application.Common.Responses;
using Project.Application.Validators;
using Project.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Project.Application.Common.Dtos;

namespace Project.API.Controllers;

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
