using Project.Application.Common.Responses;
using Project.Application.Validators;
using Project.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.Common.Dtos;

namespace Project.API.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("stats")]
    [Authorize]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _dashboardService.GetStatsAsync();

        return Ok(new ApiResponse<DashboardDto>
        {
            Success = true,
            Message = "Dashboard stats retrieved successfully.",
            Data = stats
        });
    }
}
