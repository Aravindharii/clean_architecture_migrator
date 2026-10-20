using LibraryManagement.Api.Common.Responses;
using LibraryManagement.Api.DTOs;
using LibraryManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/copies")]
public class CopiesController : ControllerBase
{
    private readonly ICopyService _copyService;

    public CopiesController(ICopyService copyService)
    {
        _copyService = copyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? bookId = null, [FromQuery] string? status = null)
    {
        IEnumerable<CopyDto> copies;

        if (bookId.HasValue || status != null)
            copies = await _copyService.QueryAsync(bookId, status);
        else
            copies = await _copyService.GetAllAsync();

        return Ok(new ApiResponse<IEnumerable<CopyDto>>
        {
            Success = true,
            Message = "Copies retrieved successfully.",
            Data = copies
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var copy = await _copyService.GetByIdAsync(id);

        return Ok(new ApiResponse<CopyDto>
        {
            Success = true,
            Message = "Copy retrieved successfully.",
            Data = copy
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Create([FromBody] CopyCreateValidator dto)
    {
        var copy = await _copyService.CreateAsync(dto);

        return CreatedAtAction(nameof(GetById), new { id = copy.Id }, new ApiResponse<CopyDto>
        {
            Success = true,
            Message = "Copy created successfully.",
            Data = copy
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Update(int id, [FromBody] CopyUpdateValidator dto)
    {
        await _copyService.UpdateAsync(id, dto);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Copy updated successfully."
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Delete(int id)
    {
        await _copyService.DeleteAsync(id);

        return NoContent();
    }

    [HttpGet("deleted")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDeleted()
    {
        var copies = await _copyService.GetDeletedAsync();

        return Ok(new ApiResponse<IEnumerable<CopyDto>>
        {
            Success = true,
            Message = "Deleted copies retrieved successfully.",
            Data = copies
        });
    }

    [HttpPost("{id}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Restore(int id)
    {
        await _copyService.RestoreAsync(id);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Copy restored successfully."
        });
    }

    [HttpDelete("{id}/hard")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> HardDelete(int id)
    {
        await _copyService.HardDeleteAsync(id);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Copy permanently deleted."
        });
    }
}
