using System.Security.Claims;
using Project.Application.Common.Responses;
using Project.Application.Validators;
using Project.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.Common.Dtos;

namespace Project.API.Controllers;

[ApiController]
[Route("api/lending")]
public class LendingController : ControllerBase
{
    private readonly ILoanService _loanService;

    public LendingController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    [HttpPost("checkout")]
    [Authorize(Roles = "Admin,Librarian,Member")]
    public async Task<IActionResult> Checkout([FromBody] LoanCreateValidator dto)
    {
        var loan = await _loanService.CreateAsync(dto);

        return CreatedAtAction(
            null,
            new { id = loan.Id },
            new ApiResponse<LoanDto>
            {
                Success = true,
                Message = "Book checked out successfully.",
                Data = loan
            });
    }

    [HttpPost("return")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Return([FromBody] LoanReturnValidator dto)
    {
        await _loanService.ReturnLoanAsync(dto.LoanId);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Book returned successfully."
        });
    }

    [HttpGet("history")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> History()
    {
        var loans = await _loanService.GetAllAsync();

        return Ok(new ApiResponse<IEnumerable<LoanDto>>
        {
            Success = true,
            Message = "Loan history retrieved successfully.",
            Data = loans
        });
    }

    [HttpGet("my-loans")]
    [Authorize]
    public async Task<IActionResult> MyLoans()
    {
        var userId = GetUserIdFromClaims()
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var loans = await _loanService.GetLoansForUserAsync(userId);

        return Ok(new ApiResponse<IEnumerable<LoanDto>>
        {
            Success = true,
            Message = "Your loans retrieved successfully.",
            Data = loans
        });
    }

    private int? GetUserIdFromClaims()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out var userId))
            return userId;
        return null;
    }
}
