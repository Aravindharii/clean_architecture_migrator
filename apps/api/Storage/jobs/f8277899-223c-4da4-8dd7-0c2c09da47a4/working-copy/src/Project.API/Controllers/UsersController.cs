using System.Security.Claims;
using Project.Application.Common.Exceptions;
using Project.Application.Common.Responses;
using Project.Application.Common.Webhooks;
using Project.Application.Validators;
using Project.Domain.Entities;
using Project.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.API.Middleware;
using Project.Application.Common.Dtos;

namespace Project.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IWebhookEventPublisher _webhookEventPublisher;

    public UsersController(IUserRepository userRepository, IWebhookEventPublisher webhookEventPublisher)
    {
        _userRepository = userRepository;
        _webhookEventPublisher = webhookEventPublisher;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var role = ParseRole(request.Role);

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<object>
        {
            Success = true,
            Message = "User created successfully.",
            Data = new
            {
                user.Id,
                Name = $"{user.FirstName} {user.LastName}",
                user.Email,
                Role = user.Role.ToString().ToLower(),
                user.IsActive,
                user.CreatedAt,
                user.UpdatedAt
            }
        });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userRepository.GetAllAsync();

        var userDtos = users.Select(u => new
        {
            u.Id,
            Name = $"{u.FirstName} {u.LastName}",
            u.Email,
            Role = u.Role.ToString().ToLower(),
            u.IsActive,
            u.CreatedAt,
            u.UpdatedAt
        });

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Users retrieved successfully.",
            Data = userDtos
        });
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"User with ID {id} not found.");

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "User retrieved successfully.",
            Data = new
            {
                user.Id,
                Name = $"{user.FirstName} {user.LastName}",
                user.Email,
                Role = user.Role.ToString().ToLower(),
                user.IsActive,
                user.CreatedAt,
                user.UpdatedAt
            }
        });
    }

    [HttpPut("{id}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateUserRoleRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"User with ID {id} not found.");

        var newRole = ParseRole(request.Role);

        if (user.Role == newRole)
            throw new BadRequestException($"User already has the role '{newRole.ToString().ToLower()}'.");

        var currentUserId = GetUserIdFromClaims();
        if (currentUserId != null && user.Id == currentUserId.Value)
            throw new ForbiddenException("You cannot change your own role.");

        if (user.Role == UserRole.Admin && newRole != UserRole.Admin && await _userRepository.CountAdminsAsync() <= 1)
            throw new BadRequestException("Cannot demote the last admin. Promote another user to admin first.");

        user.Role = newRole;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "User role updated successfully.",
            Data = new
            {
                user.Id,
                Role = user.Role.ToString().ToLower(),
                user.UpdatedAt
            }
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userRepository.GetByIdIncludingDeletedAsync(id)
            ?? throw new NotFoundException($"User with ID {id} not found.");
        if (user.IsDeleted)
            throw new BadRequestException($"User with ID {id} has already been deleted.");

        var currentUserId = GetUserIdFromClaims();
        if (currentUserId != null && user.Id == currentUserId.Value)
            throw new ForbiddenException("You cannot delete your own account.");

        if (user.Role == UserRole.Admin && await _userRepository.CountAdminsAsync() <= 1)
            throw new BadRequestException("Cannot delete the last admin. Promote another user to admin first.");

        var deletedBy = User.FindFirst(ClaimTypes.Email)?.Value;
        await _userRepository.SoftDeleteAsync(user, deletedBy);

        _webhookEventPublisher.TryPublish(
            WebhookEventTypes.UserDeleted,
            new { userId = user.Id, email = user.Email });

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "User deleted successfully."
        });
    }

    private static UserRole ParseRole(string role) =>
        Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new BadRequestException("Role must be one of: admin, librarian, member.");

    private int? GetUserIdFromClaims()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out var userId))
            return userId;
        return null;
    }
}
