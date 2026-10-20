using LibraryManagement.Api.DTOs;

namespace LibraryManagement.Api.Services.Interfaces;

public interface IHealthService
{
    Task<SystemHealthDto> GetSystemHealthAsync();
}
