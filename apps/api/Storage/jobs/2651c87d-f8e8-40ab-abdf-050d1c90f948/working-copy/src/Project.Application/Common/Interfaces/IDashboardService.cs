using Project.Application.Validators;
using Project.Application.Common.Dtos;

namespace Project.Application.Common.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetStatsAsync();
}
