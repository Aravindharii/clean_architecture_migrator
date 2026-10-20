using LibraryManagement.Api.DTOs;

namespace LibraryManagement.Api.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetStatsAsync();
}
