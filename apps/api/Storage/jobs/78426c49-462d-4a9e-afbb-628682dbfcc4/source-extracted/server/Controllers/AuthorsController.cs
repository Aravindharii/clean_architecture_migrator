using LibraryManagement.Api.Common.Responses;
using LibraryManagement.Api.DTOs;
using LibraryManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/authors")]
public class AuthorsController : ControllerBase
{
    private readonly IAuthorService _authorService;

    public AuthorsController(IAuthorService authorService)
    {
        _authorService = authorService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var authors = await _authorService.GetAllAsync();

        return Ok(new ApiResponse<IEnumerable<AuthorDto>>
        {
            Success = true,
            Message = "Authors retrieved successfully.",
            Data = authors
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var author = await _authorService.GetByIdAsync(id);

        return Ok(new ApiResponse<AuthorDto>
        {
            Success = true,
            Message = "Author retrieved successfully.",
            Data = author
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Create([FromBody] AuthorCreateValidator dto)
    {
        var author = await _authorService.CreateAsync(dto);

        return CreatedAtAction(nameof(GetById), new { id = author.Id }, new ApiResponse<AuthorDto>
        {
            Success = true,
            Message = "Author created successfully.",
            Data = author
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Update(int id, [FromBody] AuthorUpdateValidator dto)
    {
        await _authorService.UpdateAsync(id, dto);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Author updated successfully."
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Delete(int id)
    {
        await _authorService.DeleteAsync(id);

        return NoContent();
    }

    [HttpGet("deleted")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDeleted()
    {
        var authors = await _authorService.GetDeletedAsync();

        return Ok(new ApiResponse<IEnumerable<AuthorDto>>
        {
            Success = true,
            Message = "Deleted authors retrieved successfully.",
            Data = authors
        });
    }

    [HttpPost("{id}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Restore(int id)
    {
        await _authorService.RestoreAsync(id);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Author restored successfully."
        });
    }

    [HttpDelete("{id}/hard")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> HardDelete(int id)
    {
        await _authorService.HardDeleteAsync(id);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Author permanently deleted."
        });
    }
}
